using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpellResearchSynthesizer.Classes
{
    public class SpellConfiguration
    {
        public Dictionary<string, (List<SpellInfo> NewSpells, List<SpellInfo> RemovedSpells)> Mods = new();

        public static SpellConfiguration FromJson(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string spellconf, ArchetypeList allowedArchetypes)
        {
            SpellConfiguration config = new();
            if (spellconf.StartsWith("{"))
            {
                JObject data = JObject.Parse(spellconf);
                if (data == null) return config;
                config = ParseMysticismFormat(state, data, allowedArchetypes);
            }
            else if (spellconf.StartsWith("["))
            {
                JArray data = JArray.Parse(spellconf);
                if (data == null) return config;
                config = ParseJSONFormat(state, data, allowedArchetypes);
            }
            return config;
        }

        private static SpellConfiguration ParseMysticismFormat(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, JObject data, ArchetypeList allowedArchetypes)
        {
            // Mysticism JSON Patch handling
            SpellConfiguration config = new();
            JToken? newSpells = data["newSpells"];
            if (newSpells != null)
            {
                foreach (JToken newSpell in newSpells)
                {
                    SpellInfo? spell = ParseMysticismSpellEntry(state, newSpell, allowedArchetypes);
                    if (spell == null) continue;

                    if (!config.Mods.ContainsKey(spell.SpellESP))
                    {
                        config.Mods.Add(spell.SpellESP, (new List<SpellInfo>(), new List<SpellInfo>()));
                    }
                    config.Mods[spell.SpellESP].NewSpells.Add(spell);
                }
            }
            JToken? removedSpells = data["removedSpells"];
            if (removedSpells != null)
            {
                foreach (JToken removedSpell in removedSpells)
                {
                    SpellInfo? spell = ParseMysticismSpellEntry(state, removedSpell, allowedArchetypes);
                    if (spell == null) continue;

                    if (!config.Mods.ContainsKey(spell.SpellESP))
                    {
                        config.Mods.Add(spell.SpellESP, (new List<SpellInfo>(), new List<SpellInfo>()));
                    }
                    config.Mods[spell.SpellESP].RemovedSpells.Add(spell);
                }
            }
            return config;
        }

        private static SpellInfo? ParseMysticismSpellEntry(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, JToken newSpell, ArchetypeList allowedArchetypes)
        {
            string name = (string?)newSpell["name"] ?? string.Empty;
            if (name == string.Empty)
            {
                Console.WriteLine("Error reading spell name");
                return null;
            }
            string spellID = (string?)newSpell["spellId"] ?? string.Empty;
            if (spellID == string.Empty)
            {
                Console.WriteLine($"Error reading spell ID for {name}");
                return null;
            }
            string skill = (string?)newSpell["school"] ?? string.Empty;
            if (skill == string.Empty)
            {
                Console.WriteLine($"Error: Skill not found");
                return null;
            }
            if (!allowedArchetypes.Skills.Any(archetype => archetype.Name.ToLower() == skill.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == skill.ToLower())))
            {
                Console.WriteLine($"Error: Skill \"{skill}\" not known");
                return null;
            }
            string level = (string?)newSpell["tier"] ?? string.Empty;
            if (level == string.Empty)
            {
                Console.WriteLine($"Error: Spell level not found");
                return null;
            }
            if (!allowedArchetypes.Levels.Any(archetype => archetype.Name.ToLower() == level.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == level.ToLower())))
            {
                Console.WriteLine($"Error: Spell level \"{level}\" not known");
                return null;
            }
            string castingType = (string?)newSpell["castingType"] ?? string.Empty;
            if (castingType == string.Empty)
            {
                Console.WriteLine($"Error: Casting type not found");
                return null;
            }
            if (!allowedArchetypes.CastingTypes.Any(archetype => archetype.Name.ToLower() == castingType.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == castingType.ToLower())))
            {
                Console.WriteLine($"Error: Casting type \"{castingType}\" not known");
                return null;
            }
            JArray? target = (JArray?)newSpell["targeting"];
            if (target == null)
            {
                Console.WriteLine("Targeting type list not found");
                return null;
            }
            List<AliasedArchetype> foundTargets = new();
            foreach (string? targetType in target)
            {
                if (string.IsNullOrEmpty(targetType))
                {
                    Console.WriteLine("Empty targeting type found in list");
                    return null;
                }
                AliasedArchetype? arch = allowedArchetypes.Targets.FirstOrDefault(archetype => archetype.Name.ToLower() == targetType.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == targetType.ToLower()));
                if (arch == null)
                {
                    Console.WriteLine($"Targeting type {targetType} not known");
                    return null;
                }
                foundTargets.Add(arch);
            }
            JArray? elements = (JArray?)newSpell["elements"];
            List<AliasedArchetype> foundElements = new();
            if (elements != null)
            {
                foreach (string? element in elements)
                {
                    if (string.IsNullOrEmpty(element))
                    {
                        Console.WriteLine("Empty element found in list");
                        continue;
                    }
                    AliasedArchetype? arch = allowedArchetypes.Elements.FirstOrDefault(archetype => archetype.Name.ToLower() == element.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == element.ToLower()));
                    if (arch == null)
                    {
                        Console.WriteLine($"Element {element} not known");
                        continue;
                    }
                    foundElements.Add(arch);
                }
            }

            JArray? techniques = (JArray?)newSpell["techniques"];
            List<AliasedArchetype> foundTechniques = new();
            if (techniques != null)
            {
                foreach (string? technique in techniques)
                {
                    if (string.IsNullOrEmpty(technique))
                    {
                        Console.WriteLine("Empty technique found in list");
                        continue;
                    }
                    AliasedArchetype? arch = allowedArchetypes.Techniques.FirstOrDefault(archetype => archetype.Name.ToLower() == technique.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == technique.ToLower()));
                    if (arch == null)
                    {
                        Console.WriteLine($"Technique {technique} not known");
                        continue;
                    }
                    foundTechniques.Add(arch);
                }
            }
            string? tomeID = (string?)newSpell["tomeId"];
            string? scrollID = (string?)newSpell["scrollId"];
            bool discoverable = (bool?)newSpell["discoverable"] ?? true;
            SpellInfo spell = new()
            {
                SpellID = spellID,
                Name = name,
                School = skill,
                Tier = level,
                CastingType = castingType,
                Targeting = foundTargets,
                Elements = foundElements,
                Techniques = foundTechniques,
                TomeID = tomeID,
                ScrollID = scrollID,
                Enabled = discoverable
            };
            ISpellGetter? spellForm = CheckSpellFormID(state, spell.SpellESP, spell.SpellFormID);
            if (spellForm == null)
            {
                return null;
            }
            else
            {
                spell.SpellForm = spellForm;
            }
            if (!string.IsNullOrEmpty(spell.TomeID))
            {
                IBookGetter? tome = CheckTomeFormID(state, spell.TomeESP ?? "", spell.TomeFormID ?? "");
                if (tome == null)
                {
                    return null;
                }
                spell.TomeForm = tome;
            }
            if (!string.IsNullOrEmpty(spell.ScrollID))
            {
                IScrollGetter? scroll = CheckScrollFormID(state, spell.ScrollESP ?? "", spell.ScrollFormID ?? "");
                if (scroll == null)
                {
                    return null;
                }
                spell.ScrollForm = scroll;
            }
            return spell;
        }
        private static SpellConfiguration ParseJSONFormat(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, JArray data, ArchetypeList allowedArchetypes)
        {
            // Non-Mysticism JSON Patch handling
            SpellConfiguration config = new();
            foreach (JToken newSpell in data)
            {
                SpellInfo? spell = ParseJSONSpellEntry(state, newSpell, allowedArchetypes);
                if (spell == null) continue;
                if (!config.Mods.ContainsKey(spell.SpellESP))
                {
                    config.Mods.Add(spell.SpellESP, (new List<SpellInfo>(), new List<SpellInfo>()));
                }
                config.Mods[spell.SpellESP].NewSpells.Add(spell);
            }
            return config;
        }

        private static SpellInfo? ParseJSONSpellEntry(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, JToken newSpell, ArchetypeList allowedArchetypes)
        {

            string name = (string?)newSpell["comment"] ?? string.Empty;
            if (name == string.Empty)
            {
                Console.WriteLine("Error reading spell name");
                return null;
            }
            string spellID = (string?)newSpell["spell"] ?? string.Empty;
            if (spellID == string.Empty)
            {
                Console.WriteLine($"Error reading spell ID for {name}");
                return null;
            }
            string skill = (string?)newSpell["skill"] ?? string.Empty;
            if (skill == string.Empty)
            {
                Console.WriteLine($"Error: Skill not found");
                return null;
            }
            if (!allowedArchetypes.Skills.Any(archetype => archetype.Name.ToLower() == skill.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == skill.ToLower())))
            {
                Console.WriteLine($"Error: Skill \"{skill}\" not known");
                return null;
            }
            string level = (string?)newSpell["level"] ?? string.Empty;
            if (level == string.Empty)
            {
                Console.WriteLine($"Error: Spell level not found");
                return null;
            }
            if (!allowedArchetypes.Levels.Any(archetype => archetype.Name.ToLower() == level.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == level.ToLower())))
            {
                Console.WriteLine($"Error: Spell level \"{level}\" not known");
                return null;
            }
            string castingType = (string?)newSpell["casting"] ?? string.Empty;
            if (castingType == string.Empty)
            {
                Console.WriteLine($"Error: Casting type not found");
                return null;
            }
            if (!allowedArchetypes.CastingTypes.Any(archetype => archetype.Name.ToLower() == castingType.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == castingType.ToLower())))
            {
                Console.WriteLine($"Error: Casting type \"{castingType}\" not known");
                return null;
            }
            JArray? target = (JArray?)newSpell["target"];
            if (target == null)
            {
                Console.WriteLine("Targeting type list not found");
                return null;
            }
            List<AliasedArchetype> foundTargets = new();
            foreach (string? targetType in target)
            {
                if (string.IsNullOrEmpty(targetType))
                {
                    Console.WriteLine("Empty targeting type found in list");
                    continue;
                }
                AliasedArchetype? arch = allowedArchetypes.Targets.FirstOrDefault(archetype => archetype.Name.ToLower() == targetType.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == targetType.ToLower()));
                if (arch == null)
                {
                    Console.WriteLine($"Targeting type {targetType} not known");
                    continue;
                }
                foundTargets.Add(arch);
            }
            JArray? elements = (JArray?)newSpell["elements"];
            List<AliasedArchetype> foundElements = new();
            if (elements != null)
            {
                foreach (string? element in elements)
                {
                    if (string.IsNullOrEmpty(element))
                    {
                        Console.WriteLine("Empty element found in list");
                        continue;
                    }
                    AliasedArchetype? arch = allowedArchetypes.Elements.FirstOrDefault(archetype => archetype.Name.ToLower() == element.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == element.ToLower()));
                    if (arch == null)
                    {
                        Console.WriteLine($"Element {element} not known");
                        continue;
                    }
                    foundElements.Add(arch);
                }
            }
            JArray? techniques = (JArray?)newSpell["techniques"];
            List<AliasedArchetype> foundTechniques = new();
            if (techniques != null)
            {
                foreach (string? technique in techniques)
                {
                    if (string.IsNullOrEmpty(technique))
                    {
                        Console.WriteLine("Empty technique found in list");
                        continue;
                    }
                    AliasedArchetype? arch = allowedArchetypes.Techniques.FirstOrDefault(archetype => archetype.Name.ToLower() == technique.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == technique.ToLower()));
                    if (arch == null)
                    {
                        Console.WriteLine($"Technique {technique} not known");
                        continue;
                    }
                    foundTechniques.Add(arch);
                }
            }
            string? tomeID = (string?)newSpell["tome"];
            string? scrollID = (string?)newSpell["scroll"];
            SpellInfo spell = new()
            {
                SpellID = spellID,
                Name = name,
                School = skill,
                Tier = level,
                CastingType = castingType,
                Targeting = foundTargets,
                Elements = foundElements,
                Techniques = foundTechniques,
                TomeID = tomeID,
                ScrollID = scrollID
            };
            ISpellGetter? spellForm = CheckSpellFormID(state, spell.SpellESP, spell.SpellFormID);
            if (spellForm == null)
            {
                return null;
            }
            else
            {
                spell.SpellForm = spellForm;
            }
            if (!string.IsNullOrEmpty(spell.TomeID))
            {
                Console.WriteLine(spell.TomeID);
                IBookGetter? tome = CheckTomeFormID(state, spell.TomeESP ?? "", spell.TomeFormID ?? "");
                if (tome == null)
                {
                    return null;
                }
                spell.TomeForm = tome;
            }
            if (!string.IsNullOrEmpty(spell.ScrollID))
            {
                IScrollGetter? scroll = CheckScrollFormID(state, spell.ScrollESP ?? "", spell.ScrollFormID ?? "");
                if (scroll == null)
                {
                    return null;
                }
                spell.ScrollForm = scroll;
            }
            return spell;
        }



        private static readonly Regex rx = new("^.*\\(\\s*(?<fid>(0x)?[a-fA-F0-9]+),\\s\"(?<esp>.*\\.es[pml])\".*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        private static readonly Regex rskill = new("^.*_SR_ListSpellsSkill(?<skill>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rcasting = new("^.*_SR_ListSpellsCasting(?<casting>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rlevel = new("^.*_SR_ListAllSpells[1-5](?<level>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rtarget = new("^.*_SR_ListSpellsTarget(?<target>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rtechnique = new("^.*_SR_ListSpellsTechnique(?<technique>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex relement = new("^.*_SR_ListSpellsElement(?<element>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static SpellConfiguration FromPsc(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string spellconf, ArchetypeList allowedArchetypes)
        {
            SpellConfiguration config = new();
            SpellInfo? spellInfo = null;
            int nestLevel = 0;
            foreach (string line in spellconf.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries & StringSplitOptions.TrimEntries))
            {
                if (line.Trim().StartsWith("if "))
                {
                    nestLevel++;
                }
                else if (line.Trim().StartsWith("endif"))
                {
                    nestLevel--;
                    if (nestLevel == 0 && !string.IsNullOrEmpty(spellInfo?.SpellFormID))
                    {
                        if (!config.Mods.ContainsKey(spellInfo.SpellESP))
                        {
                            config.Mods.Add(spellInfo.SpellESP, (new List<SpellInfo>(), new List<SpellInfo>()));
                        }
                        config.Mods[spellInfo.SpellESP].NewSpells.Add(spellInfo);
                        spellInfo = new SpellInfo();
                    }
                }
                if (line.Contains("TempSpell", StringComparison.OrdinalIgnoreCase) && line.Contains("GetFormFromFile", StringComparison.OrdinalIgnoreCase))
                {
                    MatchCollection matches = rx.Matches(line);
                    spellInfo = new();
                    string fid = matches.First().Groups["fid"].Value.Trim();
                    string esp = matches.First().Groups["esp"].Value.Trim();
                    spellInfo.SpellID = string.Format("__formData|{0}|{1}", esp, fid);
                    ISpellGetter? spellForm = CheckSpellFormID(state, spellInfo.SpellESP, spellInfo.SpellFormID);
                    if (spellForm?.Name?.String == null)
                    {
                        spellInfo = null;
                        continue;
                    }
                    spellInfo.SpellForm = spellForm;
                    spellInfo.Name = spellForm.Name.String;
                }
                else if (line.Contains("TempScroll", StringComparison.OrdinalIgnoreCase) && line.Contains("GetFormFromFile", StringComparison.OrdinalIgnoreCase))
                {
                    MatchCollection matches = rx.Matches(line);
                    spellInfo = new();
                    string fid = matches.First().Groups["fid"].Value.Trim();
                    string esp = matches.First().Groups["esp"].Value.Trim();
                    spellInfo.ScrollID = string.Format("__formData|{0}|{1}", esp, fid);
                    IScrollGetter? scroll = CheckScrollFormID(state, spellInfo.ScrollESP ?? "", spellInfo.ScrollFormID ?? "");
                    if (scroll == null)
                    {
                        spellInfo.ScrollID = null;
                    }
                    spellInfo.ScrollForm = scroll;
                }
                else if (line.Contains("RemoveAddedForm", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                else if (line.Contains("TempTome", StringComparison.OrdinalIgnoreCase) && line.Contains("GetFormFromFile", StringComparison.OrdinalIgnoreCase) && spellInfo != null)
                {
                    MatchCollection matches = rx.Matches(line);
                    string fid = matches.First().Groups["fid"].Value.Trim();
                    string esp = matches.First().Groups["esp"].Value.Trim();

                    spellInfo.TomeID = string.Format("__formData|{0}|{1}", esp, fid);
                    IBookGetter? tome = CheckTomeFormID(state, spellInfo.TomeESP ?? "", spellInfo.TomeFormID ?? "");
                    if (tome == null)
                    {
                        spellInfo.TomeID = null;
                    }
                    spellInfo.TomeForm = tome;
                }
                // spellcount >= 1 to ignore all the spellresearch import stuff
                else if (spellInfo != null)
                {
                    MatchCollection mskill = rskill.Matches(line);
                    MatchCollection mcasting = rcasting.Matches(line);
                    MatchCollection mlevel = rlevel.Matches(line);
                    MatchCollection mtarget = rtarget.Matches(line);
                    MatchCollection mtechnique = rtechnique.Matches(line);
                    MatchCollection melement = relement.Matches(line);
                    if (mskill.Count > 0)
                    {
                        string match = mskill.First().Groups["skill"].Value.Trim();
                        AliasedArchetype? school = allowedArchetypes.Skills.FirstOrDefault(archetype => archetype.Name.ToLower() == match.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == match.ToLower()));
                        if (school == null)
                        {
                            Console.WriteLine($"School {match} not found");
                            continue;
                        }
                        spellInfo.School = school.Name;
                    }
                    else if (mcasting.Count > 0)
                    {
                        string match = mcasting.First().Groups["casting"].Value.Trim();
                        AliasedArchetype? castingType = allowedArchetypes.CastingTypes.FirstOrDefault(archetype => archetype.Name.ToLower() == match.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == match.ToLower()));
                        if (castingType == null)
                        {
                            Console.WriteLine($"Casting type {match} not found");
                            continue;
                        }
                        spellInfo.CastingType = castingType.Name;
                    }
                    else if (mlevel.Count > 0)
                    {
                        string match = mlevel.First().Groups["level"].Value.Trim();
                        AliasedArchetype? level = allowedArchetypes.Levels.FirstOrDefault(archetype => archetype.Name.ToLower() == match.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == match.ToLower()));
                        if (level == null)
                        {
                            Console.WriteLine($"Level {match} not found");
                            continue;
                        }
                        spellInfo.Tier = level.Name;
                    }
                    else if (mtarget.Count > 0)
                    {
                        string match = mtarget.First().Groups["target"].Value.Trim();
                        AliasedArchetype? target = allowedArchetypes.Targets.FirstOrDefault(archetype => archetype.Name.ToLower() == match.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == match.ToLower()));
                        if (target == null)
                        {
                            Console.WriteLine($"Targeting type {match} not found");
                            continue;
                        }
                        spellInfo.Targeting.Add(target);
                    }
                    else if (mtechnique.Count > 0)
                    {
                        string match = mtechnique.First().Groups["technique"].Value.Trim();
                        AliasedArchetype? technique = allowedArchetypes.Techniques.FirstOrDefault(archetype => archetype.Name.ToLower() == match.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == match.ToLower()));
                        if (technique == null)
                        {
                            Console.WriteLine($"Technique {match} not found");
                            continue;
                        }
                        spellInfo.Techniques.Add(technique);
                    }
                    else if (melement.Count > 0)
                    {
                        string match = melement.First().Groups["element"].Value.Trim();
                        AliasedArchetype? element = allowedArchetypes.Elements.FirstOrDefault(archetype => archetype.Name.ToLower() == match.ToLower() || archetype.Aliases.Any(alias => alias.ToLower() == match.ToLower()));
                        if (element == null)
                        {
                            Console.WriteLine($"Element {match} not found");
                            continue;
                        }
                        spellInfo.Elements.Add(element);
                    }

                }
            }
            return config;
        }

        private static ISpellGetter? CheckSpellFormID(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string spellESP, string spellFormID)
        {
            ISkyrimModGetter? mod = state.LoadOrder[spellESP].Mod;
            if (mod == null) return null;
            Console.WriteLine($"Resolving spell ID {spellFormID} from {spellESP}");
            if (spellFormID.Length >= 8)
            {
                spellFormID = spellFormID[(spellFormID.Length - 6)..].ToLower();
            }
            else
            {
                spellFormID = Convert.ToInt32(spellFormID).ToString("X6").ToLower();
            }
            ISpellGetter? spell = mod.Spells.FirstOrDefault(s => s.FormKey.ID.ToString("X6").ToLower() == spellFormID);
            if (spell == null)
            {
                if (mod.MasterReferences.Any())
                {
                    IMasterReferenceGetter? master = mod.MasterReferences[mod.MasterReferences.Count - 1];
                    Console.WriteLine($"Trying master {master.Master.FileName}");
                    return CheckSpellFormID(state, master.Master.FileName, int.Parse(spellFormID, System.Globalization.NumberStyles.HexNumber).ToString().PadLeft(6, '0'));
                }
                Console.WriteLine($"Couldn't resolve spell ID {spellFormID} from {spellESP}");
                return null;
            }
            else
            {
                return spell;
            }
        }
        private static IBookGetter? CheckTomeFormID(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string tomeESP, string tomeFormID)
        {
            ISkyrimModGetter? mod = state.LoadOrder[tomeESP].Mod;
            if (mod == null) return null;
            Console.WriteLine($"Resolving tome ID {tomeFormID} from {tomeESP}");
            if (tomeFormID.Length >= 8)
            {
                tomeFormID = tomeFormID[(tomeFormID.Length - 6)..].ToLower();
            }
            else
            {
                tomeFormID = Convert.ToInt32(tomeFormID).ToString("X6").ToLower();
            }
            IBookGetter? tome = mod.Books.FirstOrDefault(b => b.FormKey.ID.ToString("X6").ToLower() == tomeFormID);
            if (tome == null)
            {
                if (mod.MasterReferences.Any())
                {
                    IMasterReferenceGetter? master = mod.MasterReferences[mod.MasterReferences.Count - 1];
                    Console.WriteLine($"Trying master {master.Master.FileName}");
                    return CheckTomeFormID(state, master.Master.FileName, int.Parse(tomeFormID, System.Globalization.NumberStyles.HexNumber).ToString().PadLeft(6, '0'));
                }
                Console.WriteLine($"Couldn't resolve tome ID {tomeFormID} from {tomeESP}");
                return null;
            }
            else
            {
                return tome;
            }
        }
        private static IScrollGetter? CheckScrollFormID(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string scrollESP, string scrollFormID)
        {
            ISkyrimModGetter? mod = state.LoadOrder[scrollESP].Mod;
            if (mod == null) return null;
            Console.WriteLine($"Resolving scroll ID {scrollFormID} from {scrollESP}");
            if (scrollFormID.Length >= 8)
            {
                scrollFormID = scrollFormID[(scrollFormID.Length - 6)..].ToLower();
            }
            else
            {
                scrollFormID = Convert.ToInt32(scrollFormID).ToString("X6").ToLower();
            }
            IScrollGetter? scroll = mod.Scrolls.FirstOrDefault(s => s.FormKey.ID.ToString("X6").ToLower() == scrollFormID);
            if (scroll == null)
            {
                if (mod.MasterReferences.Any())
                {
                    IMasterReferenceGetter? master = mod.MasterReferences[mod.MasterReferences.Count - 1];
                    Console.WriteLine($"Trying master {master.Master.FileName}");
                    return CheckScrollFormID(state, master.Master.FileName, int.Parse(scrollFormID, System.Globalization.NumberStyles.HexNumber).ToString().PadLeft(6, '0'));
                }
                Console.WriteLine($"Couldn't resolve scroll ID {scrollFormID} from {scrollESP}");
                return null;
            }
            else
            {
                return scroll;
            }
        }
    }
}
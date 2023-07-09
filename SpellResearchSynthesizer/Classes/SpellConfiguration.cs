using Mutagen.Bethesda.Plugins.Exceptions;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static SpellResearchSynthesizer.Classes.Archetype;

namespace SpellResearchSynthesizer.Classes
{
    public class SpellConfiguration
    {
        public Dictionary<string, ModSpellData> Mods = new();

        public static SpellConfiguration FromJson(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string spellconf)
        {
            SpellConfiguration config = new();
            if (spellconf.StartsWith("{"))
            {
                JObject data = JObject.Parse(spellconf);
                if (data == null) return config;
                config = ParseMysticismFormat(state, data);
            }
            else if (spellconf.StartsWith("["))
            {
                JArray data = JArray.Parse(spellconf);
                if (data == null) return config;
                config = ParseJSONFormat(state, data);
            }
            config.ClearDuplicateArchetypes();
            return config;
        }

        private static SpellConfiguration ParseMysticismFormat(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, JObject data)
        {
            // Mysticism JSON Patch handling
            SpellConfiguration config = new();
            JToken? newSpells = data["newSpells"];
            if (newSpells != null)
            {
                foreach (JToken newSpell in newSpells)
                {
                    SpellInfo? spell = ParseMysticismSpellEntry(state, newSpell);
                    if (spell == null) continue;

                    if (!config.Mods.ContainsKey(spell.SpellESP))
                    {
                        config.Mods.Add(spell.SpellESP, new ModSpellData());
                    }
                    config.Mods[spell.SpellESP].NewSpells.Add(spell);
                }
            }
            JToken? removedSpells = data["removedSpells"];
            if (removedSpells != null)
            {
                foreach (JToken removedSpell in removedSpells)
                {
                    SpellInfo? spell = ParseMysticismSpellEntry(state, removedSpell);
                    if (spell == null) continue;

                    if (!config.Mods.ContainsKey(spell.SpellESP))
                    {
                        config.Mods.Add(spell.SpellESP, new ModSpellData());
                    }
                    config.Mods[spell.SpellESP].RemovedSpells.Add(spell);
                }
            }
            JToken? newArtifacts = data["newArtifacts"];
            if (newArtifacts != null)
            {
                foreach (JToken newArtifact in newArtifacts)
                {
                    ArtifactInfo? artifact = ParseArtifactJSON(state, newArtifact);
                    if (artifact == null) continue;

                    if (!config.Mods.ContainsKey(artifact.ArtifactESP))
                    {
                        config.Mods.Add(artifact.ArtifactESP, new ModSpellData());
                    }
                    config.Mods[artifact.ArtifactESP].NewArtifacts.Add(artifact);
                }
            }
            return config;
        }

        private void ClearDuplicateArchetypes()
        {
            foreach (KeyValuePair<string, ModSpellData> m in Mods)
            {
                foreach (SpellInfo spell in m.Value.NewSpells)
                {
                    spell.Targeting = spell.Targeting.Distinct().ToList();
                    spell.Elements = spell.Elements.Distinct().ToList();
                    spell.Techniques = spell.Techniques.Distinct().ToList();
                }
                foreach (ArtifactInfo artifact in m.Value.NewArtifacts)
                {
                    artifact.Schools = artifact.Schools.Distinct().ToList();
                    artifact.CastingTypes = artifact.CastingTypes.Distinct().ToList();
                    artifact.Targeting = artifact.Targeting.Distinct().ToList();
                    artifact.Elements = artifact.Elements.Distinct().ToList();
                    artifact.Techniques = artifact.Techniques.Distinct().ToList();
                }
            }
        }

        private static SpellInfo? ParseMysticismSpellEntry(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, JToken newSpell)
        {
            ArchetypeInfo archetypes = ArchetypeInfo.Instance;
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
            string skillName = (string?)newSpell["school"] ?? string.Empty;
            if (skillName == string.Empty)
            {
                Console.WriteLine($"Error: Skill not found");
                return null;
            }
            Archetype? skill = archetypes.GetArchetype(ArchetypeType.Skill, skillName);
            if (skill == null)
            {
                Console.WriteLine($"Error: Skill \"{skillName}\" not known");
                return null;
            }
            string levelName = (string?)newSpell["tier"] ?? string.Empty;
            if (levelName == string.Empty)
            {
                Console.WriteLine($"Error: Spell level not found");
                return null;
            }
            Archetype? level = archetypes.GetArchetype(ArchetypeType.Tier, levelName);
            if (level == null)
            {
                Console.WriteLine($"Error: Spell level \"{levelName}\" not known");
                return null;
            }
            string castingTypeName = (string?)newSpell["castingType"] ?? string.Empty;
            if (castingTypeName == string.Empty)
            {
                Console.WriteLine($"Error: Casting type not found");
                return null;
            }
            Archetype? castingType = archetypes.GetArchetype(ArchetypeType.CastingType, castingTypeName);
            if (castingType == null)
            {
                Console.WriteLine($"Error: Casting type \"{castingTypeName}\" not known");
                return null;
            }
            JToken? target = newSpell["targeting"];
            if (target == null)
            {
                Console.WriteLine("Targeting type list not found");
                return null;
            }
            List<Archetype> foundTargets = new();
            JArray targetArr = target is JArray array ? array : new JArray { target.ToString() };
            foreach (string? targetType in targetArr.Select(v => v.ToString()))
            {
                if (string.IsNullOrEmpty(targetType))
                {
                    Console.WriteLine("Empty targeting type found in list");
                    return null;
                }
                Archetype? arch = archetypes.GetArchetype(ArchetypeType.Targeting, targetType);
                if (arch == null)
                {
                    Console.WriteLine($"Targeting type {targetType} not known");
                    return null;
                }
                foundTargets.Add(arch);
            }
            JArray? elements = (JArray?)newSpell["elements"];
            List<Archetype> foundElements = new();
            if (elements != null)
            {
                foreach (string? element in elements.Select(e => e.ToString()))
                {
                    if (string.IsNullOrEmpty(element))
                    {
                        Console.WriteLine("Empty element found in list");
                        continue;
                    }
                    Archetype? arch = archetypes.GetArchetype(ArchetypeType.Element, element);
                    if (arch == null)
                    {
                        Console.WriteLine($"Element {element} not known");
                        continue;
                    }
                    foundElements.Add(arch);
                }
            }

            JArray? techniques = (JArray?)newSpell["techniques"];
            List<Archetype> foundTechniques = new();
            if (techniques != null)
            {
                foreach (string? technique in techniques.Select(t => t.ToString()))
                {
                    if (string.IsNullOrEmpty(technique))
                    {
                        Console.WriteLine("Empty technique found in list");
                        continue;
                    }
                    Archetype? arch = archetypes.GetArchetype(ArchetypeType.Technique, technique);
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
            bool hardRemoved = (bool?)newSpell["hardRemoved"] ?? false;
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
                Enabled = discoverable,
                HardRemoved = hardRemoved
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
        private static ArtifactInfo? ParseArtifactJSON(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, JToken newArtifact)
        {
            ArchetypeInfo archetypes = ArchetypeInfo.Instance;
            string name = (string?)newArtifact["name"] ?? string.Empty;
            if (name == string.Empty)
            {
                Console.WriteLine("Error reading artifact name");
                return null;
            }
            string artifactID = (string?)newArtifact["artifactID"] ?? string.Empty;
            if (artifactID == string.Empty)
            {
                Console.WriteLine($"Error reading artifact ID for {name}");
                return null;
            }
            int? tier = (int?)newArtifact["tier"];
            if (tier == null)
            {
                Console.WriteLine($"Error getting tier for {name}");
                return null;
            }
            if (!archetypes.ArtifactTiers.Contains((int)tier))
            {
                Console.WriteLine($"Tier {tier} not allowed");
            }
            JArray? schools = (JArray?)newArtifact["schools"];
            List<Archetype> foundSchools = new();
            if (schools != null)
            {
                foreach (string? schoolName in schools.Select(s => s.ToString()))
                {
                    if (string.IsNullOrEmpty(schoolName))
                    {
                        Console.WriteLine("Empty school found in list");
                        return null;
                    }
                    Archetype? school = archetypes.GetArchetype(ArchetypeType.Skill, schoolName);
                    if (school == null)
                    {
                        Console.WriteLine($"School {schoolName} not known");
                        return null;
                    }
                    foundSchools.Add(school);
                }
            }
            JArray? castingTypes = (JArray?)newArtifact["castingTypes"];
            List<Archetype> foundCastingTypes = new();
            if (castingTypes != null)
            {
                foreach (string? castingTypeName in castingTypes.Select(ct => ct.ToString()))
                {
                    if (string.IsNullOrEmpty(castingTypeName))
                    {
                        Console.WriteLine("Empty casting type found in list");
                        return null;
                    }
                    Archetype? castingType = archetypes.GetArchetype(ArchetypeType.CastingType, castingTypeName);
                    if (castingType == null)
                    {
                        Console.WriteLine($"Casting type {castingTypeName} not known");
                        return null;
                    }
                    foundSchools.Add(castingType);
                }
            }
            JArray? target = (JArray?)newArtifact["targeting"];
            List<Archetype> foundTargets = new();
            if (target != null)
            {
                foreach (string? targetType in target.Select(t => t.ToString()))
                {
                    if (string.IsNullOrEmpty(targetType))
                    {
                        Console.WriteLine("Empty targeting type found in list");
                        return null;
                    }
                    Archetype? arch = archetypes.GetArchetype(ArchetypeType.Targeting, targetType);
                    if (arch == null)
                    {
                        Console.WriteLine($"Targeting type {targetType} not known");
                        return null;
                    }
                    foundTargets.Add(arch);
                }
            }
            JArray? elements = (JArray?)newArtifact["elements"];
            List<Archetype> foundElements = new();
            if (elements != null)
            {
                foreach (string? element in elements.Select(t => t.ToString()))
                {
                    if (string.IsNullOrEmpty(element))
                    {
                        Console.WriteLine("Empty element found in list");
                        continue;
                    }
                    Archetype? arch = archetypes.GetArchetype(ArchetypeType.Element, element);
                    if (arch == null)
                    {
                        Console.WriteLine($"Element {element} not known");
                        continue;
                    }
                    foundElements.Add(arch);
                }
            }

            JArray? techniques = (JArray?)newArtifact["techniques"];
            List<Archetype> foundTechniques = new();
            if (techniques != null)
            {
                foreach (string? technique in techniques.Select(t => t.ToString()))
                {
                    if (string.IsNullOrEmpty(technique))
                    {
                        Console.WriteLine("Empty technique found in list");
                        continue;
                    }
                    Archetype? arch = archetypes.GetArchetype(ArchetypeType.Technique, technique);
                    if (arch == null)
                    {
                        Console.WriteLine($"Technique {technique} not known");
                        continue;
                    }
                    foundTechniques.Add(arch);
                }
            }
            bool equippable = (bool?)newArtifact["equippable"] ?? false;
            bool equippableArtifact = (bool?)newArtifact["equippableArtifact"] ?? false;
            bool equippableText = (bool?)newArtifact["equippableText"] ?? false;
            ArtifactInfo artifact = new()
            {
                Name = name,
                ArtifactID = artifactID,
                Tier = (int)tier,
                Schools = foundSchools,
                CastingTypes = foundCastingTypes,
                Targeting = foundTargets,
                Elements = foundElements,
                Techniques = foundTechniques,
                Equippable = equippable,
                EquippableArtifact = equippableArtifact,
                EquippableText = equippableText
            };
            IItemGetter? artifactForm = CheckArtifactFormID(state, artifact.ArtifactESP, artifact.ArtifactFormID);
            if (artifactForm == null)
            {
                return null;
            }
            else
            {
                artifact.ArtifactForm = artifactForm;
                return artifact;
            }
        }
        private static SpellConfiguration ParseJSONFormat(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, JArray data)
        {
            // Non-Mysticism JSON Patch handling
            SpellConfiguration config = new();
            foreach (JToken newSpell in data)
            {
                SpellInfo? spell = ParseJSONSpellEntry(state, newSpell);
                if (spell == null) continue;
                if (!config.Mods.ContainsKey(spell.SpellESP))
                {
                    config.Mods.Add(spell.SpellESP, new ModSpellData());
                }
                config.Mods[spell.SpellESP].NewSpells.Add(spell);
            }
            return config;
        }

        private static SpellInfo? ParseJSONSpellEntry(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, JToken newSpell)
        {
            ArchetypeInfo archetypes = ArchetypeInfo.Instance;
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
            string skillName = (string?)newSpell["skill"] ?? string.Empty;
            if (skillName == string.Empty)
            {
                Console.WriteLine($"Error: Skill not found");
                return null;
            }
            Archetype? skill = archetypes.GetArchetype(ArchetypeType.Skill, skillName);
            if (skill == null)
            {
                Console.WriteLine($"Error: Skill \"{skillName}\" not known");
                return null;
            }
            string levelName = (string?)newSpell["level"] ?? string.Empty;
            if (levelName == string.Empty)
            {
                Console.WriteLine($"Error: Spell level not found");
                return null;
            }
            Archetype? level = archetypes.GetArchetype(ArchetypeType.Tier, levelName);
            if (level == null)
            {
                Console.WriteLine($"Error: Spell level \"{levelName}\" not known");
                return null;
            }
            string castingTypeName = (string?)newSpell["casting"] ?? string.Empty;
            if (castingTypeName == string.Empty)
            {
                Console.WriteLine($"Error: Casting type not found");
                return null;
            }
            Archetype? castingType = archetypes.GetArchetype(ArchetypeType.CastingType, castingTypeName);
            if (castingType == null)
            {
                Console.WriteLine($"Error: Casting type \"{castingTypeName}\" not known");
                return null;
            }
            JToken? target = newSpell["target"];
            if (target == null)
            {
                Console.WriteLine("Targeting type list not found");
                return null;
            }
            List<Archetype> foundTargets = new();
            JArray targetArr = target is JArray array ? array : new JArray { target.ToString() };
            foreach (string? targetType in targetArr.Select(t => t.ToString()))
            {
                if (string.IsNullOrEmpty(targetType))
                {
                    Console.WriteLine("Empty targeting type found in list");
                    continue;
                }
                Archetype? arch = archetypes.GetArchetype(ArchetypeType.Targeting, targetType);
                if (arch == null)
                {
                    Console.WriteLine($"Targeting type {targetType} not known");
                    continue;
                }
                foundTargets.Add(arch);
            }
            JArray? elements = (JArray?)newSpell["elements"];
            List<Archetype> foundElements = new();
            if (elements != null)
            {
                foreach (string? element in elements.Select(t => t.ToString()))
                {
                    if (string.IsNullOrEmpty(element))
                    {
                        Console.WriteLine("Empty element found in list");
                        continue;
                    }
                    Archetype? arch = archetypes.GetArchetype(ArchetypeType.Element, element);
                    if (arch == null)
                    {
                        Console.WriteLine($"Element {element} not known");
                        continue;
                    }
                    foundElements.Add(arch);
                }
            }
            JArray? techniques = (JArray?)newSpell["techniques"];
            List<Archetype> foundTechniques = new();
            if (techniques != null)
            {
                foreach (string? technique in techniques.Select(t => t.ToString()))
                {
                    if (string.IsNullOrEmpty(technique))
                    {
                        Console.WriteLine("Empty technique found in list");
                        continue;
                    }
                    Archetype? arch = archetypes.GetArchetype(ArchetypeType.Technique, technique);
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

        private static readonly Regex rUndiscoverable = new("^.*_SR_ListUndiscoverableSpells.*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rLevel = new("^.*_SR_ListAllSpells[1-5](?<level>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rSkill = new("^.*_SR_ListSpellsSkill(?<skill>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rCasting = new("^.*_SR_ListSpellsCasting(?<casting>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rTarget = new("^.*_SR_ListSpellsTarget(?<target>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rTechnique = new("^.*_SR_ListSpellsTechnique(?<technique>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rElement = new("^.*_SR_ListSpellsElement(?<element>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rArtifactTier = new("^.*_SR_ListArtifactsAllBase(?<tier>[0-5]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rArtifactSkill = new("^.*_SR_ListArtifactsBaseSkill(?<skill>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rArtifactCasting = new("^.*_SR_ListArtifactsBaseCasting(?<casting>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rArtifactTarget = new("^.*_SR_ListArtifactsBaseTarget(?<target>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rArtifactTechnique = new("^.*_SR_ListArtifactsBaseTechnique(?<technique>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rArtifactElement = new("^.*_SR_ListArtifactsBaseElement(?<element>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rArtifactEquippableAll = new("^.*_SR_ListEquippableAll(?<equippable>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rArtifactEquippableArtifact = new("^.*_SR_ListEquippableArtifacts(?<equippableArtifact>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rArtifactEquippableText = new("^.*_SR_ListEquippableTexts(?<equippableText>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rAlchemicalEffectElement = new("^.*_SR_ListAlchEffectsElement(?<element>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rAlchemicalEffectTechnique = new("^.*_SR_ListAlchEffectsTechnique(?<technique>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static SpellConfiguration FromPsc(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string spellconf)
        {
            ArchetypeInfo archetypes = ArchetypeInfo.Instance;
            SpellConfiguration config = new();
            SpellInfo? spellInfo = null;
            ArtifactInfo? artifactInfo = null;
            AlchemyEffectInfo? effectInfo = null;
            int nestLevel = 0;
            int spellIndent = 0;
            foreach (string line in spellconf.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries & StringSplitOptions.TrimEntries))
            {
                if (line.Trim().ToLower().StartsWith("if ") || line.Trim().ToLower().StartsWith("if("))
                {
                    nestLevel++;
                }
                else if (line.Trim().ToLower().StartsWith("endif"))
                {
                    nestLevel--;
                    if (nestLevel <= spellIndent)
                    {
                        if (spellInfo?.SpellForm != null)
                        {

                            if (!config.Mods.ContainsKey(spellInfo.SpellESP))
                            {
                                config.Mods.Add(spellInfo.SpellESP, new ModSpellData());
                            }
                            config.Mods[spellInfo.SpellESP].NewSpells.Add(spellInfo);
                        }
                        spellInfo = null;
                        if (artifactInfo?.ArtifactForm != null)
                        {
                            if (!config.Mods.ContainsKey(artifactInfo.ArtifactESP))
                            {
                                config.Mods.Add(artifactInfo.ArtifactESP, new ModSpellData());
                            }
                            config.Mods[artifactInfo.ArtifactESP].NewArtifacts.Add(artifactInfo);
                        }
                        artifactInfo = null;
                        if (effectInfo?.EffectForm != null)
                        {
                            if (!config.Mods.ContainsKey(effectInfo.EffectESP))
                            {
                                config.Mods.Add(effectInfo.EffectESP, new ModSpellData());
                            }
                            config.Mods[effectInfo.EffectESP].NewAlchemyEffects.Add(effectInfo);
                        }
                        effectInfo = null;
                    }
                }
                if (line.Contains("TempSpell", StringComparison.OrdinalIgnoreCase) && line.Contains("GetFormFromFile", StringComparison.OrdinalIgnoreCase))
                {
                    spellIndent = nestLevel;
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
                else if ((line.Contains("TempIngredient", StringComparison.OrdinalIgnoreCase) || line.Contains("TempArtifact", StringComparison.OrdinalIgnoreCase)) && line.Contains("GetFormFromFile", StringComparison.OrdinalIgnoreCase))
                {
                    spellIndent = nestLevel;
                    MatchCollection matches = rx.Matches(line);
                    artifactInfo = new();
                    string fid = matches.First().Groups["fid"].Value.Trim();
                    string esp = matches.First().Groups["esp"].Value.Trim();
                    artifactInfo.ArtifactID = string.Format("__formData|{0}|{1}", esp, fid);
                    IItemGetter? artifactForm = CheckArtifactFormID(state, artifactInfo.ArtifactESP, artifactInfo.ArtifactFormID);
                    if (artifactForm is IIngredientGetter ingredient && ingredient.Name?.String != null)
                    {
                        artifactInfo.ArtifactForm = ingredient;
                        artifactInfo.Name = ingredient.Name.String;
                    }
                    else if (artifactForm is IMiscItemGetter miscItem && miscItem.Name?.String != null)
                    {
                        artifactInfo.ArtifactForm = miscItem;
                        artifactInfo.Name = miscItem.Name.String;
                    }
                    else
                    {
                        artifactInfo = null;
                        continue;
                    }
                }
                else if (line.Contains("TempScroll", StringComparison.OrdinalIgnoreCase) && line.Contains("GetFormFromFile", StringComparison.OrdinalIgnoreCase))
                {
                    MatchCollection matches = rx.Matches(line);
                    spellInfo ??= new();
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
                else if (line.Contains("TempEffect", StringComparison.OrdinalIgnoreCase) && line.Contains("GetFormFromFile", StringComparison.OrdinalIgnoreCase))
                {
                    spellIndent = nestLevel;
                    MatchCollection matches = rx.Matches(line);
                    effectInfo = new();
                    string fid = matches.First().Groups["fid"].Value.Trim();
                    string esp = matches.First().Groups["esp"].Value.Trim();
                    effectInfo.EffectID = string.Format("__formData|{0}|{1}", esp, fid);
                    IMagicEffectGetter? effectForm = CheckMagicEffectFormID(state, effectInfo.EffectESP, effectInfo.EffectFormID);
                    if (effectForm?.Name?.String == null)
                    {
                        effectInfo = null;
                        continue;
                    }
                    effectInfo.EffectForm = effectForm;
                    effectInfo.Name = effectForm.Name.String;
                }
                else if (line.Contains("RemoveAddedForm", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                else if (line.Contains("TempTome", StringComparison.OrdinalIgnoreCase) && line.Contains("GetFormFromFile", StringComparison.OrdinalIgnoreCase) && spellInfo != null)
                {
                    MatchCollection matches = rx.Matches(line);
                    spellInfo ??= new();
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
                    MatchCollection mUndiscoverable = rUndiscoverable.Matches(line);
                    MatchCollection mSkill = rSkill.Matches(line);
                    MatchCollection mCasting = rCasting.Matches(line);
                    MatchCollection mLevel = rLevel.Matches(line);
                    MatchCollection mTarget = rTarget.Matches(line);
                    MatchCollection mTechnique = rTechnique.Matches(line);
                    MatchCollection mElement = rElement.Matches(line);
                    if (mUndiscoverable.Count > 0)
                    {
                        spellInfo.Enabled = false;
                    }
                    else if (mSkill.Count > 0)
                    {
                        string match = mSkill.First().Groups["skill"].Value.Trim();
                        Archetype? school = archetypes.GetArchetype(ArchetypeType.Skill, match);
                        if (school == null)
                        {
                            Console.WriteLine($"School {match} not found");
                            continue;
                        }
                        spellInfo.School = school;
                    }
                    else if (mCasting.Count > 0)
                    {
                        string match = mCasting.First().Groups["casting"].Value.Trim();
                        Archetype? castingType = archetypes.GetArchetype(ArchetypeType.CastingType, match);
                        if (castingType == null)
                        {
                            Console.WriteLine($"Casting type {match} not found");
                            continue;
                        }
                        spellInfo.CastingType = castingType;
                    }
                    else if (mLevel.Count > 0)
                    {
                        string match = mLevel.First().Groups["level"].Value.Trim();
                        Archetype? level = archetypes.GetArchetype(ArchetypeType.Tier, match);
                        if (level == null)
                        {
                            Console.WriteLine($"Level {match} not found");
                            continue;
                        }
                        spellInfo.Tier = level;
                    }
                    else if (mTarget.Count > 0)
                    {
                        string match = mTarget.First().Groups["target"].Value.Trim();
                        Archetype? target = archetypes.GetArchetype(ArchetypeType.Targeting, match);
                        if (target == null)
                        {
                            Console.WriteLine($"Targeting type {match} not found");
                            continue;
                        }
                        spellInfo.Targeting.Add(target);
                    }
                    else if (mTechnique.Count > 0)
                    {
                        string match = mTechnique.First().Groups["technique"].Value.Trim();
                        Archetype? technique = archetypes.GetArchetype(ArchetypeType.Technique, match);
                        if (technique == null)
                        {
                            Console.WriteLine($"Technique {match} not found");
                            continue;
                        }
                        spellInfo.Techniques.Add(technique);
                    }
                    else if (mElement.Count > 0)
                    {
                        string match = mElement.First().Groups["element"].Value.Trim();
                        Archetype? element = archetypes.GetArchetype(ArchetypeType.Element, match);
                        if (element == null)
                        {
                            Console.WriteLine($"Element {match} not found");
                            continue;
                        }
                        spellInfo.Elements.Add(element);
                    }

                }
                else if (artifactInfo != null)
                {
                    MatchCollection mSkill = rArtifactSkill.Matches(line);
                    MatchCollection mCasting = rArtifactCasting.Matches(line);
                    MatchCollection mTier = rArtifactTier.Matches(line);
                    MatchCollection mTarget = rArtifactTarget.Matches(line);
                    MatchCollection mTechnique = rArtifactTechnique.Matches(line);
                    MatchCollection mElement = rArtifactElement.Matches(line);
                    MatchCollection mEquippableAll = rArtifactEquippableAll.Matches(line);
                    MatchCollection mEquippableArtifact = rArtifactEquippableArtifact.Matches(line);
                    MatchCollection mEquippableText = rArtifactEquippableText.Matches(line);
                    if (mSkill.Count > 0)
                    {
                        string match = mSkill.First().Groups["skill"].Value.Trim();
                        Archetype? school = archetypes.GetArchetype(ArchetypeType.Skill, match);
                        if (school == null)
                        {
                            Console.WriteLine($"School {match} not found");
                            continue;
                        }
                        artifactInfo.Schools.Add(school);
                    }
                    else if (mCasting.Count > 0)
                    {
                        string match = mCasting.First().Groups["casting"].Value.Trim();
                        Archetype? castingType = archetypes.GetArchetype(ArchetypeType.CastingType, match);
                        if (castingType == null)
                        {
                            Console.WriteLine($"Casting type {match} not found");
                            continue;
                        }
                        artifactInfo.CastingTypes.Add(castingType);
                    }
                    else if (mTier.Count > 0)
                    {
                        string match = mTier.First().Groups["tier"].Value.Trim();
                        int tier = int.TryParse(match, out int _tier) ? _tier : -1;
                        if (!archetypes.ArtifactTiers.Contains(tier))
                        {
                            Console.WriteLine($"Tier {tier} not found");
                            continue;
                        }
                        artifactInfo.Tier = tier;
                    }
                    else if (mTarget.Count > 0)
                    {
                        string match = mTarget.First().Groups["target"].Value.Trim();
                        Archetype? target = archetypes.GetArchetype(ArchetypeType.Targeting, match);
                        if (target == null)
                        {
                            Console.WriteLine($"Targeting type {match} not found");
                            continue;
                        }
                        artifactInfo.Targeting.Add(target);
                    }
                    else if (mTechnique.Count > 0)
                    {
                        string match = mTechnique.First().Groups["technique"].Value.Trim();
                        Archetype? technique = archetypes.GetArchetype(ArchetypeType.Technique, match);
                        if (technique == null)
                        {
                            Console.WriteLine($"Technique {match} not found");
                            continue;
                        }
                        artifactInfo.Techniques.Add(technique);
                    }
                    else if (mElement.Count > 0)
                    {
                        string match = mElement.First().Groups["element"].Value.Trim();
                        Archetype? element = archetypes.GetArchetype(ArchetypeType.Element, match);
                        if (element == null)
                        {
                            Console.WriteLine($"Element {match} not found");
                            continue;
                        }
                        artifactInfo.Elements.Add(element);
                    }
                    else if (mEquippableAll.Count > 0)
                    {
                        artifactInfo.Equippable = true;
                    }
                    else if (mEquippableArtifact.Count > 0)
                    {
                        artifactInfo.EquippableArtifact = true;
                    }
                    else if (mEquippableText.Count > 0)
                    {
                        artifactInfo.EquippableText = true;
                    }

                }
                else if (effectInfo != null)
                {
                    MatchCollection mTechnique = rAlchemicalEffectTechnique.Matches(line);
                    MatchCollection mElement = rAlchemicalEffectElement.Matches(line);

                    if (mTechnique.Count > 0)
                    {
                        string match = mTechnique.First().Groups["technique"].Value.Trim();
                        Archetype? technique = archetypes.GetArchetype(ArchetypeType.Technique, match);
                        if (technique == null)
                        {
                            Console.WriteLine($"Technique {match} not found");
                            continue;
                        }
                        effectInfo.Techniques.Add(technique);
                    }
                    else if (mElement.Count > 0)
                    {
                        string match = mElement.First().Groups["element"].Value.Trim();
                        Archetype? element = archetypes.GetArchetype(ArchetypeType.Element, match);
                        if (element == null)
                        {
                            Console.WriteLine($"Element {match} not found");
                            continue;
                        }
                        effectInfo.Elements.Add(element);
                    }
                }
            }
            config.ClearDuplicateArchetypes();
            return config;
        }

        private static ISpellGetter? CheckSpellFormID(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string spellESP, string spellFormID)
        {
            ISkyrimModGetter? mod;
            try
            {
                mod = state.LoadOrder[spellESP].Mod;
            }
            catch (MissingModException)
            {
                Console.WriteLine($"Mod {spellESP} not found in load order.");
                mod = null;
            }
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
            ISkyrimModGetter? mod;
            try
            {
                mod = state.LoadOrder[tomeESP].Mod;
            }
            catch (MissingModException)
            {
                Console.WriteLine($"Mod {tomeESP} not found in load order.");
                mod = null;
            }
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
            ISkyrimModGetter? mod;
            try
            {
                mod = state.LoadOrder[scrollESP].Mod;
            }
            catch (MissingModException)
            {
                Console.WriteLine($"Mod {scrollESP} not found in load order.");
                mod = null;
            }
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
        private static IItemGetter? CheckArtifactFormID(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string artifactESP, string artifactFormID)
        {
            Console.WriteLine($"Resolving artifact ID {artifactFormID} from {artifactESP}");
            ISkyrimModGetter? mod = state.LoadOrder[artifactESP].Mod;
            if (mod == null) return null;
            if (artifactFormID.Length >= 8)
            {
                artifactFormID = artifactFormID[(artifactFormID.Length - 6)..].ToLower();
            }
            else
            {
                artifactFormID = Convert.ToInt32(artifactFormID).ToString("X6").ToLower();
            }
            List<IItemGetter> allItems = mod.AlchemicalApparatuses.Select(x => (IItemGetter)x).ToList();
            allItems.AddRange(mod.Ingredients.Select(x => (IItemGetter)x).ToList());
            allItems.AddRange(mod.MiscItems.Select(x => (IItemGetter)x).ToList());
            IItemGetter? artifact = allItems.FirstOrDefault(i => i.FormKey.ID.ToString("X6").ToLower() == artifactFormID);
            if (artifact == null)
            {
                if (mod.MasterReferences.Any())
                {
                    IMasterReferenceGetter? master = mod.MasterReferences[mod.MasterReferences.Count - 1];
                    Console.WriteLine($"Trying master {master.Master.FileName}");
                    return CheckArtifactFormID(state, master.Master.FileName, int.Parse(artifactFormID, System.Globalization.NumberStyles.HexNumber).ToString().PadLeft(6, '0'));
                }
                Console.WriteLine($"Couldn't resolve artifact ID {artifactFormID} from {artifactESP}");
                return null;
            }
            else
            {
                return artifact;
            }
        }

        private static IMagicEffectGetter? CheckMagicEffectFormID(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string effectESP, string effectFormID)
        {
            ISkyrimModGetter? mod;
            try
            {
                mod = state.LoadOrder[effectESP].Mod;
            }
            catch (MissingModException)
            {
                Console.WriteLine($"Mod {effectESP} not found in load order.");
                mod = null;
            }
            if (mod == null) return null;
            Console.WriteLine($"Resolving effect ID {effectFormID} from {effectESP}");
            if (effectFormID.Length >= 8)
            {
                effectFormID = effectFormID[(effectFormID.Length - 6)..].ToLower();
            }
            else
            {
                effectFormID = Convert.ToInt32(effectFormID).ToString("X6").ToLower();
            }
            IMagicEffectGetter? effect = mod.MagicEffects.FirstOrDefault(s => s.FormKey.ID.ToString("X6").ToLower() == effectFormID);
            if (effect == null)
            {
                if (mod.MasterReferences.Any())
                {
                    IMasterReferenceGetter? master = mod.MasterReferences[mod.MasterReferences.Count - 1];
                    Console.WriteLine($"Trying master {master.Master.FileName}");
                    return CheckMagicEffectFormID(state, master.Master.FileName, int.Parse(effectFormID, System.Globalization.NumberStyles.HexNumber).ToString().PadLeft(6, '0'));
                }
                Console.WriteLine($"Couldn't resolve effect ID {effectFormID} from {effectESP}");
                return null;
            }
            else
            {
                return effect;
            }
        }

        internal bool Validate(out ValidationResults validationResults)
        {
            validationResults = new();
            bool valid = true;
            foreach (KeyValuePair<string, ModSpellData> modRecord in Mods)
            {
                foreach (IGrouping<string, SpellInfo> g in modRecord.Value.NewSpells.GroupBy(s => $"{s.SpellESP}|0x{s.SpellFormID}").Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() > 1))
                {
                    valid = false;
                    validationResults.DuplicateSpells.Add(g.Key, g.Select(s => s.Name));
                }
                foreach (IGrouping<string?, SpellInfo> g in modRecord.Value.NewSpells.Where(s => s.TomeForm != null).GroupBy(s => $"{s.TomeESP}|0x{s.TomeFormID}").Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() > 1))
                {
                    valid = false;
                    validationResults.DuplicateTomes.Add(g.Key ?? "", g.Select(s => s.Name));
                }
                foreach (IGrouping<string?, SpellInfo> g in modRecord.Value.NewSpells.Where(s => s.ScrollForm != null).GroupBy(s => $"{s.ScrollESP}|0x{s.ScrollFormID}").Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() > 1))
                {
                    valid = false;
                    validationResults.DuplicateScrolls.Add(g.Key ?? "", g.Select(s => s.Name));
                }
            }
            return valid;
        }
    }
}
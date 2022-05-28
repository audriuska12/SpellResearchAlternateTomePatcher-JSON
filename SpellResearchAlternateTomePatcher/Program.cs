using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Threading.Tasks;

using System.IO;
using System.Text.RegularExpressions;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;

using Newtonsoft.Json.Linq;
using System.Diagnostics;
using SpellResearchAlternateTomePatcher.Classes;
using Newtonsoft.Json;

namespace SpellResearchAlternateTomePatcher
{

    public class Program
    {
        static Lazy<Settings> settings = null!;

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "alteredtomespatch.esp")
                .SetAutogeneratedSettings(
                    nickname: "Altered Tomes Patcher Settings",
                    path: "alteredtomesettings.json",
                    out settings)
                .Run(args);

        }


        private static IBookGetter? FixFormIDAndResolve(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ModKey mk, string fid)
        {

            var fkey = FixFormID(fid, mk);

            var bookLink = new FormLink<IBookGetter>(fkey);


            // if things are normal
            if (bookLink.TryResolve(state.LinkCache, out var bookRecord))
            {
                return bookRecord;
            }

            //if things are messed up 
            else
            {

                var mod = state.LoadOrder.TryGetValue(mk);
                var masters = mod?.Mod?.ModHeader.MasterReferences ?? new List<MasterReference>();

                foreach (var master in masters)
                {
                    var mfkey = FixFormID(fid, master.Master);
                    var mbookLink = new FormLink<IBookGetter>(mfkey);
                    if (mbookLink.TryResolve(state.LinkCache, out var masterBookRecord))
                    {
                        return masterBookRecord;
                    }
                }
            }

            Console.WriteLine("ERROR: Could not fix {0}", fkey);
            return null;
        }

        // fixes some things with formids in spellresearch scripts and returns a formkey
        private static FormKey FixFormID(string fid, ModKey mk)
        {
            string? fkeystr = fid + ":" + mk.FileName;

            // hex value and too many zeroes
            if (fid.Contains("0x", StringComparison.OrdinalIgnoreCase))
            {
                // first try to convert directly
                if (FormKey.TryFactory(fkeystr, out FormKey formKey))
                {
                    return formKey;
                }

                // handle ESL's
                if (fid[..2].Equals("FE"))
                {
                    fid = "00000" + fid.Substring(5, 3);
                }
                else
                {
                    // to make str processing easier
                    fid = fid.Replace("0x", "00").PadLeft(6, '0');

                    // hardcoded load order for some reason, (papyrus is fine with this apparently)
                    if (fid.Length == 8 && !fid[..2].Equals("00"))
                    {
                        fid = "00" + fid.Substring(2, 6);
                    }
                    // needs to be 8 digits for mutagen
                    if (fid.Length > 6)
                    {
                        fid = fid.Substring(fid.Length - 6, 6);
                    }
                }
                fkeystr = fid + ":" + mk.FileName;
                return FormKey.Factory(fkeystr);
            }
            // decimal value
            else
            {
                int num = Convert.ToInt32(fid, 10);
                string h = num.ToString("X");
                if (!h.Contains("0x", StringComparison.OrdinalIgnoreCase))
                {
                    h = "0x" + h;
                }
                return FixFormID(h, mk);

            }
        }

        // removes 'Spell Tome' elements from description name
        public static string FixName(IBookGetter book, Regex rnamefix)
        {
            var n = book?.Name?.ToString() ?? "";
            MatchCollection mnamefix = rnamefix.Matches(n);
            if (mnamefix.Count > 0)
            {
                n = mnamefix.First().Groups["tomename"].Value.Trim();
            }

            return n;
        }
        private static readonly string[] wovels = { "a", "e", "i", "o", "u" };

        // Creates a string description of a spell given its archetypes
        private static string ProcessText(SpellInfo spell, ArchetypeVisualInfo archetypemap, LevelSettings s)
        {
            string strbuilder = "";
            strbuilder += $"{(wovels.Contains(spell.Tier[0..1]) ? "An " : "A ")}{spell.Tier[0..1].ToUpper() + spell.Tier[1..]} spell of the ";
            if (s.useFontColor)
            {
                strbuilder += $"<font color='{(archetypemap.Archetypes[spell.School.ToLower()].Color ?? "#000000")}'>";
            }
            strbuilder += spell.School[0..1].ToUpper() + spell.School[1..];
            if (s.useFontColor)
            {
                strbuilder += "</font><font color='#000000'>";
            }
            strbuilder += " school, ";
            switch (spell.CastingType)
            {
                case "concentration":
                    {
                        strbuilder += "cast through steady concentration. ";
                        break;
                    }
                case "fireandforget":
                    {
                        strbuilder += "cast in a single moment. ";
                        break;
                    }
                default:
                    break;
            }

            foreach (AliasedArchetype target in spell.Targeting)
            {
                switch (target.Name.ToLower())
                {
                    case "actor":
                        {

                            strbuilder += "This spell is fired where aimed. ";
                            break;
                        }
                    case "area":
                        {
                            strbuilder += "This spell has an area of effect. ";
                            break;
                        }
                    case "location":
                        {
                            strbuilder += "This spell is cast in a specific location. ";
                            break;
                        }
                    case "self":
                        {
                            strbuilder += "This spell is cast on oneself. ";
                            break;
                        }
                    default: break;
                }
            }

            if (spell.Elements.Count > 0)
            {
                strbuilder += $"Channels the element{(spell.Elements.Count > 1 ? "s" : string.Empty)} of ";
                if (s.useFontColor)
                {
                    strbuilder += "</font>";
                }
                int idx = 0;
                foreach (AliasedArchetype e in spell.Elements)
                {
                    if (idx > 0 && idx == spell.Elements.Count - 1)
                        strbuilder += " and ";
                    else if (idx > 0)
                    {
                        strbuilder += ", ";
                    }
                    if (s.useFontColor)
                    {
                        strbuilder += $"<font color='{archetypemap.Archetypes[e.Name.ToLower()]?.Color ?? "#000000"}'>";
                    }
                    strbuilder += e.Name[0..1].ToUpper() + e.Name[1..];
                    if (s.useFontColor)
                    {
                        strbuilder += "</font>";
                    }
                    idx += 1;
                }
                strbuilder += ". ";
            }

            if (spell.Techniques.Count > 0)
            {
                if (spell.Elements.Count > 0 && s.useFontColor)
                {
                    strbuilder += "<font color='#000000'>";
                }
                strbuilder += $"Utilizes the technique{(spell.Techniques.Count > 1 ? s : string.Empty)} of ";
                if (s.useFontColor)
                {
                    strbuilder += "</font>";
                }
                int idx = 0;
                foreach (AliasedArchetype t in spell.Techniques)
                {
                    if (idx > 0 && idx == spell.Techniques.Count - 1)
                        strbuilder += " and ";
                    else if (idx > 0)
                    {
                        strbuilder += ", ";
                    }
                    if (s.useFontColor)
                    {
                        strbuilder += $"<font color='{archetypemap.Archetypes[t.Name.ToLower()]?.Color ?? "#000000"}'>";
                    }
                    strbuilder += t.Name[0..1].ToUpper() + t.Name[1..];
                    if (s.useFontColor)
                    {
                        strbuilder += "</font>";
                    }
                    idx += 1;
                }
                strbuilder += ".";
            }

            return strbuilder;
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            ArchetypeList allowedArchetypes = LoadArchetypes(state);
            ArchetypeVisualInfo archConfig = LoadArchetypeVisualInfo(state);
            List<string> processedMods = new();
            List<(string mod, string json)> mods = new();
            mods.AddRange(GetJsonHardlinkedMods());
            mods.AddRange(GetJsonDiscoveredMods(state).Except(mods));
            foreach ((string modName, string modPath) in mods)
            {
                Console.WriteLine($"Importing spells for {modName} from file {modPath}");
                if (processedMods.Contains(modName))
                {
                    Console.WriteLine($"Mod {modName} already imported, skipping");
                    continue;
                }
                if (!state.LoadOrder.ContainsKey(modName))
                {
                    Console.WriteLine($"Mod {modName} not found");
                    continue;
                }
                string jsonPath = Path.Combine(state.DataFolderPath, modPath);
                if (!File.Exists(jsonPath))
                {
                    Console.WriteLine($"JSON file {jsonPath} not found");
                    continue;
                }
                string spellconf = File.ReadAllText(jsonPath);
                SpellConfiguration spellInfo = SpellConfiguration.From(spellconf, allowedArchetypes);
                ProcessSpells(state, modName, spellInfo, archConfig);
                processedMods.Add(modName);
            }

            /*foreach (string modpsc in settings.Value.pscnames)
            {
            if (modpsc.Trim().Equals("")) { continue; }

                Console.WriteLine(modpsc);

                string modname = modpsc.Split(";")[0].Trim();
                string pscname = modpsc.Split(";")[1].Trim();

                var modKey = ModKey.FromFileName(modname);
                if (!state.LoadOrder.ContainsKey(modKey))
                {
                    Console.WriteLine("WARNING: Mod not Found: {0}", modname);
                    continue;
                }

                var scriptPath = Path.Combine(state.DataFolderPath, "scripts", "source", pscname);
                if (!File.Exists(scriptPath))
                {
                    Console.WriteLine("WARNING: Script not Found: {0}", scriptPath);
                    continue;

                }

                string[] lines = File.ReadAllLines(scriptPath);
                int spellcount = 0;
                Dictionary<string, dynamic> archetypemap = new Dictionary<string, dynamic>();
                Regex rx = new Regex("^.*\\(\\s*(?<fid>(0x)?[a-fA-F0-9]+),\\s\"(?<esp>.*\\.es[pml])\".*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                Regex rnamefix = new Regex("^.+\\s+(Tome)\\:?(?<tomename>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);


                Regex rskill = new Regex("^.*_SR_ListSpellsSkill(?<skill>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                Regex rcasting = new Regex("^.*_SR_ListSpellsCasting(?<casting>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                Regex rlevel = new Regex("^.*_SR_ListAllSpells[1-5](?<level>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                Regex rtarget = new Regex("^.*_SR_ListSpellsTarget(?<target>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                Regex rtechnique = new Regex("^.*_SR_ListSpellsTechnique(?<technique>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                Regex relement = new Regex("^.*_SR_ListSpellsElement(?<element>[A-Za-z]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                // parse the psc
                foreach (string line in lines)
                {
                    // start of spell in psc
                    if (line.Contains("TempSpell", StringComparison.OrdinalIgnoreCase) && line.Contains("GetFormFromFile", StringComparison.OrdinalIgnoreCase))
                    {
                        spellcount++;
                        archetypemap = new Dictionary<string, dynamic>();
                        archetypemap["skill"] = "";
                        archetypemap["casting"] = "";
                        archetypemap["level"] = "";
                        archetypemap["target"] = new List<string>();
                        archetypemap["technique"] = new List<string>();
                        archetypemap["element"] = new List<string>();
                    }
                    // ignore removeaddedform
                    else if (line.Contains("RemoveAddedForm", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    // end of spell
                    else if (line.Contains("TempTome", StringComparison.OrdinalIgnoreCase) && line.Contains("GetFormFromFile", StringComparison.OrdinalIgnoreCase) && spellcount >= 1)
                    {
                        MatchCollection matches = rx.Matches(line);
                        string fid = matches.First().Groups["fid"].Value.Trim();
                        string esp = matches.First().Groups["esp"].Value.Trim();

                        // get the modkey from the esp in the psc
                        var good_modkey = ModKey.TryFromFileName(esp, out modKey);
                        if (!good_modkey)
                        {
                            Console.WriteLine("ERROR: could not determine mod: {0} for {1}, skipping", esp, fid);
                            continue;
                        }
                        // get formkey for book and get text
                        //FormKey fkey = fixformid(fid, modKey);
                        //FormKey fkey = fixformidandresolve(state, modKey, fid);
                        IBookGetter? bookRecord = fixformidandresolve(state, modKey, fid);

                        //Console.WriteLine(fkey.ToString());
                        //var bookLink = new FormLink<IBookGetter>(fkey);

                        //if (bookLink.TryResolve(state.LinkCache, out var bookRecord))
                        if (bookRecord != null)
                        {

                            LevelSettings s;
                            if (archetypemap["level"].Equals("Novice")) { s = settings.Value.novice; }
                            else if (archetypemap["level"].Equals("Apprentice")) { s = settings.Value.apprentice; }
                            else if (archetypemap["level"].Equals("Adept")) { s = settings.Value.adept; }
                            else if (archetypemap["level"].Equals("Expert")) { s = settings.Value.Expert; }
                            else if (archetypemap["level"].Equals("Master")) { s = settings.Value.Master; }
                            else
                            {
                                s = new();
                            }
                            string desc = process_text(archetypemap, config, s).Trim();

                            var font = s.font;
                            //var font = config["Fonts"]?[archetypemap["level"]].ToString() ?? "$HandwrittenFont";
                            var name = fix_name(bookRecord, rnamefix);

                            if (font.Equals("$FalmerFont") || font.Equals("$DragonFont") || font.Equals("$MageScriptFont"))
                            {

                                char[] tagsep = new char[] { '<', '>', '#', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
                                char[] separators = new char[] { '!', '@', '$', '%', '^', '&', '*', '(', ')', '{', '}', '[', ']', '-', '_', '+', ':', '"', ';', ',', '.', '?', '~' };

                                desc = desc.ToUpper();
                                name = name.ToUpper();

                                string[] tempdesc = desc.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                                string[] tempname = name.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                                desc = String.Join("", tempdesc);
                                name = String.Join("", tempname);
                            }

                            string PREAMBLE = "<br><br><p align=\"center\"><font face='" + font + "'><font size='40'></font>";
                            string imgpath = "";
                            string img = "";

                            if (s.useImage)
                            {
                                if (archetypemap["element"].Count > 0)
                                {
                                    imgpath = config["Images"]?[archetypemap["element"][0]] ?? "";
                                }
                                else if (archetypemap["technique"].Count > 0)
                                {
                                    imgpath = config["Images"]?[archetypemap["technique"][0]] ?? "";
                                }

                                if (!imgpath.Equals(""))
                                {
                                    img = "<br><br><img src='img://" + imgpath + "' height='296' width='296'>";
                                }
                            }
                            string PAGE = "<br><br></p>[pagebreak]<br><br><p align=\"left\"><font face='" + font + "'><font size='40'></font>";
                            string POST = "</font></p>";

                            var btext = PREAMBLE + name + img + PAGE + desc + POST;
                            btext = Regex.Replace(btext, @"FONT\s*(COLOR)*", m => m.Value.ToLower());
                            Console.WriteLine("DESC: {0}", btext);

                            var bookOverride = state.PatchMod.Books.GetOrAddAsOverride(bookRecord);
                            bookOverride.Teaches = new BookTeachesNothing();
                            bookOverride.BookText = btext;

                        }
                        else
                        {
                            Console.WriteLine("ERROR: Could Not Resolve {0}", fid);
                        }

                    }
                    // spellcount >= 1 to ignore all the spellresearch import stuff
                    else if (spellcount >= 1)
                    {
                        MatchCollection mskill = rskill.Matches(line);
                        MatchCollection mcasting = rcasting.Matches(line);
                        MatchCollection mlevel = rlevel.Matches(line);
                        MatchCollection mtarget = rtarget.Matches(line);
                        MatchCollection mtechnique = rtechnique.Matches(line);
                        MatchCollection melement = relement.Matches(line);
                        if (mskill.Count > 0)
                        {
                            archetypemap["skill"] = mskill.First().Groups["skill"].Value.Trim();
                        }
                        else if (mcasting.Count > 0)
                        {
                            archetypemap["casting"] = mcasting.First().Groups["casting"].Value.Trim();
                        }
                        else if (mlevel.Count > 0)
                        {
                            archetypemap["level"] = mlevel.First().Groups["level"].Value.Trim();
                        }
                        else if (mtarget.Count > 0)
                        {
                            archetypemap["target"].Add(mtarget.First().Groups["target"].Value.Trim());
                        }
                        else if (mtechnique.Count > 0)
                        {
                            archetypemap["technique"].Add(mtechnique.First().Groups["technique"].Value.Trim());
                        }
                        else if (melement.Count > 0)
                        {
                            archetypemap["element"].Add(melement.First().Groups["element"].Value.Trim());
                        }

                    }

                }

            }
            */
        }
        private static ArchetypeList LoadArchetypes(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            string archetypeListPath = Path.Combine(state.ExtraSettingsDataPath, "archetypes.json");
            if (!File.Exists(archetypeListPath)) throw new ArgumentException($"Archetype list information missing! {archetypeListPath}");
            ArchetypeList? allowedArchetypes = JsonConvert.DeserializeObject<ArchetypeList>(File.ReadAllText(archetypeListPath));
            if (allowedArchetypes == null) throw new ArgumentException($"Error reading archetype list");
            return allowedArchetypes;
        }

        private static ArchetypeVisualInfo LoadArchetypeVisualInfo(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            string extraSettingsPath = Path.Combine(state.ExtraSettingsDataPath, "config.json");
            if (!File.Exists(extraSettingsPath)) throw new ArgumentException($"Archetype display settings missing! {extraSettingsPath}");
            string configText = File.ReadAllText(extraSettingsPath);
            ArchetypeVisualInfo archconfig = ArchetypeVisualInfo.From(configText);
            return archconfig;
        }

        private static List<(string mod, string json)> GetJsonHardlinkedMods()
        {
            return settings.Value.jsonNames.Select(x => (x.Split(";")[0].Trim(), x.Split(";")[1].Trim())).ToList();
        }

        private static List<(string mod, string json)> GetJsonDiscoveredMods(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            List<(string mod, string json)> mods = new();
            foreach (string dir in settings.Value.jsonPaths)
            {
                try
                {
                    DirectoryInfo searchDir = new(Path.Combine(state.DataFolderPath, dir));
                    foreach (FileInfo file in searchDir.GetFiles())
                    {
                        if (file.Extension == ".json")
                        {
                            string pluginName = file.Name[..file.Name.LastIndexOf(file.Extension)];
                            ModKey? mod = state.LoadOrder.FirstOrDefault(plugin => plugin.Key.Name == pluginName)?.Key;
                            if (mod == null)
                            {
                                Console.WriteLine($"Found JSON file {file.Name}, but no matching plugin");
                                continue;
                            }
                            Console.WriteLine($"Found JSON file {file.Name}");
                            mods.Add(new(mod.Value.FileName, Path.Combine(dir, file.Name)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return mods;
        }

        private static void ProcessSpells(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string modName, SpellConfiguration spellInfo, ArchetypeVisualInfo archConfig)
        {
            if (spellInfo == null)
            {
                Console.WriteLine("Failed to read JSON");
                return;
            }
            if (spellInfo.Spells.Count == 0)
            {
                Console.WriteLine("No spells found");
                return;
            }
            foreach (SpellInfo spell in spellInfo.Spells)
            {
                if (string.IsNullOrEmpty(spell.TomeESP)) continue;
                bool good_modkey = ModKey.TryFromFileName(spell.TomeESP, out ModKey modKey);
                if (!good_modkey)
                {
                    Console.WriteLine($"Could not determine ESP key {spell.SpellESP} for {modName}");
                    continue;
                }
                if (spell.TomeFormID != null)
                {
                    IBookGetter? bookRecord = FixFormIDAndResolve(state, modKey, spell.TomeFormID);
                    if (bookRecord != null)
                    {
                        LevelSettings s;
                        switch (spell.Tier.ToLower())
                        {
                            case "novice":
                                {
                                    s = settings.Value.Novice;
                                    break;
                                }
                            case "apprentice":
                                {
                                    s = settings.Value.Apprentice;
                                    break;
                                }
                            case "adept":
                                {
                                    s = settings.Value.Adept;
                                    break;
                                }
                            case "expert":
                                {
                                    s = settings.Value.Expert;
                                    break;
                                }
                            case "master":
                                {
                                    s = settings.Value.Master;
                                    break;
                                }
                            default:
                                {
                                    s = new();
                                    break;
                                }
                        }
                        if (!s.process)
                        {
                            continue;
                        }
                        string desc = ProcessText(spell, archConfig, s).Trim();

                        string? font = s.font;
                        Regex rnamefix = new("^.+\\s+(Tome)\\:?(?<tomename>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                        //var font = config["Fonts"]?[archetypemap["level"]].ToString() ?? "$HandwrittenFont";
                        string? name = FixName(bookRecord, rnamefix);

                        if (font.Equals("$FalmerFont") || font.Equals("$DragonFont") || font.Equals("$MageScriptFont"))
                        {

                            char[] tagsep = new char[] { '<', '>', '#', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
                            char[] separators = new char[] { '!', '@', '$', '%', '^', '&', '*', '(', ')', '{', '}', '[', ']', '-', '_', '+', ':', '"', ';', ',', '.', '?', '~' };

                            desc = desc.ToUpper();
                            name = name.ToUpper();

                            string[] tempdesc = desc.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                            string[] tempname = name.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                            desc = string.Join("", tempdesc);
                            name = string.Join("", tempname);
                        }

                        string PREAMBLE = $"<br><br><p align=\"center\"><font face='{font}'><font size='40'></font>";
                        string imgpath = "";
                        string img = "";

                        if (s.useImage)
                        {
                            if (spell.Elements.Count > 0)
                            {
                                imgpath = archConfig.Archetypes[spell.Elements[0].Name.ToLower()].Image ?? "";
                            }
                            else if (spell.Techniques.Count > 0)
                            {
                                imgpath = archConfig.Archetypes[spell.Techniques[0].Name.ToLower()].Image ?? "";
                            }

                            if (!imgpath.Equals(""))
                            {
                                img = $"<br><br><img src='img://{imgpath}' height='296' width='296'>";
                            }
                        }
                        string PAGE = $"<br><br></p>[pagebreak]<br><br><p align=\"left\"><font face='{font}'><font size='40'></font>";
                        string POST = "</font></p>";

                        string? btext = PREAMBLE + name + img + PAGE + desc + POST;
                        btext = Regex.Replace(btext, @"FONT\s*(COLOR)*", m => m.Value.ToLower());
                        Console.WriteLine("DESC: {0}", btext);

                        Book? bookOverride = state.PatchMod.Books.GetOrAddAsOverride(bookRecord);
                        bookOverride.Teaches = new BookTeachesNothing();
                        bookOverride.BookText = btext;
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Could Not Resolve {0}", spell.TomeFormID);
                    }
                }
            }
        }

    }

}

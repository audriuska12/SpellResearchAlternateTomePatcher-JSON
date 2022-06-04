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
using SpellResearchSynthesizer.Classes;
using Newtonsoft.Json;

namespace SpellResearchSynthesizer
{

    public class Program
    {
        static Lazy<Settings> settings = null!;

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "SpellResearchSynthesizer.esp")
                .SetAutogeneratedSettings(
                    nickname: "Spell Research Synthesizer Settings",
                    path: "synthesizer.json",
                    out settings)
                .Run(args);

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
                strbuilder += $"Utilizes the technique{(spell.Techniques.Count > 1 ? 's' : string.Empty)} of ";
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
            string extraSettingsPath = Path.Combine(state.ExtraSettingsDataPath, "config.json");
            if (!File.Exists(extraSettingsPath)) throw new ArgumentException($"Archetype display settings missing! {extraSettingsPath}");
            string configText = File.ReadAllText(extraSettingsPath);
            ArchetypeVisualInfo archConfig = LoadArchetypeVisualInfo(configText);
            JToken? researchDataLists = LoadResearchDataLists(configText);
            if (researchDataLists == null)
            {
                Console.WriteLine("Error reading data lists");
                return;
            }
            Dictionary<string, string> mods = new();
            foreach ((string mod, string file) in GetJsonHardlinkedMods())
            {
                if (!mods.ContainsKey(mod))
                {
                    mods[mod] = file;
                }
                else
                {
                    Console.WriteLine($"Duplicate detected: {file} for {mod}");
                }
            }
            foreach ((string mod, string file) in GetJsonDiscoveredMods(state))
            {
                if (!mods.ContainsKey(mod))
                {
                    mods[mod] = file;
                }
                else
                {
                    Console.WriteLine($"Duplicate detected: {file} for {mod}");
                }
            }
            foreach ((string mod, string file) in GetPscMods())
            {
                if (!mods.ContainsKey(mod))
                {
                    mods[mod] = file;
                }
                else
                {
                    Console.WriteLine($"Duplicate detected: {file} for {mod}");
                }
            }
            List<(string mod, SpellConfiguration spells)> output = new();
            foreach (Noggog.IKeyValue<IModListing<ISkyrimModGetter>, ModKey>? mod in state.LoadOrder)
            {
                string? scriptFile = mods.GetValueOrDefault(mod.Key.FileName);
                if (string.IsNullOrEmpty(scriptFile))
                {
                    continue;
                }
                Console.WriteLine($"Importing {mod} from {scriptFile}");
                if (scriptFile.EndsWith(".json"))
                {
                    string jsonPath = Path.Combine(state.DataFolderPath, scriptFile);
                    if (!File.Exists(jsonPath))
                    {
                        Console.WriteLine($"JSON file {jsonPath} not found");
                        continue;
                    }
                    string spellconf = File.ReadAllText(jsonPath);
                    output.Add((mod.Key.FileName, SpellConfiguration.FromJson(state, spellconf, allowedArchetypes)));
                }
                else if (scriptFile.EndsWith(".psc"))
                {
                    string pscPath = Path.Combine(state.DataFolderPath, scriptFile);
                    if (!File.Exists(pscPath))
                    {
                        Console.WriteLine($"PSC file {pscPath} not found");
                        continue;
                    }
                    string spellconf = File.ReadAllText(pscPath);
                    output.Add((mod.Key.FileName, SpellConfiguration.FromPsc(state, spellconf, allowedArchetypes)));
                }
            }
            SpellConfiguration cleanedOutput = CleanOutput(output);
            OutputTemplate jsonOutput = new()
            {
                ResearchDataLists = researchDataLists,
                NewSpells = cleanedOutput.Mods.SelectMany(mod => mod.Value.NewSpells).ToList(),
                RemovedSpells = cleanedOutput.Mods.SelectMany(mod => mod.Value.RemovedSpells).ToList()
            };
            string path = state.DataFolderPath + @"\SKSE\Plugins\SpellResearchSynthesizer";
            Directory.CreateDirectory(path);
            File.WriteAllText(path + @"\SynthesizedPatch.json", JsonConvert.SerializeObject(jsonOutput, Formatting.Indented));
            ProcessSpells(state, cleanedOutput, archConfig);
        }

        private static ArchetypeList LoadArchetypes(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            string archetypeListPath = Path.Combine(state.ExtraSettingsDataPath, "archetypes.json");
            if (!File.Exists(archetypeListPath)) throw new ArgumentException($"Archetype list information missing! {archetypeListPath}");
            ArchetypeList? allowedArchetypes = JsonConvert.DeserializeObject<ArchetypeList>(File.ReadAllText(archetypeListPath));
            if (allowedArchetypes == null) throw new ArgumentException($"Error reading archetype list");
            return allowedArchetypes;
        }

        private static ArchetypeVisualInfo LoadArchetypeVisualInfo(string configText)
        {
            ArchetypeVisualInfo archconfig = ArchetypeVisualInfo.From(configText);
            return archconfig;
        }

        private static JToken? LoadResearchDataLists(string configText)
        {
            return JObject.Parse(configText)["researchDataLists"];
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
                            ModKey? mod = state.LoadOrder.FirstOrDefault(plugin => plugin.Key.Name.ToLower() == pluginName.ToLower())?.Key;
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

        private static List<(string mod, string psc)> GetPscMods()
        {
            return settings.Value.pscnames.Select(x => (x.Split(";")[0].Trim(), x.Split(";")[1].Trim())).ToList();
        }
        private static SpellConfiguration CleanOutput(List<(string mod, SpellConfiguration spells)> output)
        {
            SpellConfiguration result = new();
            foreach ((_, SpellConfiguration spellConfiguration) in output)
            {
                foreach ((string mod, (List<SpellInfo> NewSpells, List<SpellInfo> RemovedSpells) spells) in spellConfiguration.Mods)
                {
                    foreach (SpellInfo spell in spells.NewSpells)
                    {
                        if (!result.Mods.ContainsKey(spell.SpellESP))
                        {
                            result.Mods.Add(spell.SpellESP, (new List<SpellInfo>(), new List<SpellInfo>()));
                        }
                        SpellInfo? oldEntry = result.Mods[spell.SpellESP].NewSpells.FirstOrDefault(x => x.SpellID.ToLower() == spell.SpellID.ToLower());
                        if (oldEntry != null)
                        {
                            result.Mods[spell.SpellESP].NewSpells.Remove(oldEntry);
                            result.Mods[spell.SpellESP].RemovedSpells.Add(oldEntry);
                        }
                        result.Mods[spell.SpellESP].NewSpells.Add(spell);
                    }
                    foreach (SpellInfo spell in spells.RemovedSpells)
                    {
                        result.Mods[spell.SpellESP].RemovedSpells.Add(spell);
                    }
                }
            }

            return result;
        }

        private static void ProcessSpells(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, SpellConfiguration spellInfo, ArchetypeVisualInfo archConfig)
        {
            if (spellInfo == null)
            {
                Console.WriteLine("Failed to read file");
                return;
            }
            if (!spellInfo.Mods.SelectMany(mod => mod.Value.NewSpells).Any())
            {
                Console.WriteLine("No spells found");
                return;
            }
            foreach ((string modName, (List<SpellInfo> spells, _)) in spellInfo.Mods)
            {
                foreach (SpellInfo spell in spells)
                {
                    if (spell.TomeForm == null) continue;
                    IBookGetter? bookRecord = spell.TomeForm;
                    if (bookRecord == null)
                    {
                        Console.WriteLine("ERROR: Could Not Resolve {0}", spell.TomeForm);
                        continue;
                    }
                    else
                    {
                        Console.WriteLine(bookRecord);
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
                        string? name = bookRecord.Name?.String ?? $"Spell Tome: {spell.Name}";

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
                }
            }
        }

    }

}

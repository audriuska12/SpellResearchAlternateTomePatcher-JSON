using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellResearchSynthesizer.Classes
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ResearchDataLists
    {
        private static ResearchDataLists? _Instance = null;
        public static ResearchDataLists Instance
        {
            get
            {
                _Instance ??= new ResearchDataLists();
                return _Instance;
            }
        }

        private static readonly Dictionary<string, Dictionary<string, string>> _SpellTiers = new() {
            { "novice", new(){
                    {"allSpells", "__formData|SpellResearch.esp|0x00133E" },
                    { "tomeSpells", "__formData|SpellResearch.esp|0x00134A" },
                    { "scrollSpells", "__formData|SpellResearch.esp|0x0051F8" },
                    { "tomes", "__formData|SpellResearch.esp|0x001344" },
                    { "scrolls", "__formData|SpellResearch.esp|0x001350" },
                }
            },

            { "apprentice", new(){
                    { "allSpells", "__formData|SpellResearch.esp|0x00133F" },
                    { "tomeSpells", "__formData|SpellResearch.esp|0x00134B" },
                    { "scrollSpells", "__formData|SpellResearch.esp|0x0051F9" },
                    { "tomes", "__formData|SpellResearch.esp|0x001345" },
                    { "scrolls", "__formData|SpellResearch.esp|0x001351" }
                }
            },

            { "adept", new(){
                    { "allSpells",  "__formData|SpellResearch.esp|0x001340" },
                    {"tomeSpells",  "__formData|SpellResearch.esp|0x00134C" },
                    {"scrollSpells", "__formData|SpellResearch.esp|0x0051FA" },
                    {"tomes", "__formData|SpellResearch.esp|0x001346" },
                    {"scrolls", "__formData|SpellResearch.esp|0x001352" }
                }
            },

            { "expert", new(){
                    { "allSpells", "__formData|SpellResearch.esp|0x001341" },
                    { "tomeSpells", "__formData|SpellResearch.esp|0x00134D" },
                    { "scrollSpells", "__formData|SpellResearch.esp|0x0051FB" },
                    { "tomes", "__formData|SpellResearch.esp|0x001347" },
                    { "scrolls", "__formData|SpellResearch.esp|0x001353" }
                }
            },

            { "master", new(){
                    { "allSpells", "__formData|SpellResearch.esp|0x001342" },
                    {"tomeSpells", "__formData|SpellResearch.esp|0x00134E" },
                    {"scrollSpells","__formData|SpellResearch.esp|0x0051FC" },
                    { "tomes", "__formData|SpellResearch.esp|0x001348" },
                    {"scrolls", "__formData|SpellResearch.esp|0x001354" }
                }
            },
        };
        [JsonProperty("spellTiers")]
        public Dictionary<string, Dictionary<string, string>> SpellTiers { get; } = _SpellTiers;

        private static readonly Dictionary<string, string> _SpellSchools = new(){
            { "alteration", "__formData|SpellResearch.esp|0x000D62" },
            { "conjuration", "__formData|SpellResearch.esp|0x000D63" },
            { "destruction", "__formData|SpellResearch.esp|0x000D64" },
            { "illusion", "__formData|SpellResearch.esp|0x000D65" },
            { "restoration", "__formData|SpellResearch.esp|0x000D66" },
        };
        [JsonProperty("spellSchools")]
        public Dictionary<string, string> SpellSchools { get; } = _SpellSchools;

        private static readonly Dictionary<string, string> _SpellCastingTypes = new()
        {
            { "concentration", "__formData|SpellResearch.esp|0x000D67" },
            { "fireandforget", "__formData|SpellResearch.esp|0x000D68" },
        };

        [JsonProperty("spellCastingTypes")]
        public Dictionary<string, string> SpellCastingTypes { get; } = _SpellCastingTypes;

        private static readonly Dictionary<string, string> _SpellTargetingTypes = new() {
            { "actor", "__formData|SpellResearch.esp|0x000D69" },
            { "area", "__formData|SpellResearch.esp|0x000D6A" },
            { "location", "__formData|SpellResearch.esp|0x000D6B" },
            { "self", "__formData|SpellResearch.esp|0x000D6C" },
        };
        [JsonProperty("spellTargeting")]
        public Dictionary<string, string> SpellTargetingTypes { get; } = _SpellTargetingTypes;
        private static readonly Dictionary<string, string> _SpellElements = new()
        {
            { "acid", "__formData|SpellResearch.esp|0x000D7C" },
            { "air", "__formData|SpellResearch.esp|0x000D7D" },
            { "apparition", "__formData|SpellResearch.esp|0x000D7E" },
            { "arcane", "__formData|SpellResearch.esp|0x000D7F" },
            { "armor", "__formData|SpellResearch.esp|0x000D80" },
            { "construct", "__formData|SpellResearch.esp|0x000D81" },
            { "creature", "__formData|SpellResearch.esp|0x000D82" },
            { "daedra", "__formData|SpellResearch.esp|0x000D83" },
            { "disease", "__formData|SpellResearch.esp|0x000D84" },
            { "earth", "__formData|SpellResearch.esp|0x000D85" },
            { "fire", "__formData|SpellResearch.esp|0x000D86" },
            { "flesh", "__formData|SpellResearch.esp|0x000D87" },
            { "force", "__formData|SpellResearch.esp|0x000D88" },
            { "frost", "__formData|SpellResearch.esp|0x000D89" },
            { "health", "__formData|SpellResearch.esp|0x000D8A" },
            { "human", "__formData|SpellResearch.esp|0x000D8B" },
            { "life", "__formData|SpellResearch.esp|0x000D8C" },
            { "light", "__formData|SpellResearch.esp|0x000D8D" },
            { "magicka", "__formData|SpellResearch.esp|0x000D8E" },
            { "metal", "__formData|SpellResearch.esp|0x000D8F" },
            { "nature", "__formData|SpellResearch.esp|0x000D90" },
            { "poison", "__formData|SpellResearch.esp|0x000D91" },
            { "resistance", "__formData|SpellResearch.esp|0x000D92" },
            { "shadow", "__formData|SpellResearch.esp|0x000D93" },
            { "shield", "__formData|SpellResearch.esp|0x000D94" },
            { "shock", "__formData|SpellResearch.esp|0x000D95" },
            { "soul", "__formData|SpellResearch.esp|0x000D96" },
            { "stamina", "__formData|SpellResearch.esp|0x000D97" },
            { "sun", "__formData|SpellResearch.esp|0x000D98" },
            { "time", "__formData|SpellResearch.esp|0x000D99" },
            { "trap", "__formData|SpellResearch.esp|0x000D9A" },
            { "undead", "__formData|SpellResearch.esp|0x000D9B" },
            { "water", "__formData|SpellResearch.esp|0x000D9C" },
            { "weapon", "__formData|SpellResearch.esp|0x000D9D" },
        };

        [JsonProperty("spellElements")]
        public Dictionary<string, string> SpellElements { get; } = _SpellElements;

        private static readonly Dictionary<string, string> _SpellTechniques = new()
        {
            { "cloak", "__formData|SpellResearch.esp|0x000D6D" },
            { "control", "__formData|SpellResearch.esp|0x000D6E" },
            { "courage", "__formData|SpellResearch.esp|0x000D6F" },
            { "curing", "__formData|SpellResearch.esp|0x000D70" },
            { "curse", "__formData|SpellResearch.esp|0x000D71" },
            { "fear", "__formData|SpellResearch.esp|0x000D72" },
            { "frenzy", "__formData|SpellResearch.esp|0x000D73" },
            { "infuse", "__formData|SpellResearch.esp|0x000D74" },
            { "pacify", "__formData|SpellResearch.esp|0x000D75" },
            { "sense", "__formData|SpellResearch.esp|0x000D76" },
            { "siphon", "__formData|SpellResearch.esp|0x000D77" },
            { "strengthen", "__formData|SpellResearch.esp|0x000D78" },
            { "summoning", "__formData|SpellResearch.esp|0x000D79" },
            { "telekinesis", "__formData|SpellResearch.esp|0x000D7A" },
            { "transform", "__formData|SpellResearch.esp|0x000D7B" }
        };

        [JsonProperty("spellTechniques")]
        public Dictionary<string, string> SpellTechniques { get; } = _SpellTechniques;

        private static readonly Dictionary<string, string> _Other = new()
        {
            { "undiscoverable", "__formData|SpellResearch.esp|0x05ADF0" }
        };

        [JsonProperty("other")]
        public Dictionary<string, string> Other { get; } = _Other;

        private static readonly Dictionary<string, string> _ArtifactTiers = new()
        {
            { "0", "__formData|SpellResearch.esp|0x00BB23" },
            { "1", "__formData|SpellResearch.esp|0x00BB24" },
            { "2", "__formData|SpellResearch.esp|0x00BB25" },
            { "3", "__formData|SpellResearch.esp|0x00BB26" },
            { "4", "__formData|SpellResearch.esp|0x00BB27" },
            { "5", "__formData|SpellResearch.esp|0x00BB28" },
        };

        [JsonProperty("artifactTiers")]
        public Dictionary<string, string> ArtifactTiers { get; } = _ArtifactTiers;

        private static readonly Dictionary<string, string> _ArtifactSchools = new()
        {
            { "concentration", "__formData|SpellResearch.esp|0x00BB2A" },
            { "fireandforget", "__formData|SpellResearch.esp|0x00BB2B" },
        };

        [JsonProperty("artifactSchools")]
        public Dictionary<string, string> ArtifactSchools { get; } = _ArtifactSchools;

        private static readonly Dictionary<string, string> _ArtifactCastingTypes = new()
        {
            { "alteration", "__formData|SpellResearch.esp|0x00BB4E" },
            { "conjuration", "__formData|SpellResearch.esp|0x00BB4F" },
            { "destruction", "__formData|SpellResearch.esp|0x00BB50" },
            { "illusion", "__formData|SpellResearch.esp|0x00BB51" },
            { "restoration", "__formData|SpellResearch.esp|0x00BB52" },
        };

        [JsonProperty("artifactCastingTypes")]
        public Dictionary<string, string> ArtifactCastingTypes { get; } = _ArtifactCastingTypes;

        private static readonly Dictionary<string, string> _ArtifactTargetingTypes = new()
        {
            { "actor", "__formData|SpellResearch.esp|0x00BB53" },
            { "area", "__formData|SpellResearch.esp|0x00BB54" },
            { "location", "__formData|SpellResearch.esp|0x00BB55" },
            { "self", "__formData|SpellResearch.esp|0x00BB56" },
        };

        [JsonProperty("artifactTargeting")]
        public Dictionary<string, string> ArtifactTargetingTypes { get; } = _ArtifactTargetingTypes;

        private static readonly Dictionary<string, string> _ArtifactElements = new()
        {
            { "acid", "__formData|SpellResearch.esp|0x00BB2C" },
            { "air", "__formData|SpellResearch.esp|0x00BB2D" },
            { "apparition", "__formData|SpellResearch.esp|0x00BB2E" },
            { "arcane", "__formData|SpellResearch.esp|0x00BB2F" },
            { "armor", "__formData|SpellResearch.esp|0x00BB30" },
            { "construct", "__formData|SpellResearch.esp|0x00BB31" },
            { "creature", "__formData|SpellResearch.esp|0x00BB32" },
            { "daedra", "__formData|SpellResearch.esp|0x00BB33" },
            { "disease", "__formData|SpellResearch.esp|0x00BB34" },
            { "earth", "__formData|SpellResearch.esp|0x00BB35" },
            { "fire", "__formData|SpellResearch.esp|0x00BB36" },
            { "flesh", "__formData|SpellResearch.esp|0x00BB37" },
            { "force", "__formData|SpellResearch.esp|0x00BB38" },
            { "frost", "__formData|SpellResearch.esp|0x00BB39" },
            { "health", "__formData|SpellResearch.esp|0x00BB3A" },
            { "human", "__formData|SpellResearch.esp|0x00BB3B" },
            { "life", "__formData|SpellResearch.esp|0x00BB3C" },
            { "light", "__formData|SpellResearch.esp|0x00BB3D" },
            { "magicka", "__formData|SpellResearch.esp|0x00BB3E" },
            { "metal", "__formData|SpellResearch.esp|0x00BB3F" },
            { "nature", "__formData|SpellResearch.esp|0x00BB40" },
            { "poison", "__formData|SpellResearch.esp|0x00BB41" },
            { "resistance", "__formData|SpellResearch.esp|0x00BB42" },
            { "shadow", "__formData|SpellResearch.esp|0x00BB43" },
            { "shield", "__formData|SpellResearch.esp|0x00BB44" },
            { "shock", "__formData|SpellResearch.esp|0x00BB45" },
            { "soul", "__formData|SpellResearch.esp|0x00BB46" },
            { "stamina", "__formData|SpellResearch.esp|0x00BB47" },
            { "sun", "__formData|SpellResearch.esp|0x00BB48" },
            { "time", "__formData|SpellResearch.esp|0x00BB49" },
            { "trap", "__formData|SpellResearch.esp|0x00BB4A" },
            { "undead", "__formData|SpellResearch.esp|0x00BB4B" },
            { "water", "__formData|SpellResearch.esp|0x00BB4C" },
            { "weapon", "__formData|SpellResearch.esp|0x00BB4D" },
        };

        [JsonProperty("artifactElements")]
        public Dictionary<string, string> ArtifactElements { get; } = _ArtifactElements;

        private static readonly Dictionary<string, string> _ArtifactTechniques = new()
        {
            { "cloak", "__formData|SpellResearch.esp|0x00BB57" },
            { "control", "__formData|SpellResearch.esp|0x00BB58" },
            { "courage", "__formData|SpellResearch.esp|0x00BB59" },
            { "curing", "__formData|SpellResearch.esp|0x00BB5A" },
            { "curse", "__formData|SpellResearch.esp|0x00BB5B" },
            { "fear", "__formData|SpellResearch.esp|0x00BB5C" },
            { "frenzy", "__formData|SpellResearch.esp|0x00BB5D" },
            { "infuse", "__formData|SpellResearch.esp|0x00BB5E" },
            { "pacify", "__formData|SpellResearch.esp|0x00BB5F" },
            { "sense", "__formData|SpellResearch.esp|0x00BB60" },
            { "siphon", "__formData|SpellResearch.esp|0x00BB61" },
            { "strengthen", "__formData|SpellResearch.esp|0x00BB62" },
            { "summoning", "__formData|SpellResearch.esp|0x00BB63" },
            { "telekinesis", "__formData|SpellResearch.esp|0x00BB64" },
            { "transform", "__formData|SpellResearch.esp|0x00BB65" },
        };

        [JsonProperty("artifactTechniques")]
        public Dictionary<string, string> ArtifactTechniques { get; } = _ArtifactTechniques;

        private static readonly Dictionary<string, string> _ArtifactOther = new()
        {
            { "equippableAll", "__formData|SpellResearch.esp|0x03BAA3" },
            { "equippableArtifacts", "__formData|SpellResearch.esp|0x03C570" },
            { "equippableTexts", "__formData|SpellResearch.esp|0x03C571" }
        };

        [JsonProperty("artifactOther")]
        public Dictionary<string, string> ArtifactOther { get; } = _ArtifactOther;

        private static readonly Dictionary<string, string> _AlchemyElements = new()
        {
            { "acid", "__formData|SpellResearch.esp|0x041BFA" },
            { "air", "__formData|SpellResearch.esp|0x041C32" },
            { "apparition", "__formData|SpellResearch.esp|0x041BFB" },
            { "arcane", "__formData|SpellResearch.esp|0x041BFC" },
            { "armor", "__formData|SpellResearch.esp|0x041BFD" },
            { "construct", "__formData|SpellResearch.esp|0x041BFE" },
            { "creature", "__formData|SpellResearch.esp|0x041BFF" },
            { "daedra", "__formData|SpellResearch.esp|0x041C00" },
            { "disease", "__formData|SpellResearch.esp|0x041C01" },
            { "earth", "__formData|SpellResearch.esp|0x041C02" },
            { "fire", "__formData|SpellResearch.esp|0x041C03" },
            { "flesh", "__formData|SpellResearch.esp|0x041C04" },
            { "force", "__formData|SpellResearch.esp|0x041C05" },
            { "frost", "__formData|SpellResearch.esp|0x041C06" },
            { "health", "__formData|SpellResearch.esp|0x041C07" },
            { "human", "__formData|SpellResearch.esp|0x041C08" },
            { "life", "__formData|SpellResearch.esp|0x041C09" },
            { "light", "__formData|SpellResearch.esp|0x041C0A" },
            { "magicka", "__formData|SpellResearch.esp|0x041C0B" },
            { "metal", "__formData|SpellResearch.esp|0x041C0C" },
            { "nature", "__formData|SpellResearch.esp|0x041C0D" },
            { "poison", "__formData|SpellResearch.esp|0x041C0E" },
            { "resistance", "__formData|SpellResearch.esp|0x041C0F" },
            { "shadow", "__formData|SpellResearch.esp|0x041C10" },
            { "shield", "__formData|SpellResearch.esp|0x041C11" },
            { "shock", "__formData|SpellResearch.esp|0x041C12" },
            { "soul", "__formData|SpellResearch.esp|0x041C13" },
            { "stamina", "__formData|SpellResearch.esp|0x041C14" },
            { "sun", "__formData|SpellResearch.esp|0x041C15" },
            { "time", "__formData|SpellResearch.esp|0x041C16" },
            { "trap", "__formData|SpellResearch.esp|0x041C17" },
            { "undead", "__formData|SpellResearch.esp|0x041C18" },
            { "water", "__formData|SpellResearch.esp|0x041C19" },
            { "weapon", "__formData|SpellResearch.esp|0x041C1A" },
        };

        [JsonProperty("alchElements")]
        public Dictionary<string, string> AlchemyElements { get; } = _AlchemyElements;

        private static readonly Dictionary<string, string> _AlchemyTechniques = new()
        {
            { "cloak", "__formData|SpellResearch.esp|0x041C24" },
            { "control", "__formData|SpellResearch.esp|0x041C25" },
            { "courage", "__formData|SpellResearch.esp|0x041C26" },
            { "curing", "__formData|SpellResearch.esp|0x041C27" },
            { "curse", "__formData|SpellResearch.esp|0x041C28" },
            { "fear", "__formData|SpellResearch.esp|0x041C29" },
            { "frenzy", "__formData|SpellResearch.esp|0x041C2A" },
            { "infuse", "__formData|SpellResearch.esp|0x041C2B" },
            { "pacify", "__formData|SpellResearch.esp|0x041C2C" },
            { "sense", "__formData|SpellResearch.esp|0x041C2D" },
            { "siphon", "__formData|SpellResearch.esp|0x041C2E" },
            { "strengthen", "__formData|SpellResearch.esp|0x041C2F" },
            { "summoning", "__formData|SpellResearch.esp|0x041C30" },
            { "telekinesis", "__formData|SpellResearch.esp|0x041C31" },
            { "transform", "__formData|SpellResearch.esp|0x041BF7" },
        };

        [JsonProperty("alchTechniques")]
        public Dictionary<string, string> AlchemyTechniques { get; } = _AlchemyTechniques;
    }
}

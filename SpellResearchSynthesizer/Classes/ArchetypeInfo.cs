using DynamicData;
using Mutagen.Bethesda.Fallout4;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SpellResearchSynthesizer.Classes.Archetype;

namespace SpellResearchSynthesizer.Classes
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ArchetypeInfo
    {
        public static ArchetypeInfo Instance { get; private set; } = new() { };
        private ArchetypeInfo()
        {
            Levels = GenerateAliasDictionary(GenerateLevelList());
            Skills = GenerateAliasDictionary(GenerateSkillList());
            CastingTypes = GenerateAliasDictionary(GenerateCastingTypeList());
            Targets = GenerateAliasDictionary(GenerateTargetList());
            Elements = GenerateAliasDictionary(GenerateElementList());
            Techniques = GenerateAliasDictionary(GenerateTechniqueList());
            ArtifactTiers = new int[] { 0, 1, 2, 3, 4, 5 };
        }

        private static Dictionary<string, Archetype> GenerateAliasDictionary(List<Archetype> archetypes)
        {
            Dictionary<string, Archetype> dict = new();
            foreach (Archetype archetype in archetypes)
            {
                dict[archetype.Name.ToLower()] = archetype;
                foreach (string alias in archetype.Aliases)
                {
                    dict[alias.ToLower()] = archetype;
                }
            }
            return dict;
        }

        private static List<Archetype> GenerateLevelList()
        {
            return new List<Archetype> {
                new Archetype("Novice", null),
                new Archetype("Apprentice", null),
                new Archetype("Adept", null),
                new Archetype("Expert", null),
                new Archetype("Master", null),
            };
        }

        private List<Archetype> GenerateSkillList()
        {
            string type = "Skill";
            return new List<string>
            {
                "Alteration",
                "Conjuration",
                "Destruction",
                "Illusion",
                "Restoration",
            }.Select(s => new Archetype(s, GenerateTierMap(type, s))).ToList();
        }

        private List<Archetype> GenerateCastingTypeList()
        {
            string type = "Casting";
            return new List<Archetype> {
                new Archetype("FireAndForget", new List<string>{ "FireForget" }, GenerateTierMap(type, "FireForget")),
                new Archetype("Concentration", GenerateTierMap(type, "Concentration")),
            };
        }

        private List<Archetype> GenerateTargetList()
        {
            string type = "Target";
            return new List<Archetype> {
                new Archetype("Actor", new List<string>{ "Aimed" }, GenerateTierMap(type, "Actor")),
                new Archetype("Location", GenerateTierMap(type, "Location")),
                new Archetype("Self", GenerateTierMap(type, "Self")),
                new Archetype("Area", new List<string>{ "AreaOfEffect", "AOE"}, GenerateTierMap(type, "AOE")),
            };
        }

        private List<Archetype> GenerateElementList()
        {
            string type = "Element";
            return new List<Archetype> {
                new Archetype("Acid", GenerateTierMap(type, "Acid")),
                new Archetype("Air", GenerateTierMap(type, "Air")),
                new Archetype("Apparition", new List<string>{ "Apparitions" }, GenerateTierMap(type, "Apparition")),
                new Archetype("Arcane", new List<string> { "ArcaneEnergy" }, GenerateTierMap(type, "Arcane")),
                new Archetype("Armor", new List<string> { "Armour"}, GenerateTierMap(type, "Armor")),
                new Archetype("Construct", new List<string> { "Constructs" }, GenerateTierMap(type, "Construct")),
                new Archetype("Creature", new List<string> { "Creatures" }, GenerateTierMap(type, "Creature")),
                new Archetype("Daedra", GenerateTierMap(type, "Daedra")),
                new Archetype("Disease", GenerateTierMap(type, "Disease")),
                new Archetype("Earth", GenerateTierMap(type, "Earth")),
                new Archetype("Fire", GenerateTierMap(type, "Fire")),
                new Archetype("Flesh", GenerateTierMap(type, "Flesh")),
                new Archetype("Force", GenerateTierMap(type, "Force")),
                new Archetype("Frost", GenerateTierMap(type, "Frost")),
                new Archetype("Health", GenerateTierMap(type, "Health")),
                new Archetype("Human", new List<string> { "Humans", "Mortal", "Mortals" }, GenerateTierMap(type, "Human")),
                new Archetype("Life", GenerateTierMap(type, "Life")),
                new Archetype("Light", GenerateTierMap(type, "Light")),
                new Archetype("Magicka", GenerateTierMap(type, "Magicka")),
                new Archetype("Metal", GenerateTierMap(type, "Metal")),
                new Archetype("Nature", GenerateTierMap(type, "Nature")),
                new Archetype("Poison", GenerateTierMap(type, "Poison")),
                new Archetype("Resistance", GenerateTierMap(type, "Resistance")),
                new Archetype("Shadow", new List<string> { "Shadows" }, GenerateTierMap(type, "Shadow")),
                new Archetype("Shield", new List<string> { "Shields" }, GenerateTierMap(type, "Shield")),
                new Archetype("Shock", GenerateTierMap(type, "Shock")),
                new Archetype("Soul", new List<string> { "Souls" }, GenerateTierMap(type, "Soul")),
                new Archetype("Stamina", GenerateTierMap(type, "Stamina")),
                new Archetype("Sun", GenerateTierMap(type, "Sun")),
                new Archetype("Time", GenerateTierMap(type, "Time")),
                new Archetype("Trap", new List<string> { "Traps"}, GenerateTierMap(type, "Trap")),
                new Archetype("Undead", GenerateTierMap(type, "Undead")),
                new Archetype("Water", GenerateTierMap(type, "Water")),
                new Archetype("Weapon", new List<string> { "Weapons" }, GenerateTierMap(type, "Weapon")),
            };
        }

        private List<Archetype> GenerateTechniqueList()
        {
            string type = "Technique";
            return new List<Archetype>
            {
                new Archetype("Cloak", new List<string> { "Cloaking" }, GenerateTierMap(type, "Cloak")),
                new Archetype("Control", new List<string> { "Controlling" }, GenerateTierMap(type, "Control")),
                new Archetype("Courage", GenerateTierMap(type, "Courage")),
                new Archetype("Curing", GenerateTierMap(type, "Curing")),
                new Archetype("Curse", new List<string> { "Cursing" }, GenerateTierMap(type, "Curse")),
                new Archetype("Fear", GenerateTierMap(type, "Fear")),
                new Archetype("Frenzy", GenerateTierMap(type, "Frenzy")),
                new Archetype("Infuse", new List<string> { "Infusing" }, GenerateTierMap(type, "Infuse")),
                new Archetype("Pacify", new List<string> { "Pacifying" }, GenerateTierMap(type, "Pacify")),
                new Archetype("Sense", new List<string> { "Sensing" }, GenerateTierMap(type, "Sense")),
                new Archetype("Siphon", new List<string> { "Siphoning" }, GenerateTierMap(type, "Siphon")),
                new Archetype("Strengthen", new List<string> { "Strengthening" }, GenerateTierMap(type, "Strengthen")),
                new Archetype("Summoning", GenerateTierMap(type, "Summoning")),
                new Archetype("Telekinesis", GenerateTierMap(type, "Telekinesis")),
                new Archetype("Transform", new List<string> { "Transforming" }, GenerateTierMap(type, "Transform")),
            };
        }

        private Dictionary<Archetype, ArchetypeTierInfo> GenerateTierMap(string type, string archetype)
        {
            return Levels.ToDictionary(l => l.Value, l => new ArchetypeTierInfo($"_SR_GlobalArchetype{type}{archetype}{Levels.Keys.IndexOf(l.Key) + 1}{l.Value.Name}"));
        }

        public Archetype? GetArchetype(ArchetypeType type, string name)
        {
            name = name.ToLower();
            switch (type)
            {
                case ArchetypeType.Tier:
                    {
                        return Levels[name];
                    }
                case ArchetypeType.Skill:
                    {
                        return Skills[name];
                    }
                case ArchetypeType.CastingType:
                    {
                        return CastingTypes[name];
                    }
                case ArchetypeType.Targeting:
                    {
                        return Targets[name];
                    }
                case ArchetypeType.Element:
                    {
                        return Elements[name];
                    }
                case ArchetypeType.Technique:
                    {
                        return Techniques[name];
                    }
                default: return null;
            }
        }
        private Dictionary<string, Archetype> Levels { get; set; } = new();

        [JsonProperty(PropertyName = "levels")]
        public List<Archetype> LevelJsonList => Levels.Values.Distinct().ToList();
        private Dictionary<string, Archetype> CastingTypes { get; set; } = new();
        [JsonProperty(PropertyName = "castingTypes")]
        public List<Archetype> CastingTypeJsonList => CastingTypes.Values.Distinct().ToList();
        private Dictionary<string, Archetype> Targets { get; set; } = new();
        [JsonProperty(PropertyName = "targets")]
        public List<Archetype> TargetsJsonList => Targets.Values.Distinct().ToList();
        private Dictionary<string, Archetype> Skills { get; set; } = new();
        [JsonProperty(PropertyName = "skills")]
        public List<Archetype> SkillJsonList => Skills.Values.Distinct().ToList();
        private Dictionary<string, Archetype> Elements { get; set; } = new();
        [JsonProperty(PropertyName = "elements")]
        public List<Archetype> ElementsJsonList => Elements.Values.Distinct().ToList();
        private Dictionary<string, Archetype> Techniques { get; set; } = new();
        [JsonProperty(PropertyName = "techniques")]
        public List<Archetype> TechniquesJsonList => Techniques.Values.Distinct().ToList();
        [JsonProperty("artifactTiers")]
        public int[] ArtifactTiers { get; }
    }
}

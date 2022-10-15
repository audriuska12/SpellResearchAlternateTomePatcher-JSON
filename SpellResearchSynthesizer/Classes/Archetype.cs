using Newtonsoft.Json;
using Noggog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpellResearchSynthesizer.Classes
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Archetype
    {
        public enum ArchetypeType
        {
            Tier,
            Skill,
            CastingType,
            Targeting,
            Element,
            Technique
        }
        public Archetype() { }
        public Archetype(string name, Dictionary<Archetype, ArchetypeTierInfo>? tiers) : this(name, new List<string>(), tiers)
        {

        }
        public Archetype(string name, List<string> aliases, Dictionary<Archetype, ArchetypeTierInfo>? tiers)
        {
            Name = name;
            Aliases = aliases;
            Tiers = tiers;
        }

        public Dictionary<Archetype, ArchetypeTierInfo>? Tiers = null;
        public string Name { get; set; } = "";
        public List<string> Aliases { get; set; } = new List<string>();

        public class Converter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Archetype);
            }

            public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            {
                if (value is Archetype arch)
                {
                    writer.WriteValue(arch.Name.ToLower());
                }
                else throw new Exception($"Only archetype accepted - is {value?.GetType().Name}, {value}");
            }

            public override bool CanRead => false;
        }
    }
}

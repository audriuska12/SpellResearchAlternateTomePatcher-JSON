using Mutagen.Bethesda.Skyrim;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellResearchSynthesizer.Classes
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AlchemyEffectInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        public string EffectID { get; set; } = string.Empty;
        public IMagicEffectGetter? EffectForm { get; set; }
        public string EffectESP => EffectForm == null ? string.IsNullOrEmpty(EffectID) ? "" : EffectID.Split('|')[1] : EffectForm.FormKey.ModKey.FileName.ToString().ToLower();
        public string EffectFormID => EffectForm == null ? string.IsNullOrEmpty(EffectID) ? "" : EffectID.Split('|')[2] : EffectForm.FormKey.ID.ToString("X6").ToLower();
        [JsonProperty("effectId")]
        public string JsonEffectID => $"__formData|{EffectESP}|0x{EffectFormID}";
        public List<Archetype> Targeting { get; set; } = new List<Archetype>();
        [JsonProperty("elements", ItemConverterType = typeof(Archetype.Converter))]
        public List<Archetype> Elements { get; set; } = new List<Archetype>();
        [JsonProperty("techniques", ItemConverterType = typeof(Archetype.Converter))]
        public List<Archetype> Techniques { get; set; } = new List<Archetype>();
    }
}

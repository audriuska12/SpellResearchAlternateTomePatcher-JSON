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
    public class ArtifactInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        public string ArtifactID { get; set; } = string.Empty;
        public IItemGetter? ArtifactForm { get; set; }
        public string ArtifactESP => ArtifactForm == null ? string.IsNullOrEmpty(ArtifactID) ? "" : ArtifactID.Split('|')[1] : ArtifactForm.FormKey.ModKey.FileName.ToString().ToLower();
        public string ArtifactFormID => ArtifactForm == null ? string.IsNullOrEmpty(ArtifactID) ? "" : ArtifactID.Split('|')[2] : ArtifactForm.FormKey.ID.ToString("X6").ToLower();
        [JsonProperty("artifactID")]
        public string JsonArtifactID => $"__formData|{ArtifactESP}|0x{ArtifactFormID}";
        [JsonProperty("tier")]
        public int Tier { get; set; } = 0;
        [JsonProperty("schools")]
        public List<Archetype> Schools { get; set; } = new();
        [JsonProperty("castingTypes")]
        public List<Archetype> CastingTypes { get; set; } = new();
        [JsonProperty("targeting", ItemConverterType = typeof(Archetype.Converter))]
        public List<Archetype> Targeting { get; set; } = new();
        [JsonProperty("elements", ItemConverterType = typeof(Archetype.Converter))]
        public List<Archetype> Elements { get; set; } = new List<Archetype>();
        [JsonProperty("techniques", ItemConverterType = typeof(Archetype.Converter))]
        public List<Archetype> Techniques { get; set; } = new List<Archetype>();
        [JsonProperty("equippableAll")]
        public bool Equippable { get; set; } = false;
        [JsonProperty("equippableArtifact")]
        public bool EquippableArtifact { get; set; } = false;
        [JsonProperty("equippableText")]
        public bool EquippableText { get; set; } = false;
    }
}

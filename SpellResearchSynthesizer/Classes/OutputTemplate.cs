using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellResearchSynthesizer.Classes
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
    public class OutputTemplate
    {
        [JsonProperty("newSpells")]
        public List<SpellInfo> NewSpells { get; set; } = new List<SpellInfo>();
        [JsonProperty("removedSpells")]
        public List<SpellInfo> RemovedSpells { get; set; } = new List<SpellInfo>();
        [JsonProperty("newArtifacts")]
        public List<ArtifactInfo> NewArtifacts { get; set; } = new List<ArtifactInfo>();
        [JsonProperty("removedArtifacts")]
        public List<ArtifactInfo> RemovedArtifacts { get; set; } = new List<ArtifactInfo>();
        [JsonProperty("newEffects")]
        public List<AlchemyEffectInfo> NewEffects { get; set; } = new List<AlchemyEffectInfo>();
        [JsonProperty("researchDataLists")]
        public ResearchDataLists? ResearchDataLists { get; set; }
    }
}

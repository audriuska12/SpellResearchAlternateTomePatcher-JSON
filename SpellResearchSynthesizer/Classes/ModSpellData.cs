using System.Collections.Generic;

namespace SpellResearchSynthesizer.Classes
{
    public class ModSpellData
    {
        public List<SpellInfo> NewSpells = new();
        public List<SpellInfo> RemovedSpells = new();
        public List<AlchemyEffectInfo> NewAlchemyEffects = new();
        public List<ArtifactInfo> NewArtifacts = new();
        public List<ArtifactInfo> RemovedArtifacts = new();
    }
}

using System.Collections.Generic;

namespace SpellResearchSynthesizer
{
    internal class ValidationResults
    {
        public Dictionary<string, IEnumerable<string>> DuplicateSpells = new();
        public Dictionary<string, IEnumerable<string>> DuplicateTomes = new();
        public Dictionary<string, IEnumerable<string>> DuplicateScrolls = new();
    }
}
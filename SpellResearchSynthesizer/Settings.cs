using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellResearchSynthesizer
{
    public class Settings
    {
        public LevelSettings Novice = new();
        public LevelSettings Apprentice = new();
        public LevelSettings Adept = new();
        public LevelSettings Expert = new();
        public LevelSettings Master = new();
        public bool IgnoreDiscoverable = false;
        public bool RemoveStartingSpells = false;
        public bool GenerateFLMIni = true;
        public bool StopOnInvalidPatch = true;
        public List<string> jsonNames = new();
        public List<string> jsonPaths = new();
        public List<string> pscnames = new();
    }

    public class LevelSettings
    {
        public bool process = true;
        public string font = "";
        public bool useFontColor;
        public bool useImage;

    }
}

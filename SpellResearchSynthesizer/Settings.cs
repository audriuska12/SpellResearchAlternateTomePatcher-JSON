using System.Collections.Generic;

namespace SpellResearchSynthesizer
{
    public class Settings
    {
        public LevelSettings Novice = new();
        public LevelSettings Apprentice = new();
        public LevelSettings Adept = new();
        public LevelSettings Expert = new();
        public LevelSettings Master = new();
        public bool FirstPerson = true;
        public bool IgnoreDiscoverable = false;
        public bool RemoveStartingSpells = true;
        public bool GenerateFLMIni = true;
        public bool ConvertPSCToJson = false;
        public bool ExperimentalTeachesSpellFix = false;
        public List<string> jsonNames = new();
        public List<string> jsonPaths = new();
        public List<string> pscnames = new();
    }

    public class LevelSettings
    {
        public bool Process = true;
        public string Font = "";
        public bool UseFontColor;
        public bool UseImage;
        public int NoviceExperience;
        public int ApprenticeExperience;
        public int AdeptExperience;
        public int ExpertExperience;
        public int MasterExperience;
        public int SkillRequired;
    }
}

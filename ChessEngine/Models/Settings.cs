using System.Collections.Generic;

namespace ChessEngine.Models
{
    public class Settings
    {
        public int Threads { get; set; }
        public int MultiPV { get; set; }
        public int SkillLevel { get; set; }
        

        public Settings(
            int threads = 0,
            int multiPV = 3,
            int skillLevel = 20
        )
        {
            Threads = threads;
            MultiPV = multiPV;
            SkillLevel = skillLevel;
        }

        public Dictionary<string, string> GetPropertiesAsDictionary()
        {
            return new Dictionary<string, string>
            {
                ["Threads"] = Threads.ToString(),
                ["MultiPV"] = MultiPV.ToString(),
                ["Skill Level"] = SkillLevel.ToString()
            };
        }
    }
}
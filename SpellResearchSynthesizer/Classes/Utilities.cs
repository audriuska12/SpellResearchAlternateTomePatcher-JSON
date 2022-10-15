using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellResearchSynthesizer.Classes
{
    internal static class Utilities
    {
        public static string CapitalizeFirst(this string str) {
            return str.Length > 1 ? str[0..1].ToUpper() + str[1..] : str.ToUpper();
        }
    }
}

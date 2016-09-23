using System;
using System.Text.RegularExpressions;

namespace JetEntityFrameworkProvider
{
    static class StringHelpers
    {
        public static string ReplaceCaseInsensitive(this string s, string oldValue, string newValue)
        {
            return Regex.Replace(s, oldValue, newValue, RegexOptions.IgnoreCase);
        }
    }
}

using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class Formatter
{
    public static string Format(string template, Dictionary<string, object> data)
    {
        return Regex.Replace(
            template, "{(.*?)}", m =>
            {
                string key = m.Groups[1].Value;
                return data.ContainsKey(key) ? data[key].ToString() : m.Value;
            }
        );
    }
}

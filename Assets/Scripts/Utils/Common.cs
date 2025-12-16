using System;
using System.Collections.Generic;

public static class CommonUtils
{
    public static string GetUUIDstring() => Guid.NewGuid().ToString("N");

    public static string DictToString(Dictionary<string, object> dict)
    {
        if (dict == null || dict.Count == 0)
            return "";

        List<string> parts = new List<string>();

        foreach (var kv in dict)
        {
            parts.Add($"{kv.Key}={kv.Value}");
        }

        return string.Join(", ", parts);
    }
}

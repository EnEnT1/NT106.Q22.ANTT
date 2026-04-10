using System;
using System.IO;

namespace Healthcare.Server.Helpers
{
    public static class EnvLoader
    {
        public static void Load(string filePath = ".env")
        {
            if (!File.Exists(filePath))
                return;

            foreach (var rawLine in File.ReadAllLines(filePath))
            {
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                int index = line.IndexOf('=');
                if (index <= 0)
                    continue;

                string key = line.Substring(0, index).Trim();
                string value = line.Substring(index + 1).Trim();

                if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                    value = value.Substring(1, value.Length - 2);

                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
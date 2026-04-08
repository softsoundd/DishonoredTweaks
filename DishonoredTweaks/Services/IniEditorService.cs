using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DishonoredTweaks.Services
{
    public sealed class IniEditorService
    {
        public string? ReadValue(string filePath, string section, string key)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            string[] lines = File.ReadAllLines(filePath);
            string sectionHeader = $"[{section}]";
            bool inSection = false;

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.Equals(sectionHeader, StringComparison.OrdinalIgnoreCase))
                {
                    inSection = true;
                    continue;
                }

                if (!inSection)
                {
                    continue;
                }

                if (trimmed.StartsWith("[", StringComparison.Ordinal))
                {
                    break;
                }

                if (trimmed.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed.Substring(key.Length + 1).Trim();
                }
            }

            return null;
        }

        public void SetValues(string filePath, string section, IReadOnlyDictionary<string, string> keyValueMap)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("INI file not found.", filePath);
            }

            if (keyValueMap.Count == 0)
            {
                return;
            }

            FileInfo fileInfo = new(filePath);
            bool wasReadOnly = fileInfo.IsReadOnly;
            if (wasReadOnly)
            {
                fileInfo.IsReadOnly = false;
            }

            try
            {
                List<string> lines = File.ReadAllLines(filePath).ToList();
                string sectionHeader = $"[{section}]";

                int sectionStart = -1;
                int sectionEndExclusive = lines.Count;

                for (int i = 0; i < lines.Count; i++)
                {
                    string trimmed = lines[i].Trim();
                    if (trimmed.Equals(sectionHeader, StringComparison.OrdinalIgnoreCase))
                    {
                        sectionStart = i;
                        for (int j = i + 1; j < lines.Count; j++)
                        {
                            if (lines[j].TrimStart().StartsWith("[", StringComparison.Ordinal))
                            {
                                sectionEndExclusive = j;
                                break;
                            }
                        }

                        break;
                    }
                }

                if (sectionStart == -1)
                {
                    if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
                    {
                        lines.Add(string.Empty);
                    }

                    lines.Add(sectionHeader);
                    foreach ((string key, string value) in keyValueMap)
                    {
                        lines.Add($"{key}={value}");
                    }
                }
                else
                {
                    HashSet<string> updatedKeys = new(StringComparer.OrdinalIgnoreCase);
                    for (int i = sectionStart + 1; i < sectionEndExclusive; i++)
                    {
                        string trimmed = lines[i].Trim();
                        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith(";", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        int separator = trimmed.IndexOf('=');
                        if (separator <= 0)
                        {
                            continue;
                        }

                        string lineKey = trimmed.Substring(0, separator).Trim();
                        if (!keyValueMap.TryGetValue(lineKey, out string? value))
                        {
                            continue;
                        }

                        int indentLength = lines[i].Length - lines[i].TrimStart().Length;
                        string indent = lines[i].Substring(0, indentLength);
                        lines[i] = $"{indent}{lineKey}={value}";
                        updatedKeys.Add(lineKey);
                    }

                    int insertIndex = sectionEndExclusive;
                    foreach ((string key, string value) in keyValueMap)
                    {
                        if (updatedKeys.Contains(key))
                        {
                            continue;
                        }

                        lines.Insert(insertIndex, $"{key}={value}");
                        insertIndex++;
                    }
                }

                File.WriteAllLines(filePath, lines);
            }
            finally
            {
                if (wasReadOnly)
                {
                    fileInfo.IsReadOnly = true;
                }
            }
        }

        public void SetReadOnly(string filePath, bool isReadOnly)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            FileInfo fileInfo = new(filePath)
            {
                IsReadOnly = isReadOnly
            };
        }

        public void RemoveKeys(string filePath, string section, IEnumerable<string> keys)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            HashSet<string> keysToRemove = new(keys, StringComparer.OrdinalIgnoreCase);
            if (keysToRemove.Count == 0)
            {
                return;
            }

            FileInfo fileInfo = new(filePath);
            bool wasReadOnly = fileInfo.IsReadOnly;
            if (wasReadOnly)
            {
                fileInfo.IsReadOnly = false;
            }

            try
            {
                List<string> lines = File.ReadAllLines(filePath).ToList();
                string sectionHeader = $"[{section}]";

                int sectionStart = -1;
                int sectionEndExclusive = lines.Count;
                for (int i = 0; i < lines.Count; i++)
                {
                    string trimmed = lines[i].Trim();
                    if (!trimmed.Equals(sectionHeader, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    sectionStart = i;
                    for (int j = i + 1; j < lines.Count; j++)
                    {
                        if (lines[j].TrimStart().StartsWith("[", StringComparison.Ordinal))
                        {
                            sectionEndExclusive = j;
                            break;
                        }
                    }

                    break;
                }

                if (sectionStart < 0)
                {
                    return;
                }

                bool removedAny = false;
                for (int i = sectionEndExclusive - 1; i > sectionStart; i--)
                {
                    string trimmed = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith(";", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    int separator = trimmed.IndexOf('=');
                    if (separator <= 0)
                    {
                        continue;
                    }

                    string lineKey = trimmed[..separator].Trim();
                    if (!keysToRemove.Contains(lineKey))
                    {
                        continue;
                    }

                    lines.RemoveAt(i);
                    removedAny = true;
                }

                if (removedAny)
                {
                    File.WriteAllLines(filePath, lines);
                }
            }
            finally
            {
                if (wasReadOnly)
                {
                    fileInfo.IsReadOnly = true;
                }
            }
        }
    }
}

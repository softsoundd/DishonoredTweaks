using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DishonoredTweaks.Models;

namespace DishonoredTweaks.Services
{
    public sealed class InputBindingsService
    {
        private const string PlayerInputSection = "Engine.PlayerInput";
        private const string JumpCommand = "GBA_Jump";
        private const string InteractCommand = "GBA_Use | Dis_PlayerChoice_RequestSkip | OnRelease Dis_PlayerChoice_RequestSkip_Released";
        private const string ConsoleCommandPrefix = "set Console ConsoleKey ";
        private const string FpsCommandPrefix = "set Engine MaxSmoothedFrameRate ";
        private const string CheatCommand = "set PlayerController CheatClass class'DishonoredCheatManager' | EnableCheats";
        private const string MouseScrollUp = "MouseScrollUp";
        private const string MouseScrollDown = "MouseScrollDown";

        public void EnsureQuickSaveSlot(string inputIniPath, int slotIndex)
        {
            if (slotIndex <= 0)
            {
                throw new InvalidOperationException("Quick save slot index must be greater than zero.");
            }

            if (!File.Exists(inputIniPath))
            {
                throw new FileNotFoundException("DishonoredInput.ini not found.", inputIniPath);
            }

            FileInfo fileInfo = new(inputIniPath);
            bool wasReadOnly = fileInfo.IsReadOnly;
            if (wasReadOnly)
            {
                fileInfo.IsReadOnly = false;
            }

            try
            {
                List<string> lines = File.ReadAllLines(inputIniPath).ToList();
                if (!TryFindSection(lines, PlayerInputSection, out int sectionStart, out int sectionEndExclusive))
                {
                    throw new InvalidOperationException("Could not find [Engine.PlayerInput] section in DishonoredInput.ini.");
                }

                bool saveUpdated = false;
                bool loadUpdated = false;
                int lastBaseBindingLine = -1;
                int firstManagedBindingLine = -1;

                for (int i = sectionStart + 1; i < sectionEndExclusive; i++)
                {
                    if (TryParseBaseBindingLine(lines[i], out string? name, out _))
                    {
                        lastBaseBindingLine = i;
                        if (string.Equals(name, "GBA_QuickSave", StringComparison.Ordinal))
                        {
                            lines[i] = CreateBaseBindingLine("GBA_QuickSave", $"Dis_Save {slotIndex}");
                            saveUpdated = true;
                        }
                        else if (string.Equals(name, "GBA_QuickLoad", StringComparison.Ordinal))
                        {
                            lines[i] = CreateBaseBindingLine("GBA_QuickLoad", $"Dis_Load {slotIndex}");
                            loadUpdated = true;
                        }
                    }
                    else if (firstManagedBindingLine < 0 && lines[i].TrimStart().StartsWith("m_PCBindings=", StringComparison.Ordinal))
                    {
                        firstManagedBindingLine = i;
                    }
                }

                int insertIndex = firstManagedBindingLine >= 0
                    ? firstManagedBindingLine
                    : (lastBaseBindingLine >= 0 ? lastBaseBindingLine + 1 : sectionStart + 1);

                if (!saveUpdated)
                {
                    lines.Insert(insertIndex, CreateBaseBindingLine("GBA_QuickSave", $"Dis_Save {slotIndex}"));
                    insertIndex++;
                }

                if (!loadUpdated)
                {
                    lines.Insert(insertIndex, CreateBaseBindingLine("GBA_QuickLoad", $"Dis_Load {slotIndex}"));
                }

                File.WriteAllLines(inputIniPath, lines);
            }
            finally
            {
                if (wasReadOnly)
                {
                    fileInfo.IsReadOnly = true;
                }
            }
        }

        public InputBindingOptions LoadOptions(string inputIniPath)
        {
            InputBindingOptions options = new();
            if (!File.Exists(inputIniPath))
            {
                return options;
            }

            string[] lines = File.ReadAllLines(inputIniPath);
            if (!TryFindSection(lines, PlayerInputSection, out int start, out int endExclusive))
            {
                return options;
            }

            List<FpsBindingEntry> fpsBindings = new();
            for (int i = start + 1; i < endExclusive; i++)
            {
                if (!TryParseBindingLine(lines[i], out string? name, out string? command))
                {
                    continue;
                }

                if (string.Equals(command, JumpCommand, StringComparison.Ordinal))
                {
                    options.JumpScrollDirection = ConvertMouseNameToDirection(name);
                }
                else if (string.Equals(command, InteractCommand, StringComparison.Ordinal))
                {
                    options.InteractScrollDirection = ConvertMouseNameToDirection(name);
                }
                else if (command.StartsWith(FpsCommandPrefix, StringComparison.Ordinal))
                {
                    string fpsValue = command.Substring(FpsCommandPrefix.Length);
                    fpsBindings.Add(new FpsBindingEntry
                    {
                        Key = name,
                        Value = fpsValue
                    });
                }
                else if (command.StartsWith(ConsoleCommandPrefix, StringComparison.Ordinal))
                {
                    options.ConsoleKey = name;
                }
                else if (string.Equals(command, CheatCommand, StringComparison.Ordinal))
                {
                    options.CheatKey = name;
                }
            }

            options.FpsBindings = fpsBindings;
            return options;
        }

        public void ApplyBindings(string inputIniPath, InputBindingOptions options)
        {
            if (!File.Exists(inputIniPath))
            {
                throw new FileNotFoundException("DishonoredInput.ini not found.", inputIniPath);
            }

            FileInfo fileInfo = new(inputIniPath);
            bool wasReadOnly = fileInfo.IsReadOnly;
            if (wasReadOnly)
            {
                fileInfo.IsReadOnly = false;
            }

            try
            {
                List<string> lines = File.ReadAllLines(inputIniPath).ToList();
                if (!TryFindSection(lines, PlayerInputSection, out int sectionStart, out int sectionEndExclusive))
                {
                    throw new InvalidOperationException("Could not find [Engine.PlayerInput] section in DishonoredInput.ini.");
                }

                List<string> sectionBody = lines
                    .Skip(sectionStart + 1)
                    .Take(sectionEndExclusive - sectionStart - 1)
                    .ToList();

                sectionBody = sectionBody
                    .Where(line => !IsManagedBindingLine(line))
                    .ToList();

                List<string> newBindings = BuildManagedBindingLines(options);

                int insertionIndex = FindBindingInsertionIndex(sectionBody);
                sectionBody.InsertRange(insertionIndex, newBindings);

                List<string> rebuilt = new();
                rebuilt.AddRange(lines.Take(sectionStart + 1));
                rebuilt.AddRange(sectionBody);
                rebuilt.AddRange(lines.Skip(sectionEndExclusive));

                File.WriteAllLines(inputIniPath, rebuilt);
            }
            finally
            {
                if (wasReadOnly)
                {
                    fileInfo.IsReadOnly = true;
                }
            }
        }

        private static List<string> BuildManagedBindingLines(InputBindingOptions options)
        {
            List<string> lines = new();

            if (TryGetMouseNameFromDirection(options.JumpScrollDirection, out string jumpMouseName))
            {
                lines.Add(CreateBindingLine(jumpMouseName, JumpCommand));
            }

            if (TryGetMouseNameFromDirection(options.InteractScrollDirection, out string interactMouseName))
            {
                lines.Add(CreateBindingLine(interactMouseName, InteractCommand));
            }

            foreach (FpsBindingEntry fpsBinding in options.FpsBindings)
            {
                if (string.IsNullOrWhiteSpace(fpsBinding.Key) || string.IsNullOrWhiteSpace(fpsBinding.Value))
                {
                    continue;
                }

                if (!double.TryParse(fpsBinding.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                {
                    continue;
                }

                lines.Add(CreateBindingLine(fpsBinding.Key.Trim(), FpsCommandPrefix + fpsBinding.Value.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(options.ConsoleKey))
            {
                string key = options.ConsoleKey.Trim();
                lines.Add(CreateBindingLine(key, ConsoleCommandPrefix + key));
            }

            if (!string.IsNullOrWhiteSpace(options.CheatKey))
            {
                lines.Add(CreateBindingLine(options.CheatKey.Trim(), CheatCommand));
            }

            return lines;
        }

        private static int FindBindingInsertionIndex(List<string> sectionBody)
        {
            int lastBinding = -1;
            for (int i = 0; i < sectionBody.Count; i++)
            {
                if (sectionBody[i].TrimStart().StartsWith("m_PCBindings=", StringComparison.Ordinal))
                {
                    lastBinding = i;
                }
            }

            return lastBinding >= 0 ? lastBinding + 1 : 0;
        }

        private static bool IsManagedBindingLine(string line)
        {
            if (!TryParseBindingLine(line, out string? name, out string? command))
            {
                return false;
            }

            bool isManagedScroll =
                (string.Equals(command, JumpCommand, StringComparison.Ordinal) ||
                 string.Equals(command, InteractCommand, StringComparison.Ordinal)) &&
                (string.Equals(name, MouseScrollUp, StringComparison.Ordinal) ||
                 string.Equals(name, MouseScrollDown, StringComparison.Ordinal));

            return isManagedScroll ||
                   command.StartsWith(FpsCommandPrefix, StringComparison.Ordinal) ||
                   command.StartsWith(ConsoleCommandPrefix, StringComparison.Ordinal) ||
                   string.Equals(command, CheatCommand, StringComparison.Ordinal);
        }

        private static string ConvertMouseNameToDirection(string name)
        {
            if (string.Equals(name, MouseScrollUp, StringComparison.Ordinal))
            {
                return "ScrollUp";
            }

            if (string.Equals(name, MouseScrollDown, StringComparison.Ordinal))
            {
                return "ScrollDown";
            }

            return "None";
        }

        private static bool TryGetMouseNameFromDirection(string direction, out string name)
        {
            name = string.Empty;
            if (string.Equals(direction, "ScrollUp", StringComparison.OrdinalIgnoreCase))
            {
                name = MouseScrollUp;
                return true;
            }

            if (string.Equals(direction, "ScrollDown", StringComparison.OrdinalIgnoreCase))
            {
                name = MouseScrollDown;
                return true;
            }

            return false;
        }

        private static string CreateBindingLine(string name, string command)
        {
            return $"m_PCBindings=(Name=\"{name}\",Command=\"{command}\")";
        }

        private static string CreateBaseBindingLine(string name, string command)
        {
            return $"BaseBindings=(Name=\"{name}\",Command=\"{command}\")";
        }

        private static bool TryFindSection(IReadOnlyList<string> lines, string section, out int sectionStart, out int sectionEndExclusive)
        {
            string sectionHeader = $"[{section}]";
            sectionStart = -1;
            sectionEndExclusive = lines.Count;

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

                return true;
            }

            return false;
        }

        private static bool TryParseBindingLine(string line, out string name, out string command)
        {
            name = string.Empty;
            command = string.Empty;

            string trimmed = line.Trim();
            if (!trimmed.StartsWith("m_PCBindings=(", StringComparison.Ordinal) || !trimmed.EndsWith(")", StringComparison.Ordinal))
            {
                return false;
            }

            int nameStart = trimmed.IndexOf("Name=\"", StringComparison.Ordinal);
            int commandStart = trimmed.IndexOf(",Command=\"", StringComparison.Ordinal);
            if (nameStart < 0 || commandStart < 0)
            {
                return false;
            }

            nameStart += "Name=\"".Length;
            int nameEnd = trimmed.IndexOf('"', nameStart);
            if (nameEnd < 0)
            {
                return false;
            }

            int commandValueStart = commandStart + ",Command=\"".Length;
            int commandValueEnd = trimmed.LastIndexOf("\")", StringComparison.Ordinal);
            if (commandValueEnd < 0 || commandValueEnd <= commandValueStart)
            {
                return false;
            }

            name = trimmed.Substring(nameStart, nameEnd - nameStart);
            command = trimmed.Substring(commandValueStart, commandValueEnd - commandValueStart);
            return true;
        }

        private static bool TryParseBaseBindingLine(string line, out string name, out string command)
        {
            name = string.Empty;
            command = string.Empty;

            string trimmed = line.Trim();
            if (!trimmed.StartsWith("BaseBindings=(", StringComparison.Ordinal) || !trimmed.EndsWith(")", StringComparison.Ordinal))
            {
                return false;
            }

            int nameStart = trimmed.IndexOf("Name=\"", StringComparison.Ordinal);
            int commandStart = trimmed.IndexOf(",Command=\"", StringComparison.Ordinal);
            if (nameStart < 0 || commandStart < 0)
            {
                return false;
            }

            nameStart += "Name=\"".Length;
            int nameEnd = trimmed.IndexOf('"', nameStart);
            if (nameEnd < 0)
            {
                return false;
            }

            int commandValueStart = commandStart + ",Command=\"".Length;
            int commandValueEnd = trimmed.LastIndexOf("\")", StringComparison.Ordinal);
            if (commandValueEnd < 0 || commandValueEnd <= commandValueStart)
            {
                return false;
            }

            name = trimmed.Substring(nameStart, nameEnd - nameStart);
            command = trimmed.Substring(commandValueStart, commandValueEnd - commandValueStart);
            return true;
        }
    }
}

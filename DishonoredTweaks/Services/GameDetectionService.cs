using System;
using System.IO;

namespace DishonoredTweaks.Services
{
    public sealed class GameDetectionService
    {
        public bool IsValidGameDirectory(string? gameDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(gameDirectoryPath))
            {
                return false;
            }

            string exePath = Path.Combine(gameDirectoryPath, "Binaries", "Win32", "Dishonored.exe");
            string cookedPath = Path.Combine(gameDirectoryPath, "DishonoredGame", "CookedPCConsole");
            return File.Exists(exePath) && Directory.Exists(cookedPath);
        }

        public string GetGameExePath(string gameDirectoryPath)
        {
            return Path.Combine(gameDirectoryPath, "Binaries", "Win32", "Dishonored.exe");
        }

        public string GetConfigDirectory()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentsPath, "My Games", "Dishonored", "DishonoredGame", "Config");
        }

        public string GetEngineIniPath()
        {
            return Path.Combine(GetConfigDirectory(), "DishonoredEngine.ini");
        }

        public string GetInputIniPath()
        {
            return Path.Combine(GetConfigDirectory(), "DishonoredInput.ini");
        }

        public bool ConfigFilesExist(out string engineIniPath, out string inputIniPath)
        {
            engineIniPath = GetEngineIniPath();
            inputIniPath = GetInputIniPath();
            return File.Exists(engineIniPath) && File.Exists(inputIniPath);
        }
    }
}

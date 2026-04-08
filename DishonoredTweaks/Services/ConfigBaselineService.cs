using System;
using System.IO;
using System.Linq;

namespace DishonoredTweaks.Services
{
    public sealed class ConfigBaselineService
    {
        private const string TemplatesRootDirectoryName = "ConfigTemplates";
        private const string EngineIniFileName = "DishonoredEngine.ini";
        private const string InputIniFileName = "DishonoredInput.ini";

        public void ApplyPatchBaseline(string configDirectoryPath, string patchVersion)
        {
            if (string.IsNullOrWhiteSpace(configDirectoryPath))
            {
                throw new InvalidOperationException("Config directory path is not set.");
            }

            string profile = patchVersion switch
            {
                PatchDownloadService.PatchTarget12Base => "1.2",
                PatchDownloadService.PatchTarget13Kod => "1.3",
                PatchDownloadService.PatchTarget14Brigmore => "1.4-1.5",
                PatchDownloadService.PatchTarget14Daud => "1.4-1.5",
                PatchDownloadService.PatchTarget15Latest => "1.4-1.5",
                _ => throw new InvalidOperationException($"Unsupported patch version '{patchVersion}'.")
            };

            Directory.CreateDirectory(configDirectoryPath);

            string destinationEnginePath = Path.Combine(configDirectoryPath, EngineIniFileName);
            string destinationInputPath = Path.Combine(configDirectoryPath, InputIniFileName);
            using Stream sourceEngineStream = OpenTemplateStream(profile, EngineIniFileName);
            using Stream sourceInputStream = OpenTemplateStream(profile, InputIniFileName);

            CopyTemplateFile(sourceEngineStream, destinationEnginePath);
            CopyTemplateFile(sourceInputStream, destinationInputPath);
        }

        private static Stream OpenTemplateStream(string profile, string fileName)
        {
            string[] resourceNames = typeof(ConfigBaselineService).Assembly.GetManifestResourceNames();
            string suffix = $".{TemplatesRootDirectoryName}.{profile}.{fileName}";
            string? resourceName = resourceNames.FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            resourceName ??= resourceNames.FirstOrDefault(name => IsTemplateResourceMatch(name, profile, fileName));

            if (resourceName is null)
            {
                string availableTemplateResources = string.Join(
                    ", ",
                    resourceNames.Where(name => name.Contains($".{TemplatesRootDirectoryName}.", StringComparison.OrdinalIgnoreCase)));

                throw new FileNotFoundException(
                    $"Missing embedded config template resource '{TemplatesRootDirectoryName}/{profile}/{fileName}'. " +
                    $"Available template resources: {availableTemplateResources}.");
            }

            Stream? stream = typeof(ConfigBaselineService).Assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                throw new InvalidOperationException($"Unable to open embedded config template resource '{resourceName}'.");
            }

            return stream;
        }

        private static bool IsTemplateResourceMatch(string resourceName, string profile, string fileName)
        {
            string marker = $".{TemplatesRootDirectoryName}.";
            int markerIndex = resourceName.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                return false;
            }

            string fileSuffix = $".{fileName}";
            int fileSuffixIndex = resourceName.LastIndexOf(fileSuffix, StringComparison.OrdinalIgnoreCase);
            if (fileSuffixIndex <= markerIndex)
            {
                return false;
            }

            int profileStartIndex = markerIndex + marker.Length;
            string embeddedProfile = resourceName.Substring(profileStartIndex, fileSuffixIndex - profileStartIndex);
            return string.Equals(
                NormaliseProfileToken(embeddedProfile),
                NormaliseProfileToken(profile),
                StringComparison.Ordinal);
        }

        private static string NormaliseProfileToken(string value)
        {
            return new string(value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }

        private static void CopyTemplateFile(Stream sourceStream, string destinationPath)
        {
            if (File.Exists(destinationPath))
            {
                FileInfo existing = new(destinationPath)
                {
                    IsReadOnly = false
                };
            }

            using FileStream destinationStream = new(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            sourceStream.CopyTo(destinationStream);
        }
    }
}

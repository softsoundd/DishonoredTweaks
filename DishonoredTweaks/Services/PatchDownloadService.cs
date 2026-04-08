using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DishonoredTweaks.Services
{
    public sealed class PatchApplyOptions
    {
        public string GameDirectoryPath { get; set; } = string.Empty;
        public string PatchVersion { get; set; } = "1.5-latest";
        public bool ApplyBoyleFix { get; set; }
        public bool ApplyTimshFix { get; set; }
    }

    public sealed class PatchDownloadService
    {
        public const string PatchTarget12Base = "1.2-base";
        public const string PatchTarget13Kod = "1.3-kod";
        public const string PatchTarget14Brigmore = "1.4-bw";
        public const string PatchTarget14Daud = "1.4-daud";
        public const string PatchTarget15Latest = "1.5-latest";

        private const string Patch12Url = "https://github.com/softsoundd/DishonoredTweaks/releases/download/Patches/Patch.1.2.Base.Game.zip";
        private const string Patch13Url = "https://github.com/softsoundd/DishonoredTweaks/releases/download/Patches/Patch.1.3.Knife.of.Dunwall.zip";
        private const string Patch14BrigmoreUrl = "https://github.com/softsoundd/DishonoredTweaks/releases/download/Patches/Patch.1.4.Brigmore.Witches.zip";
        private const string RngFixUrl = "https://github.com/softsoundd/DishonoredTweaks/releases/download/Patches/Dishonored.RNG.Fix.zip";
        private const string DaudhonoredUrl = "https://github.com/softsoundd/DishonoredTweaks/releases/download/Patches/Patch.1.4.Daudhonored.Patch.zip";

        private const string BoyleFixRelativePath = "DishonoredGame/CookedPCConsole/L_Boyle_Int_Script.upk";
        private const string TimshFixRelativePath = "DishonoredGame/DLC/PCConsole/DLC06/DLC06_Timsh_Streets_Scripts.upk";

        private readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromMinutes(20)
        };

        public async Task ApplyPatchAsync(
            PatchApplyOptions options,
            IProgress<string>? statusProgress = null,
            IProgress<double?>? downloadProgress = null,
            CancellationToken cancellationToken = default)
        {
            ValidateOptions(options);

            string tempRoot = Path.Combine(Path.GetTempPath(), "DishonoredTweaks", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            try
            {
                statusProgress?.Report("Downloading and applying selected patch payload...");
                switch (options.PatchVersion)
                {
                    case PatchTarget12Base:
                        await DownloadExtractAndOverlayAsync(Patch12Url, tempRoot, options.GameDirectoryPath, statusProgress, downloadProgress, cancellationToken);
                        break;
                    case PatchTarget13Kod:
                        await DownloadExtractAndOverlayAsync(Patch13Url, tempRoot, options.GameDirectoryPath, statusProgress, downloadProgress, cancellationToken);
                        break;
                    case PatchTarget14Brigmore:
                        await Apply14BrigmoreAsync(tempRoot, options.GameDirectoryPath, statusProgress, downloadProgress, cancellationToken);
                        break;
                    case PatchTarget14Daud:
                        await Apply14BrigmoreAsync(tempRoot, options.GameDirectoryPath, statusProgress, downloadProgress, cancellationToken);
                        await DownloadExtractAndOverlayAsync(DaudhonoredUrl, tempRoot, options.GameDirectoryPath, statusProgress, downloadProgress, cancellationToken);
                        break;
                    case PatchTarget15Latest:
                        statusProgress?.Report("Using latest 1.5 baseline (no downpatch payload).");
                        break;
                }

                if (options.ApplyBoyleFix || options.ApplyTimshFix)
                {
                    await DownloadAndApplyRngFixesAsync(
                        options.ApplyBoyleFix,
                        options.ApplyTimshFix,
                        tempRoot,
                        options.GameDirectoryPath,
                        statusProgress,
                        downloadProgress,
                        cancellationToken);
                }

                downloadProgress?.Report(100);
                statusProgress?.Report("Patch payloads applied.");
            }
            finally
            {
                TryDeleteDirectory(tempRoot);
            }
        }

        private async Task Apply14BrigmoreAsync(
            string tempRoot,
            string gameDirectoryPath,
            IProgress<string>? statusProgress,
            IProgress<double?>? downloadProgress,
            CancellationToken cancellationToken)
        {
            statusProgress?.Report("Applying 1.4 Brigmore Witches...");
            await DownloadExtractAndOverlayAsync(Patch14BrigmoreUrl, tempRoot, gameDirectoryPath, statusProgress, downloadProgress, cancellationToken);
        }

        private async Task DownloadAndApplyRngFixesAsync(
            bool applyBoyleFix,
            bool applyTimshFix,
            string tempRoot,
            string gameDirectoryPath,
            IProgress<string>? statusProgress,
            IProgress<double?>? downloadProgress,
            CancellationToken cancellationToken)
        {
            statusProgress?.Report("Downloading Dishonored RNG fix payload...");
            string extractRoot = await DownloadAndExtractAsync(RngFixUrl, tempRoot, "rngfix", statusProgress, downloadProgress, cancellationToken);

            if (applyBoyleFix)
            {
                statusProgress?.Report("Applying Boyle RNG fix...");
                CopyRelativeFileFromPayload(extractRoot, BoyleFixRelativePath, gameDirectoryPath);
            }

            if (applyTimshFix)
            {
                statusProgress?.Report("Applying Timsh RNG fix...");
                CopyRelativeFileFromPayload(extractRoot, TimshFixRelativePath, gameDirectoryPath);
            }
        }

        private async Task DownloadExtractAndOverlayAsync(
            string url,
            string tempRoot,
            string gameDirectoryPath,
            IProgress<string>? statusProgress,
            IProgress<double?>? downloadProgress,
            CancellationToken cancellationToken)
        {
            string folderName = Guid.NewGuid().ToString("N");
            string extractRoot = await DownloadAndExtractAsync(url, tempRoot, folderName, statusProgress, downloadProgress, cancellationToken);
            statusProgress?.Report("Applying extracted files...");
            CopyDirectoryContents(extractRoot, gameDirectoryPath);
        }

        private async Task<string> DownloadAndExtractAsync(
            string url,
            string tempRoot,
            string folderName,
            IProgress<string>? statusProgress,
            IProgress<double?>? downloadProgress,
            CancellationToken cancellationToken)
        {
            string zipPath = Path.Combine(tempRoot, folderName + ".zip");
            string extractPath = Path.Combine(tempRoot, folderName);
            Directory.CreateDirectory(extractPath);

            await DownloadZipAsync(url, zipPath, downloadProgress, cancellationToken);

            statusProgress?.Report("Extracting downloaded payload...");
            ExtractZipSafely(zipPath, extractPath);
            File.Delete(zipPath);

            return ResolvePayloadRoot(extractPath);
        }

        private async Task DownloadZipAsync(
            string url,
            string destinationPath,
            IProgress<double?>? downloadProgress,
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;
            await using Stream sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using FileStream destinationStream = new(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

            byte[] buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;
            while ((bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;

                if (totalBytes.HasValue && totalBytes.Value > 0)
                {
                    downloadProgress?.Report((double)totalRead / totalBytes.Value * 100d);
                }
                else
                {
                    downloadProgress?.Report(null);
                }
            }
        }

        private static string ResolvePayloadRoot(string extractedPath)
        {
            if (ContainsGameRootLayout(extractedPath))
            {
                return extractedPath;
            }

            // some payloads are wrapped in one extra top-level folder,
            // find the first nested folder that exposes gameroot layout
            foreach (string directory in Directory.GetDirectories(extractedPath, "*", SearchOption.AllDirectories))
            {
                if (ContainsGameRootLayout(directory))
                {
                    return directory;
                }
            }

            return extractedPath;
        }

        private static bool ContainsGameRootLayout(string path)
        {
            return
                Directory.Exists(Path.Combine(path, "Binaries")) ||
                Directory.Exists(Path.Combine(path, "DishonoredGame"));
        }

        private static void ExtractZipSafely(string zipPath, string destinationDirectory)
        {
            using System.IO.Compression.ZipArchive archive = System.IO.Compression.ZipFile.OpenRead(zipPath);
            foreach (System.IO.Compression.ZipArchiveEntry entry in archive.Entries)
            {
                string normalisedEntryPath = entry.FullName.Replace('\\', '/');
                if (string.IsNullOrWhiteSpace(normalisedEntryPath))
                {
                    continue;
                }

                string targetPath = Path.GetFullPath(Path.Combine(destinationDirectory, normalisedEntryPath));
                string fullDestinationPath = Path.GetFullPath(destinationDirectory + Path.DirectorySeparatorChar);

                if (!targetPath.StartsWith(fullDestinationPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Zip contains an invalid path entry.");
                }

                if (normalisedEntryPath.EndsWith("/", StringComparison.Ordinal))
                {
                    Directory.CreateDirectory(targetPath);
                    continue;
                }

                string? targetDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                using Stream source = entry.Open();
                using FileStream destination = new(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
                source.CopyTo(destination);
            }
        }

        private static void CopyDirectoryContents(string sourceRoot, string destinationRoot)
        {
            foreach (string file in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(sourceRoot, file);
                string destinationPath = Path.Combine(destinationRoot, relativePath);
                CopyFileWithOverwrite(file, destinationPath);
            }
        }

        private static void CopyRelativeFileFromPayload(string payloadRoot, string relativePath, string destinationRoot)
        {
            string normalisedRelative = relativePath.Replace('\\', '/');
            string directCandidate = Path.Combine(payloadRoot, normalisedRelative.Replace('/', Path.DirectorySeparatorChar));
            string? sourcePath = File.Exists(directCandidate) ? directCandidate : null;

            if (sourcePath == null)
            {
                string fileName = Path.GetFileName(normalisedRelative);
                string[] candidates = Directory.GetFiles(payloadRoot, fileName, SearchOption.AllDirectories);
                sourcePath = candidates.FirstOrDefault(path =>
                    path.Replace('\\', '/').EndsWith(normalisedRelative, StringComparison.OrdinalIgnoreCase));
            }

            if (sourcePath == null)
            {
                throw new FileNotFoundException($"Could not find required patch file '{relativePath}' in payload.");
            }

            string destinationPath = Path.Combine(destinationRoot, normalisedRelative.Replace('/', Path.DirectorySeparatorChar));
            CopyFileWithOverwrite(sourcePath, destinationPath);
        }

        private static void CopyFileWithOverwrite(string sourcePath, string destinationPath)
        {
            string? targetDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            if (File.Exists(destinationPath))
            {
                FileInfo destination = new(destinationPath)
                {
                    IsReadOnly = false
                };
            }

            File.Copy(sourcePath, destinationPath, true);
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                        }
                        catch
                        {
                        }
                    }

                    Directory.Delete(path, true);
                }
            }
            catch
            {
            }
        }

        private static void ValidateOptions(PatchApplyOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.GameDirectoryPath))
            {
                throw new InvalidOperationException("Game directory is not set.");
            }

            if (!Directory.Exists(options.GameDirectoryPath))
            {
                throw new DirectoryNotFoundException("Game directory not found.");
            }

            if (options.PatchVersion != PatchTarget12Base &&
                options.PatchVersion != PatchTarget13Kod &&
                options.PatchVersion != PatchTarget14Brigmore &&
                options.PatchVersion != PatchTarget14Daud &&
                options.PatchVersion != PatchTarget15Latest)
            {
                throw new InvalidOperationException("Unsupported patch version.");
            }
        }
    }
}

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DishonoredTweaks.Services
{
    public sealed class BackupService
    {
        private readonly string _backupRoot;

        public BackupService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _backupRoot = Path.Combine(appData, "DishonoredTweaks", "Backups");
        }

        public string GetBackupPath(string gameDirectoryPath)
        {
            string hash = ComputeStableHash(gameDirectoryPath);
            return Path.Combine(_backupRoot, hash, "Baseline");
        }

        public bool BackupExists(string gameDirectoryPath)
        {
            string backupPath = GetBackupPath(gameDirectoryPath);
            string exePath = Path.Combine(backupPath, "Binaries", "Win32", "Dishonored.exe");
            return Directory.Exists(backupPath) && File.Exists(exePath);
        }

        public async Task EnsureBackupAsync(
            string gameDirectoryPath,
            IProgress<string>? statusProgress = null,
            CancellationToken cancellationToken = default)
        {
            if (BackupExists(gameDirectoryPath))
            {
                statusProgress?.Report("Baseline backup found.");
                return;
            }

            string backupPath = GetBackupPath(gameDirectoryPath);
            statusProgress?.Report("Creating baseline backup...");

            await Task.Run(() =>
            {
                if (Directory.Exists(backupPath))
                {
                    ForceDeleteDirectory(backupPath);
                }

                Directory.CreateDirectory(backupPath);
                CopyDirectory(gameDirectoryPath, backupPath, statusProgress, cancellationToken);
            }, cancellationToken);

            statusProgress?.Report("Baseline backup created.");
        }

        public async Task RestoreBackupAsync(
            string gameDirectoryPath,
            IProgress<string>? statusProgress = null,
            CancellationToken cancellationToken = default)
        {
            string backupPath = GetBackupPath(gameDirectoryPath);
            if (!Directory.Exists(backupPath))
            {
                throw new InvalidOperationException("No baseline backup exists yet.");
            }

            statusProgress?.Report("Restoring baseline backup...");
            await Task.Run(() =>
            {
                DeleteDirectoryContents(gameDirectoryPath, cancellationToken);
                CopyDirectory(backupPath, gameDirectoryPath, statusProgress, cancellationToken);
            }, cancellationToken);

            statusProgress?.Report("Baseline backup restored.");
        }

        private static void CopyDirectory(
            string sourceDirectory,
            string destinationDirectory,
            IProgress<string>? statusProgress,
            CancellationToken cancellationToken)
        {
            string[] allDirectories = Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories);
            foreach (string directory in allDirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string relativeDirectory = Path.GetRelativePath(sourceDirectory, directory);
                Directory.CreateDirectory(Path.Combine(destinationDirectory, relativeDirectory));
            }

            string[] allFiles = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
            int copiedFiles = 0;
            foreach (string file in allFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string relativeFile = Path.GetRelativePath(sourceDirectory, file);
                string destinationFile = Path.Combine(destinationDirectory, relativeFile);
                string? parent = Path.GetDirectoryName(destinationFile);
                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                File.Copy(file, destinationFile, true);
                copiedFiles++;

                if (copiedFiles % 250 == 0)
                {
                    statusProgress?.Report($"Copying files... {copiedFiles}");
                }
            }
        }

        private static void DeleteDirectoryContents(string directoryPath, CancellationToken cancellationToken)
        {
            DirectoryInfo root = new(directoryPath);
            if (!root.Exists)
            {
                return;
            }

            foreach (FileInfo file in root.GetFiles("*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                file.IsReadOnly = false;
                file.Delete();
            }

            DirectoryInfo[] directories = root.GetDirectories("*", SearchOption.AllDirectories);
            Array.Sort(directories, (a, b) => b.FullName.Length.CompareTo(a.FullName.Length));
            foreach (DirectoryInfo directory in directories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                directory.Attributes = FileAttributes.Directory;
                directory.Delete(true);
            }
        }

        private static void ForceDeleteDirectory(string directoryPath)
        {
            DirectoryInfo root = new(directoryPath);
            if (!root.Exists)
            {
                return;
            }

            foreach (FileInfo file in root.GetFiles("*", SearchOption.AllDirectories))
            {
                file.IsReadOnly = false;
            }

            root.Delete(true);
        }

        private static string ComputeStableHash(string value)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()));
            StringBuilder builder = new();
            foreach (byte hashByte in hashBytes)
            {
                builder.Append(hashByte.ToString("x2"));
            }

            return builder.ToString()[..16];
        }
    }
}

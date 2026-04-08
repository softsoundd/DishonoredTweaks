using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DishonoredTweaks.Services
{
    public enum DetectedExecutableVersion
    {
        Unknown,
        V12BaseGame,
        V13KnifeOfDunwall,
        V14BrigmoreWitches,
        V14DaudHonored,
        V15Latest,
        V14OrV15Dlc07Family
    }

    public sealed class ExecutableDetectionResult
    {
        public DetectedExecutableVersion Version { get; set; } = DetectedExecutableVersion.Unknown;
        public string Confidence { get; set; } = "unknown";
        public string Reason { get; set; } = "No detection data.";
    }

    public sealed class ExecutableDetectionService
    {
        private sealed class VersionProfile
        {
            public DetectedExecutableVersion Version { get; set; } = DetectedExecutableVersion.Unknown;
            public string Label { get; set; } = string.Empty;
            public long FileSize { get; set; }
            public uint EntryPoint { get; set; }
            public uint SizeOfImage { get; set; }
            public uint TextVirtualSize { get; set; }
        }

        private sealed class PeFingerprint
        {
            public long FileSize { get; set; }
            public uint EntryPoint { get; set; }
            public uint SizeOfImage { get; set; }
            public uint? TextVirtualSize { get; set; }
        }

        // Dishonored.exe signatures
        private const string Hash12Full = "3A2874F164C3B19534F4D2947C2B61DC76E184565FCBFD8D2A7FCCABB734999C";
        private const string Hash13Full = "8CFE1EB123AC0D0CBC996A6549C611C8F6177950D84B781EDFB55C77495B7404";
        private const string Hash14Full = "AB5DA1CEEDEA46D3558583502A6B15DA97739DCBD237F3E6BD8276AB6CA8DF0D";
        private const string Hash15Full = "66443F3D68A6C658B0ED943260C3EB2F4CC9E3400D5809A94D6EC41EB725E17E";

        // marker checks from embedded build paths
        private const string Marker12 = "UnrealEngine3DLC\\Development\\Src\\Core\\Src\\Core.cpp";
        private const string Marker13 = "UnrealEngine3DLC06\\Development\\Src\\Core\\Src\\Core.cpp";
        private const string Marker14Family = "UnrealEngine3DLC07\\Development\\Src\\Core\\Src\\Core.cpp";
        private const string Marker15Import = "libcurl.dll";
        private const string DaudMarkerRelativePath = "DishonoredGame\\DLC\\PCConsole\\DLC06\\DishonoredUI.ini";
        private const string DaudMarkerText = "DLC06_Daudhonored_P";

        // PE fingerprint scoring
        private static readonly List<VersionProfile> VersionProfiles = new()
        {
            new VersionProfile
            {
                Version = DetectedExecutableVersion.V12BaseGame,
                Label = "1.2 - Base Game",
                FileSize = 17372160,
                EntryPoint = 0x00A7D197,
                SizeOfImage = 0x01160000,
                TextVirtualSize = 11833279
            },
            new VersionProfile
            {
                Version = DetectedExecutableVersion.V13KnifeOfDunwall,
                Label = "1.3 - Knife of Dunwall",
                FileSize = 17680176,
                EntryPoint = 0x00A9D3A7,
                SizeOfImage = 0x011AB000,
                TextVirtualSize = 11968895
            },
            new VersionProfile
            {
                Version = DetectedExecutableVersion.V14BrigmoreWitches,
                Label = "1.4 - Brigmore Witches",
                FileSize = 18014512,
                EntryPoint = 0x00AC3B27,
                SizeOfImage = 0x011FD000,
                TextVirtualSize = 12130543
            },
            new VersionProfile
            {
                Version = DetectedExecutableVersion.V15Latest,
                Label = "1.5 - Latest",
                FileSize = 18041856,
                EntryPoint = 0x00AC4673,
                SizeOfImage = 0x01207000,
                TextVirtualSize = 12133407
            }
        };

        public ExecutableDetectionResult Detect(string? gameDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(gameDirectoryPath))
            {
                return new ExecutableDetectionResult
                {
                    Version = DetectedExecutableVersion.Unknown,
                    Confidence = "none",
                    Reason = "Game directory is not set."
                };
            }

            string exePath = Path.Combine(gameDirectoryPath, "Binaries", "Win32", "Dishonored.exe");
            if (!File.Exists(exePath))
            {
                return new ExecutableDetectionResult
                {
                    Version = DetectedExecutableVersion.Unknown,
                    Confidence = "none",
                    Reason = "Dishonored.exe not found."
                };
            }

            try
            {
                bool hasDaudMarker = HasDaudMarker(gameDirectoryPath);
                byte[] bytes = File.ReadAllBytes(exePath);
                string fullHash = ComputeSha256(bytes, bytes.Length);
                if (fullHash.Equals(Hash12Full, StringComparison.OrdinalIgnoreCase))
                {
                    return ExactMatch(DetectedExecutableVersion.V12BaseGame, "Exact full hash matches 1.2 base game executable.");
                }

                if (fullHash.Equals(Hash13Full, StringComparison.OrdinalIgnoreCase))
                {
                    return ExactMatch(DetectedExecutableVersion.V13KnifeOfDunwall, "Exact full hash matches 1.3 Knife of Dunwall executable.");
                }

                if (fullHash.Equals(Hash14Full, StringComparison.OrdinalIgnoreCase))
                {
                    return ExactMatch(
                        hasDaudMarker ? DetectedExecutableVersion.V14DaudHonored : DetectedExecutableVersion.V14BrigmoreWitches,
                        hasDaudMarker
                            ? "Executable matches 1.4 DLC07 branch and DaudHonored patch marker was found."
                            : "Exact full hash matches 1.4 Brigmore Witches executable.");
                }

                if (fullHash.Equals(Hash15Full, StringComparison.OrdinalIgnoreCase))
                {
                    return ExactMatch(DetectedExecutableVersion.V15Latest, "Exact full hash matches 1.5 latest executable.");
                }

                bool hasMarker12 = ContainsAscii(bytes, Marker12);
                bool hasMarker13 = ContainsAscii(bytes, Marker13);
                bool hasMarker14Family = ContainsAscii(bytes, Marker14Family);
                bool hasLibcurl = ContainsAscii(bytes, Marker15Import);

                if (hasLibcurl && hasMarker14Family)
                {
                    return new ExecutableDetectionResult
                    {
                        Version = DetectedExecutableVersion.V15Latest,
                        Confidence = "high",
                        Reason = "RE markers indicate DLC07 branch plus libcurl import (1.5 latest)."
                    };
                }

                if (hasLibcurl)
                {
                    return new ExecutableDetectionResult
                    {
                        Version = DetectedExecutableVersion.V15Latest,
                        Confidence = "high",
                        Reason = "Found libcurl import marker used by latest Steamless executable."
                    };
                }

                if (hasMarker13)
                {
                    return new ExecutableDetectionResult
                    {
                        Version = DetectedExecutableVersion.V13KnifeOfDunwall,
                        Confidence = "medium",
                        Reason = "RE marker string indicates DLC06 (1.3 Knife of Dunwall family)."
                    };
                }

                if (hasMarker14Family)
                {
                    if (TryReadPeFingerprint(bytes, out PeFingerprint fingerprintFromMarker) &&
                        TryScoreProfiles(fingerprintFromMarker, out VersionProfile markerProfile, out int markerScore, out int markerScoreGap) &&
                        (markerProfile.Version == DetectedExecutableVersion.V14BrigmoreWitches || markerProfile.Version == DetectedExecutableVersion.V15Latest) &&
                        markerScore >= 10)
                    {
                        DetectedExecutableVersion resolvedMarkerVersion =
                            hasDaudMarker && markerProfile.Version == DetectedExecutableVersion.V14BrigmoreWitches
                                ? DetectedExecutableVersion.V14DaudHonored
                                : markerProfile.Version;

                        return new ExecutableDetectionResult
                        {
                            Version = resolvedMarkerVersion,
                            Confidence = markerScore >= 15 && markerScoreGap >= 2 ? "high" : "medium",
                            Reason = hasDaudMarker && markerProfile.Version == DetectedExecutableVersion.V14BrigmoreWitches
                                ? $"DLC07 marker and DaudHonored patch marker found; PE profile matches 1.4 branch (score {markerScore})."
                                : $"DLC07 marker found and PE profile best matches {markerProfile.Label} (score {markerScore})."
                        };
                    }

                    if (hasDaudMarker)
                    {
                        return new ExecutableDetectionResult
                        {
                            Version = DetectedExecutableVersion.V14DaudHonored,
                            Confidence = "medium",
                            Reason = "DaudHonored patch marker found in DLC06 UI configuration."
                        };
                    }

                    return new ExecutableDetectionResult
                    {
                        Version = DetectedExecutableVersion.V14OrV15Dlc07Family,
                        Confidence = "medium",
                        Reason = "RE marker string indicates DLC07 (1.4 Brigmore / 1.5 latest family)."
                    };
                }

                if (hasMarker12)
                {
                    return new ExecutableDetectionResult
                    {
                        Version = DetectedExecutableVersion.V12BaseGame,
                        Confidence = "medium",
                        Reason = "RE marker string indicates DLC base-game branch (1.2 family)."
                    };
                }

                if (TryReadPeFingerprint(bytes, out PeFingerprint fingerprint) &&
                    TryScoreProfiles(fingerprint, out VersionProfile profile, out int score, out int scoreGap))
                {
                    DetectedExecutableVersion resolvedProfileVersion =
                        hasDaudMarker && profile.Version == DetectedExecutableVersion.V14BrigmoreWitches
                            ? DetectedExecutableVersion.V14DaudHonored
                            : profile.Version;

                    if (score >= 15 && scoreGap >= 2)
                    {
                        return new ExecutableDetectionResult
                        {
                            Version = resolvedProfileVersion,
                            Confidence = "high",
                            Reason = hasDaudMarker && profile.Version == DetectedExecutableVersion.V14BrigmoreWitches
                                ? $"DaudHonored patch marker found and PE structure strongly matches the 1.4 branch (profile score {score})."
                                : $"PE structure strongly matches {profile.Label} (profile score {score})."
                        };
                    }

                    if (score >= 10)
                    {
                        return new ExecutableDetectionResult
                        {
                            Version = resolvedProfileVersion,
                            Confidence = "medium",
                            Reason = hasDaudMarker && profile.Version == DetectedExecutableVersion.V14BrigmoreWitches
                                ? $"DaudHonored patch marker found and PE structure matches the 1.4 branch (profile score {score})."
                                : $"PE structure is closest to {profile.Label} (profile score {score})."
                        };
                    }
                }

                if (hasDaudMarker)
                {
                    return new ExecutableDetectionResult
                    {
                        Version = DetectedExecutableVersion.V14DaudHonored,
                        Confidence = "medium",
                        Reason = "DaudHonored patch marker found in DLC06 UI configuration."
                    };
                }
            }
            catch (Exception ex)
            {
                return new ExecutableDetectionResult
                {
                    Version = DetectedExecutableVersion.Unknown,
                    Confidence = "none",
                    Reason = $"Executable analysis failed ({ex.Message})."
                };
            }

            return new ExecutableDetectionResult
            {
                Version = DetectedExecutableVersion.Unknown,
                Confidence = "low",
                Reason = "No known hash or RE marker matched this executable."
            };
        }

        private static ExecutableDetectionResult ExactMatch(DetectedExecutableVersion version, string reason)
        {
            return new ExecutableDetectionResult
            {
                Version = version,
                Confidence = "high",
                Reason = reason
            };
        }

        private static string ComputeSha256(byte[] data, int length)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(data, 0, length);
            StringBuilder builder = new();
            foreach (byte b in hash)
            {
                builder.Append(b.ToString("X2"));
            }

            return builder.ToString();
        }

        private static bool ContainsAscii(byte[] haystack, string needleText)
        {
            byte[] needle = Encoding.ASCII.GetBytes(needleText);
            return haystack.AsSpan().IndexOf(needle) >= 0;
        }

        private static bool HasDaudMarker(string gameDirectoryPath)
        {
            string markerPath = Path.Combine(gameDirectoryPath, DaudMarkerRelativePath);
            if (!File.Exists(markerPath))
            {
                return false;
            }

            try
            {
                string markerContents = File.ReadAllText(markerPath);
                return markerContents.IndexOf(DaudMarkerText, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryReadPeFingerprint(byte[] bytes, out PeFingerprint fingerprint)
        {
            fingerprint = new PeFingerprint();
            if (bytes.Length < 0x100)
            {
                return false;
            }

            int peOffset = ReadInt32(bytes, 0x3C);
            if (peOffset <= 0 || peOffset + 0xF8 >= bytes.Length)
            {
                return false;
            }

            if (bytes[peOffset] != 0x50 || bytes[peOffset + 1] != 0x45 || bytes[peOffset + 2] != 0x00 || bytes[peOffset + 3] != 0x00)
            {
                return false;
            }

            int fileHeaderOffset = peOffset + 4;
            ushort numberOfSections = ReadUInt16(bytes, fileHeaderOffset + 2);
            ushort optionalHeaderSize = ReadUInt16(bytes, fileHeaderOffset + 16);
            int optionalHeaderOffset = fileHeaderOffset + 20;
            if (optionalHeaderOffset + optionalHeaderSize > bytes.Length)
            {
                return false;
            }

            uint entryPoint = ReadUInt32(bytes, optionalHeaderOffset + 16);
            uint sizeOfImage = ReadUInt32(bytes, optionalHeaderOffset + 56);

            int sectionTableOffset = optionalHeaderOffset + optionalHeaderSize;
            uint? textVirtualSize = null;
            for (int i = 0; i < numberOfSections; i++)
            {
                int sectionOffset = sectionTableOffset + (40 * i);
                if (sectionOffset + 40 > bytes.Length)
                {
                    break;
                }

                string name = ReadSectionName(bytes, sectionOffset);
                if (name.Equals(".text", StringComparison.OrdinalIgnoreCase))
                {
                    textVirtualSize = ReadUInt32(bytes, sectionOffset + 8);
                    break;
                }
            }

            fingerprint.FileSize = bytes.LongLength;
            fingerprint.EntryPoint = entryPoint;
            fingerprint.SizeOfImage = sizeOfImage;
            fingerprint.TextVirtualSize = textVirtualSize;

            return true;
        }

        private static bool TryScoreProfiles(
            PeFingerprint fingerprint,
            out VersionProfile bestProfile,
            out int bestScore,
            out int scoreGap)
        {
            bestProfile = new VersionProfile();
            bestScore = int.MinValue;
            int secondBest = int.MinValue;

            foreach (VersionProfile profile in VersionProfiles)
            {
                int score = ScoreProfile(fingerprint, profile);
                if (score > bestScore)
                {
                    secondBest = bestScore;
                    bestScore = score;
                    bestProfile = profile;
                }
                else if (score > secondBest)
                {
                    secondBest = score;
                }
            }

            scoreGap = bestScore - secondBest;
            return bestScore > int.MinValue / 2;
        }

        private static int ScoreProfile(PeFingerprint fingerprint, VersionProfile profile)
        {
            int score = 0;

            long fileSizeDelta = Math.Abs(fingerprint.FileSize - profile.FileSize);
            if (fileSizeDelta <= 4096) score += 5;
            else if (fileSizeDelta <= 65536) score += 4;
            else if (fileSizeDelta <= 262144) score += 2;

            long entryDelta = Math.Abs((long)fingerprint.EntryPoint - profile.EntryPoint);
            if (entryDelta <= 0x200) score += 5;
            else if (entryDelta <= 0x2000) score += 4;
            else if (entryDelta <= 0x8000) score += 2;

            long imageDelta = Math.Abs((long)fingerprint.SizeOfImage - profile.SizeOfImage);
            if (imageDelta <= 0x2000) score += 4;
            else if (imageDelta <= 0x10000) score += 3;
            else if (imageDelta <= 0x40000) score += 2;

            if (fingerprint.TextVirtualSize.HasValue)
            {
                long textDelta = Math.Abs((long)fingerprint.TextVirtualSize.Value - profile.TextVirtualSize);
                if (textDelta <= 0x200) score += 4;
                else if (textDelta <= 0x2000) score += 3;
                else if (textDelta <= 0x8000) score += 2;
            }

            return score;
        }

        private static ushort ReadUInt16(byte[] bytes, int offset)
        {
            if (offset < 0 || offset + 2 > bytes.Length)
            {
                return 0;
            }

            return (ushort)(bytes[offset] | (bytes[offset + 1] << 8));
        }

        private static uint ReadUInt32(byte[] bytes, int offset)
        {
            if (offset < 0 || offset + 4 > bytes.Length)
            {
                return 0;
            }

            return (uint)(bytes[offset]
                | (bytes[offset + 1] << 8)
                | (bytes[offset + 2] << 16)
                | (bytes[offset + 3] << 24));
        }

        private static int ReadInt32(byte[] bytes, int offset)
        {
            return unchecked((int)ReadUInt32(bytes, offset));
        }

        private static string ReadSectionName(byte[] bytes, int offset)
        {
            int end = offset;
            int max = Math.Min(offset + 8, bytes.Length);
            while (end < max && bytes[end] != 0)
            {
                end++;
            }

            return Encoding.ASCII.GetString(bytes, offset, end - offset);
        }
    }
}

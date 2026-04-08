using System.Collections.Generic;
namespace DishonoredTweaks.Models
{
    public sealed class DishonoredConfiguration
    {
        public string? GameDirectoryPath { get; set; }
        public string? EngineIniPath { get; set; }
        public string? InputIniPath { get; set; }
    }

    public sealed class FpsBindingEntry
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public sealed class InputBindingOptions
    {
        public string JumpScrollDirection { get; set; } = "None";
        public string InteractScrollDirection { get; set; } = "None";
        public List<FpsBindingEntry> FpsBindings { get; set; } = new();
        public string ConsoleKey { get; set; } = string.Empty;
        public string CheatKey { get; set; } = string.Empty;
    }

    public sealed class AppSettings
    {
        public string? LastGameDirectoryPath { get; set; }
        public string UiLanguage { get; set; } = "en";

        public string PatchVersion { get; set; } = "1.5-latest";

        public bool BoyleFixEnabled { get; set; }
        public bool TimshFixEnabled { get; set; }

        public bool SmoothFrameRateEnabled { get; set; } = true;
        public bool DynamicLightsEnabled { get; set; }
        public bool DynamicShadowsEnabled { get; set; }
        public bool LightShaftsEnabled { get; set; }
        public bool SkipStartupMoviesEnabled { get; set; } = true;
        public string MaxSmoothedFrameRate { get; set; } = "250";
        public bool PauseOnLossFocusEnabled { get; set; }

        public string JumpScrollDirection { get; set; } = "None";
        public string InteractScrollDirection { get; set; } = "None";

        public string FpsKey1 { get; set; } = "F2";
        public string FpsValue1 { get; set; } = "5";
        public string FpsKey2 { get; set; } = "F3";
        public string FpsValue2 { get; set; } = "60";
        public string FpsKey3 { get; set; } = "F4";
        public string FpsValue3 { get; set; } = "250";
        public string FpsKey4 { get; set; } = "F7";
        public string FpsValue4 { get; set; } = "2.46";

        public string ConsoleKey { get; set; } = "Tilde";
        public string CheatKey { get; set; } = "F6";
        public string MacroDirectoryPath { get; set; } = string.Empty;
    }
}

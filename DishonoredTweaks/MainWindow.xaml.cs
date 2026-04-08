using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DishonoredTweaks.Helpers;
using DishonoredTweaks.Models;
using DishonoredTweaks.Services;

namespace DishonoredTweaks
{
    public partial class MainWindow : Window
    {
        private readonly DishonoredConfiguration _config = new();
        private readonly GameDetectionService _gameDetectionService = new();
        private readonly BackupService _backupService = new();
        private readonly PatchDownloadService _patchDownloadService = new();
        private readonly IniEditorService _iniEditorService = new();
        private readonly InputBindingsService _inputBindingsService = new();
        private readonly AppSettingsService _appSettingsService = new();
        private readonly ExecutableDetectionService _executableDetectionService = new();
        private readonly Dish2MacroService _dish2MacroService = new();
        private readonly ConfigBaselineService _configBaselineService = new();
        private readonly LocalisationService _localisationService = new();

        private bool _isUpdatingScrollCombos;
        private bool _isLoadingSettings;
        private bool _isRefreshingIniSettings;
        private bool _isRefreshingMacroConfig;
        private bool _windowReady;
        private string _lastEngineToggleAutoApplySignature = string.Empty;
        private string _lastInputAutoApplySignature = string.Empty;
        private string _lastMacroConfigAutoApplySignature = string.Empty;
        private AppSettings _settings = new();
        private string _uiLanguage = LocalisationService.EnglishLanguageCode;

        private static readonly Dictionary<string, string> Ue3KeyMap = new(StringComparer.Ordinal)
        {
            { "A", "A" }, { "B", "B" }, { "C", "C" }, { "D", "D" }, { "E", "E" }, { "F", "F" },
            { "G", "G" }, { "H", "H" }, { "I", "I" }, { "J", "J" }, { "K", "K" }, { "L", "L" },
            { "M", "M" }, { "N", "N" }, { "O", "O" }, { "P", "P" }, { "Q", "Q" }, { "R", "R" },
            { "S", "S" }, { "T", "T" }, { "U", "U" }, { "V", "V" }, { "W", "W" }, { "X", "X" },
            { "Y", "Y" }, { "Z", "Z" },
            { "D0", "Zero" }, { "D1", "One" }, { "D2", "Two" }, { "D3", "Three" }, { "D4", "Four" },
            { "D5", "Five" }, { "D6", "Six" }, { "D7", "Seven" }, { "D8", "Eight" }, { "D9", "Nine" },
            { "NumPad0", "NumPadZero" }, { "NumPad1", "NumPadOne" }, { "NumPad2", "NumPadTwo" },
            { "NumPad3", "NumPadThree" }, { "NumPad4", "NumPadFour" }, { "NumPad5", "NumPadFive" },
            { "NumPad6", "NumPadSix" }, { "NumPad7", "NumPadSeven" }, { "NumPad8", "NumPadEight" },
            { "NumPad9", "NumPadNine" },
            { "F1", "F1" }, { "F2", "F2" }, { "F3", "F3" }, { "F4", "F4" }, { "F5", "F5" }, { "F6", "F6" },
            { "F7", "F7" }, { "F8", "F8" }, { "F9", "F9" }, { "F10", "F10" }, { "F11", "F11" }, { "F12", "F12" },
            { "Escape", "Escape" }, { "Tab", "Tab" }, { "Space", "SpaceBar" }, { "Enter", "Enter" },
            { "Return", "Enter" }, { "Back", "BackSpace" }, { "Delete", "Delete" }, { "Insert", "Insert" },
            { "Home", "Home" }, { "End", "End" }, { "PageUp", "PageUp" }, { "PageDown", "PageDown" },
            { "Left", "Left" }, { "Right", "Right" }, { "Up", "Up" }, { "Down", "Down" },
            { "Oem3", "Tilde" }, { "OemMinus", "Underscore" }, { "OemPlus", "Equals" },
            { "OemOpenBrackets", "LeftBracket" }, { "OemCloseBrackets", "RightBracket" },
            { "Oem5", "Backslash" }, { "Oem1", "Semicolon" }, { "OemQuotes", "Quote" },
            { "OemComma", "Comma" }, { "OemPeriod", "Period" }, { "Oem2", "Slash" },
            { "Multiply", "Multiply" }, { "Add", "Add" }, { "Subtract", "Subtract" }, { "Divide", "Divide" },
            { "Decimal", "Decimal" }
        };

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitialiseDefaults();
            LoadPersistedSettings();
            ApplyLocalisation();
            SyncAutoApplySignatures();
            _windowReady = true;
            UpdatePatchVersionUiState();
            RefreshGameAndConfigStatus();
            RefreshExecutableDetectionState();
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }

        private void InitialiseDefaults()
        {
            SetComboSelectionByContent(LanguageComboBox, LocalisationService.EnglishLanguageCode);
            SetComboSelectionByContent(PatchVersionComboBox, PatchDownloadService.PatchTarget15Latest);
            SetComboSelectionByContent(JumpScrollComboBox, "None");
            SetComboSelectionByContent(InteractScrollComboBox, "None");
            SetDefaultFpsRows();
            ConsoleKeyTextBox.Text = "Tilde";
            CheatKeyTextBox.Text = "F6";
            MacroDirectoryTextBox.Text = _dish2MacroService.GetDefaultInstallDirectory();
            RefreshMacroStatus();
            LoadMacroConfigurationIntoUi(showDialogs: false);
            UpdatePatchVersionUiState();
        }

        private void LoadPersistedSettings()
        {
            _isLoadingSettings = true;
            try
            {
                _settings = _appSettingsService.Load();

                if (!string.IsNullOrWhiteSpace(_settings.LastGameDirectoryPath))
                {
                    _config.GameDirectoryPath = _settings.LastGameDirectoryPath;
                    GameDirectoryPathTextBlock.Text = _settings.LastGameDirectoryPath;
                }

                SetComboSelectionByContent(LanguageComboBox, _localisationService.NormaliseLanguage(_settings.UiLanguage));
                SetComboSelectionByContent(PatchVersionComboBox, NormalisePatchVersionSetting(_settings.PatchVersion));

                BoyleFixCheckBox.IsChecked = _settings.BoyleFixEnabled;
                TimshFixCheckBox.IsChecked = _settings.TimshFixEnabled;
                SmoothFrameRateCheckBox.IsChecked = _settings.SmoothFrameRateEnabled;
                DynamicLightsCheckBox.IsChecked = _settings.DynamicLightsEnabled;
                DynamicShadowsCheckBox.IsChecked = _settings.DynamicShadowsEnabled;
                LightShaftsCheckBox.IsChecked = _settings.LightShaftsEnabled;
                SkipStartupMoviesCheckBox.IsChecked = _settings.SkipStartupMoviesEnabled;
                MaxSmoothedFrameRateTextBox.Text = _settings.MaxSmoothedFrameRate;
                PauseOnLossFocusCheckBox.IsChecked = _settings.PauseOnLossFocusEnabled;

                SetComboSelectionByContent(JumpScrollComboBox, _settings.JumpScrollDirection);
                SetComboSelectionByContent(InteractScrollComboBox, _settings.InteractScrollDirection);

                FpsKey1TextBox.Text = _settings.FpsKey1;
                FpsValue1TextBox.Text = _settings.FpsValue1;
                FpsKey2TextBox.Text = _settings.FpsKey2;
                FpsValue2TextBox.Text = _settings.FpsValue2;
                FpsKey3TextBox.Text = _settings.FpsKey3;
                FpsValue3TextBox.Text = _settings.FpsValue3;
                FpsKey4TextBox.Text = _settings.FpsKey4;
                FpsValue4TextBox.Text = _settings.FpsValue4;

                ConsoleKeyTextBox.Text = _settings.ConsoleKey;
                CheatKeyTextBox.Text = _settings.CheatKey;
                MacroDirectoryTextBox.Text = string.IsNullOrWhiteSpace(_settings.MacroDirectoryPath)
                    ? _dish2MacroService.GetDefaultInstallDirectory()
                    : _settings.MacroDirectoryPath;
                RefreshMacroStatus();
                LoadMacroConfigurationIntoUi(showDialogs: false);
            }
            finally
            {
                _isLoadingSettings = false;
            }
        }

        private AppSettings BuildSettingsFromUi()
        {
            return new AppSettings
            {
                LastGameDirectoryPath = _config.GameDirectoryPath,
                UiLanguage = _localisationService.NormaliseLanguage(GetSelectedComboContent(LanguageComboBox, LocalisationService.EnglishLanguageCode)),

                PatchVersion = GetSelectedComboContent(PatchVersionComboBox, PatchDownloadService.PatchTarget15Latest),

                BoyleFixEnabled = BoyleFixCheckBox.IsChecked == true,
                TimshFixEnabled = TimshFixCheckBox.IsChecked == true,

                SmoothFrameRateEnabled = SmoothFrameRateCheckBox.IsChecked == true,
                DynamicLightsEnabled = DynamicLightsCheckBox.IsChecked == true,
                DynamicShadowsEnabled = DynamicShadowsCheckBox.IsChecked == true,
                LightShaftsEnabled = LightShaftsCheckBox.IsChecked == true,
                SkipStartupMoviesEnabled = SkipStartupMoviesCheckBox.IsChecked == true,
                MaxSmoothedFrameRate = MaxSmoothedFrameRateTextBox.Text,
                PauseOnLossFocusEnabled = PauseOnLossFocusCheckBox.IsChecked == true,

                JumpScrollDirection = GetSelectedComboContent(JumpScrollComboBox, "None"),
                InteractScrollDirection = GetSelectedComboContent(InteractScrollComboBox, "None"),

                FpsKey1 = FpsKey1TextBox.Text,
                FpsValue1 = FpsValue1TextBox.Text,
                FpsKey2 = FpsKey2TextBox.Text,
                FpsValue2 = FpsValue2TextBox.Text,
                FpsKey3 = FpsKey3TextBox.Text,
                FpsValue3 = FpsValue3TextBox.Text,
                FpsKey4 = FpsKey4TextBox.Text,
                FpsValue4 = FpsValue4TextBox.Text,

                ConsoleKey = ConsoleKeyTextBox.Text,
                CheatKey = CheatKeyTextBox.Text,
                MacroDirectoryPath = GetSelectedMacroDirectory(applyDefaultIfEmpty: false)
            };
        }

        private void SaveSettings()
        {
            if (_isLoadingSettings || !_windowReady)
            {
                return;
            }

            _settings = BuildSettingsFromUi();
            _appSettingsService.Save(_settings);
        }

        private string T(string key)
        {
            return _localisationService.GetText(_uiLanguage, key);
        }

        private void ApplyLocalisation()
        {
            _uiLanguage = _localisationService.NormaliseLanguage(GetSelectedComboContent(LanguageComboBox, LocalisationService.EnglishLanguageCode));

            Title = T("window.title");
            LanguageButton.ToolTip = T("label.language");
            DialogHelper.ConfirmationYesText = T("dialog.yes");
            DialogHelper.ConfirmationNoText = T("dialog.no");
            DialogHelper.OkText = T("dialog.ok");
            LanguageEnglishComboBoxItem.Content = T("language.english");
            LanguageRussianComboBoxItem.Content = T("language.russian");

            SelectGameDirectoryButtonTextBlock.Text = T("action.selectGameDirectory");
            OpenConfigFolderButtonTextBlock.Text = T("action.openConfigFolder");
            if (string.IsNullOrWhiteSpace(_config.GameDirectoryPath))
            {
                GameDirectoryPathTextBlock.Text = T("status.noDirectorySelected");
            }

            PatchTweaksTabHeaderTextBlock.Text = T("tab.patchTweaks");
            PatchSelectionHeaderTextBlock.Text = T("header.patchSelection");
            PatchVersionLabelTextBlock.Text = T("label.patchVersion");
            PatchVersion12ComboBoxItem.Content = T("patch.1.2");
            PatchVersion13ComboBoxItem.Content = T("patch.1.3");
            PatchVersion14BrigmoreComboBoxItem.Content = T("patch.1.4bw");
            PatchVersion14DaudComboBoxItem.Content = T("patch.1.4daud");
            PatchVersion15ComboBoxItem.Content = T("patch.1.5");
            ExtraPatchesHeaderTextBlock.Text = T("header.extraPatches");
            BoyleFixCheckBox.Content = T("checkbox.boyle");
            TimshFixCheckBox.Content = T("checkbox.timsh");
            ApplySelectedPatchesButtonTextBlock.Text = T("action.applySelectedPatches");
            RestoreBackupButtonTextBlock.Text = T("action.restoreBackup");

            GameTweaksTabHeaderTextBlock.Text = T("tab.gameTweaks");
            EngineTweaksHeaderTextBlock.Text = T("header.engineTweaks");
            FramerateControlHeaderTextBlock.Text = T("header.framerateControl");
            SmoothFrameRateCheckBox.Content = T("checkbox.framerateLimiter");
            FramerateLabelTextBlock.Text = T("label.framerate");
            ApplyFpsButton.Content = T("action.apply");
            DynamicLightsCheckBox.Content = T("checkbox.dynamicLights");
            DynamicShadowsCheckBox.Content = T("checkbox.dynamicShadows");
            LightShaftsCheckBox.Content = T("checkbox.lightShafts");
            SkipStartupMoviesCheckBox.Content = T("checkbox.skipStartupMovies");
            PauseOnLossFocusCheckBox.Content = T("checkbox.pauseWhenUnfocused");
            MaterialDesignThemes.Wpf.HintAssist.SetHint(MaxSmoothedFrameRateTextBox, T("text.fpsHint"));

            InputTweaksTabHeaderTextBlock.Text = T("tab.inputTweaks");
            ScrollWheelHeaderTextBlock.Text = T("header.scrollWheel");
            JumpLabelTextBlock.Text = T("label.jump");
            JumpNoneComboBoxItem.Content = T("choice.none");
            JumpScrollUpComboBoxItem.Content = T("choice.scrollUp");
            JumpScrollDownComboBoxItem.Content = T("choice.scrollDown");
            InteractLabelTextBlock.Text = T("label.interact");
            InteractNoneComboBoxItem.Content = T("choice.none");
            InteractScrollUpComboBoxItem.Content = T("choice.scrollUp");
            InteractScrollDownComboBoxItem.Content = T("choice.scrollDown");
            FpsKeysHeaderTextBlock.Text = T("header.fpsKeys");
            FpsKeysDescriptionTextBlock.Text = T("text.fpsKeysDescription");
            FpsKeysKeyHeaderTextBlock.Text = T("text.key");
            FpsKeysValueHeaderTextBlock.Text = T("text.fps");
            Bind1LabelTextBlock.Text = T("label.bind1");
            Bind2LabelTextBlock.Text = T("label.bind2");
            Bind3LabelTextBlock.Text = T("label.bind3");
            Bind4LabelTextBlock.Text = T("label.bind4");
            ConsoleCheatsHeaderTextBlock.Text = T("header.consoleCheats");
            ConsoleBindKeyLabelTextBlock.Text = T("label.consoleBindKey");
            EnableCheatsKeyLabelTextBlock.Text = T("label.enableCheatsKey");
            Dish2MacroHeaderTextBlock.Text = T("header.dish2Macro");
            MacroDirectoryLabelTextBlock.Text = T("label.macroDirectory");
            BrowseMacroButton.Content = T("action.browse");
            MacroSettingsHeaderTextBlock.Text = T("header.macroSettings");
            MacroDownBindLabelTextBlock.Text = T("label.macroDownBind");
            MacroUpBindLabelTextBlock.Text = T("label.macroUpBind");
            MacroIntervalLabelTextBlock.Text = T("label.macroInterval");
            DownloadMacroButtonTextBlock.Text = T("action.downloadUpdateMacro");
            LaunchMacroButtonTextBlock.Text = T("action.launchMacro");
            OpenMacroFolderButtonTextBlock.Text = T("action.openMacroFolder");

            ApplyInfoButtonTooltips();

            if (string.IsNullOrWhiteSpace(ExecutableDetectionTextBlock.Text) || ExecutableDetectionTextBlock.Text == "Detecting...")
            {
                ExecutableDetectionTextBlock.Text = T("status.detecting");
            }

            RefreshGameAndConfigStatus();
            RefreshExecutableDetectionState();
            RefreshMacroStatus();
        }

        private void ShowLanguageSelector()
        {
            LanguageComboBox.Visibility = Visibility.Visible;
            LanguageComboBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                LanguageComboBox.Focus();
                LanguageComboBox.IsDropDownOpen = true;
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void HideLanguageSelector()
        {
            LanguageComboBox.IsDropDownOpen = false;
            LanguageComboBox.Visibility = Visibility.Collapsed;
        }

        private void ApplyInfoButtonTooltips()
        {
            foreach (Button button in FindVisualChildren<Button>(this))
            {
                if (button.Tag is string)
                {
                    button.ToolTip = T("common.information");
                }
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is T typedChild)
                {
                    yield return typedChild;
                }

                foreach (T descendant in FindVisualChildren<T>(child))
                {
                    yield return descendant;
                }
            }
        }

        private string LocaliseRuntimeStatusMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return message;
            }

            const string copyingPrefix = "Copying files... ";
            if (message.StartsWith(copyingPrefix, StringComparison.Ordinal))
            {
                return T("status.copyingFiles") + " " + message.Substring(copyingPrefix.Length);
            }

            return message switch
            {
                "Restoring baseline backup..." => T("status.restoringBaselineBackup"),
                "Downloading and applying selected patch payload..." => T("status.downloadingApplyingSelectedPatchPayload"),
                "Downloading Dishonored RNG fix payload..." => T("status.downloadingDishonoredRngFixPayload"),
                "Downloading Dish2Macro..." => T("status.downloadingDish2Macro"),
                "Extracting Dish2Macro..." => T("status.extractingDish2Macro"),
                "Installing Dish2Macro..." => T("status.installingDish2Macro"),
                "Dish2Macro installed." => T("status.dish2MacroInstalled"),
                "Framerate updated." => T("status.framerateUpdated"),
                "Game settings updated." => T("status.gameSettingsUpdated"),
                "Input settings updated." => T("status.inputSettingsUpdated"),
                "Dish2Macro settings updated." => T("status.dish2MacroSettingsUpdated"),
                "Patch applied." => T("status.patchApplied"),
                "Patch apply failed." => T("status.patchApplyFailed"),
                "Backup restored." => T("status.backupRestored"),
                "Baseline restore failed." => T("status.restoreFailed"),
                _ => message
            };
        }

        private void RefreshGameAndConfigStatus()
        {
            bool isValidGameDirectory = _gameDetectionService.IsValidGameDirectory(_config.GameDirectoryPath);

            if (_gameDetectionService.ConfigFilesExist(out string engineIniPath, out string inputIniPath))
            {
                _config.EngineIniPath = engineIniPath;
                _config.InputIniPath = inputIniPath;
                ConfigStatusTextBlock.Text = T("status.config.found");
                ConfigStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                LoadIniSettings();
            }
            else
            {
                _config.EngineIniPath = null;
                _config.InputIniPath = null;
                ConfigStatusTextBlock.Text = T("status.config.notFound");
                ConfigStatusTextBlock.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }

            if (isValidGameDirectory && _config.GameDirectoryPath != null)
            {
                bool hasBackup = _backupService.BackupExists(_config.GameDirectoryPath);
                BaselineStatusTextBlock.Text = hasBackup ? T("status.backup.ready") : T("status.backup.notCreated");
                BaselineStatusTextBlock.Foreground = hasBackup ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.OrangeRed;
            }
            else
            {
                BaselineStatusTextBlock.Text = T("status.backup.unknown");
                BaselineStatusTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
            }

            RefreshExecutableDetectionState();
            UpdateStatus(T("status.ready"));
        }

        private void RefreshExecutableDetectionState()
        {
            if (PatchVersionComboBox == null ||
                ExecutableDetectionTextBlock == null)
            {
                return;
            }

            if (!_gameDetectionService.IsValidGameDirectory(_config.GameDirectoryPath))
            {
                ExecutableDetectionTextBlock.Text = T("status.detected.selectFolder");
                return;
            }

            ExecutableDetectionResult detection = _executableDetectionService.Detect(_config.GameDirectoryPath);
            string detectedVersionText = FormatDetectedExecutableVersion(detection.Version);

            ExecutableDetectionTextBlock.Text = detection.Version == DetectedExecutableVersion.Unknown
                ? T("status.detected.unknown")
                : string.Format(CultureInfo.CurrentCulture, T("status.detected.format"), detectedVersionText);
        }

        private void LoadIniSettings()
        {
            if (string.IsNullOrWhiteSpace(_config.EngineIniPath) || string.IsNullOrWhiteSpace(_config.InputIniPath))
            {
                return;
            }

            _isRefreshingIniSettings = true;
            try
            {
                try
                {
                    string? maxFps = _iniEditorService.ReadValue(_config.EngineIniPath, "Engine.Engine", "MaxSmoothedFrameRate")
                        ?? _iniEditorService.ReadValue(_config.EngineIniPath, "SystemSettings", "MaxSmoothedFrameRate");
                    if (!string.IsNullOrWhiteSpace(maxFps))
                    {
                        MaxSmoothedFrameRateTextBox.Text = maxFps;
                    }

                    string? pauseOnFocus = _iniEditorService.ReadValue(_config.EngineIniPath, "Engine.Engine", "bPauseOnLossOfFocus");
                    if (!string.IsNullOrWhiteSpace(pauseOnFocus))
                    {
                        PauseOnLossFocusCheckBox.IsChecked = pauseOnFocus.Equals("true", StringComparison.OrdinalIgnoreCase);
                    }

                    string? smoothFps = _iniEditorService.ReadValue(_config.EngineIniPath, "Engine.Engine", "bSmoothFrameRate")
                        ?? _iniEditorService.ReadValue(_config.EngineIniPath, "SystemSettings", "bSmoothFrameRate");
                    SmoothFrameRateCheckBox.IsChecked = "true".Equals(smoothFps, StringComparison.OrdinalIgnoreCase);
                    string? dynLights = _iniEditorService.ReadValue(_config.EngineIniPath, "SystemSettings", "DynamicLights");
                    DynamicLightsCheckBox.IsChecked = "true".Equals(dynLights, StringComparison.OrdinalIgnoreCase);
                    string? dynShadows = _iniEditorService.ReadValue(_config.EngineIniPath, "SystemSettings", "DynamicShadows");
                    DynamicShadowsCheckBox.IsChecked = "true".Equals(dynShadows, StringComparison.OrdinalIgnoreCase);
                    string? lightShafts = _iniEditorService.ReadValue(_config.EngineIniPath, "SystemSettings", "bAllowLightShafts");
                    LightShaftsCheckBox.IsChecked = "true".Equals(lightShafts, StringComparison.OrdinalIgnoreCase);
                    string? skipMovies = _iniEditorService.ReadValue(_config.EngineIniPath, "FullScreenMovie", "bForceNoStartupMovies");
                    SkipStartupMoviesCheckBox.IsChecked = "true".Equals(skipMovies, StringComparison.OrdinalIgnoreCase);

                    InputBindingOptions options = _inputBindingsService.LoadOptions(_config.InputIniPath);
                    SetComboSelectionByContent(JumpScrollComboBox, string.IsNullOrWhiteSpace(options.JumpScrollDirection) ? "None" : options.JumpScrollDirection);
                    SetComboSelectionByContent(InteractScrollComboBox, string.IsNullOrWhiteSpace(options.InteractScrollDirection) ? "None" : options.InteractScrollDirection);

                    if (options.FpsBindings.Count > 0)
                    {
                        SetFpsRowFromEntry(0, options.FpsBindings.ElementAtOrDefault(0));
                        SetFpsRowFromEntry(1, options.FpsBindings.ElementAtOrDefault(1));
                        SetFpsRowFromEntry(2, options.FpsBindings.ElementAtOrDefault(2));
                        SetFpsRowFromEntry(3, options.FpsBindings.ElementAtOrDefault(3));
                    }
                    else
                    {
                        SetDefaultFpsRows();
                    }

                    if (!string.IsNullOrWhiteSpace(options.ConsoleKey))
                    {
                        ConsoleKeyTextBox.Text = options.ConsoleKey;
                    }

                    if (!string.IsNullOrWhiteSpace(options.CheatKey))
                    {
                        CheatKeyTextBox.Text = options.CheatKey;
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Failed to load INI values: {ex.Message}");
                }
            }
            finally
            {
                _isRefreshingIniSettings = false;
                SyncAutoApplySignatures();
            }
        }

        private void SetFpsRowFromEntry(int rowIndex, FpsBindingEntry? entry)
        {
            if (entry == null)
            {
                return;
            }

            switch (rowIndex)
            {
                case 0:
                    FpsKey1TextBox.Text = entry.Key;
                    FpsValue1TextBox.Text = entry.Value;
                    break;
                case 1:
                    FpsKey2TextBox.Text = entry.Key;
                    FpsValue2TextBox.Text = entry.Value;
                    break;
                case 2:
                    FpsKey3TextBox.Text = entry.Key;
                    FpsValue3TextBox.Text = entry.Value;
                    break;
                case 3:
                    FpsKey4TextBox.Text = entry.Key;
                    FpsValue4TextBox.Text = entry.Value;
                    break;
            }
        }

        private void SetDefaultFpsRows()
        {
            FpsKey1TextBox.Text = "F2";
            FpsValue1TextBox.Text = "5";
            FpsKey2TextBox.Text = "F3";
            FpsValue2TextBox.Text = "60";
            FpsKey3TextBox.Text = "F4";
            FpsValue3TextBox.Text = "250";
            FpsKey4TextBox.Text = "F7";
            FpsValue4TextBox.Text = "2.46";
        }

        private void SetComboSelectionByContent(System.Windows.Controls.ComboBox comboBox, string content)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Tag is string tag && tag.Equals(content, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }

                if (item.Content is string text && text.Equals(content, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private static string NormalisePatchVersionSetting(string? patchVersion)
        {
            if (string.IsNullOrWhiteSpace(patchVersion))
            {
                return PatchDownloadService.PatchTarget15Latest;
            }

            string normalised = patchVersion.Trim();
            return normalised switch
            {
                "1.2" => PatchDownloadService.PatchTarget12Base,
                "1.3" => PatchDownloadService.PatchTarget13Kod,
                "1.4" => PatchDownloadService.PatchTarget14Brigmore,
                _ => normalised
            };
        }

        private void SelectGameDirectory_Click(object sender, RoutedEventArgs e)
        {
            using System.Windows.Forms.FolderBrowserDialog dialog = new()
            {
                Description = "Select your Dishonored installation directory",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            string selectedPath = dialog.SelectedPath;
            if (!_gameDetectionService.IsValidGameDirectory(selectedPath))
            {
                DialogHelper.ShowMessage("Invalid Directory",
                    "The selected directory does not look like a valid Dishonored install.\n\n" +
                    "Expected: Binaries/Win32/Dishonored.exe and DishonoredGame/CookedPCConsole.",
                    DialogHelper.MessageType.Warning);
                return;
            }

            _config.GameDirectoryPath = selectedPath;
            GameDirectoryPathTextBlock.Text = selectedPath;
            RefreshGameAndConfigStatus();
            SaveSettings();
        }

        private void OpenConfigFolder_Click(object sender, RoutedEventArgs e)
        {
            string configPath = _gameDetectionService.GetConfigDirectory();
            try
            {
                if (!Directory.Exists(configPath))
                {
                    Directory.CreateDirectory(configPath);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = configPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                DialogHelper.ShowMessage("Error", $"Failed to open config folder: {ex.Message}", DialogHelper.MessageType.Error);
            }
        }

        private string GetSelectedMacroDirectory(bool applyDefaultIfEmpty)
        {
            string selected = (MacroDirectoryTextBox.Text ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(selected))
            {
                return selected;
            }

            string fallback = _dish2MacroService.GetDefaultInstallDirectory();
            if (applyDefaultIfEmpty)
            {
                MacroDirectoryTextBox.Text = fallback;
            }

            return fallback;
        }

        private void RefreshMacroStatus()
        {
            if (MacroStatusTextBlock == null || LaunchMacroButton == null)
            {
                return;
            }

            string macroDirectory = GetSelectedMacroDirectory(applyDefaultIfEmpty: false);
            bool installed = _dish2MacroService.IsInstalled(macroDirectory);
            MacroStatusTextBlock.Text = installed
                ? T("status.macro.installed")
                : T("status.macro.notInstalled");
            MacroStatusTextBlock.Foreground = installed ? Brushes.Green : Brushes.OrangeRed;
            LaunchMacroButton.IsEnabled = installed;
        }

        private void LoadMacroConfigurationIntoUi(bool showDialogs)
        {
            if (MacroDownBindTextBox == null || MacroUpBindTextBox == null || MacroIntervalTextBox == null)
            {
                return;
            }

            string macroDirectory = GetSelectedMacroDirectory(applyDefaultIfEmpty: false);
            _isRefreshingMacroConfig = true;
            try
            {
                Dish2MacroConfiguration configuration = _dish2MacroService.LoadConfiguration(macroDirectory);
                SetMacroBindTextBox(MacroDownBindTextBox, configuration.DownBindVirtualKey);
                SetMacroBindTextBox(MacroUpBindTextBox, configuration.UpBindVirtualKey);
                MacroIntervalTextBox.Text = configuration.IntervalMilliseconds.ToString(CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                if (showDialogs)
                {
                    DialogHelper.ShowMessage("Macro Config Error", ex.Message, DialogHelper.MessageType.Warning);
                }
                else
                {
                    UpdateStatus($"Dish2Macro config load issue: {ex.Message}");
                }
            }
            finally
            {
                _isRefreshingMacroConfig = false;
                SyncMacroAutoApplySignature();
            }
        }

        private bool ApplyMacroConfiguration(bool showDialogs)
        {
            string macroDirectory = GetSelectedMacroDirectory(applyDefaultIfEmpty: true);

            int? downBind = GetMacroBindFromTextBox(MacroDownBindTextBox);
            int? upBind = GetMacroBindFromTextBox(MacroUpBindTextBox);

            if (downBind == null && !string.IsNullOrWhiteSpace(MacroDownBindTextBox.Text))
            {
                if (showDialogs)
                {
                    DialogHelper.ShowMessage("Invalid Macro Bind",
                        "Scroll down bind is invalid. Pick a key/mouse button or clear the field.",
                        DialogHelper.MessageType.Warning);
                }
                else
                {
                    UpdateStatus("Dish2Macro settings not applied: scroll down bind is invalid.");
                }

                return false;
            }

            if (upBind == null && !string.IsNullOrWhiteSpace(MacroUpBindTextBox.Text))
            {
                if (showDialogs)
                {
                    DialogHelper.ShowMessage("Invalid Macro Bind",
                        "Scroll up bind is invalid. Pick a key/mouse button or clear the field.",
                        DialogHelper.MessageType.Warning);
                }
                else
                {
                    UpdateStatus("Dish2Macro settings not applied: scroll up bind is invalid.");
                }

                return false;
            }

            if (!int.TryParse(MacroIntervalTextBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int intervalMs) || intervalMs <= 0)
            {
                if (showDialogs)
                {
                    DialogHelper.ShowMessage("Invalid Interval",
                        "Macro interval must be a whole number greater than 0.",
                        DialogHelper.MessageType.Warning);
                }
                else
                {
                    UpdateStatus("Dish2Macro settings not applied: interval must be greater than 0.");
                }

                return false;
            }

            try
            {
                Dish2MacroConfiguration configuration = new()
                {
                    DownBindVirtualKey = downBind,
                    UpBindVirtualKey = upBind,
                    IntervalMilliseconds = intervalMs
                };

                _dish2MacroService.SaveConfiguration(macroDirectory, configuration);
                SetMacroBindTextBox(MacroDownBindTextBox, configuration.DownBindVirtualKey);
                SetMacroBindTextBox(MacroUpBindTextBox, configuration.UpBindVirtualKey);
                MacroIntervalTextBox.Text = configuration.IntervalMilliseconds.ToString(CultureInfo.InvariantCulture);
                SyncMacroAutoApplySignature();
                UpdateStatus(T("status.dish2MacroSettingsUpdated"));

                if (showDialogs)
                {
                    DialogHelper.ShowMessage("Success", "Dish2Macro.ini updated.", DialogHelper.MessageType.Success);
                }

                return true;
            }
            catch (Exception ex)
            {
                if (showDialogs)
                {
                    DialogHelper.ShowMessage("Macro Config Error", ex.Message, DialogHelper.MessageType.Error);
                }
                else
                {
                    UpdateStatus($"Dish2Macro settings not applied: {ex.Message}");
                }

                return false;
            }
        }

        private static void SetMacroBindTextBox(TextBox textBox, int? virtualKey)
        {
            if (virtualKey.HasValue)
            {
                int vk = virtualKey.Value;
                textBox.Tag = vk;
                textBox.Text = GetMacroBindDisplayName(vk);
                return;
            }

            textBox.Tag = null;
            textBox.Text = string.Empty;
        }

        private static int? GetMacroBindFromTextBox(TextBox textBox)
        {
            if (textBox.Tag is int tagValue)
            {
                return tagValue;
            }

            string raw = (textBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            return null;
        }

        private static string GetMacroBindDisplayName(int virtualKey)
        {
            return virtualKey switch
            {
                0x01 => "Mouse1",
                0x02 => "Mouse2",
                0x04 => "Mouse3",
                0x05 => "Mouse4",
                0x06 => "Mouse5",
                _ => KeyInterop.KeyFromVirtualKey(virtualKey) switch
                {
                    Key key when key == Key.None => $"VK {virtualKey}",
                    Key.Space => "Space",
                    Key key => key.ToString()
                }
            };
        }

        private void PatchVersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_windowReady)
            {
                return;
            }

            UpdatePatchVersionUiState();
            SaveSettings();
        }

        private void UpdatePatchVersionUiState()
        {
            if (PatchVersionComboBox == null ||
                ExecutableDetectionTextBlock == null)
            {
                return;
            }

            RefreshExecutableDetectionState();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _uiLanguage = _localisationService.NormaliseLanguage(GetSelectedComboContent(LanguageComboBox, LocalisationService.EnglishLanguageCode));
            if (!_windowReady || _isLoadingSettings)
            {
                return;
            }

            ApplyLocalisation();
            SaveSettings();
            HideLanguageSelector();
        }

        private void LanguageButton_Click(object sender, RoutedEventArgs e)
        {
            if (LanguageComboBox.Visibility == Visibility.Visible)
            {
                HideLanguageSelector();
                return;
            }

            ShowLanguageSelector();
        }

        private void LanguageComboBox_DropDownClosed(object sender, EventArgs e)
        {
            HideLanguageSelector();
        }

        private async void ApplyPatchButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_gameDetectionService.IsValidGameDirectory(_config.GameDirectoryPath))
            {
                DialogHelper.ShowMessage("Error", "Please select a valid game directory first.", DialogHelper.MessageType.Error);
                return;
            }

            string patchVersion = GetSelectedComboContent(PatchVersionComboBox, PatchDownloadService.PatchTarget15Latest);

            bool confirmed = await DialogHelper.ShowConfirmationAsync(
                T("dialog.confirmPatchApply.title"),
                T("dialog.confirmPatchApply.message"));
            if (!confirmed)
            {
                return;
            }

            SetBusyState(true);
            try
            {
                IProgress<string> status = new Progress<string>(UpdateStatus);
                IProgress<double?> progress = new Progress<double?>(UpdateProgressBar);

                await _backupService.EnsureBackupAsync(_config.GameDirectoryPath!, status);
                await _backupService.RestoreBackupAsync(_config.GameDirectoryPath!, status);

                PatchApplyOptions options = new()
                {
                    GameDirectoryPath = _config.GameDirectoryPath!,
                    PatchVersion = patchVersion,
                    ApplyBoyleFix = BoyleFixCheckBox.IsChecked == true,
                    ApplyTimshFix = TimshFixCheckBox.IsChecked == true
                };

                await _patchDownloadService.ApplyPatchAsync(options, status, progress);
                ApplyPatchSpecificConfigBaselines(patchVersion, status);
                ReapplyManagedSettingsAfterPatch(status);

                UpdateStatus(T("status.patchApplied"));
                await DialogHelper.ShowMessageAsync(T("dialog.success"), T("dialog.patchAppliedSuccessfully"), DialogHelper.MessageType.Success);
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Patch Failed", ex.Message, DialogHelper.MessageType.Error);
                UpdateStatus(T("status.patchApplyFailed"));
            }
            finally
            {
                SetBusyState(false);
                RefreshGameAndConfigStatus();
                SaveSettings();
            }
        }

        private void ApplyPatchSpecificConfigBaselines(string patchVersion, IProgress<string>? statusProgress)
        {
            string configDirectory = _gameDetectionService.GetConfigDirectory();
            statusProgress?.Report("Applying patch baseline config templates...");
            _configBaselineService.ApplyPatchBaseline(configDirectory, patchVersion);

            string engineIniPath = _gameDetectionService.GetEngineIniPath();
            string inputIniPath = _gameDetectionService.GetInputIniPath();
            _config.EngineIniPath = engineIniPath;
            _config.InputIniPath = inputIniPath;

            int quickSaveSlot = patchVersion == PatchDownloadService.PatchTarget12Base ? 13 : 15;
            _inputBindingsService.EnsureQuickSaveSlot(inputIniPath, quickSaveSlot);
            statusProgress?.Report($"Patch baseline config templates applied (quicksave slot {quickSaveSlot}).");
        }

        private void ReapplyManagedSettingsAfterPatch(IProgress<string>? statusProgress)
        {
            if (string.IsNullOrWhiteSpace(_config.EngineIniPath) || string.IsNullOrWhiteSpace(_config.InputIniPath))
            {
                return;
            }

            statusProgress?.Report("Re-applying Dishonored Tweaks settings...");

            _ = ApplyEngineToggleSettings(showDialogs: false);
            _ = ApplyFpsLimitSetting(showDialogs: false);
            _ = ApplyInputSettings(showDialogs: false);

            statusProgress?.Report("Dishonored Tweaks settings re-applied.");
        }

        private async void RestoreBaselineButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_gameDetectionService.IsValidGameDirectory(_config.GameDirectoryPath))
            {
                DialogHelper.ShowMessage("Error", "Please select a valid game directory first.", DialogHelper.MessageType.Error);
                return;
            }

            bool confirmed = await DialogHelper.ShowConfirmationAsync(
                T("dialog.confirmBaselineRestore.title"),
                T("dialog.confirmBaselineRestore.message"));
            if (!confirmed)
            {
                return;
            }

            SetBusyState(true);
            try
            {
                IProgress<string> status = new Progress<string>(UpdateStatus);

                await _backupService.EnsureBackupAsync(_config.GameDirectoryPath!, status);
                await _backupService.RestoreBackupAsync(_config.GameDirectoryPath!, status);

                await DialogHelper.ShowMessageAsync(T("dialog.success"), T("dialog.backupRestoredSuccessfully"), DialogHelper.MessageType.Success);
                UpdateStatus(T("status.backupRestored"));
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Restore Failed", ex.Message, DialogHelper.MessageType.Error);
                UpdateStatus(T("status.restoreFailed"));
            }
            finally
            {
                SetBusyState(false);
                RefreshGameAndConfigStatus();
                SaveSettings();
            }
        }

        private void MacroDirectoryTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!_windowReady || _isLoadingSettings)
            {
                return;
            }

            GetSelectedMacroDirectory(applyDefaultIfEmpty: true);
            RefreshMacroStatus();
            LoadMacroConfigurationIntoUi(showDialogs: false);
            SaveSettings();
        }

        private void BrowseMacroDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            string currentDirectory = GetSelectedMacroDirectory(applyDefaultIfEmpty: false);
            using System.Windows.Forms.FolderBrowserDialog dialog = new()
            {
                Description = "Select where Dish2Macro should be installed",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true,
                SelectedPath = Directory.Exists(currentDirectory) ? currentDirectory : _dish2MacroService.GetDefaultInstallDirectory()
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            MacroDirectoryTextBox.Text = dialog.SelectedPath;
            RefreshMacroStatus();
            LoadMacroConfigurationIntoUi(showDialogs: false);
            SaveSettings();
        }

        private async void DownloadMacroButton_Click(object sender, RoutedEventArgs e)
        {
            string macroDirectory = GetSelectedMacroDirectory(applyDefaultIfEmpty: true);

            SetBusyState(true);
            try
            {
                IProgress<string> status = new Progress<string>(UpdateStatus);
                IProgress<double?> progress = new Progress<double?>(UpdateProgressBar);

                await _dish2MacroService.InstallAsync(macroDirectory, status, progress);

                RefreshMacroStatus();
                LoadMacroConfigurationIntoUi(showDialogs: false);
                SaveSettings();
                UpdateStatus(T("status.dish2MacroInstalled"));
                await DialogHelper.ShowMessageAsync(T("dialog.success"), T("dialog.dish2MacroDownloadedInstalled"), DialogHelper.MessageType.Success);
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Macro Setup Failed", ex.Message, DialogHelper.MessageType.Error);
                UpdateStatus("Dish2Macro setup failed.");
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private void LaunchMacroButton_Click(object sender, RoutedEventArgs e)
        {
            string macroDirectory = GetSelectedMacroDirectory(applyDefaultIfEmpty: true);
            string macroExecutablePath = _dish2MacroService.GetExecutablePath(macroDirectory);
            if (!File.Exists(macroExecutablePath))
            {
                DialogHelper.ShowMessage("Macro Not Found",
                    "Dish2Macro.exe was not found in the selected macro directory.\n\nDownload the macro first.",
                    DialogHelper.MessageType.Warning);
                RefreshMacroStatus();
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = macroExecutablePath,
                    WorkingDirectory = macroDirectory,
                    UseShellExecute = true
                });

                UpdateStatus("Dish2Macro launched.");
            }
            catch (Exception ex)
            {
                DialogHelper.ShowMessage("Error", $"Failed to launch Dish2Macro: {ex.Message}", DialogHelper.MessageType.Error);
            }
        }

        private void OpenMacroFolderButton_Click(object sender, RoutedEventArgs e)
        {
            string macroDirectory = GetSelectedMacroDirectory(applyDefaultIfEmpty: true);
            try
            {
                Directory.CreateDirectory(macroDirectory);
                Process.Start(new ProcessStartInfo
                {
                    FileName = macroDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                DialogHelper.ShowMessage("Error", $"Failed to open macro folder: {ex.Message}", DialogHelper.MessageType.Error);
            }
        }

        private void MacroSetting_LostFocus(object sender, RoutedEventArgs e)
        {
            TryAutoApplyMacroSettings();
        }

        private void TryAutoApplyMacroSettings()
        {
            if (!CanAutoApplyMacroSettings())
            {
                return;
            }

            string currentSignature = BuildMacroConfigAutoApplySignature();
            if (string.Equals(currentSignature, _lastMacroConfigAutoApplySignature, StringComparison.Ordinal))
            {
                return;
            }

            _lastMacroConfigAutoApplySignature = currentSignature;
            ApplyMacroConfiguration(showDialogs: false);
        }

        private bool CanAutoApplyMacroSettings()
        {
            return _windowReady && !_isLoadingSettings && !_isRefreshingMacroConfig;
        }

        private void MacroBindTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_isRefreshingMacroConfig)
            {
                return;
            }

            if (sender is not TextBox textBox)
            {
                return;
            }

            e.Handled = true;
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.Back || key == Key.Delete)
            {
                SetMacroBindTextBox(textBox, null);
                TryAutoApplyMacroSettings();
                return;
            }

            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            if (virtualKey <= 0 || virtualKey > 0xFE)
            {
                return;
            }

            SetMacroBindTextBox(textBox, virtualKey);
            TryAutoApplyMacroSettings();
        }

        private void MacroBindTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isRefreshingMacroConfig)
            {
                return;
            }

            if (sender is not TextBox textBox)
            {
                return;
            }

            if (e.ChangedButton == MouseButton.Left && !textBox.IsKeyboardFocusWithin)
            {
                return;
            }

            e.Handled = true;
            int? virtualKey = e.ChangedButton switch
            {
                MouseButton.Left => 0x01,
                MouseButton.Right => 0x02,
                MouseButton.Middle => 0x04,
                MouseButton.XButton1 => 0x05,
                MouseButton.XButton2 => 0x06,
                _ => null
            };

            if (virtualKey.HasValue)
            {
                SetMacroBindTextBox(textBox, virtualKey);
                TryAutoApplyMacroSettings();
            }
        }

        private void MacroBindTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // macro bind captures keyboard/mouse buttons, not wheel-up/wheel-down as bind keys
            e.Handled = true;
        }

        private void ApplyEngineTweaksButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFpsLimitSetting(showDialogs: true);
        }

        private void EngineToggleSetting_Changed(object sender, RoutedEventArgs e)
        {
            TryAutoApplyEngineSettings();
        }

        private void InputSetting_LostFocus(object sender, RoutedEventArgs e)
        {
            TryAutoApplyInputSettings();
        }

        private void TryAutoApplyEngineSettings()
        {
            if (!CanAutoApplySettings())
            {
                return;
            }

            string currentSignature = BuildEngineToggleAutoApplySignature();
            if (string.Equals(currentSignature, _lastEngineToggleAutoApplySignature, StringComparison.Ordinal))
            {
                return;
            }

            _lastEngineToggleAutoApplySignature = currentSignature;
            SaveSettings();
            ApplyEngineToggleSettings(showDialogs: false);
        }

        private void TryAutoApplyInputSettings()
        {
            if (!CanAutoApplySettings())
            {
                return;
            }

            string currentSignature = BuildInputAutoApplySignature();
            if (string.Equals(currentSignature, _lastInputAutoApplySignature, StringComparison.Ordinal))
            {
                return;
            }

            _lastInputAutoApplySignature = currentSignature;
            SaveSettings();
            ApplyInputSettings(showDialogs: false);
        }

        private bool CanAutoApplySettings()
        {
            return _windowReady && !_isLoadingSettings && !_isRefreshingIniSettings;
        }

        private void SyncAutoApplySignatures()
        {
            _lastEngineToggleAutoApplySignature = BuildEngineToggleAutoApplySignature();
            _lastInputAutoApplySignature = BuildInputAutoApplySignature();
            SyncMacroAutoApplySignature();
        }

        private string BuildEngineToggleAutoApplySignature()
        {
            return string.Join("|",
                SmoothFrameRateCheckBox.IsChecked == true ? "1" : "0",
                DynamicLightsCheckBox.IsChecked == true ? "1" : "0",
                DynamicShadowsCheckBox.IsChecked == true ? "1" : "0",
                LightShaftsCheckBox.IsChecked == true ? "1" : "0",
                SkipStartupMoviesCheckBox.IsChecked == true ? "1" : "0",
                PauseOnLossFocusCheckBox.IsChecked == true ? "1" : "0");
        }

        private string BuildInputAutoApplySignature()
        {
            return string.Join("|",
                NormaliseSignatureText(GetSelectedComboContent(JumpScrollComboBox, "None")),
                NormaliseSignatureText(GetSelectedComboContent(InteractScrollComboBox, "None")),
                NormaliseSignatureText(FpsKey1TextBox.Text),
                NormaliseSignatureText(FpsValue1TextBox.Text),
                NormaliseSignatureText(FpsKey2TextBox.Text),
                NormaliseSignatureText(FpsValue2TextBox.Text),
                NormaliseSignatureText(FpsKey3TextBox.Text),
                NormaliseSignatureText(FpsValue3TextBox.Text),
                NormaliseSignatureText(FpsKey4TextBox.Text),
                NormaliseSignatureText(FpsValue4TextBox.Text),
                NormaliseSignatureText(ConsoleKeyTextBox.Text),
                NormaliseSignatureText(CheatKeyTextBox.Text));
        }

        private void SyncMacroAutoApplySignature()
        {
            _lastMacroConfigAutoApplySignature = BuildMacroConfigAutoApplySignature();
        }

        private string BuildMacroConfigAutoApplySignature()
        {
            string downBindToken = MacroDownBindTextBox.Tag is int downTag
                ? downTag.ToString(CultureInfo.InvariantCulture)
                : NormaliseSignatureText(MacroDownBindTextBox.Text);
            string upBindToken = MacroUpBindTextBox.Tag is int upTag
                ? upTag.ToString(CultureInfo.InvariantCulture)
                : NormaliseSignatureText(MacroUpBindTextBox.Text);

            return string.Join("|",
                downBindToken,
                upBindToken,
                NormaliseSignatureText(MacroIntervalTextBox.Text));
        }

        private static string NormaliseSignatureText(string? value)
        {
            return (value ?? string.Empty).Trim();
        }

        private bool ApplyEngineToggleSettings(bool showDialogs)
        {
            if (string.IsNullOrWhiteSpace(_config.EngineIniPath))
            {
                if (showDialogs)
                {
                    DialogHelper.ShowMessage("Error",
                        "DishonoredEngine.ini was not found in Documents.\n\n" +
                        "Launch Dishonored once to generate config files, then try again.",
                        DialogHelper.MessageType.Error);
                }
                else
                {
                    UpdateStatus("Game settings not applied: launch Dishonored once to generate config files.");
                }

                return false;
            }

            try
            {
                bool pauseOnLossFocus = PauseOnLossFocusCheckBox.IsChecked == true;
                bool smoothFps = SmoothFrameRateCheckBox.IsChecked == true;
                bool dynamicLights = DynamicLightsCheckBox.IsChecked == true;
                bool dynamicShadows = DynamicShadowsCheckBox.IsChecked == true;
                bool lightShafts = LightShaftsCheckBox.IsChecked == true;
                bool skipMovies = SkipStartupMoviesCheckBox.IsChecked == true;

                Dictionary<string, string> systemSettings = new()
                {
                    { "DynamicLights", dynamicLights ? "true" : "false" },
                    { "DynamicShadows", dynamicShadows ? "true" : "false" },
                    { "bAllowLightShafts", lightShafts ? "true" : "false" }
                };

                _iniEditorService.SetValues(_config.EngineIniPath, "FullScreenMovie", new Dictionary<string, string>
                {
                    { "bForceNoStartupMovies", skipMovies ? "true" : "false" }
                });

                _iniEditorService.SetValues(_config.EngineIniPath, "SystemSettings", systemSettings);
                _iniEditorService.RemoveKeys(_config.EngineIniPath, "SystemSettings", new[] { "bSmoothFrameRate" });
                _iniEditorService.SetValues(_config.EngineIniPath, "Engine.Engine", new Dictionary<string, string>
                {
                    { "bSmoothFrameRate", smoothFps ? "true" : "false" },
                    { "bPauseOnLossOfFocus", pauseOnLossFocus ? "true" : "false" }
                });

                SetConfigFilesReadOnly();
                SaveSettings();
                UpdateStatus(T("status.gameSettingsUpdated"));
                return true;
            }
            catch (Exception ex)
            {
                if (showDialogs)
                {
                    DialogHelper.ShowMessage("Error", $"Failed to apply game settings: {ex.Message}", DialogHelper.MessageType.Error);
                }
                else
                {
                    UpdateStatus($"Game settings not applied: {ex.Message}");
                }

                return false;
            }
        }

        private bool ApplyFpsLimitSetting(bool showDialogs)
        {
            if (string.IsNullOrWhiteSpace(_config.EngineIniPath))
            {
                if (showDialogs)
                {
                    DialogHelper.ShowMessage("Error",
                        "DishonoredEngine.ini was not found in Documents.\n\n" +
                        "Launch Dishonored once to generate config files, then try again.",
                        DialogHelper.MessageType.Error);
                }
                else
                {
                    UpdateStatus("Framerate not applied: launch Dishonored once to generate config files.");
                }

                return false;
            }

            if (!double.TryParse(MaxSmoothedFrameRateTextBox.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double maxFps) || maxFps <= 1)
            {
                if (showDialogs)
                {
                    DialogHelper.ShowMessage("Invalid Value", "FPS must be a number greater than 1.", DialogHelper.MessageType.Warning);
                }
                else
                {
                    UpdateStatus("Framerate not applied: FPS must be greater than 1.");
                }

                return false;
            }

            try
            {
                string maxFpsString = FormatDouble(maxFps);
                _iniEditorService.RemoveKeys(_config.EngineIniPath, "SystemSettings", new[] { "MaxSmoothedFrameRate" });
                _iniEditorService.SetValues(_config.EngineIniPath, "Engine.Engine", new Dictionary<string, string>
                {
                    { "MaxSmoothedFrameRate", maxFpsString }
                });

                MaxSmoothedFrameRateTextBox.Text = maxFpsString;
                SetConfigFilesReadOnly();
                SaveSettings();
                UpdateStatus(T("status.framerateUpdated"));
                return true;
            }
            catch (Exception ex)
            {
                if (showDialogs)
                {
                    DialogHelper.ShowMessage("Error", $"Failed to apply framerate: {ex.Message}", DialogHelper.MessageType.Error);
                }
                else
                {
                    UpdateStatus($"Framerate not applied: {ex.Message}");
                }

                return false;
            }
        }

        private bool ApplyInputSettings(bool showDialogs)
        {
            if (string.IsNullOrWhiteSpace(_config.InputIniPath) || string.IsNullOrWhiteSpace(_config.EngineIniPath))
            {
                if (showDialogs)
                {
                    DialogHelper.ShowMessage("Error",
                        "DishonoredEngine.ini or DishonoredInput.ini was not found in Documents.\n\n" +
                        "Launch Dishonored once to generate config files, then try again.",
                        DialogHelper.MessageType.Error);
                }
                else
                {
                    UpdateStatus("Input settings not applied: launch Dishonored once to generate config files.");
                }

                return false;
            }

            string jumpDirection = GetSelectedComboContent(JumpScrollComboBox, "None");
            string interactDirection = GetSelectedComboContent(InteractScrollComboBox, "None");
            if (!jumpDirection.Equals("None", StringComparison.OrdinalIgnoreCase) &&
                jumpDirection.Equals(interactDirection, StringComparison.OrdinalIgnoreCase))
            {
                if (showDialogs)
                {
                    DialogHelper.ShowMessage("Invalid Scroll Configuration",
                        "Jump and Interact cannot use the same scroll direction.",
                        DialogHelper.MessageType.Warning);
                }
                else
                {
                    UpdateStatus("Input settings not applied: Jump and Interact cannot use the same scroll direction.");
                }

                return false;
            }

            List<FpsBindingEntry> fpsBindings;
            try
            {
                fpsBindings = GetFpsBindingsFromUi();
            }
            catch (Exception ex)
            {
                if (showDialogs)
                {
                    DialogHelper.ShowMessage("Invalid FPS Bind", ex.Message, DialogHelper.MessageType.Warning);
                }
                else
                {
                    UpdateStatus($"Input settings not applied: {ex.Message}");
                }

                return false;
            }

            try
            {
                InputBindingOptions options = new()
                {
                    JumpScrollDirection = jumpDirection,
                    InteractScrollDirection = interactDirection,
                    FpsBindings = fpsBindings,
                    ConsoleKey = ConsoleKeyTextBox.Text.Trim(),
                    CheatKey = CheatKeyTextBox.Text.Trim()
                };

                _inputBindingsService.ApplyBindings(_config.InputIniPath, options);
                SetConfigFilesReadOnly();
                SaveSettings();
                UpdateStatus(T("status.inputSettingsUpdated"));
                return true;
            }
            catch (Exception ex)
            {
                if (showDialogs)
                {
                    DialogHelper.ShowMessage("Error", $"Failed to apply input settings: {ex.Message}", DialogHelper.MessageType.Error);
                }
                else
                {
                    UpdateStatus($"Input settings not applied: {ex.Message}");
                }

                return false;
            }
        }

        private List<FpsBindingEntry> GetFpsBindingsFromUi()
        {
            List<FpsBindingEntry> rows = new()
            {
                new FpsBindingEntry { Key = FpsKey1TextBox.Text.Trim(), Value = FpsValue1TextBox.Text.Trim() },
                new FpsBindingEntry { Key = FpsKey2TextBox.Text.Trim(), Value = FpsValue2TextBox.Text.Trim() },
                new FpsBindingEntry { Key = FpsKey3TextBox.Text.Trim(), Value = FpsValue3TextBox.Text.Trim() },
                new FpsBindingEntry { Key = FpsKey4TextBox.Text.Trim(), Value = FpsValue4TextBox.Text.Trim() }
            };

            List<FpsBindingEntry> cleaned = new();
            foreach (FpsBindingEntry row in rows)
            {
                bool hasKey = !string.IsNullOrWhiteSpace(row.Key);
                bool hasValue = !string.IsNullOrWhiteSpace(row.Value);

                if (!hasKey && !hasValue)
                {
                    continue;
                }

                if (!hasKey || !hasValue)
                {
                    throw new InvalidOperationException("Each FPS row must include both a key and framerate value.");
                }

                if (!double.TryParse(row.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || value <= 1)
                {
                    throw new InvalidOperationException("Each FPS bind value must be a decimal number greater than 1.");
                }

                cleaned.Add(new FpsBindingEntry
                {
                    Key = row.Key,
                    Value = FormatDouble(value)
                });
            }

            return cleaned;
        }

        private static string FormatDouble(double value)
        {
            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }

        private void SetConfigFilesReadOnly()
        {
            if (!string.IsNullOrWhiteSpace(_config.EngineIniPath))
            {
                _iniEditorService.SetReadOnly(_config.EngineIniPath, true);
            }

            if (!string.IsNullOrWhiteSpace(_config.InputIniPath))
            {
                _iniEditorService.SetReadOnly(_config.InputIniPath, true);
            }
        }

        private void ScrollDirectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingScrollCombos)
            {
                return;
            }

            try
            {
                _isUpdatingScrollCombos = true;
                string jumpDirection = GetSelectedComboContent(JumpScrollComboBox, "None");
                string interactDirection = GetSelectedComboContent(InteractScrollComboBox, "None");

                if (!jumpDirection.Equals("None", StringComparison.OrdinalIgnoreCase) &&
                    jumpDirection.Equals(interactDirection, StringComparison.OrdinalIgnoreCase))
                {
                    if (ReferenceEquals(sender, JumpScrollComboBox))
                    {
                        SetComboSelectionByContent(InteractScrollComboBox, "None");
                    }
                    else
                    {
                        SetComboSelectionByContent(JumpScrollComboBox, "None");
                    }
                }
            }
            finally
            {
                _isUpdatingScrollCombos = false;
            }

            TryAutoApplyInputSettings();
        }

        private static string GetSelectedComboContent(System.Windows.Controls.ComboBox comboBox, string fallback)
        {
            if (comboBox.SelectedItem is ComboBoxItem item)
            {
                if (item.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
                {
                    return tag;
                }

                if (item.Content is string text)
                {
                    return text;
                }
            }

            return fallback;
        }

        private string FormatDetectedExecutableVersion(DetectedExecutableVersion version)
        {
            return version switch
            {
                DetectedExecutableVersion.V12BaseGame => T("version.1.2"),
                DetectedExecutableVersion.V13KnifeOfDunwall => T("version.1.3"),
                DetectedExecutableVersion.V14BrigmoreWitches => T("version.1.4bw"),
                DetectedExecutableVersion.V14DaudHonored => T("version.1.4daud"),
                DetectedExecutableVersion.V15Latest => T("version.1.5"),
                DetectedExecutableVersion.V14OrV15Dlc07Family => T("version.1.4or1.5"),
                _ => T("common.unknown")
            };
        }

        private void KeyPickerTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = true;
        }

        private void KeyPickerTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not System.Windows.Controls.TextBox textBox)
            {
                return;
            }

            if (e.ChangedButton == MouseButton.Left && !textBox.IsKeyboardFocusWithin)
            {
                return;
            }

            e.Handled = true;
            string? mouseKey = e.ChangedButton switch
            {
                MouseButton.Left => "LeftMouseButton",
                MouseButton.Right => "RightMouseButton",
                MouseButton.Middle => "MiddleMouseButton",
                MouseButton.XButton1 => "ThumbMouseButton",
                MouseButton.XButton2 => "ThumbMouseButton2",
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(mouseKey))
            {
                textBox.Text = mouseKey;
                TryAutoApplyInputSettings();
            }
        }

        private void KeyPickerTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not System.Windows.Controls.TextBox textBox)
            {
                return;
            }

            e.Handled = true;
            textBox.Text = e.Delta > 0 ? "MouseScrollUp" : "MouseScrollDown";
            TryAutoApplyInputSettings();
        }

        private void KeyPickerTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (sender is not System.Windows.Controls.TextBox textBox)
            {
                return;
            }

            e.Handled = true;
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (key == Key.Back || key == Key.Delete)
            {
                textBox.Clear();
                TryAutoApplyInputSettings();
                return;
            }

            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            string keyName = key.ToString();
            if (Ue3KeyMap.TryGetValue(keyName, out string? ue3Key))
            {
                textBox.Text = ue3Key;
                TryAutoApplyInputSettings();
                return;
            }

            if (keyName.Length == 1 && char.IsLetter(keyName[0]))
            {
                textBox.Text = keyName.ToUpperInvariant();
                TryAutoApplyInputSettings();
            }
        }

        private void ShowSettingInfo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string tag)
            {
                return;
            }

            (string title, string message) = _localisationService.GetSettingInfo(_uiLanguage, tag);

            DialogHelper.ShowMessage(title, message, DialogHelper.MessageType.Information);
        }

        private void SetBusyState(bool isBusy)
        {
            MainTabControl.IsEnabled = !isBusy;
            DownloadProgressBar.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
            if (!isBusy)
            {
                DownloadProgressBar.IsIndeterminate = false;
                DownloadProgressBar.Value = 0;
            }
        }

        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text = LocaliseRuntimeStatusMessage(message);
        }

        private void UpdateProgressBar(double? value)
        {
            if (!value.HasValue)
            {
                DownloadProgressBar.IsIndeterminate = true;
                return;
            }

            DownloadProgressBar.IsIndeterminate = false;
            DownloadProgressBar.Value = Math.Clamp(value.Value, 0, 100);
        }
    }
}
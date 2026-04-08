using System;
using System.Collections.Generic;

namespace DishonoredTweaks.Services
{
    public sealed class LocalisationService
    {
        public const string EnglishLanguageCode = "en";
        public const string RussianLanguageCode = "ru";

        private static readonly Dictionary<string, string> EnglishTexts = new(StringComparer.Ordinal)
        {
            ["window.title"] = "Dishonored Tweaks",
            ["common.information"] = "Information",
            ["common.unknown"] = "Unknown",
            ["label.language"] = "Language",
            ["language.english"] = "English",
            ["language.russian"] = "Русский",
            ["action.selectGameDirectory"] = "Select Game Directory",
            ["action.openConfigFolder"] = "Open Config Folder",
            ["action.apply"] = "Apply",
            ["action.applySelectedPatches"] = "Apply Selected Patches",
            ["action.restoreBackup"] = "Restore Backup",
            ["action.browse"] = "Browse",
            ["action.downloadUpdateMacro"] = "Download / Update Macro",
            ["action.launchMacro"] = "Launch Macro",
            ["action.openMacroFolder"] = "Open Macro Folder",
            ["status.noDirectorySelected"] = "No directory selected.",
            ["status.config.na"] = "Documents Configs: N/A",
            ["status.config.found"] = "Documents Configs: Found",
            ["status.config.notFound"] = "Documents Configs: Not Found",
            ["status.backup.unknown"] = "Baseline Backup: Unknown",
            ["status.backup.ready"] = "Baseline Backup: Ready",
            ["status.backup.notCreated"] = "Baseline Backup: Not Created",
            ["status.detecting"] = "Detecting...",
            ["status.detected.selectFolder"] = "Detected version: select a game folder.",
            ["status.detected.unknown"] = "Detected version: Unknown (you can still pick a patch manually).",
            ["status.detected.format"] = "Detected version: {0}",
            ["status.ready"] = "Ready.",
            ["status.macro.installed"] = "Dish2Macro: Installed",
            ["status.macro.notInstalled"] = "Dish2Macro: Not installed",
            ["dialog.yes"] = "Yes",
            ["dialog.no"] = "No",
            ["dialog.ok"] = "OK",
            ["dialog.success"] = "Success",
            ["dialog.confirmPatchApply.title"] = "Confirm Patch Apply",
            ["dialog.confirmPatchApply.message"] = "This will restore your backup and apply the selected patch.\n\nContinue?",
            ["dialog.confirmBaselineRestore.title"] = "Confirm Baseline Restore",
            ["dialog.confirmBaselineRestore.message"] = "This will overwrite your current game files with the saved baseline backup.\n\nContinue?",
            ["dialog.patchAppliedSuccessfully"] = "Patch applied successfully.",
            ["dialog.backupRestoredSuccessfully"] = "Backup restored successfully.",
            ["status.patchApplied"] = "Patch applied.",
            ["status.patchApplyFailed"] = "Patch apply failed.",
            ["status.restoreFailed"] = "Baseline restore failed.",
            ["status.backupRestored"] = "Backup restored.",
            ["status.restoringBaselineBackup"] = "Restoring baseline backup...",
            ["status.copyingFiles"] = "Copying files...",
            ["status.downloadingApplyingSelectedPatchPayload"] = "Downloading and applying selected patch payload...",
            ["status.downloadingDishonoredRngFixPayload"] = "Downloading Dishonored RNG fix payload...",
            ["status.downloadingDish2Macro"] = "Downloading Dish2Macro...",
            ["status.extractingDish2Macro"] = "Extracting Dish2Macro...",
            ["status.installingDish2Macro"] = "Installing Dish2Macro...",
            ["status.dish2MacroInstalled"] = "Dish2Macro installed.",
            ["dialog.dish2MacroDownloadedInstalled"] = "Dish2Macro downloaded and installed.",
            ["status.framerateUpdated"] = "Framerate updated.",
            ["status.gameSettingsUpdated"] = "Game settings updated.",
            ["status.inputSettingsUpdated"] = "Input settings updated.",
            ["status.dish2MacroSettingsUpdated"] = "Dish2Macro settings updated.",
            ["tab.patchTweaks"] = "Patch Tweaks",
            ["tab.gameTweaks"] = "Game Tweaks",
            ["tab.inputTweaks"] = "Input Tweaks",
            ["header.patchSelection"] = "Patch Selection",
            ["header.extraPatches"] = "Extra Patches",
            ["header.engineTweaks"] = "Engine Tweaks",
            ["header.framerateControl"] = "Framerate Control",
            ["header.scrollWheel"] = "Scroll Wheel",
            ["header.fpsKeys"] = "FPS Keys",
            ["header.consoleCheats"] = "Console & Cheats",
            ["header.dish2Macro"] = "Dish2Macro",
            ["header.macroSettings"] = "Macro Settings",
            ["label.patchVersion"] = "Patch Version:",
            ["label.framerate"] = "Framerate (FPS):",
            ["label.jump"] = "Jump:",
            ["label.interact"] = "Interact:",
            ["label.bind1"] = "Bind 1:",
            ["label.bind2"] = "Bind 2:",
            ["label.bind3"] = "Bind 3:",
            ["label.bind4"] = "Bind 4:",
            ["label.consoleBindKey"] = "Console Bind Key:",
            ["label.enableCheatsKey"] = "Enable cheats key:",
            ["label.macroDirectory"] = "Macro directory:",
            ["label.macroDownBind"] = "Scroll down bind:",
            ["label.macroUpBind"] = "Scroll up bind:",
            ["label.macroInterval"] = "Interval (ms):",
            ["patch.1.2"] = "1.2 - Base Game",
            ["patch.1.3"] = "1.3 - Knife of Dunwall",
            ["patch.1.4bw"] = "1.4 - Brigmore Witches",
            ["patch.1.4daud"] = "1.4 - DaudHonored",
            ["patch.1.5"] = "1.5 - Latest",
            ["choice.none"] = "None",
            ["choice.scrollUp"] = "ScrollUp",
            ["choice.scrollDown"] = "ScrollDown",
            ["checkbox.boyle"] = "Boyle RNG fix",
            ["checkbox.timsh"] = "Timsh RNG fix",
            ["checkbox.framerateLimiter"] = "Framerate limiter",
            ["checkbox.dynamicLights"] = "Dynamic lights",
            ["checkbox.dynamicShadows"] = "Dynamic shadows",
            ["checkbox.lightShafts"] = "Light shafts",
            ["checkbox.skipStartupMovies"] = "Skip startup movies",
            ["checkbox.pauseWhenUnfocused"] = "Pause when unfocused",
            ["text.fpsHint"] = "FPS value > 1",
            ["text.fpsKeysDescription"] = "Each row sets your FPS to the entered value.",
            ["text.key"] = "Key",
            ["text.fps"] = "FPS",
            ["version.1.2"] = "1.2 - Base Game",
            ["version.1.3"] = "1.3 - Knife of Dunwall",
            ["version.1.4bw"] = "1.4 - Brigmore Witches",
            ["version.1.4daud"] = "1.4 - DaudHonored",
            ["version.1.5"] = "1.5 - Latest",
            ["version.1.4or1.5"] = "1.4 or 1.5"
        };

        private static readonly Dictionary<string, string> RussianTexts = new(StringComparer.Ordinal)
        {
            ["window.title"] = "Dishonored Tweaks",
            ["common.information"] = "Информация",
            ["common.unknown"] = "Неизвестно",
            ["label.language"] = "Язык:",
            ["language.english"] = "English",
            ["language.russian"] = "Русский",
            ["action.selectGameDirectory"] = "Выбрать папку игры",
            ["action.openConfigFolder"] = "Открыть папку конфигурации",
            ["action.apply"] = "Применить",
            ["action.applySelectedPatches"] = "Применить выбранные патчи",
            ["action.restoreBackup"] = "Восстановить резервную копию",
            ["action.browse"] = "Обзор",
            ["action.downloadUpdateMacro"] = "Скачать / обновить макрос",
            ["action.launchMacro"] = "Запустить макрос",
            ["action.openMacroFolder"] = "Открыть папку макроса",
            ["status.noDirectorySelected"] = "Папка игры не выбрана.",
            ["status.config.na"] = "Конфигурации в папке «Документы»: Н/Д",
            ["status.config.found"] = "Конфигурации в папке «Документы»: Найдены",
            ["status.config.notFound"] = "Конфигурации в папке «Документы»: Не найдены",
            ["status.backup.unknown"] = "Базовая резервная копия: Неизвестно",
            ["status.backup.ready"] = "Базовая резервная копия: Готова",
            ["status.backup.notCreated"] = "Базовая резервная копия: Не создана",
            ["status.detecting"] = "Определение версии...",
            ["status.detected.selectFolder"] = "Обнаруженная версия: выберите папку игры.",
            ["status.detected.unknown"] = "Обнаруженная версия: Неизвестно (патч можно выбрать вручную).",
            ["status.detected.format"] = "Обнаруженная версия: {0}",
            ["status.ready"] = "Готово.",
            ["status.macro.installed"] = "Dish2Macro: Установлен",
            ["status.macro.notInstalled"] = "Dish2Macro: Не установлен",
            ["dialog.yes"] = "Да",
            ["dialog.no"] = "Нет",
            ["dialog.ok"] = "OK",
            ["dialog.success"] = "Успешно",
            ["dialog.confirmPatchApply.title"] = "Подтверждение применения патча",
            ["dialog.confirmPatchApply.message"] = "Это действие восстановит резервную копию и применит выбранный патч.\n\nПродолжить?",
            ["dialog.confirmBaselineRestore.title"] = "Подтверждение восстановления базовой копии",
            ["dialog.confirmBaselineRestore.message"] = "Это действие перезапишет текущие файлы игры сохранённой базовой резервной копией.\n\nПродолжить?",
            ["dialog.patchAppliedSuccessfully"] = "Патч успешно применён.",
            ["dialog.backupRestoredSuccessfully"] = "Резервная копия успешно восстановлена.",
            ["status.patchApplied"] = "Патч применён.",
            ["status.patchApplyFailed"] = "Не удалось применить патч.",
            ["status.restoreFailed"] = "Не удалось восстановить базовую копию.",
            ["status.backupRestored"] = "Резервная копия восстановлена.",
            ["status.restoringBaselineBackup"] = "Восстановление базовой резервной копии...",
            ["status.copyingFiles"] = "Копирование файлов...",
            ["status.downloadingApplyingSelectedPatchPayload"] = "Загрузка и применение выбранного патча...",
            ["status.downloadingDishonoredRngFixPayload"] = "Загрузка Dishonored RNG fix...",
            ["status.downloadingDish2Macro"] = "Загрузка Dish2Macro...",
            ["status.extractingDish2Macro"] = "Извлечение Dish2Macro...",
            ["status.installingDish2Macro"] = "Установка Dish2Macro...",
            ["status.dish2MacroInstalled"] = "Dish2Macro установлен.",
            ["dialog.dish2MacroDownloadedInstalled"] = "Dish2Macro загружен и установлен.",
            ["status.framerateUpdated"] = "Частота кадров обновлена.",
            ["status.gameSettingsUpdated"] = "Игровые настройки обновлены.",
            ["status.inputSettingsUpdated"] = "Настройки управления обновлены.",
            ["status.dish2MacroSettingsUpdated"] = "Настройки Dish2Macro обновлены.",
            ["tab.patchTweaks"] = "Настройки патчей",
            ["tab.gameTweaks"] = "Модификации игры",
            ["tab.inputTweaks"] = "Настройки ввода",
            ["header.patchSelection"] = "Выбор патча",
            ["header.extraPatches"] = "Дополнительные патчи",
            ["header.engineTweaks"] = "Настройки движка",
            ["header.framerateControl"] = "Контроль частоты кадров FPS",
            ["header.scrollWheel"] = "Колесо прокрутки",
            ["header.fpsKeys"] = "Клавиши FPS",
            ["header.consoleCheats"] = "Консоль и читы",
            ["header.dish2Macro"] = "Dish2Macro",
            ["header.macroSettings"] = "Настройки макросов",
            ["label.patchVersion"] = "Версия патча:",
            ["label.framerate"] = "Частота кадров (FPS):",
            ["label.jump"] = "Прыжок:",
            ["label.interact"] = "Взаимодействие:",
            ["label.bind1"] = "Бинд 1:",
            ["label.bind2"] = "Бинд 2:",
            ["label.bind3"] = "Бинд 3:",
            ["label.bind4"] = "Бинд 4:",
            ["label.consoleBindKey"] = "Бинд кнопка консоли:",
            ["label.enableCheatsKey"] = "Клавиша включения читов:",
            ["label.macroDirectory"] = "Папка макроса:",
            ["label.macroDownBind"] = "Бинд прокрутки вниз:",
            ["label.macroUpBind"] = "Бинд прокрутки вверх:",
            ["label.macroInterval"] = "Интервал (мс):",
            ["patch.1.2"] = "1.2 - Базовая игра",
            ["patch.1.3"] = "1.3 - Knife of Dunwall",
            ["patch.1.4bw"] = "1.4 - Brigmore Witches",
            ["patch.1.4daud"] = "1.4 - DaudHonored",
            ["patch.1.5"] = "1.5 - Последняя",
            ["choice.none"] = "Нет",
            ["choice.scrollUp"] = "Прокрутка вверх",
            ["choice.scrollDown"] = "Прокрутка вниз",
            ["checkbox.boyle"] = "Boyle RNG Fix",
            ["checkbox.timsh"] = "Timsh RNG Fix",
            ["checkbox.framerateLimiter"] = "Ограничитель FPS",
            ["checkbox.dynamicLights"] = "Динамическое освещение",
            ["checkbox.dynamicShadows"] = "Динамические тени",
            ["checkbox.lightShafts"] = "Световые лучи",
            ["checkbox.skipStartupMovies"] = "Пропуск вступительных видео",
            ["checkbox.pauseWhenUnfocused"] = "Пауза при сворачивании игры",
            ["text.fpsHint"] = "Значение FPS > 1",
            ["text.fpsKeysDescription"] = "Каждая строка ставит твой FPS в указанное значение.",
            ["text.key"] = "Клавиша",
            ["text.fps"] = "FPS",
            ["version.1.2"] = "1.2 - Базовая игра",
            ["version.1.3"] = "1.3 - Knife of Dunwall",
            ["version.1.4bw"] = "1.4 - Brigmore Witches",
            ["version.1.4daud"] = "1.4 - DaudHonored",
            ["version.1.5"] = "1.5 - Последняя",
            ["version.1.4or1.5"] = "1.4 или 1.5"
        };

        public string NormaliseLanguage(string? languageCode)
        {
            return string.Equals(languageCode, RussianLanguageCode, StringComparison.OrdinalIgnoreCase)
                ? RussianLanguageCode
                : EnglishLanguageCode;
        }

        public string GetText(string languageCode, string key)
        {
            string normalised = NormaliseLanguage(languageCode);
            Dictionary<string, string> dictionary = normalised == RussianLanguageCode ? RussianTexts : EnglishTexts;
            if (dictionary.TryGetValue(key, out string? value))
            {
                return value;
            }

            return EnglishTexts.TryGetValue(key, out string? fallback) ? fallback : key;
        }

        public (string Title, string Message) GetSettingInfo(string languageCode, string tag)
        {
            bool ru = NormaliseLanguage(languageCode) == RussianLanguageCode;
            return tag switch
            {
                "patch-version" => ru
                    ? ("Версия патча", "Выберите версию игры для установки.\n\nПри применении патча сначала восстанавливается базовая резервная копия, затем применяются выбранные файлы.\n\n1.2: Используется для ранов базовой игры\n1.3: Используется для ранов Knife of Dunwall\n1.4: Используется для ранов Brigmore Witches\n1.5: Последняя версия игры, по сути такая же, как 1.4.")
                    : ("Patch Version", "Pick the game version you want installed.\n\nApplying a patch restores your baseline backup first, then applies the selected files.\n\n1.2: Used for base game runs\n1.3: Used for Knife of Dunwall runs\n1.4: Used for Brigmore Witches runs\n1.5: Latest game version, functionally the same as 1.4."),
                "boyle-rng-fix" => ru
                    ? ("Boyle RNG Fix", "Этот мод убирает рандом в миссии «Последний приём леди Бойл», делая так, что сестра Бойл наверху лестницы всегда будет Эсма, устраняя рандом для некоторых спидран маршрутов.\n\nОбратите внимание: этот фикс ЗАПРЕЩЁН в некоторых категориях спидранов — проверьте правила перед использованием.")
                    : ("Boyle RNG Fix", "This mod removes the randomness in Lady Boyle's Last Party, making the Boyle sister that spawns on top of the stairs always be Esma, removing the RNG requirement for certain speedrun routes.\n\nNote that this fix is BANNED in certain speedrunning categories, check the rules before you run with the fix."),
                "timsh-rng-fix" => ru
                    ? ("Timsh RNG Fix", "Этот мод фиксирует место появления Барристера Тимша — теперь он всегда находится на первом этаже своего особняка, устраняя рандом в спидранах.\n\nОбратите внимание: этот фикс ЗАПРЕЩЁН в некоторых категориях спидранов — проверьте правила перед использованием.")
                    : ("Timsh RNG Fix", "This mod fixes the spawn location of Barrister Timsh to be on the first floor of his mansion, removing the RNG requirement for speedruns.\n\nNote that this fix is BANNED in certain speedrunning categories, check the rules before you run with the fix."),
                "framerate-limiter" => ru
                    ? ("Ограничитель FPS", "Ограничивает частоту кадров значением, указанным ниже.\n\nОтключите, если хотите снять ограничение.")
                    : ("Framerate Limiter", "Keeps gameplay capped at the FPS value entered below.\n\nDisable it if you want an uncapped framerate."),
                "framerate-fps" => ru
                    ? ("Частота кадров (FPS)", "Устанавливает максимальный FPS (можно задать любое значение от 1).\n\nСпидраны должны выполняться максимум в 250 кадров в секунду — любое значение выше может привести к отказу в подтверждении спидрана.\n\nРекомендуемое значение: 250.")
                    : ("Framerate (FPS)", "Sets the FPS cap used by the game, and can be any decimal value starting at 1.\n\nSpeedruns are required to be at a maximum of 250 frames per second; any higher may have your speedrun rejected.\n\nRecommended value: 250."),
                "dynamic-lights" => ru
                    ? ("Динамическое освещение", "Переключает освещение в реальном времени.\n\nОтключение обычно повышает производительность и убирает визуальный шум.")
                    : ("Dynamic Lights", "Toggles real-time light effects.\n\nTurning this off usually improves performance and reduces visual clutter."),
                "dynamic-shadows" => ru
                    ? ("Динамические тени", "Переключает тени в реальном времени.\n\nОтключение улучшает производительность и видимость.")
                    : ("Dynamic Shadows", "Toggles real-time shadow rendering.\n\nTurning this off usually improves performance and can improve visibility."),
                "light-shafts" => ru
                    ? ("Световые лучи", "Переключает эффекты объемного светового луча.\n\nОтключение уменьшает постобработку.")
                    : ("Light Shafts", "Toggles volumetric light-beam effects.\n\nTurning this off reduces post-processing visuals."),
                "skip-startup-movies" => ru
                    ? ("Пропуск вступительных видео", "Пропускает логотипы и заставки, ускоряя переход в меню.")
                    : ("Skip Startup Movies", "Skips logo and intro videos so the game reaches the menu faster."),
                "pause-unfocused" => ru
                    ? ("Пауза при сворачивании игры", "Когда включено, игра ставится на паузу при сворачивании (например при Alt+Tab).")
                    : ("Pause When Unfocused", "When enabled, the game pauses while unfocused (for example when alt-tabbed)."),
                "jump-scroll" => ru
                    ? ("Бинд прыжка на колесо", "Привязывает прыжок к прокрутке вверх или вниз.\n\nПрыжок и взаимодействие не могут использовать одно и то же направление прокрутки.")
                    : ("Jump Scroll Bind", "Binds Jump to mouse wheel up or down.\n\nJump and Interact cannot use the same scroll direction."),
                "interact-scroll" => ru
                    ? ("Бинд взаимодействия на колесо", "Привязывает действие Interact/Use к прокрутке вверх или вниз.\n\nПрыжок и взаимодействие не могут использовать одно и то же направление прокрутки.")
                    : ("Interact Scroll Bind", "Binds Interact/Use to mouse wheel up or down.\n\nJump and Interact cannot use the same scroll direction."),
                "fps-keys" => ru
                    ? ("Клавиши FPS", "Каждая строка привязывает клавишу, которая мгновенно меняет лимит FPS в игре.\n\nПолезно для техник передвижения, таймингов и некоторых глитчей.\n\nИспользование таких клавиш запрещено в категории glitchless.")
                    : ("FPS Keys", "Each row binds a key that instantly switches your FPS cap in-game.\n\nUseful for movement tech, setup timings, and certain glitches.\n\nFPS keys are banned in the glitchless category."),
                "console-bind" => ru
                    ? ("Бинд кнопки консоли", "Устанавливает клавишу, используемую для открытия внутриигровой консоли разработчика.")
                    : ("Console Bind Key", "Sets the key used to open the in-game developer console."),
                "enable-cheats" => ru
                    ? ("Клавиша включения читов", "Назначает клавишу для включения режима читов в консоли разработчика.\n\nПолезно для тестирования, построения маршрутов и тренировок.\n\nРежим читов полностью запрещён во всех категориях спидранов.")
                    : ("Enable Cheats Key", "Binds a key that enables cheat mode in the developer console. Useful for testing, routing and practicing the game.\n\nCheat mode is entirely banned in all speedrunning categories."),
                "macro-directory" => ru
                    ? ("Папка макроса", "Выберите место установки Dish2Macro.\n\nИспользуйте «Скачать / обновить макрос» для загрузки последних файлов в эту папку.")
                    : ("Macro Directory", "Choose where Dish2Macro should be installed.\n\nUse Download / Update Macro to fetch the latest release files into this folder."),
                "macro-down-bind" => ru
                    ? ("Бинд прокрутки вниз", "Клавиша/кнопка, которая запускает повторяющиеся события прокрутки вниз при удерживании.")
                    : ("Scroll Down Bind", "Key/button that triggers repeated scroll-down events while held."),
                "macro-up-bind" => ru
                    ? ("Бинд прокрутки вверх", "Клавиша/кнопка, которая запускает повторяющиеся события прокрутки вверх при удерживании.")
                    : ("Scroll Up Bind", "Key/button that triggers repeated scroll-up events while held."),
                "macro-interval" => ru
                    ? ("Интервал макроса", "Как часто макрос отправляет события прокрутки в миллисекундах.\n\nМеньшее значение — больше событий. Значение по умолчанию: 5.")
                    : ("Macro Interval", "How often the macro sends scroll events, in milliseconds.\n\nLower values send more events. Default is 5."),
                _ => ru
                    ? ("Информация", "Для этого параметра нет дополнительной информации.")
                    : ("Information", "No additional details are available for this setting.")
            };
        }
    }
}

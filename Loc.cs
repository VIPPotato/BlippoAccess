using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MelonLoader;
using NobleRobot;

namespace BlippoAccess
{
    /// <summary>
    /// Mod-localized string store with language routing based on the game's current language.
    /// </summary>
    public static class Loc
    {
        private enum LanguageKey
        {
            AmericanEnglish,
            Japanese,
            French,
            LatinAmericanSpanish,
            German,
            Dutch,
            BrazilianPortuguese,
            Italian,
            ChineseSimplified,
            ChineseTraditional,
            Korean,
            Russian,
            BritishEnglish,
            CanadianFrench,
            EuropeanSpanish,
            Portuguese
        }

        private static readonly Dictionary<LanguageKey, Dictionary<string, string>> _strings = CreateLanguageDictionaries();
        private const string ExternalLocalizationFolderName = "BlippoAccessLocalization";

        private static bool _initialized;
        private static LanguageKey _currentLanguage = LanguageKey.AmericanEnglish;

        /// <summary>
        /// Initializes localization dictionaries and reads the current game language.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            InitializeStrings();
            LoadExternalOverrides();
            RefreshLanguage();
            _initialized = true;
        }

        /// <summary>
        /// Refreshes the active mod language from the game language setting.
        /// </summary>
        public static void RefreshLanguage()
        {
            _currentLanguage = MapGameLanguage(GetGameLanguage());
        }

        /// <summary>
        /// Reloads external localization overrides from disk.
        /// </summary>
        /// <returns>The number of override entries applied across all language files.</returns>
        public static int ReloadExternalOverrides()
        {
            var applied = LoadExternalOverrides();
            RefreshLanguage();
            return applied;
        }

        /// <summary>
        /// Gets a localized string by key with English fallback.
        /// </summary>
        /// <param name="key">The localization key.</param>
        /// <returns>Localized string if found, otherwise English fallback or the key itself.</returns>
        public static string Get(string key)
        {
            if (!_initialized)
            {
                Initialize();
            }

            RefreshLanguage();

            if (_strings[_currentLanguage].TryGetValue(key, out var value))
            {
                return value;
            }

            if (_strings[LanguageKey.AmericanEnglish].TryGetValue(key, out var fallback))
            {
                return fallback;
            }

            return key;
        }

        /// <summary>
        /// Gets a localized and formatted string using standard format placeholders.
        /// </summary>
        /// <param name="key">The localization key.</param>
        /// <param name="args">Formatting values for placeholders like {0} and {1}.</param>
        /// <returns>Formatted localized string.</returns>
        public static string Get(string key, params object[] args)
        {
            var template = Get(key);
            if (args == null || args.Length == 0)
            {
                return template;
            }

            return string.Format(CultureInfo.InvariantCulture, template, args);
        }

        private static Dictionary<LanguageKey, Dictionary<string, string>> CreateLanguageDictionaries()
        {
            var result = new Dictionary<LanguageKey, Dictionary<string, string>>();
            foreach (LanguageKey language in Enum.GetValues(typeof(LanguageKey)))
            {
                result[language] = new Dictionary<string, string>(StringComparer.Ordinal);
            }

            return result;
        }

        private static Language GetGameLanguage()
        {
            try
            {
                return LanguageUtility.currentLanguage;
            }
            catch
            {
                return Language.AMERICAN_ENGLISH;
            }
        }

        private static LanguageKey MapGameLanguage(Language gameLanguage)
        {
            switch (gameLanguage)
            {
                case Language.AMERICAN_ENGLISH:
                    return LanguageKey.AmericanEnglish;
                case Language.JAPANESE:
                    return LanguageKey.Japanese;
                case Language.FRENCH:
                    return LanguageKey.French;
                case Language.LATIN_AMERICAN_SPANISH:
                    return LanguageKey.LatinAmericanSpanish;
                case Language.GERMAN:
                    return LanguageKey.German;
                case Language.DUTCH:
                    return LanguageKey.Dutch;
                case Language.BRAZILIAN_PORTUGUESE:
                    return LanguageKey.BrazilianPortuguese;
                case Language.ITALIAN:
                    return LanguageKey.Italian;
                case Language.CHINESE_SIMPLIFIED:
                    return LanguageKey.ChineseSimplified;
                case Language.CHINESE_TRADITIONAL:
                    return LanguageKey.ChineseTraditional;
                case Language.KOREAN:
                    return LanguageKey.Korean;
                case Language.RUSSIAN:
                    return LanguageKey.Russian;
                case Language.BRITISH_ENGLISH:
                    return LanguageKey.BritishEnglish;
                case Language.CANADIAN_FRENCH:
                    return LanguageKey.CanadianFrench;
                case Language.EUROPEAN_SPANISH:
                    return LanguageKey.EuropeanSpanish;
                case Language.PORTUGUESE:
                    return LanguageKey.Portuguese;
                default:
                    return LanguageKey.AmericanEnglish;
            }
        }

        private static void Add(string key, string english, IDictionary<LanguageKey, string> overrides = null)
        {
            foreach (var dictionary in _strings.Values)
            {
                dictionary[key] = english;
            }

            if (overrides == null)
            {
                return;
            }

            foreach (var pair in overrides)
            {
                _strings[pair.Key][key] = pair.Value;
            }
        }

        private static int LoadExternalOverrides()
        {
            var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ExternalLocalizationFolderName);
            if (!Directory.Exists(folderPath))
            {
                return 0;
            }

            var totalApplied = 0;
            foreach (LanguageKey language in Enum.GetValues(typeof(LanguageKey)))
            {
                var fileName = GetOverrideFileName(language);
                var filePath = Path.Combine(folderPath, fileName);
                if (!File.Exists(filePath))
                {
                    continue;
                }

                var applied = ApplyOverridesFromFile(language, filePath);
                if (applied > 0)
                {
                    MelonLogger.Msg($"Loaded {applied} localization overrides for {language} from {fileName}.");
                }

                totalApplied += applied;
            }

            return totalApplied;
        }

        private static int ApplyOverridesFromFile(LanguageKey language, string filePath)
        {
            var applied = 0;
            var fileName = Path.GetFileName(filePath);
            var lines = File.ReadAllLines(filePath);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                line = line.Trim();
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal))
                {
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    MelonLogger.Warning($"Skipping invalid localization override in {fileName} at line {i + 1}.");
                    continue;
                }

                var key = line.Substring(0, separatorIndex).Trim();
                var value = line.Substring(separatorIndex + 1).Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    MelonLogger.Warning($"Skipping localization override with empty key in {fileName} at line {i + 1}.");
                    continue;
                }

                _strings[language][key] = DecodeOverrideValue(value);
                applied++;
            }

            return applied;
        }

        private static string DecodeOverrideValue(string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOf('\\') < 0)
            {
                return value;
            }

            var buffer = new char[value.Length];
            var bufferIndex = 0;
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c != '\\' || i + 1 >= value.Length)
                {
                    buffer[bufferIndex++] = c;
                    continue;
                }

                var escaped = value[++i];
                switch (escaped)
                {
                    case 'n':
                        buffer[bufferIndex++] = '\n';
                        break;
                    case 'r':
                        buffer[bufferIndex++] = '\r';
                        break;
                    case 't':
                        buffer[bufferIndex++] = '\t';
                        break;
                    case '\\':
                        buffer[bufferIndex++] = '\\';
                        break;
                    case '=':
                        buffer[bufferIndex++] = '=';
                        break;
                    default:
                        buffer[bufferIndex++] = escaped;
                        break;
                }
            }

            return new string(buffer, 0, bufferIndex);
        }

        private static string GetOverrideFileName(LanguageKey language)
        {
            switch (language)
            {
                case LanguageKey.AmericanEnglish:
                    return "en-us.txt";
                case LanguageKey.Japanese:
                    return "ja-jp.txt";
                case LanguageKey.French:
                    return "fr-fr.txt";
                case LanguageKey.LatinAmericanSpanish:
                    return "es-419.txt";
                case LanguageKey.German:
                    return "de-de.txt";
                case LanguageKey.Dutch:
                    return "nl-nl.txt";
                case LanguageKey.BrazilianPortuguese:
                    return "pt-br.txt";
                case LanguageKey.Italian:
                    return "it-it.txt";
                case LanguageKey.ChineseSimplified:
                    return "zh-hans.txt";
                case LanguageKey.ChineseTraditional:
                    return "zh-hant.txt";
                case LanguageKey.Korean:
                    return "ko-kr.txt";
                case LanguageKey.Russian:
                    return "ru-ru.txt";
                case LanguageKey.BritishEnglish:
                    return "en-gb.txt";
                case LanguageKey.CanadianFrench:
                    return "fr-ca.txt";
                case LanguageKey.EuropeanSpanish:
                    return "es-es.txt";
                case LanguageKey.Portuguese:
                    return "pt-pt.txt";
                default:
                    return "en-us.txt";
            }
        }

        private static void InitializeStrings()
        {
            // Core mod messages
            Add("mod_loaded", "Blippo Access mod loaded. F1 reports current context. F2 repeats last announcement. F3 stops speech. F4 reports tuned channel. F5 reloads localization. F6/F7 browse announcement history. F8 reads controls help. F9 reports inbox summary. F10 reports signal diagnostics. F12 toggles debug mode.");
            Add("debug_enabled", "Debug mode enabled.");
            Add("debug_disabled", "Debug mode disabled.");
            Add("repeat_last_none", "No previous announcement to repeat.");
            Add("announcement_history_empty", "Announcement history is empty.");
            Add("localization_overrides_reloaded", "Localization overrides reloaded. {0} entries applied.");
            Add("localization_overrides_reloaded_none", "Localization overrides reloaded. No override entries found.");
            Add("help_general", "Use arrow keys to navigate. Enter activates. Back returns.");
            Add("help_program_guide", "Program guide: arrows navigate channels and shows. Enter tunes selected channel. Back returns to broadcast. Menu opens control menu.");
            Add("help_broadcast", "Broadcast: use channel controls to switch channels. Toggle captions, data mode, and info panel as needed.");
            Add("help_control_menu", "Control menu: up and down move through options. Enter activates. Back goes to previous menu.");
            Add("help_control_menu_data_manager", "Control menu data screens: up and down move options. Use previous and next controls to switch pages and hear updated summaries. Enter activates. Back goes to previous menu.");
            Add("help_control_menu_factory_reset", "Factory reset permanently erases all viewer data and settings. Back cancels. Enter on Factory Reset confirms.");
            Add("help_messages_list", "Messages list: up and down navigate messages. Enter opens selected message. Back returns.");
            Add("help_messages_content", "Message view: up and down navigate Back and Load Packette actions. Enter activates. Back returns to message list.");
            Add("help_femtofax", "Femtofax: select a program to open. Use previous and next controls to move between messages. Home returns to program list.");
            Add("help_tuner_calibration", "Tuner calibration: adjust all four tuner values until locked. Then confirm to continue.");
            Add("help_packette_load", "Packette load: wait for scan steps to complete. Activate continue or load when prompted.");
            Add("help_credits", "Credits: use left and right to skip sections. Back returns.");
            Add("help_signal_loss", "Signal loss: follow on-screen choices. Tuner calibration may be required.");
            Add("broadcast_context_unavailable", "Broadcast context is not available.");
            Add("where_am_i_screen_unknown", "Unknown screen");
            Add("where_am_i_unknown_item", "Unknown item");
            Add("where_am_i_no_focus", "No focused item");
            Add("where_am_i_screen_only", "{0}");
            Add("where_am_i_screen_detail", "{0}. {1}");
            Add("where_am_i_broadcast_with_focus", "{0}. Focus: {1}");
            Add("where_am_i_submenu_item", "{0}: {1}");
            Add("where_am_i_data_submenu_with_focus", "{0}. Data summary: {1}");
            Add("where_am_i_data_submenu_no_focus", "{0}. Data summary: {1}");
            Add("where_am_i_factory_reset_with_focus", "{0}. {1} {2}");
            Add("where_am_i_factory_reset_no_focus", "{0} {1}");
            Add("where_am_i_item_position", "{0}, {1} of {2}");
            Add("where_am_i_item_value", "{0}: {1}");
            Add("where_am_i_item_value_position", "{0}: {1}, {2} of {3}");
            Add("where_am_i_femtofax_program", "Program: {0}");
            Add("where_am_i_femtofax_program_message", "Program: {0}. Message {1}: {2}");
            Add("where_am_i_femtofax_program_message_no_subject", "Program: {0}. Message {1}");
            Add("where_am_i_femtofax_message", "Message {0}: {1}");
            Add("where_am_i_femtofax_message_no_subject", "Message {0}");
            Add("where_am_i_femtofax_with_focus", "{0}. Focus: {1}");
            Add("where_am_i_packette_status_channels", "{0}. {1} channels found");
            Add("where_am_i_packette_channels_only", "{0} channels found");
            Add("where_am_i_packette_with_focus", "{0}. Focus: {1}");
            Add("where_am_i_tuner_ready", "Calibration status: ready to confirm");
            Add("where_am_i_tuner_adjusting", "Calibration status: adjusting values");
            Add("where_am_i_tuner_with_focus", "{0}. Focus: {1}");
            Add("where_am_i_signal_loss_only", "{0}");
            Add("where_am_i_signal_loss_with_values", "{0}. {1}");
            Add("where_am_i_credits_with_focus", "{0}. Focus: {1}");
            Add("factory_reset_submenu_warning", "Factory reset menu. This permanently erases all viewer data and settings.");
            Add("factory_reset_focus_warning", "Warning: activating this will erase all progress.");
            Add("factory_reset_cancel_hint", "Back cancels factory reset.");
            Add("factory_reset_submitting", "Factory reset confirmed.");
            Add("factory_reset_completed", "Factory reset complete. Starting first-time packette setup.");
            Add("state_on", "on");
            Add("state_off", "off");
            Add("state_read", "read");
            Add("state_unread", "unread");

            // System screens
            Add("screen_control_menu", "Control menu.");
            Add("screen_messages", "Messages.");
            Add("screen_tuner_calibration", "Tuner calibration.");
            Add("screen_packette_load", "Packette load.");
            Add("screen_credits", "Credits.");
            Add("screen_program_guide", "Program guide.");
            Add("screen_broadcast_display", "Broadcast display.");
            Add("screen_femtofax", "Femtofax.");
            Add("screen_signal_loss", "Signal loss.");
            Add("screen_unknown", "Unknown screen.");

            // Context hints
            Add("hint_program_guide", "Back tunes to the current channel. Menu opens control menu.");
            Add("hint_tuner_calibration", "Adjust all four values until locked, then confirm.");
            Add("hint_signal_loss", "Signal has degraded. Follow on-screen options.");

            // Signal loss modal states
            Add("signal_loss", "Signal warning.");
            Add("signal_loss_warning", "Signal drift detected. Tuner calibration recommended.");
            Add("signal_loss_prompt", "Signal now wonked. Please calibrate tuner.");
            Add("signal_loss_force", "Signal lost. Tuner calibration required.");
            Add("signal_loss_modal_text", "{0}");
            Add("signal_strength_values", "E V R P {0}. E A R C P {1}.");
            Add("signal_loss_hint", "Open tuner calibration when ready.");
            Add("signal_loss_warning_hint", "Select OK to continue watching for now.");
            Add("signal_loss_prompt_hint", "Choose calibrate to tune now, or ignore to postpone.");
            Add("signal_loss_force_hint", "Calibration is required to continue.");
            Add("signal_loss_focus_button", "{0}, {1} of {2}");
            Add("signal_loss_button_calibrate", "Calibrate");
            Add("signal_loss_button_ignore", "Ignore");
            Add("signal_loss_button_ok", "OK");
            Add("signal_status_modal_none", "stable");
            Add("signal_status_modal_warning", "warning");
            Add("signal_status_modal_prompt", "prompt");
            Add("signal_status_modal_force", "critical");
            Add("signal_status_summary", "Signal {0}. E V R P {1}. E A R C P {2}.");
            Add("signal_status_unavailable", "Signal diagnostics are not available.");

            // Broadcast tuning announcements
            Add("broadcast_unknown_channel", "Unknown channel");
            Add("broadcast_channel_only", "Channel {0}, {1}");
            Add("broadcast_channel_show", "Channel {0}, {1}. Now showing {2}");
            Add("broadcast_now_showing", "Now showing {0}");
            Add("broadcast_captions_on", "Captions enabled");
            Add("broadcast_captions_off", "Captions disabled");
            Add("broadcast_data_mode_on", "Data mode enabled");
            Add("broadcast_data_mode_off", "Data mode disabled");
            Add("broadcast_info_panel_shown", "Program info shown");
            Add("broadcast_info_panel_hidden", "Program info hidden");
            Add("submenu_focus_combined", "{0}: {1}");

            // Control menu focus announcements
            Add("control_submenu_opened", "Control menu: {0}");
            Add("control_focus_option", "{0}");
            Add("control_focus_option_position", "{2}, {0} of {1}");
            Add("control_focus_option_value", "{0}: {1}");
            Add("control_focus_option_value_position", "{2}: {3}, {0} of {1}");
            Add("control_packette_label", "Packette {0}");

            // Messages menu and content announcements
            Add("messages_menu", "Messages menu");
            Add("messages_submenu_opened", "Messages: {0}");
            Add("messages_entry_combined", "Messages menu. {0}. {1}");
            Add("messages_entry_empty", "{0}");
            Add("messages_entry_first_unavailable", "No message row is currently focused");
            Add("messages_focus_item", "{2}, message {0} of {1}. {3}");
            Add("messages_focus_item_no_pos", "Message: {0}. {1}");
            Add("messages_focus_prev_page", "Previous messages page");
            Add("messages_focus_next_page", "Next messages page");
            Add("messages_focus_menu_button", "{0}");
            Add("messages_focus_option", "{0}");
            Add("messages_focus_option_position", "{2}, {0} of {1}");
            Add("messages_focus_option_value", "{0}: {1}");
            Add("messages_focus_option_value_position", "{2}: {3}, {0} of {1}");
            Add("messages_content_subject", "Subject: {0}");
            Add("messages_content_body", "{0}");
            Add("messages_content_intro", "Message content");
            Add("messages_content_packette_available", "Data packette available: {0}");
            Add("messages_content_packette_available_generic", "Data packette available in this message");
            Add("messages_summary_counts", "Messages: {0} total, {1} unread");
            Add("messages_summary_counts_short", "{0} total, {1} unread");
            Add("messages_summary_empty", "Messages: none received");
            Add("messages_new_available", "New message available.");
            Add("messages_summary_unavailable", "Message summary is not available.");

            // Data manager content announcements
            Add("data_manager_summary_title", "{0}");

            // Packette load announcements
            Add("packette_filename", "Packette file: {0}");
            Add("packette_step_status", "{0}");
            Add("packette_step_launch", "Preparing packette load");
            Add("packette_step_scanning_subspace", "Scanning subspace");
            Add("packette_step_signal_acquired", "Signal acquired");
            Add("packette_step_signal_acquired_hint", "Press Enter to continue");
            Add("packette_step_scanning_channels", "Scanning for channels");
            Add("packette_step_unpacking", "Unpacking data");
            Add("packette_step_scan_complete", "Scan complete");
            Add("packette_step_scan_complete_count", "Scan complete. {0} channels found");
            Add("packette_channel_found_named", "Found channel {0}: {1}. {2} found");
            Add("packette_channel_found", "Found channel {0}. {1} found");
            Add("packette_channel_found_total", "Channel found. {0} total");
            Add("packette_focus_button", "{0}, 1 of 1");
            Add("packette_button_continue", "Continue");
            Add("packette_button_load", "Load packette");

            // Credits announcements
            Add("credits_controls_hint", "Use left and right to skip sections. Use back to return.");
            Add("credits_section", "{0}, section {1} of {2}");
            Add("credits_section_text", "{0}");
            Add("credits_section_unknown", "Credits section");

            // Femtofax announcements
            Add("femtofax_focus_enter", "Open Femtofax programs");
            Add("femtofax_program_unknown", "Unknown Femtofax program");
            Add("femtofax_focus_program", "{2}, program {0} of {1}");
            Add("femtofax_focus_button", "{0}");
            Add("femtofax_nav_prev_message", "Previous message");
            Add("femtofax_nav_next_message", "Next message");
            Add("femtofax_nav_home", "Return to Femtofax home");
            Add("femtofax_subject", "Femtofax subject: {0}");
            Add("femtofax_author", "From {0}");
            Add("femtofax_rating", "Rating {0}");
            Add("femtofax_body", "{0}");
            Add("femtofax_summary_welcome", "Femtofax welcome. Choose a program");
            Add("femtofax_summary_program", "Femtofax program: {0}");

            // Program guide focus announcements
            Add("pg_focus_generic", "Program guide focus: {0}");
            Add("pg_focus_item", "{0}");
            Add("pg_focus_row", "{2}, {0} of {1}");
            Add("pg_focus_button", "{2}, top button {0} of {1}");
            Add("pg_button_expand", "Expand listings, {0}");
            Add("pg_button_menu", "Open control menu");
            Add("pg_button_messages", "Open messages");
            Add("pg_button_return_broadcast", "Return to broadcast");
            Add("pg_channel_unknown", "Unknown channel.");
            Add("pg_channel_label", "Channel {0}, {1}");
            Add("pg_item_unknown", "Unknown item.");
            Add("pg_show_unknown", "Unknown show.");
            Add("pg_grid_item_channel", "{0}: {1} on {2}");
            Add("pg_grid_item", "{0}: {1}");
            Add("pg_show_row", "Show listing: {0}");
            Add("pg_full_width_episode", "Full row for {0}, current show {1}");
            Add("pg_full_width_episode_no_channel", "Full row, current show {0}");
            Add("pg_full_width_channel", "Full row for channel {0}, {1}");
            Add("pg_slot_now", "Now");
            Add("pg_slot_next", "Next");
            Add("pg_slot_later", "Later");
            Add("pg_slot_upcoming", "Upcoming");

            // Tuner calibration focus and value announcements
            Add("tuner_focus_property", "{0}: {1}. {2}.");
            Add("tuner_value_changed", "{0}: {1}. {2}.");
            Add("tuner_focus_confirm", "Confirm calibration button, {0}.");
            Add("tuner_focus_cancel", "Cancel calibration and return.");
            Add("tuner_status_locked", "locked");
            Add("tuner_status_adjust", "adjusting");
            Add("tuner_button_available", "available");
            Add("tuner_button_unavailable", "unavailable");
            Add("tuner_ready_to_confirm", "All values locked. Confirm calibration is available.");
            Add("tuner_property_unknown", "Unknown property");
            Add("tuner_value_unknown", "Unknown value");
        }
    }
}

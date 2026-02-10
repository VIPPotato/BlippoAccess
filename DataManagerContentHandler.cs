using System;
using System.Collections.Generic;
using NobleRobot;
using UnityEngine;

namespace BlippoAccess
{
    /// <summary>
    /// Announces non-focus data shown in Control Menu data submenus such as Viewer Activity and Packette Logs.
    /// </summary>
    public sealed class DataManagerContentHandler
    {
        private const float MinimumAnnouncementIntervalSeconds = 0.45f;

        private bool _wasActive;
        private string _lastSummarySignature = string.Empty;
        private float _lastAnnouncementTime;
        private Submenu _lastSubmenu;

        /// <summary>
        /// Tracks Control Menu data submenus and announces current data values when content changes.
        /// </summary>
        public void Update()
        {
            var menu = GetControlMenu();
            if (menu == null || menu.currentSubmenu == null)
            {
                ResetState();
                return;
            }

            _wasActive = true;
            var submenu = menu.currentSubmenu;
            if (!IsDataSubmenu(submenu))
            {
                _lastSubmenu = submenu;
                _lastSummarySignature = string.Empty;
                return;
            }

            if (!TryBuildSummary(submenu, out var summary, out var signature))
            {
                return;
            }

            var submenuChanged = submenu != _lastSubmenu;
            _lastSubmenu = submenu;
            if (!submenuChanged && string.Equals(signature, _lastSummarySignature, StringComparison.Ordinal))
            {
                return;
            }

            if (Time.unscaledTime - _lastAnnouncementTime < MinimumAnnouncementIntervalSeconds)
            {
                return;
            }

            var title = GetSubmenuTitle(submenu);
            if (!string.IsNullOrWhiteSpace(title))
            {
                ScreenReader.SayQueued(Loc.Get("data_manager_summary_title", title));
            }

            ScreenReader.SayQueued(summary);
            _lastSummarySignature = signature;
            _lastAnnouncementTime = Time.unscaledTime;
            DebugLogger.Log(LogCategory.Handler, $"Data submenu summary: {summary}");
        }

        /// <summary>
        /// Builds the current Data Manager submenu summary when one is visible.
        /// </summary>
        /// <param name="submenu">The submenu to inspect.</param>
        /// <param name="title">Resolved submenu title.</param>
        /// <param name="summary">Resolved summary line.</param>
        /// <returns>True when a supported data submenu is active and has readable values.</returns>
        internal static bool TryBuildCurrentSummary(Submenu submenu, out string title, out string summary)
        {
            title = GetSubmenuTitle(submenu);
            summary = string.Empty;
            if (!IsDataSubmenu(submenu))
            {
                return false;
            }

            return TryBuildSummary(submenu, out summary, out _);
        }

        internal static bool IsDataSubmenuContext(Submenu submenu)
        {
            return IsDataSubmenu(submenu);
        }

        private void ResetState()
        {
            if (!_wasActive)
            {
                return;
            }

            _wasActive = false;
            _lastSummarySignature = string.Empty;
            _lastAnnouncementTime = 0f;
            _lastSubmenu = null;
        }

        private static ControlMenu GetControlMenu()
        {
            if (GameManager.instance == null || Bookshelf.instance == null)
            {
                return null;
            }

            if (GameManager.currentSystemScreen != SystemScreen.Type.CONTROL_MENU)
            {
                return null;
            }

            if (!Bookshelf.instance.systemScreens.ContainsKey(SystemScreen.Type.CONTROL_MENU))
            {
                return null;
            }

            var screen = Bookshelf.instance.systemScreens[SystemScreen.Type.CONTROL_MENU];
            if (screen == null || !screen.screenEnabled)
            {
                return null;
            }

            return screen as ControlMenu;
        }

        private static bool IsDataSubmenu(Submenu submenu)
        {
            var title = GetSubmenuTitle(submenu).ToLowerInvariant();
            return title.Contains("viewer activity") ||
                title.Contains("packette log") ||
                title.Contains("concatenated log");
        }

        private static string GetSubmenuTitle(Submenu submenu)
        {
            if (submenu == null)
            {
                return string.Empty;
            }

            var title = UiTextHelper.GetLocalizedText(submenu.menuTitleLocalizedText);
            if (!string.IsNullOrWhiteSpace(title))
            {
                return title;
            }

            return UiTextHelper.CleanText(submenu.gameObject.name);
        }

        private static bool TryBuildSummary(Submenu submenu, out string summary, out string signature)
        {
            summary = string.Empty;
            signature = string.Empty;
            if (submenu == null)
            {
                return false;
            }

            var values = new List<string>();
            var signatures = new List<string>();
            var fields = submenu.GetComponentsInChildren<LocalizedTextControllerAppend>(true);
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (field == null || !field.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (field.GetComponentInParent<Submenu>(true) != submenu)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(field.overrideString))
                {
                    continue;
                }

                var line = UiTextHelper.GetLocalizedText(field);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                values.Add(line);
                signatures.Add(line);
            }

            if (values.Count == 0)
            {
                return false;
            }

            summary = string.Join(". ", values);
            signature = string.Join("|", signatures);
            return true;
        }
    }
}

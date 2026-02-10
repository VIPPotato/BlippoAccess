using TMPro;
using UnityEngine;

namespace BlippoAccess
{
    /// <summary>
    /// Announces Credits navigation context and current section while credits roll.
    /// </summary>
    public sealed class CreditsHandler
    {
        private const float SectionAnnouncementCooldownSeconds = 1.25f;

        private bool _wasActive;
        private bool _entryAnnounced;
        private int _lastSectionIndex = -1;
        private float _lastAnnouncementTime;

        /// <summary>
        /// Tracks Credits screen activity and reads section changes.
        /// </summary>
        public void Update()
        {
            var credits = GetCreditsScreen();
            if (credits == null)
            {
                ResetState();
                return;
            }

            _wasActive = true;
            AnnounceEntry();
            AnnounceCurrentSection(credits);
        }

        /// <summary>
        /// Attempts to build the current credits-section announcement.
        /// </summary>
        /// <param name="announcement">Current section announcement when available.</param>
        /// <returns>True when a section announcement could be built.</returns>
        public static bool TryBuildCurrentSectionAnnouncement(out string announcement)
        {
            announcement = string.Empty;
            var credits = GetCreditsScreen();
            if (credits == null)
            {
                return false;
            }

            var currentSectionIndex = GetCurrentSectionIndex(credits);
            if (currentSectionIndex < 0)
            {
                return false;
            }

            var total = credits.sections != null ? credits.sections.Count : 0;
            var title = GetSectionTitle(credits, currentSectionIndex);
            if (string.IsNullOrWhiteSpace(title))
            {
                title = Loc.Get("credits_section_unknown");
            }

            announcement = total > 0
                ? Loc.Get("credits_section", title, currentSectionIndex + 1, total)
                : Loc.Get("credits_section_text", title);
            return true;
        }

        private void ResetState()
        {
            if (!_wasActive)
            {
                return;
            }

            _wasActive = false;
            _entryAnnounced = false;
            _lastSectionIndex = -1;
            _lastAnnouncementTime = 0f;
        }

        private void AnnounceEntry()
        {
            if (_entryAnnounced)
            {
                return;
            }

            _entryAnnounced = true;
            ScreenReader.SayQueued(Loc.Get("credits_controls_hint"));
            _lastAnnouncementTime = Time.unscaledTime;
            DebugLogger.Log(LogCategory.Handler, "Credits entry hint announced");
        }

        private void AnnounceCurrentSection(Credits credits)
        {
            var currentSectionIndex = GetCurrentSectionIndex(credits);
            if (currentSectionIndex < 0 || currentSectionIndex == _lastSectionIndex)
            {
                return;
            }

            if (Time.unscaledTime - _lastAnnouncementTime < SectionAnnouncementCooldownSeconds)
            {
                return;
            }

            _lastSectionIndex = currentSectionIndex;
            var total = credits.sections != null ? credits.sections.Count : 0;
            var title = GetSectionTitle(credits, currentSectionIndex);
            if (string.IsNullOrWhiteSpace(title))
            {
                title = Loc.Get("credits_section_unknown");
            }

            if (total > 0)
            {
                ScreenReader.Say(Loc.Get("credits_section", title, currentSectionIndex + 1, total));
            }
            else
            {
                ScreenReader.Say(Loc.Get("credits_section_text", title));
            }

            _lastAnnouncementTime = Time.unscaledTime;
            DebugLogger.Log(LogCategory.Handler, $"Credits section: {title} ({currentSectionIndex + 1}/{total})");
        }

        private static Credits GetCreditsScreen()
        {
            if (GameManager.instance == null || Bookshelf.instance == null)
            {
                return null;
            }

            if (GameManager.currentSystemScreen != SystemScreen.Type.CREDITS)
            {
                return null;
            }

            if (!Bookshelf.instance.systemScreens.ContainsKey(SystemScreen.Type.CREDITS))
            {
                return null;
            }

            var screen = Bookshelf.instance.systemScreens[SystemScreen.Type.CREDITS];
            if (screen == null || !screen.screenEnabled)
            {
                return null;
            }

            return screen as Credits;
        }

        private static int GetCurrentSectionIndex(Credits credits)
        {
            if (credits.sections == null || credits.sections.Count == 0)
            {
                return -1;
            }

            for (var i = 0; i < credits.sections.Count; i++)
            {
                var section = credits.sections[i];
                if (section == null)
                {
                    continue;
                }

                if (section.position.y > -270f && section.position.y < 0f)
                {
                    return i;
                }
            }

            return -1;
        }

        private static string GetSectionTitle(Credits credits, int sectionIndex)
        {
            if (credits.sections == null || sectionIndex < 0 || sectionIndex >= credits.sections.Count)
            {
                return string.Empty;
            }

            var section = credits.sections[sectionIndex];
            if (section == null)
            {
                return string.Empty;
            }

            var textFields = section.GetComponentsInChildren<TMP_Text>(true);
            if (textFields == null || textFields.Length == 0)
            {
                return string.Empty;
            }

            for (var i = 0; i < textFields.Length; i++)
            {
                var text = UiTextHelper.GetText(textFields[i]);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (text.Length > 140)
                {
                    continue;
                }

                return text;
            }

            return string.Empty;
        }
    }
}

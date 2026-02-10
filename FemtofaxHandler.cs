using System;
using NobleRobot;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlippoAccess
{
    /// <summary>
    /// Announces Femtofax focus changes and message content.
    /// </summary>
    public sealed class FemtofaxHandler
    {
        private const float MinimumAnnouncementIntervalSeconds = 0.08f;
        private const float EntrySummaryFallbackSeconds = 0.75f;
        private const float ContentFocusSuppressionSeconds = 2.25f;

        private bool _wasActive;
        private int _lastSelectionId = int.MinValue;
        private float _lastAnnouncementTime;
        private string _lastMessageSignature = string.Empty;
        private bool _entrySummaryAnnounced;
        private float _entryStartTime;
        private float _focusSuppressedUntil;
        private string _pendingEntrySummary = string.Empty;
        private float _pendingEntrySummaryTime;

        /// <summary>
        /// Tracks Femtofax navigation focus and reads current Femtofax frame content.
        /// </summary>
        public void Update()
        {
            var femtofax = GetFemtofaxScreen();
            if (femtofax == null)
            {
                ResetState();
                return;
            }

            _wasActive = true;
            if (_entryStartTime <= 0f)
            {
                _entryStartTime = Time.unscaledTime;
            }

            AnnounceEntrySummary(femtofax);
            FlushPendingEntrySummaryIfStale();
            AnnounceFocus(femtofax);
            AnnounceFrameContent(femtofax);
        }

        private void ResetState()
        {
            if (!_wasActive)
            {
                return;
            }

            _wasActive = false;
            _lastSelectionId = int.MinValue;
            _lastAnnouncementTime = 0f;
            _lastMessageSignature = string.Empty;
            _entrySummaryAnnounced = false;
            _entryStartTime = 0f;
            _focusSuppressedUntil = 0f;
            _pendingEntrySummary = string.Empty;
            _pendingEntrySummaryTime = 0f;
        }

        private void AnnounceEntrySummary(Femtofax femtofax)
        {
            if (_entrySummaryAnnounced)
            {
                return;
            }

            if (Time.unscaledTime - _entryStartTime < 0.25f)
            {
                return;
            }

            var program = femtofax.GetProgram();
            if (program == null || program.femtofaxProgramObject == null || program.femtofaxProgramObject.title == null)
            {
                _pendingEntrySummary = Loc.Get("femtofax_summary_welcome");
                _pendingEntrySummaryTime = Time.unscaledTime;
                _entrySummaryAnnounced = true;
                return;
            }

            var title = UiTextHelper.CleanText(program.femtofaxProgramObject.title.Get());
            if (string.IsNullOrWhiteSpace(title))
            {
                _pendingEntrySummary = Loc.Get("femtofax_summary_welcome");
            }
            else
            {
                _pendingEntrySummary = Loc.Get("femtofax_summary_program", title);
            }

            _pendingEntrySummaryTime = Time.unscaledTime;
            _entrySummaryAnnounced = true;
        }

        private void AnnounceFocus(Femtofax femtofax)
        {
            if (Time.unscaledTime < _focusSuppressedUntil)
            {
                return;
            }

            var selectedObject = EventSystem.current?.currentSelectedGameObject;
            if (selectedObject == null)
            {
                return;
            }

            var selectionId = selectedObject.GetInstanceID();
            if (selectionId == _lastSelectionId)
            {
                return;
            }

            if (Time.unscaledTime - _lastAnnouncementTime < MinimumAnnouncementIntervalSeconds)
            {
                return;
            }

            if (!TryBuildFocusAnnouncement(femtofax, selectedObject, out var announcement))
            {
                _lastSelectionId = selectionId;
                return;
            }

            var hasPendingEntrySummary = !string.IsNullOrWhiteSpace(_pendingEntrySummary);
            if (hasPendingEntrySummary)
            {
                announcement = Loc.Get("submenu_focus_combined", _pendingEntrySummary, announcement);
                _pendingEntrySummary = string.Empty;
                _pendingEntrySummaryTime = 0f;
                ScreenReader.SayQueued(announcement);
            }
            else
            {
                ScreenReader.Say(announcement);
            }

            _lastSelectionId = selectionId;
            _lastAnnouncementTime = Time.unscaledTime;
            DebugLogger.Log(LogCategory.Handler, $"Femtofax focus: {announcement}");
        }

        private void AnnounceFrameContent(Femtofax femtofax)
        {
            var program = femtofax.GetProgram();
            if (program == null || program.frame == null || !program.frame.gameObject.activeInHierarchy)
            {
                return;
            }

            if (program.frame.currentMessageIndex < 0)
            {
                return;
            }

            var subject = UiTextHelper.GetLocalizedText(program.frame.subjectText);
            var author = program.frame.authorText != null ? UiTextHelper.CleanText(program.frame.authorText.cachedTextAppend) : string.Empty;
            var body = UiTextHelper.GetLocalizedText(program.frame.messageText);
            var rating = UiTextHelper.GetText(program.frame.ratingText);

            var signature = GetProgramId(program) + "|" + program.frame.currentMessageIndex + "|" + subject + "|" + author + "|" + body + "|" + rating;
            if (signature == _lastMessageSignature)
            {
                return;
            }

            _lastMessageSignature = signature;
            if (!string.IsNullOrWhiteSpace(subject))
            {
                ScreenReader.Say(Loc.Get("femtofax_subject", subject));
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                ScreenReader.SayQueued(Loc.Get("femtofax_author", author));
            }

            if (!string.IsNullOrWhiteSpace(rating))
            {
                ScreenReader.SayQueued(Loc.Get("femtofax_rating", rating));
            }

            if (!string.IsNullOrWhiteSpace(body))
            {
                ScreenReader.SayQueued(Loc.Get("femtofax_body", body));
            }

            _focusSuppressedUntil = Time.unscaledTime + ContentFocusSuppressionSeconds;
            DebugLogger.Log(LogCategory.Handler, $"Femtofax content announced: index {program.frame.currentMessageIndex}");
        }

        private void FlushPendingEntrySummaryIfStale()
        {
            if (string.IsNullOrWhiteSpace(_pendingEntrySummary))
            {
                return;
            }

            if (Time.unscaledTime - _pendingEntrySummaryTime < EntrySummaryFallbackSeconds)
            {
                return;
            }

            ScreenReader.SayQueued(_pendingEntrySummary);
            _pendingEntrySummary = string.Empty;
            _pendingEntrySummaryTime = 0f;
            _lastAnnouncementTime = Time.unscaledTime;
        }

        private static Femtofax GetFemtofaxScreen()
        {
            if (GameManager.instance == null || Bookshelf.instance == null)
            {
                return null;
            }

            if (GameManager.currentSystemScreen != SystemScreen.Type.FEMTOFAX)
            {
                return null;
            }

            if (!Bookshelf.instance.systemScreens.ContainsKey(SystemScreen.Type.FEMTOFAX))
            {
                return null;
            }

            var screen = Bookshelf.instance.systemScreens[SystemScreen.Type.FEMTOFAX];
            if (screen == null || !screen.screenEnabled)
            {
                return null;
            }

            return screen as Femtofax;
        }

        private static bool TryBuildFocusAnnouncement(Femtofax femtofax, GameObject selectedObject, out string announcement)
        {
            announcement = null;

            if (femtofax.navbar != null)
            {
                if (UiTextHelper.IsSelectedObject(GetButtonObject(femtofax.navbar.leftButton), selectedObject))
                {
                    announcement = Loc.Get("femtofax_nav_prev_message");
                    return true;
                }

                if (UiTextHelper.IsSelectedObject(GetButtonObject(femtofax.navbar.rightButton), selectedObject))
                {
                    announcement = Loc.Get("femtofax_nav_next_message");
                    return true;
                }

                if (UiTextHelper.IsSelectedObject(GetButtonObject(femtofax.navbar.centerButton), selectedObject))
                {
                    announcement = Loc.Get("femtofax_nav_home");
                    return true;
                }
            }

            if (femtofax.welcomeScreen != null && femtofax.welcomeScreen.enterButton != null &&
                UiTextHelper.IsSelectedObject(GetButtonObject(femtofax.welcomeScreen.enterButton), selectedObject))
            {
                announcement = Loc.Get("femtofax_focus_enter");
                return true;
            }

            var femtofaxButton = selectedObject.GetComponent<FemtofaxProgramButton>() ?? selectedObject.GetComponentInParent<FemtofaxProgramButton>();
            if (femtofaxButton == null)
            {
                return false;
            }

            var label = GetProgramButtonLabel(femtofaxButton);
            if (string.IsNullOrWhiteSpace(label))
            {
                label = Loc.Get("femtofax_program_unknown");
            }

            if (TryGetProgramButtonPosition(femtofax, femtofaxButton, out var index, out var total))
            {
                announcement = Loc.Get("femtofax_focus_program", index, total, label);
                return true;
            }

            announcement = Loc.Get("femtofax_focus_button", label);
            return true;
        }

        private static string GetProgramButtonLabel(FemtofaxProgramButton button)
        {
            if (button == null)
            {
                return string.Empty;
            }

            var title = UiTextHelper.GetLocalizedText(button.title);
            if (!string.IsNullOrWhiteSpace(title))
            {
                return title;
            }

            if (button.femtofaxProgramObject != null && button.femtofaxProgramObject.title != null)
            {
                return UiTextHelper.CleanText(button.femtofaxProgramObject.title.Get());
            }

            return UiTextHelper.CleanText(button.gameObject.name);
        }

        private static bool TryGetProgramButtonPosition(Femtofax femtofax, FemtofaxProgramButton button, out int index, out int total)
        {
            index = 0;
            total = 0;
            if (femtofax == null || button == null || button.femtofaxProgramObject == null || femtofax.programs == null || femtofax.programs.Count == 0)
            {
                return false;
            }

            total = femtofax.programs.Count;
            for (var i = 0; i < femtofax.programs.Count; i++)
            {
                var program = femtofax.programs[i];
                if (program != null && program.femtofaxProgramObject != null && string.Equals(program.femtofaxProgramObject.id, button.femtofaxProgramObject.id, StringComparison.Ordinal))
                {
                    index = i + 1;
                    return true;
                }
            }

            return false;
        }

        private static string GetProgramId(FemtofaxProgram program)
        {
            if (program == null || program.femtofaxProgramObject == null || string.IsNullOrWhiteSpace(program.femtofaxProgramObject.id))
            {
                return string.Empty;
            }

            return program.femtofaxProgramObject.id;
        }

        private static GameObject GetButtonObject(FemtofaxProgramButton button)
        {
            if (button == null)
            {
                return null;
            }

            if (button.button != null)
            {
                return button.button.gameObject;
            }

            return button.gameObject;
        }
    }
}

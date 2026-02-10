using System.Collections.Generic;
using System.Linq;
using NobleRobot;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlippoAccess
{
    /// <summary>
    /// Announces message list focus and opened message content in the Messages screen.
    /// </summary>
    public sealed class MessagesHandler
    {
        private const float MinimumAnnouncementIntervalSeconds = 0.08f;
        private const float SubmenuAnnouncementFallbackSeconds = 0.75f;
        private const float ContentAnnouncementDelaySeconds = 0.45f;
        private const float InitialListMenuFocusDeferSeconds = 0.55f;

        private bool _wasActive;
        private int _lastSelectionId = int.MinValue;
        private float _lastAnnouncementTime;
        private string _lastContentSignature = string.Empty;
        private Submenu _lastSubmenu;
        private bool _entrySummaryAnnounced;
        private float _entryStartTime;
        private string _pendingSubmenuTitle = string.Empty;
        private float _pendingSubmenuChangeTime;
        private bool _hasPendingContentAnnouncement;
        private float _suppressFocusAnnouncementsUntil;
        private bool _deferInitialListMenuFocus;
        private float _deferInitialListMenuFocusUntil;
        private float _pendingContentReadyAt;
        private string _pendingContentSubject = string.Empty;
        private string _pendingContentBody = string.Empty;
        private string _pendingPacketteAnnouncement = string.Empty;

        /// <summary>
        /// Tracks messages UI focus and reads selected messages and message content.
        /// </summary>
        public void Update()
        {
            var messages = GetMessagesScreen();
            if (messages == null)
            {
                ResetState();
                return;
            }

            _wasActive = true;
            if (_entryStartTime <= 0f)
            {
                _entryStartTime = Time.unscaledTime;
            }

            AnnounceSubmenuChange(messages);
            FlushPendingSubmenuTitleIfStale(messages);
            AnnounceEntrySummary(messages);

            AnnounceFocus(messages);
            AnnounceOpenedMessageContent(messages);
            AnnouncePendingMessageContent(messages);
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
            _lastContentSignature = string.Empty;
            _lastSubmenu = null;
            _entrySummaryAnnounced = false;
            _entryStartTime = 0f;
            _pendingSubmenuTitle = string.Empty;
            _pendingSubmenuChangeTime = 0f;
            _hasPendingContentAnnouncement = false;
            _suppressFocusAnnouncementsUntil = 0f;
            _deferInitialListMenuFocus = false;
            _deferInitialListMenuFocusUntil = 0f;
            _pendingContentReadyAt = 0f;
            _pendingContentSubject = string.Empty;
            _pendingContentBody = string.Empty;
            _pendingPacketteAnnouncement = string.Empty;
        }

        private void AnnounceEntrySummary(Messages messages)
        {
            if (_entrySummaryAnnounced)
            {
                return;
            }

            if (messages.currentSubmenu == null || messages.currentSubmenu == messages.messageSubmenu)
            {
                return;
            }

            if (Time.unscaledTime - _entryStartTime < 0.35f)
            {
                return;
            }

            var total = messages.receivedMessages != null ? messages.receivedMessages.Count : 0;
            if (total <= 0)
            {
                ScreenReader.SayQueued(Loc.Get("messages_entry_empty", Loc.Get("messages_summary_empty")));
                _entrySummaryAnnounced = true;
                _lastAnnouncementTime = Time.unscaledTime;
                _suppressFocusAnnouncementsUntil = Time.unscaledTime + 0.4f;
                return;
            }

            if (!TryBuildEntryFirstMessageAnnouncement(messages, out var firstMessageAnnouncement))
            {
                if (Time.unscaledTime - _entryStartTime < 1.4f)
                {
                    return;
                }

                firstMessageAnnouncement = Loc.Get("messages_entry_first_unavailable");
            }

            var unread = CountUnreadMessages(messages);
            var counts = Loc.Get("messages_summary_counts_short", total, unread);
            var entryAnnouncement = Loc.Get("messages_entry_combined", counts, firstMessageAnnouncement);
            ScreenReader.SayQueued(entryAnnouncement);
            _entrySummaryAnnounced = true;
            _lastAnnouncementTime = Time.unscaledTime;
            _suppressFocusAnnouncementsUntil = Time.unscaledTime + 0.55f;

            var selectedObject = EventSystem.current?.currentSelectedGameObject;
            if (selectedObject != null)
            {
                _lastSelectionId = selectedObject.GetInstanceID();
            }

            DebugLogger.Log(LogCategory.Handler, $"Messages entry: {entryAnnouncement}");
        }

        private void AnnounceSubmenuChange(Messages messages)
        {
            if (messages.currentSubmenu == null || messages.currentSubmenu == _lastSubmenu)
            {
                return;
            }

            if (messages.currentSubmenu == messages.messageSubmenu)
            {
                _lastSubmenu = messages.currentSubmenu;
                _pendingSubmenuTitle = string.Empty;
                _pendingSubmenuChangeTime = 0f;
                _deferInitialListMenuFocus = false;
                _deferInitialListMenuFocusUntil = 0f;
                _lastSelectionId = int.MinValue;
                _lastAnnouncementTime = 0f;
                return;
            }

            _lastSubmenu = messages.currentSubmenu;
            var title = UiTextHelper.GetLocalizedText(messages.currentSubmenu.menuTitleLocalizedText);
            if (string.IsNullOrWhiteSpace(title))
            {
                title = UiTextHelper.CleanText(messages.currentSubmenu.gameObject.name);
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                return;
            }

            _pendingSubmenuTitle = title;
            _pendingSubmenuChangeTime = Time.unscaledTime;
            _deferInitialListMenuFocus = true;
            _deferInitialListMenuFocusUntil = Time.unscaledTime + InitialListMenuFocusDeferSeconds;
            _lastSelectionId = int.MinValue;
            _lastAnnouncementTime = 0f;
            DebugLogger.Log(LogCategory.Handler, $"Messages submenu: {title}");
        }

        private void AnnounceFocus(Messages messages)
        {
            var inMessageSubmenu = messages.currentSubmenu == messages.messageSubmenu;

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

            if (Time.unscaledTime < _suppressFocusAnnouncementsUntil)
            {
                _lastSelectionId = selectionId;
                return;
            }

            if (!inMessageSubmenu && !_entrySummaryAnnounced)
            {
                return;
            }

            if (inMessageSubmenu && _hasPendingContentAnnouncement)
            {
                _lastSelectionId = selectionId;
                return;
            }

            if (Time.unscaledTime - _lastAnnouncementTime < MinimumAnnouncementIntervalSeconds)
            {
                return;
            }

            if (!TryBuildFocusAnnouncement(messages, selectedObject, out var announcement))
            {
                _lastSelectionId = selectionId;
                return;
            }

            var hasPendingSubmenu = !inMessageSubmenu && !string.IsNullOrWhiteSpace(_pendingSubmenuTitle);
            if (hasPendingSubmenu && _deferInitialListMenuFocus && IsPageMenuSelection(messages, selectedObject))
            {
                if (Time.unscaledTime < _deferInitialListMenuFocusUntil)
                {
                    return;
                }

                _deferInitialListMenuFocus = false;
                _deferInitialListMenuFocusUntil = 0f;
            }

            if (hasPendingSubmenu && IsSubmenuHeaderEcho(_pendingSubmenuTitle, announcement))
            {
                _lastSelectionId = selectionId;
                return;
            }

            if (hasPendingSubmenu)
            {
                announcement = Loc.Get("submenu_focus_combined", _pendingSubmenuTitle, announcement);
                _pendingSubmenuTitle = string.Empty;
                _pendingSubmenuChangeTime = 0f;
                _deferInitialListMenuFocus = false;
                _deferInitialListMenuFocusUntil = 0f;
                ScreenReader.SayQueued(announcement);
            }
            else
            {
                ScreenReader.Say(announcement);
            }

            _lastSelectionId = selectionId;
            _lastAnnouncementTime = Time.unscaledTime;
            DebugLogger.Log(LogCategory.Handler, $"Messages focus: {announcement}");
        }

        private void AnnounceOpenedMessageContent(Messages messages)
        {
            if (messages.currentSubmenu != messages.messageSubmenu || messages.messageDisplay == null)
            {
                return;
            }

            var subject = UiTextHelper.GetLocalizedText(messages.messageDisplay.subjectNote.localizedText);
            var body = UiTextHelper.GetLocalizedText(messages.messageDisplay.bodyNote.localizedText);
            if (string.IsNullOrWhiteSpace(subject) && string.IsNullOrWhiteSpace(body))
            {
                return;
            }

            var signature = subject + "\n" + body;
            if (signature == _lastContentSignature)
            {
                return;
            }

            _lastContentSignature = signature;
            _pendingContentSubject = subject;
            _pendingContentBody = body;
            _pendingPacketteAnnouncement = BuildPacketteAvailabilityAnnouncement(messages);
            _pendingContentReadyAt = Time.unscaledTime + ContentAnnouncementDelaySeconds;
            _hasPendingContentAnnouncement = true;
            DebugLogger.Log(LogCategory.Handler, "Messages content scheduled");
        }

        private void AnnouncePendingMessageContent(Messages messages)
        {
            if (!_hasPendingContentAnnouncement)
            {
                return;
            }

            if (messages.currentSubmenu != messages.messageSubmenu)
            {
                ClearPendingContentAnnouncement();
                return;
            }

            if (Time.unscaledTime < _pendingContentReadyAt)
            {
                return;
            }

            var segments = new List<string>();
            if (TryBuildCurrentMessageSubmenuFocusAnnouncement(messages, out var focusAnnouncement))
            {
                segments.Add(focusAnnouncement);
            }

            segments.Add(Loc.Get("messages_content_intro"));

            if (!string.IsNullOrWhiteSpace(_pendingContentSubject))
            {
                segments.Add(Loc.Get("messages_content_subject", _pendingContentSubject));
            }

            if (!string.IsNullOrWhiteSpace(_pendingContentBody))
            {
                segments.Add(Loc.Get("messages_content_body", _pendingContentBody));
            }

            if (!string.IsNullOrWhiteSpace(_pendingPacketteAnnouncement))
            {
                segments.Add(_pendingPacketteAnnouncement);
            }

            if (segments.Count > 0)
            {
                var combinedAnnouncement = string.Join(". ", segments);
                ScreenReader.SayQueued(combinedAnnouncement);
                _lastAnnouncementTime = Time.unscaledTime;
                _suppressFocusAnnouncementsUntil = Time.unscaledTime + 0.75f;

                var selectedObject = EventSystem.current?.currentSelectedGameObject;
                if (selectedObject != null)
                {
                    _lastSelectionId = selectedObject.GetInstanceID();
                }
            }

            ClearPendingContentAnnouncement();
            DebugLogger.Log(LogCategory.Handler, "Messages content announced");
        }

        private void FlushPendingSubmenuTitleIfStale(Messages messages)
        {
            if (string.IsNullOrWhiteSpace(_pendingSubmenuTitle))
            {
                return;
            }

            if (messages.currentSubmenu == messages.messageSubmenu)
            {
                _pendingSubmenuTitle = string.Empty;
                _pendingSubmenuChangeTime = 0f;
                return;
            }

            if (Time.unscaledTime - _pendingSubmenuChangeTime < SubmenuAnnouncementFallbackSeconds)
            {
                return;
            }

            ScreenReader.SayQueued(Loc.Get("messages_submenu_opened", _pendingSubmenuTitle));
            DebugLogger.Log(LogCategory.Handler, $"Messages submenu fallback speech: {_pendingSubmenuTitle}");
            _pendingSubmenuTitle = string.Empty;
            _pendingSubmenuChangeTime = 0f;
            _deferInitialListMenuFocus = false;
            _deferInitialListMenuFocusUntil = 0f;
            _lastAnnouncementTime = Time.unscaledTime;
        }

        private void ClearPendingContentAnnouncement()
        {
            _hasPendingContentAnnouncement = false;
            _pendingContentReadyAt = 0f;
            _pendingContentSubject = string.Empty;
            _pendingContentBody = string.Empty;
            _pendingPacketteAnnouncement = string.Empty;
        }

        private static string BuildPacketteAvailabilityAnnouncement(Messages messages)
        {
            if (messages == null || messages.messageDisplay == null || messages.messageDisplay.loadPacketteButton == null)
            {
                return string.Empty;
            }

            var loadPacketteButton = messages.messageDisplay.loadPacketteButton;
            if (!loadPacketteButton.gameObject.activeInHierarchy)
            {
                return string.Empty;
            }

            var filename = UiTextHelper.GetMenuButtonValue(loadPacketteButton);
            if (!string.IsNullOrWhiteSpace(filename))
            {
                return Loc.Get("messages_content_packette_available", filename);
            }

            return Loc.Get("messages_content_packette_available_generic");
        }

        private bool TryBuildFocusAnnouncement(Messages messages, GameObject selectedObject, out string announcement)
        {
            announcement = null;

            var messageButton = selectedObject.GetComponent<MessageButton>() ?? selectedObject.GetComponentInParent<MessageButton>();
            if (messageButton != null)
            {
                announcement = BuildMessageButtonAnnouncement(messages, messageButton);
                return !string.IsNullOrWhiteSpace(announcement);
            }

            if (TryBuildPageNavigationAnnouncement(messages, selectedObject, out announcement))
            {
                return true;
            }

            var menuButton = selectedObject.GetComponent<MenuButton>() ?? selectedObject.GetComponentInParent<MenuButton>();
            if (menuButton == null)
            {
                return false;
            }

            if (!UiTextHelper.IsMenuButtonInSubmenu(messages.currentSubmenu, menuButton))
            {
                return false;
            }

            announcement = BuildMenuButtonAnnouncement(messages, menuButton);
            return !string.IsNullOrWhiteSpace(announcement);
        }

        private bool TryBuildCurrentMessageSubmenuFocusAnnouncement(Messages messages, out string announcement)
        {
            announcement = string.Empty;
            var selectedObject = EventSystem.current?.currentSelectedGameObject;
            if (selectedObject == null)
            {
                return false;
            }

            return TryBuildFocusAnnouncement(messages, selectedObject, out announcement) &&
                   !string.IsNullOrWhiteSpace(announcement);
        }

        private static bool TryBuildEntryFirstMessageAnnouncement(Messages messages, out string announcement)
        {
            announcement = string.Empty;
            if (messages == null || messages.receivedMessages == null || messages.receivedMessages.Count == 0)
            {
                return false;
            }

            var selectedObject = EventSystem.current?.currentSelectedGameObject;
            var selectedMessageButton = selectedObject != null
                ? selectedObject.GetComponent<MessageButton>() ?? selectedObject.GetComponentInParent<MessageButton>()
                : null;
            if (selectedMessageButton != null)
            {
                announcement = BuildMessageButtonAnnouncement(messages, selectedMessageButton);
                if (!string.IsNullOrWhiteSpace(announcement))
                {
                    return true;
                }
            }

            var firstMessageButton = messages.receivedMessages.FirstOrDefault(button => button != null);
            if (firstMessageButton == null)
            {
                return false;
            }

            announcement = BuildMessageButtonAnnouncement(messages, firstMessageButton);
            return !string.IsNullOrWhiteSpace(announcement);
        }

        private static int CountUnreadMessages(Messages messages)
        {
            if (messages == null || messages.receivedMessages == null || ViewerData_v1.current == null || ViewerData_v1.current.messagesInInbox == null)
            {
                return 0;
            }

            var unread = 0;
            for (var i = 0; i < messages.receivedMessages.Count; i++)
            {
                var messageButton = messages.receivedMessages[i];
                if (messageButton == null || messageButton.message == null)
                {
                    continue;
                }

                if (ViewerData_v1.current.messagesInInbox.ContainsKey(messageButton.message.id) &&
                    !ViewerData_v1.current.messagesInInbox[messageButton.message.id].read)
                {
                    unread++;
                }
            }

            return unread;
        }

        private static string BuildMessageButtonAnnouncement(Messages messages, MessageButton messageButton)
        {
            if (messageButton == null)
            {
                return string.Empty;
            }

            var subject = UiTextHelper.GetMenuButtonLabel(messageButton.menuButton);
            if (string.IsNullOrWhiteSpace(subject) && messageButton.message != null && messageButton.message.subject != null)
            {
                subject = UiTextHelper.CleanText(messageButton.message.subject.Get());
            }

            var status = Loc.Get("state_unread");
            if (messageButton.message != null && ViewerData_v1.current.messagesInInbox.ContainsKey(messageButton.message.id) && ViewerData_v1.current.messagesInInbox[messageButton.message.id].read)
            {
                status = Loc.Get("state_read");
            }

            var hasPosition = TryGetMessagePosition(messages, messageButton, out var index, out var total);
            if (hasPosition)
            {
                return Loc.Get("messages_focus_item", index, total, subject, status);
            }

            return Loc.Get("messages_focus_item_no_pos", subject, status);
        }

        private static bool TryBuildPageNavigationAnnouncement(Messages messages, GameObject selectedObject, out string announcement)
        {
            announcement = null;
            foreach (var page in GetMessagePages(messages))
            {
                if (page == null)
                {
                    continue;
                }

                if (UiTextHelper.IsSelectedObject(page.prevButton != null ? page.prevButton.gameObject : null, selectedObject))
                {
                    announcement = Loc.Get("messages_focus_prev_page");
                    return true;
                }

                if (UiTextHelper.IsSelectedObject(page.nextButton != null ? page.nextButton.gameObject : null, selectedObject))
                {
                    announcement = Loc.Get("messages_focus_next_page");
                    return true;
                }

                if (UiTextHelper.IsSelectedObject(page.menuButton != null ? page.menuButton.gameObject : null, selectedObject))
                {
                    var text = UiTextHelper.GetLocalizedText(page.menuButtonLocalizedText);
                    announcement = string.IsNullOrWhiteSpace(text) ? Loc.Get("messages_focus_menu_button", Loc.Get("messages_menu")) : Loc.Get("messages_focus_menu_button", text);
                    return true;
                }
            }

            return false;
        }

        private static string BuildMenuButtonAnnouncement(Messages messages, MenuButton menuButton)
        {
            var label = UiTextHelper.GetMenuButtonLabel(menuButton);
            var value = UiTextHelper.GetMenuButtonValue(menuButton);

            var hasPosition = TryGetButtonPosition(messages.currentSubmenu, menuButton, out var index, out var total);
            if (hasPosition && !string.IsNullOrWhiteSpace(value))
            {
                return Loc.Get("messages_focus_option_value_position", index, total, label, value);
            }

            if (hasPosition)
            {
                return Loc.Get("messages_focus_option_position", index, total, label);
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                return Loc.Get("messages_focus_option_value", label, value);
            }

            return Loc.Get("messages_focus_option", label);
        }

        private static bool IsPageMenuSelection(Messages messages, GameObject selectedObject)
        {
            foreach (var page in GetMessagePages(messages))
            {
                if (page == null || page.menuButton == null)
                {
                    continue;
                }

                if (UiTextHelper.IsSelectedObject(page.menuButton.gameObject, selectedObject))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSubmenuHeaderEcho(string submenuTitle, string announcement)
        {
            var normalizedTitle = UiTextHelper.CleanText(submenuTitle).ToLowerInvariant();
            var normalizedAnnouncement = UiTextHelper.CleanText(announcement).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedTitle) || string.IsNullOrWhiteSpace(normalizedAnnouncement))
            {
                return false;
            }

            return normalizedTitle == normalizedAnnouncement;
        }

        private static Messages GetMessagesScreen()
        {
            if (GameManager.instance == null || Bookshelf.instance == null)
            {
                return null;
            }

            if (GameManager.currentSystemScreen != SystemScreen.Type.MESSAGES)
            {
                return null;
            }

            if (!Bookshelf.instance.systemScreens.ContainsKey(SystemScreen.Type.MESSAGES))
            {
                return null;
            }

            var screen = Bookshelf.instance.systemScreens[SystemScreen.Type.MESSAGES];
            if (screen == null || !screen.screenEnabled)
            {
                return null;
            }

            return screen as Messages;
        }

        private static bool TryGetMessagePosition(Messages messages, MessageButton target, out int index, out int total)
        {
            index = 0;
            total = 0;
            if (messages == null || target == null || messages.receivedMessages == null || messages.receivedMessages.Count == 0)
            {
                return false;
            }

            total = messages.receivedMessages.Count;
            index = messages.receivedMessages.IndexOf(target) + 1;
            return index > 0 && index <= total;
        }

        private static bool TryGetButtonPosition(Submenu submenu, MenuButton target, out int index, out int total)
        {
            index = 0;
            total = 0;
            if (submenu == null || target == null || submenu.menuButtons == null || submenu.menuButtons.Count == 0)
            {
                return false;
            }

            var buttons = submenu.menuButtons.Values
                .Where(button => button != null && button.gameObject.activeInHierarchy && button.button != null && button.button.enabled && button.button.interactable)
                .OrderBy(button => button.number)
                .ToList();

            if (buttons.Count == 0)
            {
                return false;
            }

            total = buttons.Count;
            index = buttons.IndexOf(target) + 1;
            return index > 0 && index <= total;
        }

        private static IEnumerable<SubmenuMessageList> GetMessagePages(Messages messages)
        {
            yield return messages.page1;
            yield return messages.page2;
            yield return messages.page3;
            yield return messages.page4;
            yield return messages.page5;
        }
    }
}

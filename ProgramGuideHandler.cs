using System;
using NobleRobot;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlippoAccess
{
    /// <summary>
    /// Announces focused Program Guide elements with position context.
    /// </summary>
    public sealed class ProgramGuideHandler
    {
        private const float MinimumAnnouncementIntervalSeconds = 0.09f;
        private const float DuplicateAnnouncementWindowSeconds = 0.5f;

        private bool _wasActive;
        private bool _queueNextAnnouncement;
        private int _lastSelectionId = int.MinValue;
        private float _lastAnnouncementTime;
        private string _lastAnnouncement = string.Empty;

        /// <summary>
        /// Tracks Program Guide focus changes and speaks concise focus summaries.
        /// </summary>
        public void Update()
        {
            if (!IsProgramGuideActive())
            {
                ResetState();
                return;
            }

            if (!_wasActive)
            {
                _queueNextAnnouncement = true;
            }

            _wasActive = true;
            if (EventSystem.current == null)
            {
                return;
            }

            var selectedObject = EventSystem.current.currentSelectedGameObject;
            if (selectedObject == null)
            {
                return;
            }

            var selectionId = selectedObject.GetInstanceID();
            if (selectionId == _lastSelectionId)
            {
                return;
            }

            var announcement = BuildFocusAnnouncement(selectedObject);
            if (string.IsNullOrWhiteSpace(announcement))
            {
                _lastSelectionId = selectionId;
                return;
            }

            if (!TryAnnounce(announcement, selectionId))
            {
                return;
            }

            DebugLogger.Log(LogCategory.Handler, $"Program guide focus: {announcement}");
        }

        /// <summary>
        /// Attempts to build a Program Guide focus announcement for a selected object.
        /// </summary>
        /// <param name="selectedObject">Currently selected game object.</param>
        /// <param name="announcement">Built announcement text when available.</param>
        /// <returns>True when a guide-specific announcement could be built.</returns>
        public static bool TryBuildFocusAnnouncement(GameObject selectedObject, out string announcement)
        {
            announcement = BuildFocusAnnouncement(selectedObject);
            return !string.IsNullOrWhiteSpace(announcement);
        }

        private void ResetState()
        {
            if (!_wasActive)
            {
                return;
            }

            _wasActive = false;
            _queueNextAnnouncement = false;
            _lastSelectionId = int.MinValue;
            _lastAnnouncement = string.Empty;
            _lastAnnouncementTime = 0f;
        }

        private bool TryAnnounce(string announcement, int selectionId)
        {
            var now = Time.unscaledTime;
            if (announcement == _lastAnnouncement && now - _lastAnnouncementTime < DuplicateAnnouncementWindowSeconds)
            {
                _lastSelectionId = selectionId;
                return false;
            }

            if (now - _lastAnnouncementTime < MinimumAnnouncementIntervalSeconds)
            {
                return false;
            }

            if (_queueNextAnnouncement)
            {
                ScreenReader.SayQueued(announcement);
                _queueNextAnnouncement = false;
            }
            else
            {
                ScreenReader.Say(announcement);
            }

            _lastSelectionId = selectionId;
            _lastAnnouncement = announcement;
            _lastAnnouncementTime = now;
            return true;
        }

        private static bool IsProgramGuideActive()
        {
            if (GameManager.instance == null || Bookshelf.instance == null)
            {
                return false;
            }

            if (GameManager.currentSystemScreen != SystemScreen.Type.PROGRAM_GUIDE)
            {
                return false;
            }

            if (!Bookshelf.instance.systemScreens.ContainsKey(SystemScreen.Type.PROGRAM_GUIDE))
            {
                return false;
            }

            var screen = Bookshelf.instance.systemScreens[SystemScreen.Type.PROGRAM_GUIDE];
            return screen != null && screen.screenEnabled;
        }

        private static string BuildFocusAnnouncement(GameObject selectedObject)
        {
            if (ProgramGuide.instance != null)
            {
                var topButtonAnnouncement = BuildTopButtonAnnouncement(ProgramGuide.instance, selectedObject);
                if (!string.IsNullOrEmpty(topButtonAnnouncement))
                {
                    return topButtonAnnouncement;
                }
            }

            var guideSelectable = selectedObject.GetComponent<ChannelGuideSelectableItem>();
            if (guideSelectable == null)
            {
                guideSelectable = selectedObject.GetComponentInParent<ChannelGuideSelectableItem>();
            }

            if (guideSelectable == null)
            {
                return null;
            }

            var label = BuildGuideSelectableLabel(guideSelectable);
            if (TryGetChannelRowPosition(guideSelectable, out var index, out var total))
            {
                return Loc.Get("pg_focus_row", index, total, label);
            }

            return Loc.Get("pg_focus_item", label);
        }

        private static string BuildTopButtonAnnouncement(ProgramGuide guide, GameObject selectedObject)
        {
            const int topButtonCount = 4;

            if (guide.expandButton != null && IsSelected(guide.expandButton.gameObject, selectedObject))
            {
                var state = Loc.Get(guide.expandButton.isOn ? "state_on" : "state_off");
                return Loc.Get("pg_focus_button", 1, topButtonCount, Loc.Get("pg_button_expand", state));
            }

            if (guide.menuButton != null && IsSelected(guide.menuButton.gameObject, selectedObject))
            {
                return Loc.Get("pg_focus_button", 2, topButtonCount, Loc.Get("pg_button_menu"));
            }

            if (guide.messagesButton != null && IsSelected(guide.messagesButton.gameObject, selectedObject))
            {
                return Loc.Get("pg_focus_button", 3, topButtonCount, Loc.Get("pg_button_messages"));
            }

            if (guide.returnToBroadcastButton != null && IsSelected(guide.returnToBroadcastButton.gameObject, selectedObject))
            {
                return Loc.Get("pg_focus_button", 4, topButtonCount, Loc.Get("pg_button_return_broadcast"));
            }

            return null;
        }

        private static bool IsSelected(GameObject candidate, GameObject selectedObject)
        {
            return candidate != null &&
                selectedObject != null &&
                (selectedObject == candidate || selectedObject.transform.IsChildOf(candidate.transform));
        }

        private static bool TryGetChannelRowPosition(ChannelGuideSelectableItem selectable, out int index, out int total)
        {
            index = 0;
            total = 0;

            if (!(selectable.gridRow is Channel channel))
            {
                return false;
            }

            if (!channel.rowIndex.HasValue)
            {
                var fallbackIndex = FindChannelIndexById(channel);
                if (fallbackIndex < 0)
                {
                    return false;
                }

                total = GameManager.instance.navigationRows.Count;
                index = fallbackIndex + 1;
                return true;
            }

            if (GameManager.instance == null || GameManager.instance.navigationRows == null)
            {
                return false;
            }

            total = GameManager.instance.navigationRows.Count;
            if (total <= 0)
            {
                return false;
            }

            index = channel.rowIndex.Value + 1;
            if (index < 1 || index > total)
            {
                return false;
            }

            return true;
        }

        private static int FindChannelIndexById(Channel channel)
        {
            if (channel == null || channel.channelObject == null || GameManager.instance == null || GameManager.instance.navigationRows == null)
            {
                return -1;
            }

            var channelId = channel.channelObject.id;
            if (string.IsNullOrWhiteSpace(channelId))
            {
                return -1;
            }

            for (var i = 0; i < GameManager.instance.navigationRows.Count; i++)
            {
                var row = GameManager.instance.navigationRows[i];
                if (row != null && row.channelObject != null && string.Equals(row.channelObject.id, channelId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string BuildGuideSelectableLabel(ChannelGuideSelectableItem selectable)
        {
            if (selectable is ChannelBadge channelBadge)
            {
                return BuildChannelLabel(channelBadge.channel?.channelObject);
            }

            if (selectable is GridItem gridItem)
            {
                return BuildGridItemLabel(gridItem);
            }

            if (selectable is FullWidthGridItem fullWidthGridItem)
            {
                return BuildFullWidthLabel(fullWidthGridItem);
            }

            if (selectable.channel != null && selectable.channel.channelObject != null)
            {
                return BuildChannelLabel(selectable.channel.channelObject);
            }

            return CleanLabel(selectable.gameObject.name);
        }

        private static string BuildChannelLabel(ChannelObject channelObject)
        {
            if (channelObject == null)
            {
                return Loc.Get("pg_channel_unknown");
            }

            return Loc.Get("pg_channel_label", channelObject.channelNumber.ToString("00"), channelObject.callSign);
        }

        private static string BuildGridItemLabel(GridItem gridItem)
        {
            if (gridItem == null)
            {
                return Loc.Get("pg_item_unknown");
            }

            var slotLabel = GetSlotLabel(gridItem.currentPosition);
            if (gridItem.episodeObject != null)
            {
                var showTitle = GetLocalizedValue(gridItem.episodeObject.show?.title);
                if (string.IsNullOrWhiteSpace(showTitle))
                {
                    showTitle = GetLocalizedValue(gridItem.episodeObject.title);
                }

                if (string.IsNullOrWhiteSpace(showTitle))
                {
                    showTitle = Loc.Get("pg_show_unknown");
                }

                var callSign = gridItem.channel?.channelObject?.callSign;
                if (!string.IsNullOrWhiteSpace(callSign))
                {
                    return Loc.Get("pg_grid_item_channel", slotLabel, showTitle, callSign);
                }

                return Loc.Get("pg_grid_item", slotLabel, showTitle);
            }

            if (gridItem.showObject != null)
            {
                var title = GetLocalizedValue(gridItem.showObject.title);
                if (string.IsNullOrWhiteSpace(title))
                {
                    title = Loc.Get("pg_show_unknown");
                }

                return Loc.Get("pg_show_row", title);
            }

            return CleanLabel(gridItem.gameObject.name);
        }

        private static string BuildFullWidthLabel(FullWidthGridItem fullWidthGridItem)
        {
            if (fullWidthGridItem == null)
            {
                return Loc.Get("pg_item_unknown");
            }

            var channelObject = fullWidthGridItem.channel?.channelObject;
            if (fullWidthGridItem.episodeObject != null)
            {
                var showTitle = GetLocalizedValue(fullWidthGridItem.episodeObject.show?.title);
                if (string.IsNullOrWhiteSpace(showTitle))
                {
                    showTitle = Loc.Get("pg_show_unknown");
                }

                if (channelObject != null)
                {
                    return Loc.Get("pg_full_width_episode", channelObject.callSign, showTitle);
                }

                return Loc.Get("pg_full_width_episode_no_channel", showTitle);
            }

            if (channelObject != null)
            {
                return Loc.Get("pg_full_width_channel", channelObject.channelNumber.ToString("00"), channelObject.callSign);
            }

            return CleanLabel(fullWidthGridItem.gameObject.name);
        }

        private static string GetLocalizedValue(LocalizedString localizedString)
        {
            if (localizedString == null)
            {
                return string.Empty;
            }

            try
            {
                return CleanLabel(localizedString.Get());
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetSlotLabel(int currentPosition)
        {
            switch (currentPosition)
            {
                case 0:
                    return Loc.Get("pg_slot_now");
                case 1:
                    return Loc.Get("pg_slot_next");
                case 2:
                    return Loc.Get("pg_slot_later");
                default:
                    return Loc.Get("pg_slot_upcoming");
            }
        }

        private static string CleanLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var cleaned = value.Replace("(Clone)", string.Empty).Trim();
            if (cleaned.IndexOf('<') < 0 || cleaned.IndexOf('>') < 0)
            {
                return cleaned;
            }

            var buffer = new char[cleaned.Length];
            var index = 0;
            var insideTag = false;
            for (var i = 0; i < cleaned.Length; i++)
            {
                var c = cleaned[i];
                if (c == '<')
                {
                    insideTag = true;
                    continue;
                }

                if (c == '>')
                {
                    insideTag = false;
                    continue;
                }

                if (!insideTag)
                {
                    buffer[index++] = c;
                }
            }

            return new string(buffer, 0, index).Trim();
        }
    }
}

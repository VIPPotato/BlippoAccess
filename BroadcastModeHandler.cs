using UnityEngine;

namespace BlippoAccess
{
    /// <summary>
    /// Announces broadcast-related mode changes such as captions, data mode, and info panel visibility.
    /// </summary>
    public sealed class BroadcastModeHandler
    {
        private const float MinimumAnnouncementSpacingSeconds = 0.4f;
        private const float ModeToggleDebounceSeconds = 0.22f;
        private const float InfoPanelToggleSuppressionSeconds = 0.9f;
        private const float InfoPanelDuplicateAnnouncementSeconds = 2.25f;
        private const float TransitionInfoPanelSuppressionSeconds = 6f;
        private const float ScreenEntryInfoPanelSuppressionSeconds = 3f;

        private bool _initialized;
        private bool _lastCaptionsEnabled;
        private bool _lastLowDataMode;
        private bool _lastInfoPanelVisible;
        private string _lastChannelId = string.Empty;
        private string _lastEpisodeId = string.Empty;
        private float _lastAnnouncementTime;
        private float _lastInfoToggleAnnouncementTime;
        private float _lastInfoPanelAnnouncementTime;
        private float _suppressInfoPanelAnnouncementsUntil;
        private bool _wasInBroadcastDisplay;
        private bool _hasInfoPanelAnnouncementState;
        private bool _lastInfoPanelAnnouncementVisible;
        private string _pendingCaptionsAnnouncement = string.Empty;
        private string _pendingCaptionsLog = string.Empty;
        private float _pendingCaptionsReadyAt;
        private string _pendingDataModeAnnouncement = string.Empty;
        private string _pendingDataModeLog = string.Empty;
        private float _pendingDataModeReadyAt;

        /// <summary>
        /// Watches game viewer settings and announces meaningful mode toggles.
        /// </summary>
        public void Update()
        {
            if (GameManager.instance == null || ViewerData_v1.current == null || GameManager.instance.broadcastDisplay == null)
            {
                return;
            }

            var captionsEnabled = ViewerData_v1.current.captionsEnabled;
            var lowDataMode = ViewerData_v1.current.lowDataMode;
            var infoPanelVisible = GameManager.instance.broadcastDisplay.infoPanel != null && GameManager.instance.broadcastDisplay.infoPanel.activeSelf;
            var channelId = GetCurrentChannelId();
            var episodeId = GetCurrentEpisodeId();
            var inBroadcastDisplay = GameManager.currentSystemScreen == SystemScreen.Type.BROADCAST_DISPLAY;

            if (!_initialized)
            {
                _initialized = true;
                _lastCaptionsEnabled = captionsEnabled;
                _lastLowDataMode = lowDataMode;
                _lastInfoPanelVisible = infoPanelVisible;
                _lastChannelId = channelId;
                _lastEpisodeId = episodeId;
                ExtendInfoPanelSuppression(TransitionInfoPanelSuppressionSeconds);
                _wasInBroadcastDisplay = inBroadcastDisplay;
                return;
            }

            if (inBroadcastDisplay && !_wasInBroadcastDisplay)
            {
                ExtendInfoPanelSuppression(ScreenEntryInfoPanelSuppressionSeconds);
                _lastInfoPanelVisible = infoPanelVisible;
            }

            _wasInBroadcastDisplay = inBroadcastDisplay;

            if (!string.Equals(channelId, _lastChannelId, System.StringComparison.Ordinal) ||
                !string.Equals(episodeId, _lastEpisodeId, System.StringComparison.Ordinal))
            {
                _lastChannelId = channelId;
                _lastEpisodeId = episodeId;
                ExtendInfoPanelSuppression(TransitionInfoPanelSuppressionSeconds);
                _lastInfoPanelVisible = infoPanelVisible;
            }

            if (captionsEnabled != _lastCaptionsEnabled)
            {
                _lastCaptionsEnabled = captionsEnabled;
                QueueCaptionsAnnouncement(captionsEnabled);
            }

            if (lowDataMode != _lastLowDataMode)
            {
                _lastLowDataMode = lowDataMode;
                QueueDataModeAnnouncement(lowDataMode);
            }

            if (inBroadcastDisplay && infoPanelVisible != _lastInfoPanelVisible)
            {
                _lastInfoPanelVisible = infoPanelVisible;
                if (IsInfoPanelAnnouncementSuppressed())
                {
                    return;
                }

                if (Time.unscaledTime - _lastInfoToggleAnnouncementTime >= InfoPanelToggleSuppressionSeconds)
                {
                    if (ShouldSuppressDuplicateInfoPanelAnnouncement(infoPanelVisible))
                    {
                        return;
                    }

                    AnnounceModeChange(Loc.Get(infoPanelVisible ? "broadcast_info_panel_shown" : "broadcast_info_panel_hidden"), $"Info panel: {(infoPanelVisible ? "shown" : "hidden")}");
                    _lastInfoToggleAnnouncementTime = Time.unscaledTime;
                    _lastInfoPanelAnnouncementTime = Time.unscaledTime;
                    _lastInfoPanelAnnouncementVisible = infoPanelVisible;
                    _hasInfoPanelAnnouncementState = true;
                }
            }
            else if (!inBroadcastDisplay)
            {
                _lastInfoPanelVisible = infoPanelVisible;
            }

            FlushPendingModeAnnouncements();
        }

        private void ExtendInfoPanelSuppression(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            var suppressUntil = Time.unscaledTime + seconds;
            if (suppressUntil > _suppressInfoPanelAnnouncementsUntil)
            {
                _suppressInfoPanelAnnouncementsUntil = suppressUntil;
            }
        }

        private bool IsInfoPanelAnnouncementSuppressed()
        {
            return Time.unscaledTime < _suppressInfoPanelAnnouncementsUntil || BroadcastAnnouncementCoordinator.IsInfoPanelSuppressed;
        }

        private bool ShouldSuppressDuplicateInfoPanelAnnouncement(bool infoPanelVisible)
        {
            if (!_hasInfoPanelAnnouncementState)
            {
                return false;
            }

            if (_lastInfoPanelAnnouncementVisible != infoPanelVisible)
            {
                return false;
            }

            return Time.unscaledTime - _lastInfoPanelAnnouncementTime < InfoPanelDuplicateAnnouncementSeconds;
        }

        private void QueueCaptionsAnnouncement(bool captionsEnabled)
        {
            _pendingCaptionsAnnouncement = Loc.Get(captionsEnabled ? "broadcast_captions_on" : "broadcast_captions_off");
            _pendingCaptionsLog = $"Captions: {(captionsEnabled ? "on" : "off")}";
            _pendingCaptionsReadyAt = Time.unscaledTime + ModeToggleDebounceSeconds;
        }

        private void QueueDataModeAnnouncement(bool lowDataMode)
        {
            _pendingDataModeAnnouncement = Loc.Get(lowDataMode ? "broadcast_data_mode_on" : "broadcast_data_mode_off");
            _pendingDataModeLog = $"Low data mode: {(lowDataMode ? "on" : "off")}";
            _pendingDataModeReadyAt = Time.unscaledTime + ModeToggleDebounceSeconds;
        }

        private void FlushPendingModeAnnouncements()
        {
            if (Time.unscaledTime - _lastAnnouncementTime < MinimumAnnouncementSpacingSeconds)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(_pendingCaptionsAnnouncement) && Time.unscaledTime >= _pendingCaptionsReadyAt)
            {
                AnnounceModeChange(_pendingCaptionsAnnouncement, _pendingCaptionsLog);
                _pendingCaptionsAnnouncement = string.Empty;
                _pendingCaptionsLog = string.Empty;
                _pendingCaptionsReadyAt = 0f;
                return;
            }

            if (!string.IsNullOrWhiteSpace(_pendingDataModeAnnouncement) && Time.unscaledTime >= _pendingDataModeReadyAt)
            {
                AnnounceModeChange(_pendingDataModeAnnouncement, _pendingDataModeLog);
                _pendingDataModeAnnouncement = string.Empty;
                _pendingDataModeLog = string.Empty;
                _pendingDataModeReadyAt = 0f;
            }
        }

        private void AnnounceModeChange(string announcement, string log)
        {
            if (string.IsNullOrWhiteSpace(announcement))
            {
                return;
            }

            if (Time.unscaledTime - _lastAnnouncementTime < MinimumAnnouncementSpacingSeconds)
            {
                return;
            }

            ScreenReader.SayQueued(announcement);
            _lastAnnouncementTime = Time.unscaledTime;
            DebugLogger.Log(LogCategory.Handler, log);
        }

        private static string GetCurrentChannelId()
        {
            var channel = GameManager.instance != null ? GameManager.instance.currentlyTunedChannel : null;
            if (channel == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(channel.id))
            {
                return channel.id;
            }

            return string.IsNullOrWhiteSpace(channel.callSign) ? string.Empty : channel.callSign;
        }

        private static string GetCurrentEpisodeId()
        {
            var episode = GameManager.instance != null && GameManager.instance.broadcastDisplay != null
                ? GameManager.instance.broadcastDisplay.episodeObject
                : null;

            if (episode == null || string.IsNullOrWhiteSpace(episode.id))
            {
                return string.Empty;
            }

            return episode.id;
        }
    }
}

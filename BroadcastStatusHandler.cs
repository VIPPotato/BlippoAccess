using System;
using NobleRobot;
using UnityEngine;

namespace BlippoAccess
{
    /// <summary>
    /// Announces tuned channel and current show changes.
    /// </summary>
    public sealed class BroadcastStatusHandler
    {
        private const float MinimumAnnouncementIntervalSeconds = 0.18f;
        private const float PostTuneEpisodeSuppressionSeconds = 1.8f;
        private const float InfoPanelSuppressionAfterPrimaryAnnouncementSeconds = 2.8f;

        private bool _initialized;
        private string _lastChannelId = string.Empty;
        private string _lastEpisodeId = string.Empty;
        private float _lastAnnouncementTime;
        private float _suppressEpisodeAnnouncementsUntil;

        /// <summary>
        /// Announces channel/show changes when tuning occurs and suppresses noisy repeats.
        /// </summary>
        public void Update()
        {
            var gameManager = GameManager.instance;
            if (gameManager == null)
            {
                return;
            }

            var channel = gameManager.currentlyTunedChannel;
            if (channel == null)
            {
                return;
            }

            var channelId = string.IsNullOrWhiteSpace(channel.id) ? channel.callSign : channel.id;
            var episode = gameManager.broadcastDisplay != null ? gameManager.broadcastDisplay.episodeObject : null;
            var episodeId = episode != null && !string.IsNullOrWhiteSpace(episode.id) ? episode.id : string.Empty;

            if (!_initialized)
            {
                _initialized = true;
                _lastChannelId = channelId ?? string.Empty;
                _lastEpisodeId = episodeId;
                return;
            }

            var channelChanged = !string.Equals(channelId, _lastChannelId, StringComparison.Ordinal);
            var episodeChanged = !string.IsNullOrWhiteSpace(episodeId) &&
                !string.Equals(episodeId, _lastEpisodeId, StringComparison.Ordinal);

            if (!channelChanged && !episodeChanged)
            {
                return;
            }

            _lastChannelId = channelId ?? string.Empty;
            _lastEpisodeId = episodeId;

            if (Time.unscaledTime - _lastAnnouncementTime < MinimumAnnouncementIntervalSeconds)
            {
                return;
            }

            if (channelChanged)
            {
                AnnounceChannel(channel, episode);
                _lastAnnouncementTime = Time.unscaledTime;
                _suppressEpisodeAnnouncementsUntil = Time.unscaledTime + PostTuneEpisodeSuppressionSeconds;
                BroadcastAnnouncementCoordinator.SuppressInfoPanelFor(InfoPanelSuppressionAfterPrimaryAnnouncementSeconds);
                return;
            }

            if (episodeChanged &&
                Time.unscaledTime >= _suppressEpisodeAnnouncementsUntil &&
                GameManager.currentSystemScreen == SystemScreen.Type.BROADCAST_DISPLAY)
            {
                var showTitle = GetShowTitle(episode);
                if (!string.IsNullOrWhiteSpace(showTitle))
                {
                    ScreenReader.SayQueued(Loc.Get("broadcast_now_showing", showTitle));
                    DebugLogger.Log(LogCategory.Handler, $"Show changed: {showTitle}");
                    _lastAnnouncementTime = Time.unscaledTime;
                    BroadcastAnnouncementCoordinator.SuppressInfoPanelFor(InfoPanelSuppressionAfterPrimaryAnnouncementSeconds);
                }
            }
        }

        private static void AnnounceChannel(ChannelObject channel, EpisodeObject episode)
        {
            if (channel == null)
            {
                return;
            }

            var channelNumber = channel.channelNumber.ToString("00");
            var callSign = string.IsNullOrWhiteSpace(channel.callSign) ? Loc.Get("broadcast_unknown_channel") : channel.callSign;
            var showTitle = GetShowTitle(episode);

            if (string.IsNullOrWhiteSpace(showTitle))
            {
                ScreenReader.Say(Loc.Get("broadcast_channel_only", channelNumber, callSign));
                DebugLogger.Log(LogCategory.Handler, $"Channel tuned: {channelNumber} {callSign}");
                return;
            }

            ScreenReader.Say(Loc.Get("broadcast_channel_show", channelNumber, callSign, showTitle));
            DebugLogger.Log(LogCategory.Handler, $"Channel tuned: {channelNumber} {callSign}, show: {showTitle}");
        }

        private static string GetShowTitle(EpisodeObject episode)
        {
            if (episode == null)
            {
                return string.Empty;
            }

            if (episode.show != null && episode.show.title != null)
            {
                var title = StripRichText(episode.show.title.Get());
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }

            if (episode.title != null)
            {
                var episodeTitle = StripRichText(episode.title.Get());
                if (!string.IsNullOrWhiteSpace(episodeTitle))
                {
                    return episodeTitle;
                }
            }

            return string.Empty;
        }

        private static string StripRichText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (value.IndexOf('<') < 0 || value.IndexOf('>') < 0)
            {
                return value.Trim();
            }

            var buffer = new char[value.Length];
            var index = 0;
            var insideTag = false;
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
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

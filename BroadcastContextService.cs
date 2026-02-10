using System;
using NobleRobot;

namespace BlippoAccess
{
    /// <summary>
    /// Builds current broadcast context announcements from live game state.
    /// </summary>
    public static class BroadcastContextService
    {
        /// <summary>
        /// Attempts to build a channel/show announcement for the currently tuned broadcast.
        /// </summary>
        /// <param name="announcement">The built announcement when available.</param>
        /// <returns>True when context was available; otherwise false.</returns>
        public static bool TryBuildAnnouncement(out string announcement)
        {
            announcement = string.Empty;
            var gameManager = GameManager.instance;
            var channel = gameManager != null ? gameManager.currentlyTunedChannel : null;
            if (channel == null)
            {
                return false;
            }

            var channelNumber = channel.channelNumber.ToString("00");
            var callSign = string.IsNullOrWhiteSpace(channel.callSign) ? Loc.Get("broadcast_unknown_channel") : channel.callSign;
            var episode = gameManager.broadcastDisplay != null ? gameManager.broadcastDisplay.episodeObject : null;
            var showTitle = GetShowTitle(episode);

            announcement = string.IsNullOrWhiteSpace(showTitle)
                ? Loc.Get("broadcast_channel_only", channelNumber, callSign)
                : Loc.Get("broadcast_channel_show", channelNumber, callSign, showTitle);
            return true;
        }

        private static string GetShowTitle(EpisodeObject episode)
        {
            if (episode == null)
            {
                return string.Empty;
            }

            if (episode.show != null && episode.show.title != null)
            {
                var showTitle = StripRichText(episode.show.title.Get());
                if (!string.IsNullOrWhiteSpace(showTitle))
                {
                    return showTitle;
                }
            }

            if (episode.title == null)
            {
                return string.Empty;
            }

            var episodeTitle = StripRichText(episode.title.Get());
            return string.IsNullOrWhiteSpace(episodeTitle) ? string.Empty : episodeTitle;
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

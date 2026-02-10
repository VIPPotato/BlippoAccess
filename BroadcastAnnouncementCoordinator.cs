using UnityEngine;

namespace BlippoAccess
{
    /// <summary>
    /// Coordinates suppression windows between broadcast handlers to avoid speech overlap.
    /// </summary>
    public static class BroadcastAnnouncementCoordinator
    {
        private static float _infoPanelSuppressedUntil;

        /// <summary>
        /// Extends the shared info-panel suppression window.
        /// </summary>
        /// <param name="seconds">Duration to suppress info-panel announcements.</param>
        public static void SuppressInfoPanelFor(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            var suppressUntil = Time.unscaledTime + seconds;
            if (suppressUntil > _infoPanelSuppressedUntil)
            {
                _infoPanelSuppressedUntil = suppressUntil;
            }
        }

        /// <summary>
        /// Gets a value indicating whether info-panel announcements are currently suppressed.
        /// </summary>
        public static bool IsInfoPanelSuppressed => Time.unscaledTime < _infoPanelSuppressedUntil;
    }
}

using UnityEngine;

namespace BlippoAccess
{
    /// <summary>
    /// Announces when unread inbox messages become available.
    /// </summary>
    public sealed class NewMessageAlertHandler
    {
        private const float MinimumAnnouncementIntervalSeconds = 0.5f;

        private bool _initialized;
        private bool _lastNewMessageAvailable;
        private float _lastAnnouncementTime;

        /// <summary>
        /// Watches unread-message state and announces new availability on rising edge.
        /// </summary>
        public void Update()
        {
            if (ViewerData_v1.current == null)
            {
                ResetState();
                return;
            }

            var currentNewMessageAvailable = ViewerData_v1.current.newMessageAvailable;
            if (!_initialized)
            {
                _initialized = true;
                _lastNewMessageAvailable = currentNewMessageAvailable;
                return;
            }

            if (currentNewMessageAvailable == _lastNewMessageAvailable)
            {
                return;
            }

            _lastNewMessageAvailable = currentNewMessageAvailable;
            if (!currentNewMessageAvailable)
            {
                return;
            }

            if (Time.unscaledTime - _lastAnnouncementTime < MinimumAnnouncementIntervalSeconds)
            {
                return;
            }

            _lastAnnouncementTime = Time.unscaledTime;
            ScreenReader.SayQueued(Loc.Get("messages_new_available"));
            DebugLogger.Log(LogCategory.Handler, "New unread message available");
        }

        private void ResetState()
        {
            _initialized = false;
            _lastNewMessageAvailable = false;
            _lastAnnouncementTime = 0f;
        }
    }
}

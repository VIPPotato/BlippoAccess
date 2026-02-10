using System;
using NobleRobot;
using UnityEngine;

namespace BlippoAccess
{
    /// <summary>
    /// Announces stabilized show subtitle lines while in broadcast, only when captions are enabled.
    /// </summary>
    public sealed class ShowSubtitlesHandler
    {
        private const float CaptionStabilitySeconds = 0.18f;
        private const float DuplicateSuppressionSeconds = 0.45f;

        private string _activeCaption = string.Empty;
        private float _activeCaptionSince;
        private bool _activeCaptionSpoken;
        private string _lastSpokenCaption = string.Empty;
        private float _lastSpokenCaptionTime;

        /// <summary>
        /// Tracks active broadcast captions and reads each subtitle line once when it stabilizes.
        /// </summary>
        public void Update()
        {
            if (!TryGetActiveCaptionText(out var captionText))
            {
                ClearActiveCaption();
                return;
            }

            if (!string.Equals(captionText, _activeCaption, StringComparison.Ordinal))
            {
                _activeCaption = captionText;
                _activeCaptionSince = Time.unscaledTime;
                _activeCaptionSpoken = false;
                return;
            }

            if (_activeCaptionSpoken)
            {
                return;
            }

            if (Time.unscaledTime - _activeCaptionSince < CaptionStabilitySeconds)
            {
                return;
            }

            if (ShouldSuppressDuplicate(captionText))
            {
                _activeCaptionSpoken = true;
                return;
            }

            ScreenReader.SayQueued(captionText);
            _lastSpokenCaption = captionText;
            _lastSpokenCaptionTime = Time.unscaledTime;
            _activeCaptionSpoken = true;
            DebugLogger.Log(LogCategory.Handler, $"Subtitle: {captionText}");
        }

        private static bool TryGetActiveCaptionText(out string captionText)
        {
            captionText = string.Empty;
            if (GameManager.instance == null || ViewerData_v1.current == null)
            {
                return false;
            }

            if (GameManager.currentSystemScreen != SystemScreen.Type.BROADCAST_DISPLAY)
            {
                return false;
            }

            if (!ViewerData_v1.current.captionsEnabled)
            {
                return false;
            }

            var broadcastDisplay = GameManager.instance.broadcastDisplay;
            if (broadcastDisplay == null || broadcastDisplay.videoCaptions == null)
            {
                return false;
            }

            var captions = broadcastDisplay.videoCaptions;
            if (captions.captionGameObject == null || !captions.captionGameObject.activeInHierarchy)
            {
                return false;
            }

            captionText = UiTextHelper.GetText(captions.textField);
            return !string.IsNullOrWhiteSpace(captionText);
        }

        private bool ShouldSuppressDuplicate(string captionText)
        {
            if (!string.Equals(captionText, _lastSpokenCaption, StringComparison.Ordinal))
            {
                return false;
            }

            return Time.unscaledTime - _lastSpokenCaptionTime < DuplicateSuppressionSeconds;
        }

        private void ClearActiveCaption()
        {
            _activeCaption = string.Empty;
            _activeCaptionSince = 0f;
            _activeCaptionSpoken = false;
        }
    }
}

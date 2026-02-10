using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MelonLoader;

namespace BlippoAccess
{
    /// <summary>
    /// Thin wrapper around Tolk screen reader APIs.
    /// </summary>
    public static class ScreenReader
    {
        [DllImport("Tolk.dll")]
        private static extern void Tolk_Load();

        [DllImport("Tolk.dll")]
        private static extern void Tolk_Unload();

        [DllImport("Tolk.dll")]
        private static extern bool Tolk_IsLoaded();

        [DllImport("Tolk.dll")]
        private static extern bool Tolk_HasSpeech();

        [DllImport("Tolk.dll", CharSet = CharSet.Unicode)]
        private static extern bool Tolk_Output(string text, bool interrupt);

        [DllImport("Tolk.dll")]
        private static extern bool Tolk_Silence();

        [DllImport("Tolk.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr Tolk_DetectScreenReader();

        private static bool _isInitialized;
        private static bool _isAvailable;
        private static string _lastSpokenText = string.Empty;
        private static int _lastSpokenTick;
        private static readonly List<string> _announcementHistory = new List<string>();
        private static int _historyCursor = -1;

        private const int DuplicateSuppressionWindowMilliseconds = 450;
        private const int MaxAnnouncementHistorySize = 40;

        /// <summary>
        /// Initializes Tolk and caches screen reader availability.
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                Tolk_Load();
                _isAvailable = Tolk_IsLoaded() && Tolk_HasSpeech();

                if (_isAvailable)
                {
                    var readerNamePointer = Tolk_DetectScreenReader();
                    var readerName = readerNamePointer == IntPtr.Zero
                        ? "Unknown"
                        : Marshal.PtrToStringUni(readerNamePointer);
                    MelonLogger.Msg($"Screen reader detected: {readerName}");
                }
                else
                {
                    MelonLogger.Warning("Tolk loaded, but no active speech provider was detected.");
                }
            }
            catch (DllNotFoundException)
            {
                MelonLogger.Error("Tolk.dll is missing in the game directory.");
                _isAvailable = false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to initialize Tolk: {ex.Message}");
                _isAvailable = false;
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Speaks text immediately through the active screen reader.
        /// </summary>
        /// <param name="text">Text to speak.</param>
        /// <param name="interrupt">Whether current speech should be interrupted.</param>
        public static void Say(string text, bool interrupt = true)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var now = Environment.TickCount;
            if (string.Equals(text, _lastSpokenText, StringComparison.Ordinal) &&
                unchecked(now - _lastSpokenTick) < DuplicateSuppressionWindowMilliseconds)
            {
                return;
            }

            _lastSpokenText = text;
            _lastSpokenTick = now;
            AddToHistory(text);

            DebugLogger.LogScreenReader(text);

            if (!_isAvailable)
            {
                return;
            }

            try
            {
                Tolk_Output(text, interrupt);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Tolk output failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Queues text behind current speech instead of interrupting.
        /// </summary>
        /// <param name="text">Text to speak.</param>
        public static void SayQueued(string text)
        {
            Say(text, false);
        }

        /// <summary>
        /// Repeats the most recent spoken announcement.
        /// </summary>
        /// <param name="interrupt">Whether current speech should be interrupted.</param>
        /// <returns>True if a previous announcement existed, otherwise false.</returns>
        public static bool RepeatLast(bool interrupt = true)
        {
            if (string.IsNullOrWhiteSpace(_lastSpokenText))
            {
                return false;
            }

            _historyCursor = _announcementHistory.Count - 1;
            return SpeakFromHistory(_lastSpokenText, interrupt);
        }

        /// <summary>
        /// Repeats the previous announcement from history.
        /// </summary>
        /// <param name="interrupt">Whether current speech should be interrupted.</param>
        /// <returns>True if a previous announcement exists; otherwise false.</returns>
        public static bool RepeatPrevious(bool interrupt = true)
        {
            if (_announcementHistory.Count == 0)
            {
                return false;
            }

            if (_historyCursor < 0)
            {
                _historyCursor = _announcementHistory.Count - 1;
            }
            else
            {
                _historyCursor = Math.Max(0, _historyCursor - 1);
            }

            return SpeakFromHistory(_announcementHistory[_historyCursor], interrupt);
        }

        /// <summary>
        /// Repeats the next announcement from history.
        /// </summary>
        /// <param name="interrupt">Whether current speech should be interrupted.</param>
        /// <returns>True if a next announcement exists; otherwise false.</returns>
        public static bool RepeatNext(bool interrupt = true)
        {
            if (_announcementHistory.Count == 0)
            {
                return false;
            }

            if (_historyCursor < 0)
            {
                _historyCursor = _announcementHistory.Count - 1;
            }
            else
            {
                _historyCursor = Math.Min(_announcementHistory.Count - 1, _historyCursor + 1);
            }

            return SpeakFromHistory(_announcementHistory[_historyCursor], interrupt);
        }

        /// <summary>
        /// Stops current speech output.
        /// </summary>
        public static void Stop()
        {
            if (!_isAvailable)
            {
                return;
            }

            try
            {
                Tolk_Silence();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Tolk silence failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Shuts down Tolk.
        /// </summary>
        public static void Shutdown()
        {
            if (!_isInitialized)
            {
                return;
            }

            try
            {
                Tolk_Unload();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Tolk unload failed: {ex.Message}");
            }

            _isInitialized = false;
            _isAvailable = false;
        }

        /// <summary>
        /// Gets a value indicating whether speech output is currently available.
        /// </summary>
        public static bool IsAvailable => _isAvailable;

        private static void AddToHistory(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (_announcementHistory.Count > 0 &&
                string.Equals(_announcementHistory[_announcementHistory.Count - 1], text, StringComparison.Ordinal))
            {
                _historyCursor = _announcementHistory.Count - 1;
                return;
            }

            _announcementHistory.Add(text);
            if (_announcementHistory.Count > MaxAnnouncementHistorySize)
            {
                _announcementHistory.RemoveAt(0);
            }

            _historyCursor = _announcementHistory.Count - 1;
        }

        private static bool SpeakFromHistory(string text, bool interrupt)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            _lastSpokenText = text;
            _lastSpokenTick = Environment.TickCount;
            DebugLogger.LogScreenReader(text);

            if (!_isAvailable)
            {
                return true;
            }

            try
            {
                Tolk_Output(text, interrupt);
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Tolk repeat failed: {ex.Message}");
                return false;
            }
        }
    }
}

using MelonLoader;

namespace BlippoAccess
{
    /// <summary>
    /// Central debug logger for accessibility mod diagnostics.
    /// </summary>
    public static class DebugLogger
    {
        /// <summary>
        /// Logs a categorized message when debug mode is enabled.
        /// </summary>
        /// <param name="category">Log category tag.</param>
        /// <param name="message">Message text.</param>
        public static void Log(LogCategory category, string message)
        {
            if (!Main.DebugMode)
            {
                return;
            }

            MelonLogger.Msg($"{GetPrefix(category)} {message}");
        }

        /// <summary>
        /// Logs a key input event when debug mode is enabled.
        /// </summary>
        /// <param name="keyName">Pressed key name.</param>
        /// <param name="action">Optional mapped action name.</param>
        public static void LogInput(string keyName, string action = null)
        {
            if (!Main.DebugMode)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(action))
            {
                MelonLogger.Msg($"[INPUT] {keyName}");
                return;
            }

            MelonLogger.Msg($"[INPUT] {keyName} -> {action}");
        }

        /// <summary>
        /// Logs a state transition when debug mode is enabled.
        /// </summary>
        /// <param name="description">State transition description.</param>
        public static void LogState(string description)
        {
            if (!Main.DebugMode)
            {
                return;
            }

            MelonLogger.Msg($"[STATE] {description}");
        }

        /// <summary>
        /// Logs outgoing screen reader speech when debug mode is enabled.
        /// </summary>
        /// <param name="text">Speech text.</param>
        public static void LogScreenReader(string text)
        {
            if (!Main.DebugMode)
            {
                return;
            }

            MelonLogger.Msg($"[SR] {text}");
        }

        private static string GetPrefix(LogCategory category)
        {
            switch (category)
            {
                case LogCategory.ScreenReader:
                    return "[SR]";
                case LogCategory.Input:
                    return "[INPUT]";
                case LogCategory.State:
                    return "[STATE]";
                case LogCategory.Handler:
                    return "[HANDLER]";
                case LogCategory.Game:
                    return "[GAME]";
                default:
                    return "[DEBUG]";
            }
        }
    }

    /// <summary>
    /// Debug log categories.
    /// </summary>
    public enum LogCategory
    {
        /// <summary>
        /// Screen reader speech messages.
        /// </summary>
        ScreenReader,

        /// <summary>
        /// Input-related messages.
        /// </summary>
        Input,

        /// <summary>
        /// State transition messages.
        /// </summary>
        State,

        /// <summary>
        /// Handler execution messages.
        /// </summary>
        Handler,

        /// <summary>
        /// Raw game-value messages.
        /// </summary>
        Game
    }
}

using NobleRobot;

namespace BlippoAccess
{
    /// <summary>
    /// Announces system screen transitions once the target screen is fully active.
    /// </summary>
    public sealed class SystemScreenHandler
    {
        private bool _hasTrackedScreen;
        private SystemScreen.Type _lastScreen;

        /// <summary>
        /// Tracks the active system screen and announces only on real transitions.
        /// </summary>
        public void Update()
        {
            if (GameManager.instance == null || Bookshelf.instance == null)
            {
                return;
            }

            var currentScreen = GameManager.currentSystemScreen;
            if (!IsScreenEnabled(currentScreen))
            {
                return;
            }

            if (!_hasTrackedScreen)
            {
                _hasTrackedScreen = true;
                _lastScreen = currentScreen;
                AnnounceScreen(currentScreen);
                return;
            }

            if (_lastScreen == currentScreen)
            {
                return;
            }

            _lastScreen = currentScreen;
            AnnounceScreen(currentScreen);
        }

        private static bool IsScreenEnabled(SystemScreen.Type screenType)
        {
            if (!Bookshelf.instance.systemScreens.ContainsKey(screenType))
            {
                return false;
            }

            var screen = Bookshelf.instance.systemScreens[screenType];
            return screen != null && screen.screenEnabled;
        }

        private static void AnnounceScreen(SystemScreen.Type screenType)
        {
            if (ShouldSpeakScreenHeader(screenType))
            {
                ScreenReader.Say(Loc.Get(GetScreenKey(screenType)));
            }

            var hintKey = GetHintKey(screenType);
            if (!string.IsNullOrEmpty(hintKey))
            {
                ScreenReader.Say(Loc.Get(hintKey), false);
            }

            DebugLogger.Log(LogCategory.Handler, $"Screen changed: {screenType}");
        }

        private static string GetScreenKey(SystemScreen.Type screenType)
        {
            switch (screenType)
            {
                case SystemScreen.Type.CONTROL_MENU:
                    return "screen_control_menu";
                case SystemScreen.Type.MESSAGES:
                    return "screen_messages";
                case SystemScreen.Type.TUNER_CALIBRATION:
                    return "screen_tuner_calibration";
                case SystemScreen.Type.PACKETTE_LOAD:
                    return "screen_packette_load";
                case SystemScreen.Type.CREDITS:
                    return "screen_credits";
                case SystemScreen.Type.PROGRAM_GUIDE:
                    return "screen_program_guide";
                case SystemScreen.Type.BROADCAST_DISPLAY:
                    return "screen_broadcast_display";
                case SystemScreen.Type.FEMTOFAX:
                    return "screen_femtofax";
                case SystemScreen.Type.SIGNAL_LOSS:
                    return "screen_signal_loss";
                default:
                    return "screen_unknown";
            }
        }

        private static string GetHintKey(SystemScreen.Type screenType)
        {
            switch (screenType)
            {
                case SystemScreen.Type.PROGRAM_GUIDE:
                    return "hint_program_guide";
                case SystemScreen.Type.TUNER_CALIBRATION:
                    return "hint_tuner_calibration";
                default:
                    return null;
            }
        }

        private static bool ShouldSpeakScreenHeader(SystemScreen.Type screenType)
        {
            switch (screenType)
            {
                case SystemScreen.Type.CONTROL_MENU:
                case SystemScreen.Type.MESSAGES:
                case SystemScreen.Type.FEMTOFAX:
                    return false;
                default:
                    return true;
            }
        }
    }
}

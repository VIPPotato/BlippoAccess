using System.Collections;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(BlippoAccess.Main), "BlippoAccess", "0.1.0", "TODO_AUTHOR")]
[assembly: MelonGame("Noble Robot", "Blippo+")]

namespace BlippoAccess
{
    /// <summary>
    /// Main mod entry point. Initializes core systems and handles global hotkeys.
    /// </summary>
    public sealed class Main : MelonMod
    {
        private bool _gameReady;
        private SystemScreenHandler _systemScreenHandler;
        private SignalLossHandler _signalLossHandler;
        private ProgramGuideHandler _programGuideHandler;
        private TunerCalibrationHandler _tunerCalibrationHandler;
        private PacketteLoadHandler _packetteLoadHandler;
        private CreditsHandler _creditsHandler;
        private BroadcastStatusHandler _broadcastStatusHandler;
        private ControlMenuHandler _controlMenuHandler;
        private MessagesHandler _messagesHandler;
        private FemtofaxHandler _femtofaxHandler;
        private BroadcastModeHandler _broadcastModeHandler;
        private ShowSubtitlesHandler _showSubtitlesHandler;
        private DataManagerContentHandler _dataManagerContentHandler;
        private FactoryResetHandler _factoryResetHandler;
        private NewMessageAlertHandler _newMessageAlertHandler;

        /// <summary>
        /// Gets a value indicating whether debug logging is enabled.
        /// </summary>
        public static bool DebugMode { get; private set; }

        /// <summary>
        /// Initializes localization and screen reader support.
        /// </summary>
        public override void OnInitializeMelon()
        {
            Loc.Initialize();
            ScreenReader.Initialize();
            _systemScreenHandler = new SystemScreenHandler();
            _signalLossHandler = new SignalLossHandler();
            _programGuideHandler = new ProgramGuideHandler();
            _tunerCalibrationHandler = new TunerCalibrationHandler();
            _packetteLoadHandler = new PacketteLoadHandler();
            _creditsHandler = new CreditsHandler();
            _broadcastStatusHandler = new BroadcastStatusHandler();
            _controlMenuHandler = new ControlMenuHandler();
            _messagesHandler = new MessagesHandler();
            _femtofaxHandler = new FemtofaxHandler();
            _broadcastModeHandler = new BroadcastModeHandler();
            _showSubtitlesHandler = new ShowSubtitlesHandler();
            _dataManagerContentHandler = new DataManagerContentHandler();
            _factoryResetHandler = new FactoryResetHandler();
            _newMessageAlertHandler = new NewMessageAlertHandler();
            MelonCoroutines.Start(AnnounceStartupCoroutine());
        }

        /// <summary>
        /// Marks the game as ready after a scene load.
        /// </summary>
        /// <param name="buildIndex">The loaded scene build index.</param>
        /// <param name="sceneName">The loaded scene name.</param>
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            _gameReady = true;
            DebugLogger.LogState($"Scene loaded: {sceneName} ({buildIndex})");
        }

        /// <summary>
        /// Processes global hotkeys once the game is ready.
        /// </summary>
        public override void OnUpdate()
        {
            if (!_gameReady)
            {
                return;
            }

            _systemScreenHandler.Update();
            _signalLossHandler.Update();
            _programGuideHandler.Update();
            _tunerCalibrationHandler.Update();
            _packetteLoadHandler.Update();
            _creditsHandler.Update();
            _broadcastStatusHandler.Update();
            _controlMenuHandler.Update();
            _messagesHandler.Update();
            _femtofaxHandler.Update();
            _broadcastModeHandler.Update();
            _showSubtitlesHandler.Update();
            _dataManagerContentHandler.Update();
            _factoryResetHandler.Update();
            _newMessageAlertHandler.Update();
            ProcessHotkeys();
        }

        /// <summary>
        /// Shuts down the screen reader wrapper when the game exits.
        /// </summary>
        public override void OnApplicationQuit()
        {
            ScreenReader.Shutdown();
        }

        private IEnumerator AnnounceStartupCoroutine()
        {
            yield return new WaitForSeconds(1.0f);
            ScreenReader.Say(Loc.Get("mod_loaded"));
        }

        private void ProcessHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                DebugMode = !DebugMode;
                DebugLogger.LogState($"Debug mode: {(DebugMode ? "on" : "off")}");
                ScreenReader.Say(Loc.Get(DebugMode ? "debug_enabled" : "debug_disabled"));
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                DebugLogger.LogInput("F1", "WhereAmI");
                ScreenReader.Say(WhereAmIService.BuildAnnouncement());
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                DebugLogger.LogInput("F2", "RepeatLast");
                if (!ScreenReader.RepeatLast())
                {
                    ScreenReader.Say(Loc.Get("repeat_last_none"));
                }
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                DebugLogger.LogInput("F3", "StopSpeech");
                ScreenReader.Stop();
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                DebugLogger.LogInput("F4", "BroadcastContext");
                if (BroadcastContextService.TryBuildAnnouncement(out var announcement))
                {
                    ScreenReader.Say(announcement);
                    return;
                }

                ScreenReader.Say(Loc.Get("broadcast_context_unavailable"));
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                DebugLogger.LogInput("F5", "ReloadLocalizationOverrides");
                var applied = Loc.ReloadExternalOverrides();
                ScreenReader.Say(applied > 0
                    ? Loc.Get("localization_overrides_reloaded", applied)
                    : Loc.Get("localization_overrides_reloaded_none"));
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                DebugLogger.LogInput("F6", "RepeatPrevious");
                if (!ScreenReader.RepeatPrevious())
                {
                    ScreenReader.Say(Loc.Get("announcement_history_empty"));
                }
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                DebugLogger.LogInput("F7", "RepeatNext");
                if (!ScreenReader.RepeatNext())
                {
                    ScreenReader.Say(Loc.Get("announcement_history_empty"));
                }
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                DebugLogger.LogInput("F8", "ContextHelp");
                ScreenReader.Say(ContextHelpService.BuildAnnouncement());
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                DebugLogger.LogInput("F9", "InboxSummary");
                if (InboxSummaryService.TryBuildAnnouncement(out var announcement))
                {
                    ScreenReader.Say(announcement);
                    return;
                }

                ScreenReader.Say(Loc.Get("messages_summary_unavailable"));
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                DebugLogger.LogInput("F10", "SignalStatus");
                if (SignalStatusService.TryBuildAnnouncement(out var announcement))
                {
                    ScreenReader.Say(announcement);
                    return;
                }

                ScreenReader.Say(Loc.Get("signal_status_unavailable"));
            }
        }
    }
}

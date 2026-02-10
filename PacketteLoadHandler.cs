using System;
using System.Globalization;
using NobleRobot;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlippoAccess
{
    /// <summary>
    /// Announces Packette Load step changes, discovered channels, and actionable button focus.
    /// </summary>
    public sealed class PacketteLoadHandler
    {
        private const float MinimumFocusAnnouncementIntervalSeconds = 0.1f;
        private const float StepFocusSuppressionSeconds = 0.35f;

        private bool _wasActive;
        private bool _hasTrackedStep;
        private PacketteLoad.Step _lastStep;
        private int _lastSelectionId = int.MinValue;
        private int _lastChannelsFound = -1;
        private bool _wasWaitingForUnpack;
        private float _lastAnnouncementTime;
        private float _focusSuppressedUntil;
        private string _lastPacketteFilename = string.Empty;

        /// <summary>
        /// Tracks Packette Load state and reads key progress milestones.
        /// </summary>
        public void Update()
        {
            var packetteLoad = GetPacketteLoadScreen();
            if (packetteLoad == null)
            {
                ResetState();
                return;
            }

            _wasActive = true;
            AnnouncePacketteFilename(packetteLoad);
            AnnounceStepChange(packetteLoad);
            AnnounceUnpackingWait(packetteLoad);
            AnnounceFoundChannels(packetteLoad);
            AnnounceFocus(packetteLoad);
        }

        private void ResetState()
        {
            if (!_wasActive)
            {
                return;
            }

            _wasActive = false;
            _hasTrackedStep = false;
            _lastStep = PacketteLoad.Step.LAUNCH;
            _lastSelectionId = int.MinValue;
            _lastChannelsFound = -1;
            _wasWaitingForUnpack = false;
            _lastAnnouncementTime = 0f;
            _focusSuppressedUntil = 0f;
            _lastPacketteFilename = string.Empty;
        }

        private void AnnouncePacketteFilename(PacketteLoad packetteLoad)
        {
            var filename = UiTextHelper.GetText(packetteLoad.packetteFilename);
            if (string.IsNullOrWhiteSpace(filename) || string.Equals(filename, _lastPacketteFilename, StringComparison.Ordinal))
            {
                return;
            }

            _lastPacketteFilename = filename;
            ScreenReader.SayQueued(Loc.Get("packette_filename", filename));
            DebugLogger.Log(LogCategory.Handler, $"Packette filename: {filename}");
        }

        private void AnnounceStepChange(PacketteLoad packetteLoad)
        {
            var step = packetteLoad.step;
            if (_hasTrackedStep && step == _lastStep)
            {
                return;
            }

            _hasTrackedStep = true;
            _lastStep = step;
            _lastSelectionId = int.MinValue;

            var stepText = GetStatusText(packetteLoad);
            if (string.IsNullOrWhiteSpace(stepText))
            {
                stepText = GetStepFallbackText(step);
            }

            if (!string.IsNullOrWhiteSpace(stepText))
            {
                ScreenReader.Say(Loc.Get("packette_step_status", stepText));
            }

            if (step == PacketteLoad.Step.SIGNAL_ACQUIRED)
            {
                ScreenReader.SayQueued(Loc.Get("packette_step_signal_acquired_hint"));
            }
            else if (step == PacketteLoad.Step.SCAN_COMPLETE)
            {
                ScreenReader.SayQueued(Loc.Get("packette_step_scan_complete_count", GetChannelsFound(packetteLoad)));
            }

            _focusSuppressedUntil = Time.unscaledTime + StepFocusSuppressionSeconds;
            _lastAnnouncementTime = Time.unscaledTime;
            DebugLogger.Log(LogCategory.Handler, $"Packette step: {step}");
        }

        private void AnnounceUnpackingWait(PacketteLoad packetteLoad)
        {
            var waitingForUnpack = packetteLoad.waitBlock != null && packetteLoad.waitBlock.activeInHierarchy;
            if (waitingForUnpack && !_wasWaitingForUnpack)
            {
                var unpackText = GetStatusText(packetteLoad);
                if (string.IsNullOrWhiteSpace(unpackText))
                {
                    unpackText = Loc.Get("packette_step_unpacking");
                }

                ScreenReader.Say(Loc.Get("packette_step_status", unpackText));
                _focusSuppressedUntil = Time.unscaledTime + StepFocusSuppressionSeconds;
                _lastAnnouncementTime = Time.unscaledTime;
                DebugLogger.Log(LogCategory.Handler, "Packette unpacking wait");
            }

            _wasWaitingForUnpack = waitingForUnpack;
        }

        private void AnnounceFoundChannels(PacketteLoad packetteLoad)
        {
            var channelsFound = GetChannelsFound(packetteLoad);
            if (packetteLoad.step != PacketteLoad.Step.SCANNING_FOR_CHANNELS)
            {
                _lastChannelsFound = channelsFound;
                return;
            }

            if (channelsFound <= _lastChannelsFound)
            {
                return;
            }

            _lastChannelsFound = channelsFound;
            var channelNumber = UiTextHelper.GetText(packetteLoad.channelNumber);
            var statusText = GetStatusText(packetteLoad);

            if (!string.IsNullOrWhiteSpace(channelNumber) && !string.IsNullOrWhiteSpace(statusText))
            {
                ScreenReader.SayQueued(Loc.Get("packette_channel_found_named", channelNumber, statusText, channelsFound));
            }
            else if (!string.IsNullOrWhiteSpace(channelNumber))
            {
                ScreenReader.SayQueued(Loc.Get("packette_channel_found", channelNumber, channelsFound));
            }
            else
            {
                ScreenReader.SayQueued(Loc.Get("packette_channel_found_total", channelsFound));
            }

            DebugLogger.Log(LogCategory.Handler, $"Packette channel found: {channelNumber} ({channelsFound} total)");
        }

        private void AnnounceFocus(PacketteLoad packetteLoad)
        {
            if (Time.unscaledTime < _focusSuppressedUntil)
            {
                return;
            }

            var selectedObject = EventSystem.current?.currentSelectedGameObject;
            if (selectedObject == null)
            {
                return;
            }

            var selectedButton = selectedObject.GetComponent<Button>() ?? selectedObject.GetComponentInParent<Button>();
            if (selectedButton == null)
            {
                return;
            }

            var selectionId = selectedButton.gameObject.GetInstanceID();
            if (selectionId == _lastSelectionId)
            {
                return;
            }

            if (Time.unscaledTime - _lastAnnouncementTime < MinimumFocusAnnouncementIntervalSeconds)
            {
                return;
            }

            var label = GetButtonLabel(selectedButton.gameObject);
            if (string.IsNullOrWhiteSpace(label))
            {
                label = GetFallbackButtonLabel(packetteLoad.step);
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                _lastSelectionId = selectionId;
                return;
            }

            ScreenReader.Say(Loc.Get("packette_focus_button", label));
            _lastSelectionId = selectionId;
            _lastAnnouncementTime = Time.unscaledTime;
            DebugLogger.Log(LogCategory.Handler, $"Packette focus: {label}");
        }

        private static PacketteLoad GetPacketteLoadScreen()
        {
            if (GameManager.instance == null || Bookshelf.instance == null)
            {
                return null;
            }

            if (GameManager.currentSystemScreen != SystemScreen.Type.PACKETTE_LOAD)
            {
                return null;
            }

            if (!Bookshelf.instance.systemScreens.ContainsKey(SystemScreen.Type.PACKETTE_LOAD))
            {
                return null;
            }

            var screen = Bookshelf.instance.systemScreens[SystemScreen.Type.PACKETTE_LOAD];
            if (screen == null || !screen.screenEnabled)
            {
                return null;
            }

            return screen as PacketteLoad;
        }

        private static int GetChannelsFound(PacketteLoad packetteLoad)
        {
            var text = UiTextHelper.GetText(packetteLoad.channelsFoundNumberText);
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                return 0;
            }

            return value < 0 ? 0 : value;
        }

        private static string GetStatusText(PacketteLoad packetteLoad)
        {
            return UiTextHelper.GetLocalizedText(packetteLoad.statusText);
        }

        private static string GetButtonLabel(GameObject buttonObject)
        {
            if (buttonObject == null)
            {
                return string.Empty;
            }

            var localizedText = buttonObject.GetComponentInChildren<LocalizedText>();
            if (localizedText != null)
            {
                var text = UiTextHelper.GetLocalizedText(localizedText);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }

            var label = UiTextHelper.GetText(buttonObject.GetComponentInChildren<TMPro.TMP_Text>());
            if (!string.IsNullOrWhiteSpace(label))
            {
                return label;
            }

            return UiTextHelper.CleanText(buttonObject.name);
        }

        private static string GetStepFallbackText(PacketteLoad.Step step)
        {
            switch (step)
            {
                case PacketteLoad.Step.SCANNING_SUBSPACE:
                    return Loc.Get("packette_step_scanning_subspace");
                case PacketteLoad.Step.SIGNAL_ACQUIRED:
                    return Loc.Get("packette_step_signal_acquired");
                case PacketteLoad.Step.SCANNING_FOR_CHANNELS:
                    return Loc.Get("packette_step_scanning_channels");
                case PacketteLoad.Step.SCAN_COMPLETE:
                    return Loc.Get("packette_step_scan_complete");
                default:
                    return Loc.Get("packette_step_launch");
            }
        }

        private static string GetFallbackButtonLabel(PacketteLoad.Step step)
        {
            switch (step)
            {
                case PacketteLoad.Step.SIGNAL_ACQUIRED:
                    return Loc.Get("packette_button_continue");
                case PacketteLoad.Step.SCAN_COMPLETE:
                    return Loc.Get("packette_button_load");
                default:
                    return string.Empty;
            }
        }
    }
}

using System;
using System.Globalization;
using System.Collections.Generic;
using NobleRobot;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlippoAccess
{
    /// <summary>
    /// Announces signal-loss modal transitions and current tuner values.
    /// </summary>
    public sealed class SignalLossHandler
    {
        private const float MinimumFocusAnnouncementIntervalSeconds = 0.1f;

        private SignalLoss.ModalType _lastModalType = SignalLoss.ModalType.NONE;
        private int _lastSelectionId = int.MinValue;
        private float _lastFocusAnnouncementTime;
        private bool _queueNextFocusAnnouncement;

        /// <summary>
        /// Detects signal-loss modal changes and announces new modal states once.
        /// </summary>
        public void Update()
        {
            var currentModalType = SignalLoss.currentTypeUp;
            if (currentModalType != _lastModalType)
            {
                _lastModalType = currentModalType;
                _lastSelectionId = int.MinValue;
                _lastFocusAnnouncementTime = 0f;
                if (currentModalType == SignalLoss.ModalType.NONE)
                {
                    _queueNextFocusAnnouncement = false;
                    return;
                }

                AnnounceModalTransition(currentModalType);
                _queueNextFocusAnnouncement = true;
            }

            if (currentModalType == SignalLoss.ModalType.NONE)
            {
                return;
            }

            AnnounceModalFocus();
        }

        private static void AnnounceModalTransition(SignalLoss.ModalType currentModalType)
        {
            var modalAnnouncement = TryGetGameSignalLossMessage();
            if (string.IsNullOrWhiteSpace(modalAnnouncement))
            {
                modalAnnouncement = Loc.Get(GetModalKey(currentModalType));
            }

            ScreenReader.Say(Loc.Get("signal_loss_modal_text", modalAnnouncement));

            if (ViewerData_v1.current != null)
            {
                var eVrp = ((int)ViewerData_v1.current.eVRP).ToString("#,##0", CultureInfo.InvariantCulture);
                var eArcp = ((int)ViewerData_v1.current.eARCP).ToString("#,##0", CultureInfo.InvariantCulture);
                ScreenReader.Say(Loc.Get("signal_strength_values", eVrp, eArcp), false);
            }

            ScreenReader.Say(Loc.Get(GetModalHintKey(currentModalType)), false);
            DebugLogger.Log(LogCategory.Handler, $"Signal loss modal: {currentModalType}");
        }

        private void AnnounceModalFocus()
        {
            if (GameManager.currentSystemScreen != SystemScreen.Type.SIGNAL_LOSS)
            {
                return;
            }

            var signalLoss = GameManager.instance != null ? GameManager.instance.signalLoss : null;
            if (signalLoss == null)
            {
                return;
            }

            var selectedObject = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selectedObject == null)
            {
                return;
            }

            var selectionId = selectedObject.GetInstanceID();
            if (selectionId == _lastSelectionId)
            {
                return;
            }

            if (Time.unscaledTime - _lastFocusAnnouncementTime < MinimumFocusAnnouncementIntervalSeconds)
            {
                return;
            }

            if (!TryBuildFocusAnnouncement(signalLoss, selectedObject, out var announcement))
            {
                _lastSelectionId = selectionId;
                return;
            }

            if (_queueNextFocusAnnouncement)
            {
                ScreenReader.SayQueued(announcement);
                _queueNextFocusAnnouncement = false;
            }
            else
            {
                ScreenReader.Say(announcement);
            }

            _lastSelectionId = selectionId;
            _lastFocusAnnouncementTime = Time.unscaledTime;
            DebugLogger.Log(LogCategory.Handler, $"Signal loss focus: {announcement}");
        }

        private static bool TryBuildFocusAnnouncement(SignalLoss signalLoss, GameObject selectedObject, out string announcement)
        {
            announcement = null;
            if (signalLoss == null || selectedObject == null)
            {
                return false;
            }

            var activeButtons = GetActiveButtons(signalLoss);
            if (activeButtons.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < activeButtons.Count; i++)
            {
                var button = activeButtons[i];
                if (!UiTextHelper.IsSelectedObject(button.gameObject, selectedObject))
                {
                    continue;
                }

                var label = GetGuideButtonLabel(signalLoss, button);
                if (string.IsNullOrWhiteSpace(label))
                {
                    return false;
                }

                announcement = Loc.Get("signal_loss_focus_button", label, i + 1, activeButtons.Count);
                return true;
            }

            return false;
        }

        private static List<GuideButton> GetActiveButtons(SignalLoss signalLoss)
        {
            var buttons = new List<GuideButton>();
            if (signalLoss == null)
            {
                return buttons;
            }

            AddActiveButton(buttons, signalLoss.calibrateButton);
            AddActiveButton(buttons, signalLoss.ignoreButton);
            AddActiveButton(buttons, signalLoss.okButton);
            return buttons;
        }

        private static void AddActiveButton(List<GuideButton> buttons, GuideButton button)
        {
            if (button == null || !button.gameObject.activeInHierarchy)
            {
                return;
            }

            if (button.button != null && (!button.button.enabled || !button.button.interactable))
            {
                return;
            }

            buttons.Add(button);
        }

        private static string GetGuideButtonLabel(SignalLoss signalLoss, GuideButton button)
        {
            if (button == null)
            {
                return string.Empty;
            }

            if (button.localizedStringObject != null && !string.IsNullOrWhiteSpace(button.localizedStringObject.displayString))
            {
                return UiTextHelper.CleanText(button.localizedStringObject.displayString);
            }

            var textField = button.GetComponentInChildren<TMP_Text>();
            var label = UiTextHelper.GetText(textField);
            if (!string.IsNullOrWhiteSpace(label))
            {
                return label;
            }

            if (signalLoss != null)
            {
                if (button == signalLoss.calibrateButton)
                {
                    return Loc.Get("signal_loss_button_calibrate");
                }

                if (button == signalLoss.ignoreButton)
                {
                    return Loc.Get("signal_loss_button_ignore");
                }

                if (button == signalLoss.okButton)
                {
                    return Loc.Get("signal_loss_button_ok");
                }
            }

            return UiTextHelper.CleanText(button.gameObject.name);
        }

        internal static string TryGetGameSignalLossMessage()
        {
            var signalLoss = GameManager.instance?.signalLoss;
            if (signalLoss == null || signalLoss.message == null)
            {
                return string.Empty;
            }

            try
            {
                signalLoss.message.LanguageChangeHandler(false);

                if (!string.IsNullOrWhiteSpace(signalLoss.message.cachedText))
                {
                    return StripRichText(signalLoss.message.cachedText);
                }

                var localizedString = signalLoss.message.localizedString;
                if (localizedString != null)
                {
                    var localized = localizedString.Get();
                    if (!string.IsNullOrWhiteSpace(localized))
                    {
                        return StripRichText(localized);
                    }
                }

                if (signalLoss.message.textField != null && !string.IsNullOrWhiteSpace(signalLoss.message.textField.text))
                {
                    return StripRichText(signalLoss.message.textField.text);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log(LogCategory.Handler, $"Failed reading game signal-loss text: {ex.Message}");
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

        private static string GetModalKey(SignalLoss.ModalType modalType)
        {
            switch (modalType)
            {
                case SignalLoss.ModalType.WARNING:
                    return "signal_loss_warning";
                case SignalLoss.ModalType.PROMPT:
                    return "signal_loss_prompt";
                case SignalLoss.ModalType.FORCE:
                    return "signal_loss_force";
                default:
                    return "signal_loss";
            }
        }

        private static string GetModalHintKey(SignalLoss.ModalType modalType)
        {
            switch (modalType)
            {
                case SignalLoss.ModalType.WARNING:
                    return "signal_loss_warning_hint";
                case SignalLoss.ModalType.PROMPT:
                    return "signal_loss_prompt_hint";
                case SignalLoss.ModalType.FORCE:
                    return "signal_loss_force_hint";
                default:
                    return "signal_loss_hint";
            }
        }
    }
}

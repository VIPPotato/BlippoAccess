using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NobleRobot;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlippoAccess
{
    /// <summary>
    /// Builds concise current-context announcements for the F1 "where am I" hotkey.
    /// </summary>
    internal static class WhereAmIService
    {
        public static string BuildAnnouncement()
        {
            var screenName = NormalizeScreenName(GetScreenName());
            var detail = GetCurrentFocusDetail();
            if (string.IsNullOrWhiteSpace(detail))
            {
                return Loc.Get("where_am_i_screen_only", screenName);
            }

            return Loc.Get("where_am_i_screen_detail", screenName, detail);
        }

        private static string NormalizeScreenName(string screenName)
        {
            if (string.IsNullOrWhiteSpace(screenName))
            {
                return Loc.Get("where_am_i_screen_unknown");
            }

            return screenName.Trim().TrimEnd('.', '!', '?');
        }

        private static string GetScreenName()
        {
            if (GameManager.instance == null || Bookshelf.instance == null)
            {
                return Loc.Get("where_am_i_screen_unknown");
            }

            return Loc.Get(GetScreenKey(GameManager.currentSystemScreen));
        }

        private static string GetCurrentFocusDetail()
        {
            var screenType = GameManager.currentSystemScreen;
            var selectedObject = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

            if (screenType == SystemScreen.Type.BROADCAST_DISPLAY &&
                BroadcastContextService.TryBuildAnnouncement(out var broadcastDetail))
            {
                if (selectedObject == null)
                {
                    return broadcastDetail;
                }

                var focusedElement = BuildGenericDetail(selectedObject);
                if (string.IsNullOrWhiteSpace(focusedElement))
                {
                    return broadcastDetail;
                }

                return Loc.Get("where_am_i_broadcast_with_focus", broadcastDetail, focusedElement);
            }

            if (screenType == SystemScreen.Type.CONTROL_MENU)
            {
                var menu = GetControlMenu();
                if (menu != null)
                {
                    if (FactoryResetHandler.IsFactoryResetContext(menu.currentSubmenu, selectedObject))
                    {
                        var warning = Loc.Get("factory_reset_submenu_warning");
                        var cancelHint = Loc.Get("factory_reset_cancel_hint");
                        if (selectedObject != null)
                        {
                            var focusDetail = BuildMenuDetail(menu.currentSubmenu, selectedObject);
                            if (string.IsNullOrWhiteSpace(focusDetail))
                            {
                                focusDetail = BuildGenericDetail(selectedObject);
                            }

                            if (!string.IsNullOrWhiteSpace(focusDetail))
                            {
                                return Loc.Get("where_am_i_factory_reset_with_focus", focusDetail, warning, cancelHint);
                            }
                        }

                        return Loc.Get("where_am_i_factory_reset_no_focus", warning, cancelHint);
                    }

                    if (DataManagerContentHandler.TryBuildCurrentSummary(menu.currentSubmenu, out var dataTitle, out var dataSummary))
                    {
                        if (selectedObject != null)
                        {
                            var focusDetail = BuildMenuDetail(menu.currentSubmenu, selectedObject);
                            if (string.IsNullOrWhiteSpace(focusDetail))
                            {
                                focusDetail = BuildGenericDetail(selectedObject);
                            }

                            if (!string.IsNullOrWhiteSpace(focusDetail))
                            {
                                return Loc.Get("where_am_i_data_submenu_with_focus", focusDetail, dataSummary);
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(dataTitle))
                        {
                            return Loc.Get("where_am_i_data_submenu_no_focus", dataTitle, dataSummary);
                        }

                        return dataSummary;
                    }

                    if (selectedObject == null)
                    {
                        return Loc.Get("where_am_i_no_focus");
                    }

                    return BuildMenuDetail(menu.currentSubmenu, selectedObject);
                }

                if (selectedObject == null)
                {
                    return Loc.Get("where_am_i_no_focus");
                }
            }
            else if (screenType == SystemScreen.Type.MESSAGES)
            {
                if (selectedObject == null)
                {
                    return Loc.Get("where_am_i_no_focus");
                }

                var messages = GetMessagesScreen();
                if (messages != null)
                {
                    return BuildMessagesDetail(messages, selectedObject);
                }
            }
            else if (screenType == SystemScreen.Type.PROGRAM_GUIDE)
            {
                if (selectedObject == null)
                {
                    return Loc.Get("where_am_i_no_focus");
                }

                if (ProgramGuideHandler.TryBuildFocusAnnouncement(selectedObject, out var programGuideDetail))
                {
                    return programGuideDetail;
                }
            }
            else if (screenType == SystemScreen.Type.FEMTOFAX)
            {
                var femtofax = GetFemtofaxScreen();
                if (femtofax != null)
                {
                    return BuildFemtofaxDetail(femtofax, selectedObject);
                }
            }
            else if (screenType == SystemScreen.Type.PACKETTE_LOAD)
            {
                var packetteLoad = GetPacketteLoadScreen();
                if (packetteLoad != null)
                {
                    return BuildPacketteLoadDetail(packetteLoad, selectedObject);
                }
            }
            else if (screenType == SystemScreen.Type.TUNER_CALIBRATION)
            {
                var tuner = GetTunerCalibrationScreen();
                if (tuner != null)
                {
                    return BuildTunerCalibrationDetail(tuner, selectedObject);
                }
            }
            else if (screenType == SystemScreen.Type.SIGNAL_LOSS)
            {
                return BuildSignalLossDetail();
            }
            else if (screenType == SystemScreen.Type.CREDITS)
            {
                if (CreditsHandler.TryBuildCurrentSectionAnnouncement(out var creditsDetail))
                {
                    if (selectedObject == null)
                    {
                        return creditsDetail;
                    }

                    var focusDetail = BuildGenericDetail(selectedObject);
                    if (string.IsNullOrWhiteSpace(focusDetail))
                    {
                        return creditsDetail;
                    }

                    return Loc.Get("where_am_i_credits_with_focus", creditsDetail, focusDetail);
                }
            }

            if (selectedObject == null)
            {
                return Loc.Get("where_am_i_no_focus");
            }

            return BuildGenericDetail(selectedObject);
        }

        private static string BuildTunerCalibrationDetail(TunerCalibration tuner, GameObject selectedObject)
        {
            var tunerStatus = tuner.lockButton != null && tuner.lockButton.interactable
                ? Loc.Get("where_am_i_tuner_ready")
                : Loc.Get("where_am_i_tuner_adjusting");

            if (selectedObject == null)
            {
                return tunerStatus;
            }

            string focusDetail;
            if (TryGetSelectedTunerProperty(tuner, selectedObject, out var property))
            {
                var status = Loc.Get(property.locked ? "tuner_status_locked" : "tuner_status_adjust");
                focusDetail = Loc.Get("tuner_focus_property", GetTunerPropertyName(property), GetTunerPropertyValue(property), status);
            }
            else if (IsSelectedObject(tuner.lockButton != null ? tuner.lockButton.gameObject : null, selectedObject))
            {
                var availability = Loc.Get(tuner.lockButton != null && tuner.lockButton.interactable
                    ? "tuner_button_available"
                    : "tuner_button_unavailable");
                focusDetail = Loc.Get("tuner_focus_confirm", availability);
            }
            else if (IsSelectedObject(tuner.cancelButton != null ? tuner.cancelButton.gameObject : null, selectedObject))
            {
                focusDetail = Loc.Get("tuner_focus_cancel");
            }
            else
            {
                focusDetail = BuildGenericDetail(selectedObject);
            }

            if (string.IsNullOrWhiteSpace(focusDetail))
            {
                return tunerStatus;
            }

            return Loc.Get("where_am_i_tuner_with_focus", tunerStatus, focusDetail);
        }

        private static string BuildSignalLossDetail()
        {
            var modalMessage = SignalLossHandler.TryGetGameSignalLossMessage();
            if (string.IsNullOrWhiteSpace(modalMessage))
            {
                modalMessage = Loc.Get(GetSignalLossModalFallbackKey(SignalLoss.currentTypeUp));
            }

            if (ViewerData_v1.current == null)
            {
                return Loc.Get("where_am_i_signal_loss_only", modalMessage);
            }

            var eVrp = ((int)ViewerData_v1.current.eVRP).ToString("#,##0", CultureInfo.InvariantCulture);
            var eArcp = ((int)ViewerData_v1.current.eARCP).ToString("#,##0", CultureInfo.InvariantCulture);
            var signalValues = Loc.Get("signal_strength_values", eVrp, eArcp);
            return Loc.Get("where_am_i_signal_loss_with_values", modalMessage, signalValues);
        }

        private static string BuildFemtofaxDetail(Femtofax femtofax, GameObject selectedObject)
        {
            var detail = string.Empty;
            var program = femtofax.GetProgram();
            var programTitle = GetFemtofaxProgramTitle(program);
            if (program != null && program.frame != null && program.frame.gameObject.activeInHierarchy && program.frame.currentMessageIndex >= 0)
            {
                var messageIndex = program.frame.currentMessageIndex + 1;
                var subject = UiTextHelper.GetLocalizedText(program.frame.subjectText);
                if (!string.IsNullOrWhiteSpace(programTitle))
                {
                    detail = string.IsNullOrWhiteSpace(subject)
                        ? Loc.Get("where_am_i_femtofax_program_message_no_subject", programTitle, messageIndex)
                        : Loc.Get("where_am_i_femtofax_program_message", programTitle, messageIndex, subject);
                }
                else
                {
                    detail = string.IsNullOrWhiteSpace(subject)
                        ? Loc.Get("where_am_i_femtofax_message_no_subject", messageIndex)
                        : Loc.Get("where_am_i_femtofax_message", messageIndex, subject);
                }
            }
            else if (!string.IsNullOrWhiteSpace(programTitle))
            {
                detail = Loc.Get("where_am_i_femtofax_program", programTitle);
            }

            var focusDetail = selectedObject != null ? BuildGenericDetail(selectedObject) : string.Empty;
            if (!string.IsNullOrWhiteSpace(detail) && !string.IsNullOrWhiteSpace(focusDetail))
            {
                return Loc.Get("where_am_i_femtofax_with_focus", detail, focusDetail);
            }

            if (!string.IsNullOrWhiteSpace(detail))
            {
                return detail;
            }

            if (!string.IsNullOrWhiteSpace(focusDetail))
            {
                return focusDetail;
            }

            return Loc.Get("where_am_i_no_focus");
        }

        private static string BuildPacketteLoadDetail(PacketteLoad packetteLoad, GameObject selectedObject)
        {
            var status = UiTextHelper.GetLocalizedText(packetteLoad.statusText);
            if (string.IsNullOrWhiteSpace(status))
            {
                status = GetPacketteStepFallbackText(packetteLoad.step);
            }

            string detail;
            var channelsFound = GetPacketteChannelsFound(packetteLoad);
            if (!string.IsNullOrWhiteSpace(status) && channelsFound > 0 &&
                (packetteLoad.step == PacketteLoad.Step.SCANNING_FOR_CHANNELS || packetteLoad.step == PacketteLoad.Step.SCAN_COMPLETE))
            {
                detail = Loc.Get("where_am_i_packette_status_channels", status, channelsFound);
            }
            else if (!string.IsNullOrWhiteSpace(status))
            {
                detail = status;
            }
            else if (channelsFound > 0)
            {
                detail = Loc.Get("where_am_i_packette_channels_only", channelsFound);
            }
            else
            {
                detail = string.Empty;
            }

            var focusDetail = selectedObject != null ? BuildGenericDetail(selectedObject) : string.Empty;
            if (!string.IsNullOrWhiteSpace(detail) && !string.IsNullOrWhiteSpace(focusDetail))
            {
                return Loc.Get("where_am_i_packette_with_focus", detail, focusDetail);
            }

            if (!string.IsNullOrWhiteSpace(detail))
            {
                return detail;
            }

            if (!string.IsNullOrWhiteSpace(focusDetail))
            {
                return focusDetail;
            }

            return Loc.Get("where_am_i_no_focus");
        }

        private static string BuildMenuDetail(Submenu submenu, GameObject selectedObject)
        {
            var itemText = BuildMenuButtonText(submenu, selectedObject);
            if (string.IsNullOrWhiteSpace(itemText))
            {
                itemText = BuildGenericDetail(selectedObject);
            }

            var submenuTitle = GetSubmenuTitle(submenu);
            if (string.IsNullOrWhiteSpace(submenuTitle))
            {
                return itemText;
            }

            return Loc.Get("where_am_i_submenu_item", submenuTitle, itemText);
        }

        private static string BuildMessagesDetail(Messages messages, GameObject selectedObject)
        {
            if (messages == null)
            {
                return BuildGenericDetail(selectedObject);
            }

            var messageButton = selectedObject.GetComponent<MessageButton>() ?? selectedObject.GetComponentInParent<MessageButton>();
            if (messageButton != null)
            {
                return BuildMessageButtonDetail(messages, messageButton);
            }

            return BuildMenuDetail(messages.currentSubmenu, selectedObject);
        }

        private static string BuildMessageButtonDetail(Messages messages, MessageButton messageButton)
        {
            var subject = UiTextHelper.GetMenuButtonLabel(messageButton.menuButton);
            if (string.IsNullOrWhiteSpace(subject) && messageButton.message != null && messageButton.message.subject != null)
            {
                subject = UiTextHelper.CleanText(messageButton.message.subject.Get());
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                subject = Loc.Get("where_am_i_unknown_item");
            }

            if (messages != null && messages.receivedMessages != null && messages.receivedMessages.Count > 0)
            {
                var index = messages.receivedMessages.IndexOf(messageButton);
                if (index >= 0)
                {
                    return Loc.Get("where_am_i_item_position", subject, index + 1, messages.receivedMessages.Count);
                }
            }

            return subject;
        }

        private static string BuildMenuButtonText(Submenu submenu, GameObject selectedObject)
        {
            var menuButton = selectedObject.GetComponent<MenuButton>() ?? selectedObject.GetComponentInParent<MenuButton>();
            if (menuButton == null)
            {
                return string.Empty;
            }

            var label = UiTextHelper.GetMenuButtonLabel(menuButton);
            if (string.IsNullOrWhiteSpace(label))
            {
                label = Loc.Get("where_am_i_unknown_item");
            }

            var value = UiTextHelper.GetMenuButtonValue(menuButton);
            if (TryGetMenuButtonPosition(submenu, menuButton, out var index, out var total))
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Loc.Get("where_am_i_item_value_position", label, value, index, total);
                }

                return Loc.Get("where_am_i_item_position", label, index, total);
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                return Loc.Get("where_am_i_item_value", label, value);
            }

            return label;
        }

        private static string BuildGenericDetail(GameObject selectedObject)
        {
            var menuButton = selectedObject.GetComponent<MenuButton>() ?? selectedObject.GetComponentInParent<MenuButton>();
            if (menuButton != null)
            {
                var label = UiTextHelper.GetMenuButtonLabel(menuButton);
                if (!string.IsNullOrWhiteSpace(label))
                {
                    return label;
                }
            }

            return UiTextHelper.CleanText(selectedObject.name);
        }

        private static string GetSubmenuTitle(Submenu submenu)
        {
            if (submenu == null)
            {
                return string.Empty;
            }

            var title = UiTextHelper.GetLocalizedText(submenu.menuTitleLocalizedText);
            if (!string.IsNullOrWhiteSpace(title))
            {
                return title;
            }

            return UiTextHelper.CleanText(submenu.gameObject.name);
        }

        private static bool TryGetMenuButtonPosition(Submenu submenu, MenuButton target, out int index, out int total)
        {
            index = 0;
            total = 0;
            if (submenu == null || target == null || submenu.menuButtons == null || submenu.menuButtons.Count == 0)
            {
                return false;
            }

            var buttons = submenu.menuButtons.Values
                .Where(button => button != null && button.gameObject.activeInHierarchy && button.button != null && button.button.enabled && button.button.interactable)
                .OrderBy(button => button.number)
                .ToList();
            if (buttons.Count == 0)
            {
                return false;
            }

            total = buttons.Count;
            index = buttons.IndexOf(target) + 1;
            return index > 0;
        }

        private static ControlMenu GetControlMenu()
        {
            if (!Bookshelf.instance.systemScreens.ContainsKey(SystemScreen.Type.CONTROL_MENU))
            {
                return null;
            }

            var screen = Bookshelf.instance.systemScreens[SystemScreen.Type.CONTROL_MENU];
            if (screen == null || !screen.screenEnabled)
            {
                return null;
            }

            return screen as ControlMenu;
        }

        private static Messages GetMessagesScreen()
        {
            if (!Bookshelf.instance.systemScreens.ContainsKey(SystemScreen.Type.MESSAGES))
            {
                return null;
            }

            var screen = Bookshelf.instance.systemScreens[SystemScreen.Type.MESSAGES];
            if (screen == null || !screen.screenEnabled)
            {
                return null;
            }

            return screen as Messages;
        }

        private static Femtofax GetFemtofaxScreen()
        {
            if (!Bookshelf.instance.systemScreens.ContainsKey(SystemScreen.Type.FEMTOFAX))
            {
                return null;
            }

            var screen = Bookshelf.instance.systemScreens[SystemScreen.Type.FEMTOFAX];
            if (screen == null || !screen.screenEnabled)
            {
                return null;
            }

            return screen as Femtofax;
        }

        private static PacketteLoad GetPacketteLoadScreen()
        {
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

        private static TunerCalibration GetTunerCalibrationScreen()
        {
            if (!Bookshelf.instance.systemScreens.ContainsKey(SystemScreen.Type.TUNER_CALIBRATION))
            {
                return null;
            }

            var screen = Bookshelf.instance.systemScreens[SystemScreen.Type.TUNER_CALIBRATION];
            if (screen == null || !screen.screenEnabled)
            {
                return null;
            }

            return screen as TunerCalibration;
        }

        private static bool TryGetSelectedTunerProperty(TunerCalibration tuner, GameObject selectedObject, out TunerProperty property)
        {
            property = null;
            if (tuner == null || selectedObject == null)
            {
                return false;
            }

            if (IsSelectedSlider(tuner.frequencyMin, selectedObject))
            {
                property = tuner.frequencyMin;
                return true;
            }

            if (IsSelectedSlider(tuner.frequencyMax, selectedObject))
            {
                property = tuner.frequencyMax;
                return true;
            }

            if (IsSelectedSlider(tuner.eVRP, selectedObject))
            {
                property = tuner.eVRP;
                return true;
            }

            if (IsSelectedSlider(tuner.eARCP, selectedObject))
            {
                property = tuner.eARCP;
                return true;
            }

            return false;
        }

        private static bool IsSelectedSlider(TunerProperty property, GameObject selectedObject)
        {
            if (property == null || property.blippoSlider == null || property.blippoSlider.slider == null)
            {
                return false;
            }

            return IsSelectedObject(property.blippoSlider.slider.gameObject, selectedObject);
        }

        private static bool IsSelectedObject(GameObject candidate, GameObject selectedObject)
        {
            return candidate != null &&
                   selectedObject != null &&
                   (selectedObject == candidate || selectedObject.transform.IsChildOf(candidate.transform));
        }

        private static string GetTunerPropertyName(TunerProperty property)
        {
            if (property == null || property.propertyName == null || string.IsNullOrWhiteSpace(property.propertyName.text))
            {
                return Loc.Get("tuner_property_unknown");
            }

            return UiTextHelper.CleanText(property.propertyName.text);
        }

        private static string GetTunerPropertyValue(TunerProperty property)
        {
            if (property == null || property.valueText == null || string.IsNullOrWhiteSpace(property.valueText.text))
            {
                return Loc.Get("tuner_value_unknown");
            }

            return UiTextHelper.CleanText(property.valueText.text);
        }

        private static string GetSignalLossModalFallbackKey(SignalLoss.ModalType modalType)
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

        private static int GetPacketteChannelsFound(PacketteLoad packetteLoad)
        {
            if (packetteLoad == null)
            {
                return 0;
            }

            var text = UiTextHelper.GetText(packetteLoad.channelsFoundNumberText);
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var channelsFound))
            {
                return 0;
            }

            return channelsFound < 0 ? 0 : channelsFound;
        }

        private static string GetPacketteStepFallbackText(PacketteLoad.Step step)
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

        private static string GetFemtofaxProgramTitle(FemtofaxProgram program)
        {
            if (program == null)
            {
                return string.Empty;
            }

            if (program.femtofaxProgramObject != null && program.femtofaxProgramObject.title != null)
            {
                var title = UiTextHelper.CleanText(program.femtofaxProgramObject.title.Get());
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }

            return UiTextHelper.CleanText(program.gameObject != null ? program.gameObject.name : string.Empty);
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
                    return "where_am_i_screen_unknown";
            }
        }
    }
}

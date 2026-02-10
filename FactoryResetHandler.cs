using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlippoAccess
{
    /// <summary>
    /// Adds safety announcements for Factory Reset focus and confirmation flow in Control Menu.
    /// </summary>
    public sealed class FactoryResetHandler
    {
        private const float FocusWarningCooldownSeconds = 2f;
        private const float SubmitResultTimeoutSeconds = 8f;

        private bool _wasControlMenuActive;
        private int _lastSelectionId = int.MinValue;
        private float _lastFocusWarningTime;
        private Submenu _lastSubmenu;
        private bool _awaitingFactoryResetResult;
        private float _awaitingFactoryResetResultUntil;

        /// <summary>
        /// Tracks Factory Reset-related focus and announces high-risk context clearly.
        /// </summary>
        public void Update()
        {
            var menu = GetControlMenu();
            if (menu == null || menu.currentSubmenu == null)
            {
                HandlePendingResultOutsideMenu();
                ResetMenuState();
                return;
            }

            _wasControlMenuActive = true;
            AnnounceSubmenuWarningIfNeeded(menu.currentSubmenu);
            AnnounceFactoryResetFocusWarning(menu.currentSubmenu);
            TrackFactoryResetSubmit(menu.currentSubmenu);
        }

        private void ResetMenuState()
        {
            if (!_wasControlMenuActive)
            {
                return;
            }

            _wasControlMenuActive = false;
            _lastSelectionId = int.MinValue;
            _lastSubmenu = null;
            _lastFocusWarningTime = 0f;
        }

        private void HandlePendingResultOutsideMenu()
        {
            if (!_awaitingFactoryResetResult)
            {
                return;
            }

            if (GameManager.currentSystemScreen == SystemScreen.Type.PACKETTE_LOAD)
            {
                ScreenReader.Say(Loc.Get("factory_reset_completed"));
                DebugLogger.Log(LogCategory.Handler, "Factory reset completed");
                _awaitingFactoryResetResult = false;
                return;
            }

            if (Time.unscaledTime >= _awaitingFactoryResetResultUntil)
            {
                _awaitingFactoryResetResult = false;
            }
        }

        private void AnnounceSubmenuWarningIfNeeded(Submenu submenu)
        {
            if (submenu == null || submenu == _lastSubmenu)
            {
                return;
            }

            _lastSubmenu = submenu;
            if (!SubmenuContainsFactoryResetAction(submenu))
            {
                return;
            }

            ScreenReader.SayQueued(Loc.Get("factory_reset_submenu_warning"));
            ScreenReader.SayQueued(Loc.Get("factory_reset_cancel_hint"));
            DebugLogger.Log(LogCategory.Handler, "Factory reset submenu warning announced");
        }

        private void AnnounceFactoryResetFocusWarning(Submenu submenu)
        {
            var selectedObject = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selectedObject == null)
            {
                return;
            }

            var menuButton = selectedObject.GetComponent<MenuButton>() ?? selectedObject.GetComponentInParent<MenuButton>();
            if (menuButton == null)
            {
                _lastSelectionId = selectedObject.GetInstanceID();
                return;
            }

            var selectionId = selectedObject.GetInstanceID();
            if (selectionId == _lastSelectionId)
            {
                return;
            }

            _lastSelectionId = selectionId;
            if (!IsFactoryResetActionButton(menuButton))
            {
                return;
            }

            if (Time.unscaledTime - _lastFocusWarningTime < FocusWarningCooldownSeconds)
            {
                return;
            }

            _lastFocusWarningTime = Time.unscaledTime;
            ScreenReader.SayQueued(Loc.Get("factory_reset_focus_warning"));
            ScreenReader.SayQueued(Loc.Get("factory_reset_cancel_hint"));
            DebugLogger.Log(LogCategory.Handler, "Factory reset focus warning announced");
        }

        private void TrackFactoryResetSubmit(Submenu submenu)
        {
            if (submenu == null)
            {
                return;
            }

            var selectedObject = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selectedObject == null)
            {
                return;
            }

            var menuButton = selectedObject.GetComponent<MenuButton>() ?? selectedObject.GetComponentInParent<MenuButton>();
            if (!IsFactoryResetActionButton(menuButton))
            {
                return;
            }

            var submitPressed = Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter);
            if (!submitPressed)
            {
                submitPressed = Input.GetKeyDown(KeyCode.JoystickButton0);
            }
            if (!submitPressed)
            {
                return;
            }

            _awaitingFactoryResetResult = true;
            _awaitingFactoryResetResultUntil = Time.unscaledTime + SubmitResultTimeoutSeconds;
            ScreenReader.Say(Loc.Get("factory_reset_submitting"));
            DebugLogger.Log(LogCategory.Handler, "Factory reset submit detected");
        }

        private static ControlMenu GetControlMenu()
        {
            if (GameManager.instance == null || Bookshelf.instance == null)
            {
                return null;
            }

            if (GameManager.currentSystemScreen != SystemScreen.Type.CONTROL_MENU)
            {
                return null;
            }

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

        private static bool SubmenuContainsFactoryResetAction(Submenu submenu)
        {
            if (submenu == null || submenu.menuButtons == null || submenu.menuButtons.Count == 0)
            {
                return false;
            }

            foreach (var pair in submenu.menuButtons)
            {
                if (IsFactoryResetActionButton(pair.Value))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsFactoryResetContext(Submenu submenu, GameObject selectedObject)
        {
            if (SubmenuContainsFactoryResetAction(submenu))
            {
                return true;
            }

            if (selectedObject == null)
            {
                return false;
            }

            var menuButton = selectedObject.GetComponent<MenuButton>() ?? selectedObject.GetComponentInParent<MenuButton>();
            return IsFactoryResetActionButton(menuButton);
        }

        private static bool IsFactoryResetActionButton(MenuButton menuButton)
        {
            if (menuButton == null || menuButton.button == null || menuButton.button.onClick == null)
            {
                return false;
            }

            var onClick = menuButton.button.onClick;
            var persistentCount = onClick.GetPersistentEventCount();
            for (var i = 0; i < persistentCount; i++)
            {
                var methodName = onClick.GetPersistentMethodName(i);
                if (string.Equals(methodName, "FactoryReset", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            var label = UiTextHelper.GetMenuButtonLabel(menuButton);
            if (!string.IsNullOrWhiteSpace(label) &&
                label.IndexOf("factory reset", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return menuButton.gameObject.name.IndexOf("factory reset", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

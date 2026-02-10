using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlippoAccess
{
    /// <summary>
    /// Announces focused settings and submenu transitions in the control menu.
    /// </summary>
    public sealed class ControlMenuHandler
    {
        private const float MinimumAnnouncementIntervalSeconds = 0.1f;
        private const float SubmenuAnnouncementFallbackSeconds = 0.75f;

        private bool _wasActive;
        private int _lastSelectionId = int.MinValue;
        private Submenu _lastSubmenu;
        private float _lastAnnouncementTime;
        private string _pendingSubmenuTitle = string.Empty;
        private float _pendingSubmenuChangeTime;

        /// <summary>
        /// Tracks control menu focus and reads option labels/values with position context.
        /// </summary>
        public void Update()
        {
            var menu = GetControlMenu();
            if (menu == null)
            {
                ResetState();
                return;
            }

            _wasActive = true;
            AnnounceSubmenuChange(menu);
            FlushPendingSubmenuTitleIfStale();

            var selectedObject = EventSystem.current?.currentSelectedGameObject;
            if (selectedObject == null)
            {
                return;
            }

            var selectionId = selectedObject.GetInstanceID();
            if (selectionId == _lastSelectionId)
            {
                return;
            }

            var menuButton = selectedObject.GetComponent<MenuButton>() ?? selectedObject.GetComponentInParent<MenuButton>();
            if (menuButton == null)
            {
                _lastSelectionId = selectionId;
                return;
            }

            if (!UiTextHelper.IsMenuButtonInSubmenu(menu.currentSubmenu, menuButton))
            {
                _lastSelectionId = selectionId;
                return;
            }

            if (Time.unscaledTime - _lastAnnouncementTime < MinimumAnnouncementIntervalSeconds)
            {
                return;
            }

            var announcement = BuildButtonAnnouncement(menu, menuButton);
            if (string.IsNullOrWhiteSpace(announcement))
            {
                _lastSelectionId = selectionId;
                return;
            }

            var hasPendingSubmenu = !string.IsNullOrWhiteSpace(_pendingSubmenuTitle);
            if (hasPendingSubmenu && IsSubmenuHeaderEcho(_pendingSubmenuTitle, announcement))
            {
                _lastSelectionId = selectionId;
                return;
            }

            if (hasPendingSubmenu)
            {
                announcement = Loc.Get("submenu_focus_combined", _pendingSubmenuTitle, announcement);
                _pendingSubmenuTitle = string.Empty;
                _pendingSubmenuChangeTime = 0f;
                ScreenReader.SayQueued(announcement);
            }
            else
            {
                ScreenReader.Say(announcement);
            }

            _lastSelectionId = selectionId;
            _lastAnnouncementTime = Time.unscaledTime;
            DebugLogger.Log(LogCategory.Handler, $"Control menu focus: {announcement}");
        }

        private void ResetState()
        {
            if (!_wasActive)
            {
                return;
            }

            _wasActive = false;
            _lastSelectionId = int.MinValue;
            _lastSubmenu = null;
            _lastAnnouncementTime = 0f;
            _pendingSubmenuTitle = string.Empty;
            _pendingSubmenuChangeTime = 0f;
        }

        private void AnnounceSubmenuChange(ControlMenu menu)
        {
            if (menu.currentSubmenu == null || menu.currentSubmenu == _lastSubmenu)
            {
                return;
            }

            _lastSubmenu = menu.currentSubmenu;
            var title = UiTextHelper.GetLocalizedText(menu.currentSubmenu.menuTitleLocalizedText);
            if (string.IsNullOrWhiteSpace(title))
            {
                title = UiTextHelper.CleanText(menu.currentSubmenu.gameObject.name);
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                return;
            }

            _pendingSubmenuTitle = title;
            _pendingSubmenuChangeTime = Time.unscaledTime;
            _lastSelectionId = int.MinValue;
            _lastAnnouncementTime = 0f;
            DebugLogger.Log(LogCategory.Handler, $"Control submenu: {title}");
        }

        private void FlushPendingSubmenuTitleIfStale()
        {
            if (string.IsNullOrWhiteSpace(_pendingSubmenuTitle))
            {
                return;
            }

            if (Time.unscaledTime - _pendingSubmenuChangeTime < SubmenuAnnouncementFallbackSeconds)
            {
                return;
            }

            ScreenReader.SayQueued(Loc.Get("control_submenu_opened", _pendingSubmenuTitle));
            DebugLogger.Log(LogCategory.Handler, $"Control submenu fallback speech: {_pendingSubmenuTitle}");
            _pendingSubmenuTitle = string.Empty;
            _pendingSubmenuChangeTime = 0f;
            _lastAnnouncementTime = Time.unscaledTime;
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

        private static string BuildButtonAnnouncement(ControlMenu menu, MenuButton menuButton)
        {
            var label = UiTextHelper.GetMenuButtonLabel(menuButton);
            label = NormalizeLabelForSubmenu(menu.currentSubmenu, label);
            var value = UiTextHelper.GetMenuButtonValue(menuButton);

            var hasPosition = TryGetButtonPosition(menu.currentSubmenu, menuButton, out var index, out var total);
            if (hasPosition && !string.IsNullOrWhiteSpace(value))
            {
                return Loc.Get("control_focus_option_value_position", index, total, label, value);
            }

            if (hasPosition)
            {
                return Loc.Get("control_focus_option_position", index, total, label);
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                return Loc.Get("control_focus_option_value", label, value);
            }

            return Loc.Get("control_focus_option", label);
        }

        private static string NormalizeLabelForSubmenu(Submenu submenu, string label)
        {
            if (string.IsNullOrWhiteSpace(label) || submenu == null)
            {
                return label;
            }

            var submenuTitle = UiTextHelper.GetLocalizedText(submenu.menuTitleLocalizedText);
            if (string.IsNullOrWhiteSpace(submenuTitle))
            {
                submenuTitle = UiTextHelper.CleanText(submenu.gameObject.name);
            }

            var normalizedSubmenuTitle = submenuTitle.ToLowerInvariant();
            if (!normalizedSubmenuTitle.Contains("archived packette"))
            {
                return label;
            }

            if (!int.TryParse(label, out _))
            {
                return label;
            }

            return Loc.Get("control_packette_label", label);
        }

        private static bool TryGetButtonPosition(Submenu submenu, MenuButton target, out int index, out int total)
        {
            index = 0;
            total = 0;
            if (submenu == null || target == null || submenu.menuButtons == null || submenu.menuButtons.Count == 0)
            {
                return false;
            }

            var buttons = GetVisibleButtons(submenu);
            if (buttons.Count == 0)
            {
                return false;
            }

            total = buttons.Count;
            for (var i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] == target)
                {
                    index = i + 1;
                    return true;
                }
            }

            return false;
        }

        private static List<MenuButton> GetVisibleButtons(Submenu submenu)
        {
            return submenu.menuButtons.Values
                .Where(button => button != null && button.gameObject.activeInHierarchy && button.button != null && button.button.enabled && button.button.interactable)
                .OrderBy(button => button.number)
                .ToList();
        }

        private static bool IsSubmenuHeaderEcho(string submenuTitle, string announcement)
        {
            var normalizedTitle = UiTextHelper.CleanText(submenuTitle).ToLowerInvariant();
            var normalizedAnnouncement = UiTextHelper.CleanText(announcement).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedTitle) || string.IsNullOrWhiteSpace(normalizedAnnouncement))
            {
                return false;
            }

            return normalizedTitle == normalizedAnnouncement;
        }
    }
}

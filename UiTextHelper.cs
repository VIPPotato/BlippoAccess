using TMPro;
using UnityEngine;

namespace BlippoAccess
{
    /// <summary>
    /// Helpers for reading and normalizing UI text from decompiled menu components.
    /// </summary>
    internal static class UiTextHelper
    {
        public static string GetMenuButtonLabel(MenuButton menuButton)
        {
            if (menuButton == null)
            {
                return string.Empty;
            }

            var label = GetLocalizedText(menuButton.localizedText);
            if (!string.IsNullOrWhiteSpace(label))
            {
                return label;
            }

            if (menuButton.button != null)
            {
                return CleanText(menuButton.button.gameObject.name);
            }

            return CleanText(menuButton.gameObject.name);
        }

        public static string GetMenuButtonValue(MenuButton menuButton)
        {
            if (menuButton == null)
            {
                return string.Empty;
            }

            var value = GetLocalizedText(menuButton.valueLocalizedText);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            if (menuButton.valueLocalizedText != null && !string.IsNullOrWhiteSpace(menuButton.valueLocalizedText.overrideString))
            {
                return CleanText(menuButton.valueLocalizedText.overrideString);
            }

            return string.Empty;
        }

        public static string GetLocalizedText(NobleRobot.LocalizedText localizedText)
        {
            if (localizedText == null)
            {
                return string.Empty;
            }

            var appended = localizedText as NobleRobot.LocalizedTextControllerAppend;
            if (appended != null && !string.IsNullOrWhiteSpace(appended.cachedTextAppend))
            {
                return CleanText(appended.cachedTextAppend);
            }

            if (!string.IsNullOrWhiteSpace(localizedText.cachedText))
            {
                return CleanText(localizedText.cachedText);
            }

            if (localizedText.textField != null && !string.IsNullOrWhiteSpace(localizedText.textField.text))
            {
                return CleanText(localizedText.textField.text);
            }

            localizedText.LanguageChangeHandler(false);
            if (appended != null && !string.IsNullOrWhiteSpace(appended.cachedTextAppend))
            {
                return CleanText(appended.cachedTextAppend);
            }

            if (!string.IsNullOrWhiteSpace(localizedText.cachedText))
            {
                return CleanText(localizedText.cachedText);
            }

            return string.Empty;
        }

        public static string GetText(TMP_Text textField)
        {
            if (textField == null || string.IsNullOrWhiteSpace(textField.text))
            {
                return string.Empty;
            }

            return CleanText(textField.text);
        }

        public static bool IsSelectedObject(GameObject candidate, GameObject selectedObject)
        {
            return candidate != null &&
                selectedObject != null &&
                (selectedObject == candidate || selectedObject.transform.IsChildOf(candidate.transform));
        }

        public static bool IsMenuButtonInSubmenu(Submenu submenu, MenuButton menuButton)
        {
            if (submenu == null || menuButton == null)
            {
                return false;
            }

            if (submenu.menuButtons != null)
            {
                foreach (var button in submenu.menuButtons.Values)
                {
                    if (button == menuButton)
                    {
                        return true;
                    }
                }
            }

            if (menuButton.transform != null && submenu.transform != null && menuButton.transform.IsChildOf(submenu.transform))
            {
                return true;
            }

            var parentSubmenu = menuButton.GetComponentInParent<Submenu>(true);
            return parentSubmenu == submenu;
        }

        public static string CleanText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var input = value.Replace("(Clone)", string.Empty).Trim();
            if (input.IndexOf('<') < 0 || input.IndexOf('>') < 0)
            {
                return input;
            }

            var buffer = new char[input.Length];
            var index = 0;
            var insideTag = false;
            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];
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
    }
}

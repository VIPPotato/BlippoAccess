using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace BlippoAccess
{
    public class UIScanner
    {
        public static void ScanAndLogUI()
        {
            // Find all active Text components in the scene
            Text[] allTextComponents = Object.FindObjectsOfType<Text>();
            foreach (Text textComponent in allTextComponents)
            {
                if (textComponent.gameObject.activeInHierarchy && !string.IsNullOrEmpty(textComponent.text))
                {
                    MelonLogger.Msg($"[UI Text] Found: {textComponent.text} (GameObject: {textComponent.gameObject.name})");
                }
            }

            // Find all active Button components in the scene
            Button[] allButtonComponents = Object.FindObjectsOfType<Button>();
            foreach (Button buttonComponent in allButtonComponents)
            {
                if (buttonComponent.gameObject.activeInHierarchy)
                {
                    Text buttonText = buttonComponent.GetComponentInChildren<Text>();
                    if (buttonText != null && !string.IsNullOrEmpty(buttonText.text))
                    {
                        MelonLogger.Msg($"[UI Button] Found: {buttonText.text} (GameObject: {buttonComponent.gameObject.name})");
                    }
                    else
                    {
                        MelonLogger.Msg($"[UI Button] Found: {buttonComponent.gameObject.name} (No visible text)");
                    }
                }
            }
            // Future: Add more UI element types (Toggle, Slider, InputField, etc.)
        }
    }
}
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlippoAccess
{
    /// <summary>
    /// Announces tuner calibration focus and value changes for sliders and action buttons.
    /// </summary>
    public sealed class TunerCalibrationHandler
    {
        private bool _wasActive;
        private int _lastSelectionId = int.MinValue;
        private string _lastValueSignature = string.Empty;
        private float _lastValueAnnouncementTime;
        private bool _lastReadyToConfirm;

        /// <summary>
        /// Tracks tuner calibration UI focus and value changes and announces meaningful updates.
        /// </summary>
        public void Update()
        {
            if (!IsTunerCalibrationActive())
            {
                ResetState();
                return;
            }

            _wasActive = true;

            var tuner = GameManager.instance?.tunerCalibration;
            if (tuner == null)
            {
                return;
            }

            var selectedObject = EventSystem.current?.currentSelectedGameObject;
            if (selectedObject != null)
            {
                var selectionId = selectedObject.GetInstanceID();
                if (selectionId != _lastSelectionId)
                {
                    _lastSelectionId = selectionId;
                    AnnounceFocus(tuner, selectedObject);
                }
            }

            if (selectedObject != null && TryGetSelectedTunerProperty(tuner, selectedObject, out var selectedProperty))
            {
                AnnounceValueChange(selectedProperty);
            }

            var readyToConfirm = tuner.lockButton != null && tuner.lockButton.interactable;
            if (readyToConfirm != _lastReadyToConfirm)
            {
                _lastReadyToConfirm = readyToConfirm;
                if (readyToConfirm)
                {
                    ScreenReader.Say(Loc.Get("tuner_ready_to_confirm"), false);
                    DebugLogger.Log(LogCategory.Handler, "Tuner calibration ready for confirmation");
                }
            }
        }

        private void ResetState()
        {
            if (!_wasActive)
            {
                return;
            }

            _wasActive = false;
            _lastSelectionId = int.MinValue;
            _lastValueSignature = string.Empty;
            _lastValueAnnouncementTime = 0f;
            _lastReadyToConfirm = false;
        }

        private static bool IsTunerCalibrationActive()
        {
            if (GameManager.instance == null || Bookshelf.instance == null)
            {
                return false;
            }

            if (GameManager.currentSystemScreen != SystemScreen.Type.TUNER_CALIBRATION)
            {
                return false;
            }

            if (!Bookshelf.instance.systemScreens.ContainsKey(SystemScreen.Type.TUNER_CALIBRATION))
            {
                return false;
            }

            var screen = Bookshelf.instance.systemScreens[SystemScreen.Type.TUNER_CALIBRATION];
            return screen != null && screen.screenEnabled;
        }

        private void AnnounceFocus(TunerCalibration tuner, GameObject selectedObject)
        {
            if (TryGetSelectedTunerProperty(tuner, selectedObject, out var property))
            {
                var status = Loc.Get(property.locked ? "tuner_status_locked" : "tuner_status_adjust");
                var name = GetPropertyName(property);
                var value = GetPropertyValue(property);
                ScreenReader.Say(Loc.Get("tuner_focus_property", name, value, status));
                _lastValueSignature = BuildValueSignature(property);
                return;
            }

            if (IsSelectedObject(tuner.lockButton?.gameObject, selectedObject))
            {
                var availability = Loc.Get(tuner.lockButton != null && tuner.lockButton.interactable
                    ? "tuner_button_available"
                    : "tuner_button_unavailable");
                ScreenReader.Say(Loc.Get("tuner_focus_confirm", availability));
                _lastValueSignature = string.Empty;
                return;
            }

            if (IsSelectedObject(tuner.cancelButton?.gameObject, selectedObject))
            {
                ScreenReader.Say(Loc.Get("tuner_focus_cancel"));
                _lastValueSignature = string.Empty;
            }
        }

        private void AnnounceValueChange(TunerProperty property)
        {
            var signature = BuildValueSignature(property);
            if (signature == _lastValueSignature)
            {
                return;
            }

            // Keeps slider speech responsive while avoiding speech flooding on rapid drag changes.
            if (Time.unscaledTime - _lastValueAnnouncementTime < 0.12f)
            {
                return;
            }

            _lastValueAnnouncementTime = Time.unscaledTime;
            _lastValueSignature = signature;

            var status = Loc.Get(property.locked ? "tuner_status_locked" : "tuner_status_adjust");
            var name = GetPropertyName(property);
            var value = GetPropertyValue(property);
            ScreenReader.Say(Loc.Get("tuner_value_changed", name, value, status));
        }

        private static bool TryGetSelectedTunerProperty(TunerCalibration tuner, GameObject selectedObject, out TunerProperty property)
        {
            property = null;
            if (selectedObject == null || tuner == null)
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
            if (property == null || property.blippoSlider == null || property.blippoSlider.slider == null || selectedObject == null)
            {
                return false;
            }

            var sliderObject = property.blippoSlider.slider.gameObject;
            return IsSelectedObject(sliderObject, selectedObject);
        }

        private static bool IsSelectedObject(GameObject candidate, GameObject selectedObject)
        {
            return candidate != null &&
                selectedObject != null &&
                (selectedObject == candidate || selectedObject.transform.IsChildOf(candidate.transform));
        }

        private static string GetPropertyName(TunerProperty property)
        {
            if (property?.propertyName == null)
            {
                return Loc.Get("tuner_property_unknown");
            }

            var name = property.propertyName.text;
            if (string.IsNullOrWhiteSpace(name))
            {
                return Loc.Get("tuner_property_unknown");
            }

            return StripRichText(name);
        }

        private static string GetPropertyValue(TunerProperty property)
        {
            if (property?.valueText == null)
            {
                return Loc.Get("tuner_value_unknown");
            }

            var value = property.valueText.text;
            if (string.IsNullOrWhiteSpace(value))
            {
                return Loc.Get("tuner_value_unknown");
            }

            return StripRichText(value);
        }

        private static string BuildValueSignature(TunerProperty property)
        {
            return GetPropertyValue(property) + "|" + (property != null && property.locked ? "1" : "0");
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
    }
}

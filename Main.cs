using MelonLoader;
using DavyKager;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;
using TMPro;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace BlippoAccess
{
    public static class BuildInfo
    {
        public const string Name = "BlippoAccess";
        public const string Description = "Accessibility Mod for Blippo";
        public const string Author = "VIPPotato";
        public const string Company = null;
        public const string Version = "1.1.0";
        public const string DownloadLink = null;
    }

    public class BlippoAccessMod : MelonMod
    {
        private static string _currentSceneName = "";
        private GameObject _lastSelectedObject = null;
        
        // Throttling/Debounce
        private float _lastSelectionTime = 0f;
        private const float SELECTION_DEBOUNCE_DELAY = 0.1f; // Reduced to 100ms for snappier response
        private GameObject _pendingSelectedObject = null;

        // Channel Monitoring
        private string _lastChannelText = "";
        private string _lastProgramText = "";
        private float _channelCheckTimer = 0f;
        private float _messageScanTimer = 0f;

        public override void OnInitializeMelon() {
            MelonLogger.Msg("Initializing Blippo Access Mod...");
            Tolk.Load();
            Tolk.TrySAPI(true);
            string screenReader = Tolk.DetectScreenReader();
            MelonLogger.Msg($"Tolk loaded. Detected Screen Reader: {screenReader}");
            
            if (Tolk.HasSpeech())
            {
                Speak("Blippo Accessibility Mod Loaded");
            }
        }

        public override void OnSceneWasLoaded(int buildindex, string sceneName)
        {
            _currentSceneName = sceneName;
            Speak($"Scene loaded: {sceneName}");
            _lastSelectedObject = null;
            _pendingSelectedObject = null;
            _activeMessageObjects.Clear();
            _messageSettleTimers.Clear();
            _readMessageContent.Clear(); 
        }

        public override void OnApplicationQuit() 
        {
            Tolk.Unload();
        }

        private string StripRichText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            // Simple Regex to strip <...> tags
            return System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty);
        }

        public static void Speak(string text, bool interrupt = true)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            // Clean text
            string cleanText = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty);
            
            if (Tolk.HasSpeech())
            {
                Tolk.Speak(cleanText, interrupt);
            }
            MelonLogger.Msg($"[Speech] {cleanText}");
        }

        // Message Reading
        private Dictionary<int, string> _activeMessageObjects = new Dictionary<int, string>(); // InstanceID -> Text Content
        private Dictionary<int, float> _messageSettleTimers = new Dictionary<int, float>(); // InstanceID -> Time when text changed
        private HashSet<string> _readMessageContent = new HashSet<string>(); // Content that has been spoken to avoid repeats
        private const float MESSAGE_SETTLE_DELAY = 0.5f;

        public override void OnUpdate()
        {
            // 1. Handle standard UI selection with debounce
            MonitorUISelection();

            // 2. Monitor for Channel Name changes (Main View)
            MonitorChannelDisplay();

            // 3. Monitor for New/Changed Message Bodies
            // Optimization: Run scanning less frequently (every 0.1s) to avoid lag
            _messageScanTimer += Time.deltaTime;
            if (_messageScanTimer > 0.1f)
            {
                MonitorMessageBody();
                _messageScanTimer = 0f;
            }

            // 4. Manual "Read Screen" hotkey (Tab)
            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab))
            {
                ReadCurrentScreen();
            }
        }

        private void MonitorUISelection()
        {
            if (EventSystem.current == null) return;

            GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

            if (currentSelected != _lastSelectedObject)
            {
                // Selection changed
                _lastSelectedObject = currentSelected;
                _pendingSelectedObject = currentSelected;
                _lastSelectionTime = Time.time;
            }

            // Check if it's time to speak the pending object
            if (_pendingSelectedObject != null && (Time.time - _lastSelectionTime) > SELECTION_DEBOUNCE_DELAY)
            {
                string narration = GetTextFromUIObject(_pendingSelectedObject);
                
                // Prevent re-reading channel info if it's just been announced by the channel monitor
                if (!string.IsNullOrEmpty(narration) && narration != _lastChannelText && narration != _lastProgramText)
                {
                    Speak(narration, true);
                }
                _pendingSelectedObject = null; // Handled
            }
        }

        private void MonitorMessageBody()
        {
            // Find all potential message candidates
            var candidates = new List<GameObject>();

            // Helper to check validity
            bool IsValidCandidate(GameObject go, string text)
            {
                if (!go.activeInHierarchy) return false;
                if (string.IsNullOrEmpty(text)) return false;
                if (text.Length < 20) return false; // Ignore short labels
                if (go.name.Contains("Debug") || go.name.Contains("Console")) return false;
                if (text.Contains("BLIPPO+")) return false; // Filter decorative
                if (text.Contains("TRASH TYPE")) return false; // Filter noise
                
                // Prevent reading channel/program info as a "message"
                if (text == _lastChannelText || text == _lastProgramText) return false;

                return true;
            }

            // 1. Gather TextMeshProUGUI
            foreach (var t in Object.FindObjectsOfType<TextMeshProUGUI>())
            {
                if (t != null && t.text != null && IsValidCandidate(t.gameObject, t.text))
                    candidates.Add(t.gameObject);
            }

            // 2. Gather Standard Text (if not covered)
            foreach (var t in Object.FindObjectsOfType<Text>())
            {
                if (t != null && t.text != null && IsValidCandidate(t.gameObject, t.text))
                    candidates.Add(t.gameObject);
            }

            // Process Candidates
            foreach (var go in candidates)
            {
                int id = go.GetInstanceID();
                string currentText = GetTextFromUIObject(go); // Reuse helper for consistent text extraction

                // Is this object tracked?
                if (!_activeMessageObjects.ContainsKey(id))
                {
                    // New Object Discovered
                    _activeMessageObjects[id] = currentText;
                    _messageSettleTimers[id] = Time.time; // Start settling
                }
                else
                {
                    // Existing Object
                    if (_activeMessageObjects[id] != currentText)
                    {
                        // Text Changed
                        _activeMessageObjects[id] = currentText;
                        _messageSettleTimers[id] = Time.time; // Reset settle timer
                    }
                    else
                    {
                        // Text Stable. Check Timer.
                        if (Time.time - _messageSettleTimers[id] > MESSAGE_SETTLE_DELAY)
                        {
                            // Settled. Has it been read?
                            if (!_readMessageContent.Contains(currentText))
                            {
                                // New content! Read it.
                                Speak(currentText, true);
                                _readMessageContent.Add(currentText);
                            }
                        }
                    }
                }
            }

            // Cleanup: Remove objects that are no longer in candidates
            var currentIDs = candidates.Select(c => c.GetInstanceID()).ToHashSet();
            var toRemove = _activeMessageObjects.Keys.Where(k => !currentIDs.Contains(k)).ToList();
            foreach (var k in toRemove)
            {
                _activeMessageObjects.Remove(k);
                _messageSettleTimers.Remove(k);
                // We do NOT remove from _readMessageContent to avoid re-reading history if we scroll back
            }
        }

        private void MonitorChannelDisplay()
        {
            // Strict EPG Check: If we have selected an EPG item, do NOT monitor channels.
            if (_lastSelectedObject != null && _lastSelectedObject.name.Contains("Full-width Grid Item")) return;

            _channelCheckTimer += Time.deltaTime;
            if (_channelCheckTimer < 0.5f) return;
            _channelCheckTimer = 0f;

            var tmps = Object.FindObjectsOfType<TextMeshProUGUI>();
            
            string foundChannel = "";
            string foundProgram = "";

            foreach (var tmp in tmps)
            {
                if (!tmp.gameObject.activeInHierarchy) continue;
                if (tmp.text == null) continue;
                if (tmp.text.Length > 40 || tmp.text.Contains("BLIPPO+")) continue;

                // EPG Filter (skip badges)
                Transform p = tmp.transform.parent;
                bool isEPG = false;
                for(int i=0; i<3; i++) { if(p!=null && (p.name.Contains("Full-width") || p.name.Contains("Program Guide"))) { isEPG=true; break; } p = p?.parent; }
                if(isEPG) continue;

                string tName = tmp.name;
                string pName = tmp.transform.parent ? tmp.transform.parent.name : "";

                // Identify Channel Name
                if (tName.Contains("Channel Name") || pName.Contains("Channel Name"))
                {
                    string txt = tmp.text;
                    if (!int.TryParse(txt, out _)) foundChannel = txt;
                }
                // Identify Program Name (heuristic: "Program Name" or generic text near channel)
                else if (tName.Contains("Program Name") || tName.Contains("Title") || pName.Contains("Program info"))
                {
                     foundProgram = tmp.text;
                }
            }

            // Logic: Only speak if Channel changed.
            if (!string.IsNullOrEmpty(foundChannel) && foundChannel != _lastChannelText)
            {
                _lastChannelText = foundChannel;
                _lastProgramText = foundProgram;
                
                if (!string.IsNullOrEmpty(foundProgram))
                    Speak($"Channel: {foundChannel}. {foundProgram}");
                else
                    Speak($"Channel: {foundChannel}");
            }
        }

        private void ReadCurrentScreen()
        {
            Speak("Scanning screen...", true);
            StringBuilder sb = new StringBuilder();

            // Find all text objects
            var allTextHelpers = new List<TextHelper>();

            foreach (var t in Object.FindObjectsOfType<Text>()){
                if (t.gameObject.activeInHierarchy && !string.IsNullOrEmpty(t.text))
                    allTextHelpers.Add(new TextHelper { text = t.text, pos = t.transform.position, fontSize = t.fontSize });
            }
            foreach (var tmp in Object.FindObjectsOfType<TextMeshProUGUI>()){
                if (tmp.gameObject.activeInHierarchy && !string.IsNullOrEmpty(tmp.text))
                    allTextHelpers.Add(new TextHelper { text = tmp.text, pos = tmp.transform.position, fontSize = tmp.fontSize });
            }

            // Sort by vertical position (Top to Bottom), then horizontal (Left to Right)
            // Note: Screen Y increases upwards, so we sort active Descending for Top-to-Bottom
            var sortedTexts = allTextHelpers.OrderByDescending(t => t.pos.y).ThenBy(t => t.pos.x).ToList();

            foreach (var t in sortedTexts)
            {
                sb.AppendLine(t.text);
            }

            string fullText = sb.ToString();
            if (string.IsNullOrEmpty(fullText))
            {
                Speak("No text found on screen.");
            }
            else
            {
                // Chunk it if it's huge? Tolk usually handles long strings okay-ish, but let's just send it.
                Speak(fullText, true);
            }
        }

        private class TextHelper
        {
            public string text;
            public Vector3 pos;
            public float fontSize;
        }

        private string GetTextFromUIObject(GameObject go)
        {
            // 0. Special Handling for EPG Grid Items
            if (go.name.Contains("Full-width Grid Item"))
            {
                string epgInfo = GetEPGInfo(go);
                if (!string.IsNullOrEmpty(epgInfo)) return epgInfo;
            }

            // 1. Check for TextMeshProUGUI
            string text = "";
            TextMeshProUGUI tmpComp = go.GetComponent<TextMeshProUGUI>();
            if (tmpComp != null && !string.IsNullOrEmpty(tmpComp.text))
            {
                text = tmpComp.text;
            }
            else
            {
                TextMeshProUGUI[] tmpChildren = go.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmpChildren)
                {
                    if (!string.IsNullOrEmpty(tmp.text)) 
                    {
                        text = tmp.text;
                        break;
                    }
                }
            }

            // 2. Check for standard Text component if no TMP found
            if (string.IsNullOrEmpty(text))
            {
                Text textComp = go.GetComponent<Text>();
                if (textComp != null && !string.IsNullOrEmpty(textComp.text))
                {
                    text = textComp.text;
                }
                else
                {
                    Text[] textChildren = go.GetComponentsInChildren<Text>(true);
                    foreach (var t in textChildren)
                    {
                        if (!string.IsNullOrEmpty(t.text))
                        {
                            text = t.text;
                            break;
                        }
                    }
                }
            }

            // 3. Fallback to object name
            if (string.IsNullOrEmpty(text))
            {
                text = CleanObjectName(go.name);
            }

            // Custom Renaming
            if (text == "Enter Button") return "Open";

            return text;
        }

        private string GetEPGInfo(GameObject go)
        {
            Component[] components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                if (comp.GetType().Name == "FullWidthGridItem")
                {
                    string channelName = "";
                    string programTitle = "";

                    var type = comp.GetType();

                    // Try to get Channel info
                    try {
                        PropertyInfo channelProp = type.GetProperty("channel");
                        if (channelProp != null)
                        {
                            object channelObj = channelProp.GetValue(comp, null);
                            if (channelObj != null)
                            {
                                var chanType = channelObj.GetType();
                                var nameProp = chanType.GetProperty("Name") ?? chanType.GetProperty("name") ?? chanType.GetProperty("Code");
                                if (nameProp != null) channelName = nameProp.GetValue(channelObj, null)?.ToString();
                                else channelName = channelObj.ToString();
                            }
                        }
                    } catch {}

                    // Try to get Episode/Program info
                    try {
                        FieldInfo epField = type.GetField("episodeObject");
                        if (epField != null)
                        {
                            object epObj = epField.GetValue(comp);
                            if (epObj != null)
                            {
                                var epType = epObj.GetType();
                                var titleField = epType.GetField("title") ?? epType.GetField("Title") ?? epType.GetField("name") ?? epType.GetField("Name");
                                
                                if (titleField != null) 
                                {
                                    object titleValue = titleField.GetValue(epObj);
                                    if (titleValue != null)
                                    {
                                        if (titleValue is string s)
                                        {
                                            programTitle = s;
                                        }
                                        else
                                        {
                                            // Handle NobleRobot.LocalizedString or similar wrappers
                                            var valType = titleValue.GetType();
                                            
                                            // 1. Try Properties
                                            var strProp = valType.GetProperty("Value") ?? 
                                                          valType.GetProperty("value") ?? 
                                                          valType.GetProperty("StringValue") ?? 
                                                          valType.GetProperty("GetLocalizedValue");
                                            
                                            if (strProp != null) 
                                            {
                                                programTitle = strProp.GetValue(titleValue, null)?.ToString();
                                            }
                                            // 2. Try Methods
                                            else 
                                            {
                                                var method = valType.GetMethod("GetLocalizedValue") ?? valType.GetMethod("ToString");
                                                // Only use ToString if it's NOT the default object ToString (which returns type name)
                                                if (method.Name == "ToString" && method.DeclaringType == typeof(object))
                                                {
                                                    // Skip
                                                }
                                                else
                                                {
                                                    programTitle = method.Invoke(titleValue, null)?.ToString();
                                                }

                                                // 3. Try Fields (Backup)
                                                if (string.IsNullOrEmpty(programTitle) || programTitle.Contains("NobleRobot"))
                                                {
                                                    var strField = valType.GetField("m_Value", BindingFlags.NonPublic | BindingFlags.Instance) ?? 
                                                                   valType.GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance) ??
                                                                   valType.GetField("value", BindingFlags.Public | BindingFlags.Instance);
                                                     if (strField != null) programTitle = strField.GetValue(titleValue)?.ToString();
                                                }
                                            }
                                        }
                                    }
                                }
                                else 
                                {
                                    // Property fallback
                                    var titleProp = epType.GetProperty("title") ?? epType.GetProperty("Title") ?? epType.GetProperty("name") ?? epType.GetProperty("Name");
                                    if (titleProp != null) programTitle = titleProp.GetValue(epObj, null)?.ToString();
                                }
                            }
                        }
                    } catch {}

                    StringBuilder sb = new StringBuilder();
                    // Clean channel name
                    if (channelName != null && channelName.Contains("NobleRobot")) channelName = ""; 
                    
                    if (!string.IsNullOrEmpty(channelName)) sb.Append(channelName);
                    if (!string.IsNullOrEmpty(channelName) && !string.IsNullOrEmpty(programTitle)) sb.Append(", ");
                    if (!string.IsNullOrEmpty(programTitle)) sb.Append(programTitle);

                    return sb.ToString();
                }
            }
            return null;
        }

        private string CleanObjectName(string name)
        {
            return name.Replace("(Clone)", "").Trim();
        }
    }
}
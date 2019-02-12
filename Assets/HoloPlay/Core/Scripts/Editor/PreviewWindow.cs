//Copyright 2017-2018 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace HoloPlay.UI
{
    [Serializable]
    public struct GameWindowSettings
    {
        public Vector2 position;
        public bool enabled;

        public GameWindowSettings(Vector2 position, bool enabled)
        {
            this.position = position;
            this.enabled = enabled;
        }
    }

    [Serializable]
    public class PreviewWindowSettings
    {
        public GameWindowSettings[] gameWindowSettings = new GameWindowSettings[8]{
            // default to first display being enabled
            // new GameWindowSettings(Vector2.zero, true),
            // make the rest defualt
            new GameWindowSettings(), new GameWindowSettings(),
            new GameWindowSettings(), new GameWindowSettings(),
            new GameWindowSettings(), new GameWindowSettings(),
            new GameWindowSettings(), new GameWindowSettings()
        };
    }

    public static class PreviewWindow
    {
        static object gameViewSizesInstance;
        static MethodInfo getGroup;
        static int updateCount = 0;
        static bool windowOpen;
        static BindingFlags bindingFlags = 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.NonPublic;
        static int tabSize = 22 - 5; //this makes sense i promise

        public static PreviewWindowSettings previewWindowSettings;

        [MenuItem("HoloPlay/Toggle Preview %e", false, 1)]
        public static void ToggleWindow()
        {
            // set up the game view type
            var gameViewWindowType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            bool isMac = Application.platform == RuntimePlatform.OSXEditor;

            // close all open windows
            CloseActiveGameWindows();

            // if the window was already open, just set windowOpen to false and return
            if (windowOpen)
            {
                windowOpen = false;
                return;
            }

            // ? this is gonna cause lots of problems if there is no calibration
            LoadSettings();

            int displayIndex = -1;
            foreach (var setting in previewWindowSettings.gameWindowSettings)
            {
                // advance display index
                displayIndex++;

                // forget it if this display window is disabled
                if (!setting.enabled) continue;

                // create the window
                var gameViewWindow = (EditorWindow)EditorWindow.CreateInstance(gameViewWindowType);

                if (!isMac)
                {
                    var showModeType = typeof(Editor).Assembly.GetType("UnityEditor.ShowMode");
                    var showWithModeInfo = gameViewWindowType.GetMethod("ShowWithMode", bindingFlags);
                    showWithModeInfo.Invoke(gameViewWindow, new [] { Enum.ToObject(showModeType, 1) });
                }
                else
                {
                    gameViewWindow = EditorWindow.GetWindow(gameViewWindowType);
                }

                // set its size and position
                gameViewWindow.maxSize = new Vector2(Calibration.Main.screenW, Calibration.Main.screenH + tabSize);
                gameViewWindow.minSize = gameViewWindow.maxSize;
                gameViewWindow.position = new Rect(
                    setting.position.x, setting.position.y - tabSize, 
                    gameViewWindow.maxSize.x, gameViewWindow.maxSize.y);

                // set size and resolution
                SetSize(gameViewWindow, gameViewWindowType);
                SetResolution(gameViewWindow, gameViewWindowType);

                // set display number
                var displayNum = gameViewWindowType.GetField("m_TargetDisplay", bindingFlags);
                displayNum.SetValue(gameViewWindow, displayIndex);
            }

            updateCount = 0;
            windowOpen = true;
        }

        // todo: probably don't need this
        // [MenuItem("HoloPlay/Close All Preview Windows %e", false, 1)]
        public static void CloseAllPreviewWindows()
        {
            CloseAllGameWindows();
            windowOpen = false;
        }

        public static void SaveSettings()
        {
            EditorPrefs.SetString("HoloPlay-preview-settings", JsonUtility.ToJson(previewWindowSettings));
        }

        public static void LoadSettings()
        {
            float x = -2560;
            float y = 0;
            if (Calibration.Main != null)
            {
                x = -Calibration.Main.screenW;
                y = Screen.currentResolution.height - Calibration.Main.screenH;
            }

            // defualt settings
            previewWindowSettings = new PreviewWindowSettings();
            previewWindowSettings.gameWindowSettings[0].enabled = true;
            previewWindowSettings.gameWindowSettings[0].position.x = x;
            previewWindowSettings.gameWindowSettings[0].position.y = y;

            // try to load from json
            string json = EditorPrefs.GetString("HoloPlay-preview-settings", "");

            // check if the json minimally has at least one of the desired fields
            if (json.Contains("position"))
            {
                var loadedSettings = JsonUtility.FromJson<PreviewWindowSettings>(json);
                if (loadedSettings != null)
                {
                    previewWindowSettings = loadedSettings;
                    return;
                }
            }
        }

        public static GameViewSizeGroupType GetCurrentGroupType()
        {
            var getCurrentGroupTypeProp = gameViewSizesInstance.GetType().GetProperty("currentGroupType");
            return (GameViewSizeGroupType)(int)getCurrentGroupTypeProp.GetValue(gameViewSizesInstance, null);
        }

        static object GetGroup(GameViewSizeGroupType type)
        {
            return getGroup.Invoke(gameViewSizesInstance, new object[] { (int)type });
        }

        static void DelayedStuff()
        {
            // SetSize();
            updateCount++;
            if (updateCount > 10)
                EditorApplication.update -= DelayedStuff;
        }

        public static void SetSize(EditorWindow gameViewWindow, Type gameViewWindowType)
        {
            float targetScale = 1;
            BindingFlags bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var areaField = gameViewWindowType.GetField("m_ZoomArea", bindingFlags);
            var areaObj = areaField.GetValue(gameViewWindow);
            var scaleField = areaObj.GetType().GetField("m_Scale", bindingFlags);
            scaleField.SetValue(areaObj, new Vector2(targetScale, targetScale));
        }

        public static void SetResolution(EditorWindow gameViewWindow, Type gameViewWindowType)
        {
            PropertyInfo selectedSizeIndexProp = gameViewWindowType.GetProperty
            (
                "selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            Type sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instanceProp = singleType.GetProperty("instance");
            getGroup = sizesType.GetMethod("GetGroup");
            gameViewSizesInstance = instanceProp.GetValue(null, null);
            var group = GetGroup(GetCurrentGroupType());

            var getDisplayTexts = group.GetType().GetMethod("GetDisplayTexts");
            var displayTexts = (string[])getDisplayTexts.Invoke(group, null);
            int index = 0;
            for (int i = 0; i < displayTexts.Length; i++)
            {
                if (displayTexts[i].Contains("Standalone"))
                {
                    index = i;
                    break;
                }
            }
            if (index == 0)
            {
                Debug.LogWarning(Misc.debugText + "couldn't find standalone resolution in preview window");
            }

            selectedSizeIndexProp.SetValue(gameViewWindow, index, null);
        }

        /// <summary>
        /// Will close up to 8 game windows at once.
        /// It's relatively stupid and can't tell how many game windows are open, so it just does this 16 times
        /// </summary>
        public static void CloseAllGameWindows()
        {
            var gameViewWindowType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            for (int i = 0; i < 8; i++)
            {
                var window = EditorWindow.GetWindow(gameViewWindowType);
                window.Close();
            }
        }

        /// <summary>
        /// Will close only the open game windows
        /// </summary>
        public static void CloseActiveGameWindows()
        {
            var gameViewWindowType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var previewSettings = PreviewWindow.previewWindowSettings;
            if (previewSettings == null) {
                EditorWindow.GetWindow(typeof(PreviewWindowEditor));
                return;
            }
            GameWindowSettings[] settings = PreviewWindow.previewWindowSettings.gameWindowSettings;
            for (int i = 0; i < settings.Length; i++) {
                if (settings[i].enabled) {
                    var window = EditorWindow.GetWindow(gameViewWindowType);
                    window.Close();
                }
            }
        }
    }

    public class PreviewWindowEditor : EditorWindow
    {
        static EditorWindow window;
        const float pad = 6;
        Texture2D displayFull;
        string[] displayNames = {
            "Display 1", "Display 2", "Display 3", "Display 4",
            "Display 5", "Display 6", "Display 7", "Display 8",
        };
        static int selectedDisplay = 0;

        [MenuItem("HoloPlay/Preview Settings %#e", false, 2)]
        static void Init()
        {
            PreviewWindow.LoadSettings();
            window = EditorWindow.GetWindow(typeof(PreviewWindowEditor));
            window.minSize = new Vector2(400, 450);
            window.maxSize = window.minSize * 2;
            selectedDisplay = 0;

            if (window != null) window.Show();
        }

        void OnEnable()
        {
            displayFull = Resources.Load<Texture2D>("display_full");
            displayFull.filterMode = FilterMode.Point;
        }

        void OnDisable()
        {
            Resources.UnloadAsset(displayFull);
        }

        void OnGUI()
        {
            // load the settings if none are there
            if (PreviewWindow.previewWindowSettings == null)
            {
                PreviewWindow.LoadSettings();
            }

            GUILayout.Label("Selected Display: ");
            selectedDisplay = EditorGUILayout.Popup(selectedDisplay, displayNames);

            // making life a little easier
            GameWindowSettings[] settings = PreviewWindow.previewWindowSettings.gameWindowSettings;
            int i = selectedDisplay;
            
            // check if the display is activated
            settings[i].enabled = EditorGUILayout.Toggle(settings[i].enabled);

            // if it's not activated, gray out the options for this display
            GUI.enabled = settings[i].enabled;

            settings[i].position = EditorGUILayout.Vector2Field("Position", settings[i].position);
            EditorGUILayout.Space();

            GUILayout.Label("Presets: ");
            GUILayout.Label("Sets the position of the display window " +
                            "in relation to the previous display window", 
                            EditorStyles.miniLabel);
            var width = GUILayout.Width(80);
            GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                bool top = GUILayout.Button("Top", EditorStyles.miniButton, width);
                GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                bool left = GUILayout.Button("Left", EditorStyles.miniButton, width);
                GUILayout.Space(80);
                bool right = GUILayout.Button("Right", EditorStyles.miniButton, width);
                GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                bool bottom = GUILayout.Button("Bottom", EditorStyles.miniButton, width);
                GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // preset buttons
            // arranges based on the previous enabled display
            Rect previousDisplay = new Rect(0, 0, Screen.currentResolution.width, Screen.currentResolution.height);
            for (int j = i - 1; j >= 0; j--)
            {
                if (settings[j].enabled)
                {
                    previousDisplay.position = settings[j].position;
                    previousDisplay.size = new Vector2(
                        Calibration.Main.screenW,
                        Calibration.Main.screenH
                    );
                    break;
                }
            }

            if (top)
            {
                settings[i].position = new Vector2(
                    previousDisplay.x, 
                    -Calibration.Main.screenH + previousDisplay.y);
                EditorGUI.FocusTextInControl("");
                PreviewWindow.SaveSettings();
            }

            if (left)
            {
                settings[i].position = new Vector2(
                    -Calibration.Main.screenW + previousDisplay.x, 
                    previousDisplay.y);
                EditorGUI.FocusTextInControl("");
                PreviewWindow.SaveSettings();
            }

            if (right)
            {
                settings[i].position = new Vector2(
                    previousDisplay.width + previousDisplay.x, 
                    previousDisplay.y);
                EditorGUI.FocusTextInControl("");
                PreviewWindow.SaveSettings();
            }

            if (bottom)
            {
                settings[i].position = new Vector2(
                    previousDisplay.x, 
                    previousDisplay.height + previousDisplay.y);
                EditorGUI.FocusTextInControl("");
                PreviewWindow.SaveSettings();
            }

            EditorGUILayout.Space();

            // re-enable gui
            GUI.enabled = true;

            if (GUILayout.Button("Toggle Preview"))
            {
                PreviewWindow.SaveSettings();
                PreviewWindow.ToggleWindow();
                // EditorApplication.ExecuteMenuItem("HoloPlay/Toggle Preview");
            }

            EditorGUILayout.HelpBox("Toggle the previewer to affect changes", MessageType.Info);

            EditorGUILayout.HelpBox
            (
                "Note: keeping your HoloPlay Preview Window to the left is recommended. " +
                "If you are using it to the right of your main display, you may need to " +
                "adjust the x position manually, as OS zoom can sometimes cause the positioning to fail.",
                MessageType.Warning
            );

            // experimental
            if (Calibration.Main != null)
            {
                // positioning visual
                EditorGUILayout.LabelField("Positioning:");
                Rect position = EditorGUILayout.BeginVertical();
                position.y += 30; // a little padding
                float factor = 0.03f; // how much smaller this prop screen is

                // main display visual
                Rect mainDisplay = position;
                mainDisplay.width = Screen.currentResolution.width * factor;
                mainDisplay.height = Screen.currentResolution.height * factor;
                mainDisplay.x += position.width * 0.5f - mainDisplay.width * 0.5f;

                GUI.color = EditorGUIUtility.isProSkin ? Color.white : Color.black;
                GUI.DrawTexture(mainDisplay, displayFull);
                Rect mainDisplayLabel = mainDisplay;
                mainDisplayLabel.x += 4;
                mainDisplayLabel.y += 2;
                GUI.Label(mainDisplayLabel, "Main\nDisplay", EditorStyles.whiteMiniLabel);

                // extra display visuals                    
                int displayIndex = -1;
                foreach (var setting in settings)
                {
                    // advance display index
                    displayIndex++;

                    // disregard if it's disabled
                    if (!setting.enabled) continue;

                    Rect lkgDisplay = position;
                    lkgDisplay.width = Calibration.Main.screenW * factor;
                    lkgDisplay.height = Calibration.Main.screenH * factor;
                    lkgDisplay.x = mainDisplay.x + setting.position.x * factor;
                    lkgDisplay.y = mainDisplay.y + setting.position.y * factor;

                    GUI.color = Misc.guiColor;
                    GUI.DrawTexture(lkgDisplay, displayFull);
                    lkgDisplay.x += 4;
                    lkgDisplay.y += 2;
                    GUI.Label(lkgDisplay, "Display " + (displayIndex + 1), EditorStyles.whiteMiniLabel);
                }



                EditorGUILayout.EndVertical();
            }
        }
    }
}
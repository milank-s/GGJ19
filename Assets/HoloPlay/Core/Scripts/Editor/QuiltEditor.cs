//Copyright 2017-2018 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HoloPlay.UI
{
    [InitializeOnLoad]
    [CustomEditor(typeof(Quilt))]
    public class QuiltEditor : Editor
    {
        SerializedProperty captures;
        SerializedProperty overrideQuilt;
        // SerializedProperty overrideViews;
        SerializedProperty renderOverrideBehind;
        SerializedProperty quiltRT;
        SerializedProperty quiltSettings;
        SerializedProperty viewsHorizontal;
        SerializedProperty viewsVertical;
        SerializedProperty currentQuiltPreset;
        SerializedProperty viewWidth;
        SerializedProperty viewHeight;
        SerializedProperty quiltWidth;
        SerializedProperty quiltHeight;
        // SerializedProperty aspect;
        // SerializedProperty overscan;
        SerializedProperty numViews;
        SerializedProperty onQuiltSetup;
        SerializedProperty advancedFoldout;
        SerializedProperty renderIn2D;
        SerializedProperty debugPrintoutKey;
        SerializedProperty screenshot2DKey;
        SerializedProperty screenshot3DKey;
        SerializedProperty multiDisplayKey;
        SerializedProperty forceResolution;
        SerializedProperty calibrationIndex;
        SerializedProperty displayIndex;

        void OnEnable()
        {
            captures = serializedObject.FindProperty("captures");
            overrideQuilt = serializedObject.FindProperty("overrideQuilt");
            // overrideViews = serializedObject.FindProperty("overrideViews");
            renderOverrideBehind = serializedObject.FindProperty("renderOverrideBehind");
            quiltRT = serializedObject.FindProperty("quiltRT");
            quiltSettings = serializedObject.FindProperty("quiltSettings");
            viewsHorizontal = quiltSettings.FindPropertyRelative("viewsHorizontal");
            viewsVertical = quiltSettings.FindPropertyRelative("viewsVertical");
            currentQuiltPreset = serializedObject.FindProperty("currentQuiltPreset");
            viewWidth = quiltSettings.FindPropertyRelative("viewWidth");
            viewHeight = quiltSettings.FindPropertyRelative("viewHeight");
            quiltWidth = quiltSettings.FindPropertyRelative("quiltWidth");
            quiltHeight = quiltSettings.FindPropertyRelative("quiltHeight");
            // aspect = quiltSettings.FindPropertyRelative("aspect");
            // overscan = quiltSettings.FindPropertyRelative("overscan");
            numViews = quiltSettings.FindPropertyRelative("numViews");
            onQuiltSetup = serializedObject.FindProperty("onQuiltSetup");
            advancedFoldout = serializedObject.FindProperty("advancedFoldout");
            renderIn2D = serializedObject.FindProperty("renderIn2D");
            debugPrintoutKey = serializedObject.FindProperty("debugPrintoutKey");
            screenshot2DKey = serializedObject.FindProperty("screenshot2DKey");
            screenshot3DKey = serializedObject.FindProperty("screenshot3DKey");
            multiDisplayKey = serializedObject.FindProperty("multiDisplayKey");
            forceResolution = serializedObject.FindProperty("forceResolution");
            calibrationIndex = serializedObject.FindProperty("calibrationIndex");
            displayIndex = serializedObject.FindProperty("displayIndex");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Quilt quilt = (Quilt)target;

            EditorGUILayout.Space();

            GUI.color = Misc.guiColor;
            EditorGUILayout.LabelField("- Quilt -", EditorStyles.whiteMiniLabel);
            GUI.color = Color.white;

            GUI.enabled = false;
            EditorGUILayout.PropertyField(quiltRT);
            GUI.enabled = true;

            EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Take 2D Screenshot", EditorStyles.miniButton))
                    quilt.Screenshot2D();
                if (GUILayout.Button("Take 3D Screenshot", EditorStyles.miniButton))
                    quilt.Screenshot3D();
            EditorGUILayout.EndHorizontal();

            advancedFoldout.boolValue = EditorGUILayout.Foldout(
                advancedFoldout.boolValue,
                "Advanced",
                true
            );
            if (advancedFoldout.boolValue)
            {
                EditorGUI.indentLevel++;

                GUI.color = Misc.guiColor;
                EditorGUILayout.LabelField("- Captures -", EditorStyles.whiteMiniLabel);
                GUI.color = Color.white;

                EditorGUILayout.PropertyField(captures, true);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Capture", EditorStyles.miniButton))
                {
                    AddCapture(quilt);
                    EditorUtility.SetDirty(quilt);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
                EditorGUI.BeginChangeCheck();
                if (GUILayout.Button("Remove Capture", EditorStyles.miniButton))
                {
                    RemoveCapture(quilt);
                    EditorUtility.SetDirty(quilt);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();

                GUI.color = Misc.guiColor;
                EditorGUILayout.LabelField("- Quilt Settings -", EditorStyles.whiteMiniLabel);
                GUI.color = Color.white;

                EditorGUILayout.PropertyField(overrideQuilt);
                // EditorGUILayout.PropertyField(overrideViews, true);
                EditorGUILayout.PropertyField(renderOverrideBehind);
                EditorGUILayout.PropertyField(debugPrintoutKey);
                EditorGUILayout.PropertyField(screenshot2DKey);
                EditorGUILayout.PropertyField(screenshot3DKey);

                EditorGUILayout.PropertyField(forceResolution);

                List<string> tilingPresetNames = new List<string>();
                foreach (var p in System.Enum.GetValues(typeof(Quilt.QuiltPreset)))
                {
                    tilingPresetNames.Add(p.ToString());
                }
                tilingPresetNames.Add("Default (determined by quality setting)");
                tilingPresetNames.Add("Custom");

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(currentQuiltPreset);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }

                // if it's a custom
                if (currentQuiltPreset.intValue == (int)Quilt.QuiltPreset.Custom)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(viewWidth);
                    EditorGUILayout.PropertyField(viewHeight);
                    EditorGUILayout.PropertyField(numViews);
                    if (EditorGUI.EndChangeCheck())
                    {
                        quilt.ApplyPreset();
                        EditorUtility.SetDirty(quilt);
                    }
                }

                string tilingDisplay = numViews.displayName + ": " + numViews.intValue.ToString() + "\n";

                tilingDisplay += "View Tiling: " + viewsHorizontal.intValue + " x " + 
                                                   viewsVertical.intValue.ToString() + "\n";

                tilingDisplay += "View Size: " + viewWidth.intValue.ToString() + " x " +
                                                 viewHeight.intValue.ToString() + " px" + "\n";
                                                 
                tilingDisplay += "Quilt Size: " + quiltWidth.intValue.ToString() + " x " +
                                                  quiltHeight.intValue.ToString() + " px";

                EditorGUILayout.LabelField(tilingDisplay, EditorStyles.helpBox);
                EditorGUILayout.Space();

                GUI.color = Misc.guiColor;
                EditorGUILayout.LabelField("- Multi Display -", EditorStyles.whiteMiniLabel);
                GUI.color = Color.white;

                EditorGUILayout.PropertyField(calibrationIndex);

                // change display if displayIndex changes
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(displayIndex);
                if (EditorGUI.EndChangeCheck())
                {
                    quilt.QuiltCam.targetDisplay = displayIndex.intValue;
                }

                EditorGUILayout.PropertyField(multiDisplayKey);
                EditorGUILayout.HelpBox(
                    "Display Index 0 = Display 1.\n" +
                    "Unity names it Display 1 but the array is 0 indexed.\n\n" + 
                    "Keep in mind that the main display is usually Display 1, and the plugged in Looking Glass is likely Display 2.\n\n" +
                    "In most cases, you will want to leave this at 0 and set the display in the Launch window, but " +
                    "if you're using Multi Display you can set this manually for multiple Looking Glasses.", 
                    MessageType.None);

                // on quilt setup event
                EditorGUILayout.PropertyField(onQuiltSetup);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            GUI.color = Misc.guiColor;
            EditorGUILayout.LabelField("- Preview -", EditorStyles.whiteMiniLabel);
            GUI.color = Color.white;

            EditorGUILayout.PropertyField(renderIn2D);

            string previewerShortcutKey = "Ctrl + E";
            string settingsShortcutKey = "Ctrl + Shift + E";
#if UNITY_EDITOR_OSX
            previewerShortcutKey = "⌘E";
            settingsShortcutKey = "⌘^E";
#endif

            if (GUILayout.Button(new GUIContent(
                "Toggle Preview (" + previewerShortcutKey + ")",
                "If your LKG device is set up as a second display, " +
                "this will generate a game window on it to use as a " +
                "realtime preview"),
                EditorStyles.miniButton
            ))
            {
                PreviewWindow.ToggleWindow();
            }

            if (GUILayout.Button(new GUIContent(
                "Settings (" + settingsShortcutKey + ")",
                "Use to set previewer position"),
                EditorStyles.miniButton
            ))
            {
                EditorApplication.ExecuteMenuItem("HoloPlay/Preview Settings");
            }

            EditorGUILayout.Space();

            GUI.color = Misc.guiColor;
            EditorGUILayout.LabelField("- Calibration -", EditorStyles.whiteMiniLabel);
            GUI.color = Color.white;

            if (GUILayout.Button(new GUIContent(
                "Reload Calibration",
                "Reload the calibration, only really necessary if " +
                "you edited externally and the new calibration settings won't load"),
                EditorStyles.miniButton
            ))
            {
                CalibrationManager.LoadCalibrations();
                EditorUtility.SetDirty(quilt);
            }

            EditorGUILayout.Space();

            GUI.color = Misc.guiColor;
            EditorGUILayout.LabelField("- Project Settings -", EditorStyles.whiteMiniLabel);
            GUI.color = Color.white;

            if (GUILayout.Button(new GUIContent(
                "Optimization Settings",
                "Open a window that will let you select project settings " +
                "to be optimized for best performance with HoloPlay"),
                EditorStyles.miniButton
            ))
            {
                OptimizationSettings window = EditorWindow.GetWindow<OptimizationSettings>();
                window.Show();
            }

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("GameObject/HoloPlay Capture", false, 10)]
        public static void CreateHoloPlay()
        {
            var asset = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/HoloPlay/HoloPlay Capture.prefab", typeof(GameObject));
            if (asset == null)
            {
                Debug.LogWarning(Misc.debugText + "Couldn't find the holoplay capture folder or prefab. Must be at Assets/HoloPlay/HoloPlay Capture");
                return;
            }

            var clone = Instantiate(asset, Vector3.zero, Quaternion.identity);
            clone.name = asset.name;
        }

        public void AddCapture(Quilt quilt)
        {
            Undo.RegisterFullObjectHierarchyUndo(quilt, "Add Capture");
            var newCaptures = new Capture[quilt.captures.Length + 1];
            int i = 0;
            if (quilt.captures.Length == 0)
            {
                newCaptures[i] = Undo.AddComponent<Capture>(quilt.gameObject);
            }
            else
            {
                foreach (var c in captures)
                {
                    newCaptures[i] = quilt.captures[i];
                    i++;
                }
                newCaptures[i] = new GameObject("HoloPlay Capture " + (i + 1), typeof(Capture)).GetComponent<Capture>();
                Undo.RegisterCreatedObjectUndo(newCaptures[i].gameObject, "Add Capture");
            }
            quilt.captures = newCaptures;
        }

        public void RemoveCapture(Quilt quilt)
        {
            Undo.RegisterFullObjectHierarchyUndo(quilt, "Remove Capture");
            if (quilt.captures.Length == 0)
            {
                Debug.Log(Misc.debugText + "No Captures to remove!");
            }
            else
            {
                var newCaptures = new Capture[quilt.captures.Length - 1];
                if (quilt.captures.Length == 1)
                {
                    if (quilt.captures[quilt.captures.Length - 1] != null)
                    {
                        Undo.DestroyObjectImmediate(quilt.captures[quilt.captures.Length - 1]);
                        Undo.DestroyObjectImmediate(quilt.captures[quilt.captures.Length - 1].Cam.gameObject);
                    }
                }
                else
                {
                    for (int i = 0; i < quilt.captures.Length - 1; i++)
                    {
                        newCaptures[i] = quilt.captures[i];
                    }
                    if (quilt.captures[quilt.captures.Length - 1] != null)
                    {
                        Undo.DestroyObjectImmediate(quilt.captures[quilt.captures.Length - 1].gameObject);
                    }
                }
                quilt.captures = newCaptures;
            }
        }
    }
}
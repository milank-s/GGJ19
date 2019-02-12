using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HoloPlay.Extras
{
    [ExecuteInEditMode]
    public class SimpleFog : MonoBehaviour
    {
        [Range(0f, 1f)]
        public float relativeFalloff = 0.5f;
        Capture capture;

        void OnEnable()
        {
            capture = GetComponent<Capture>();
            if (capture == null)
            {
                Debug.LogWarning(Misc.debugText + "Must attach SimpleFog to a HoloPlay Capture");
            }
        }

        void Update()
        {
            if (capture == null)
            {
                return;
            }

            if (RenderSettings.fog && RenderSettings.fogMode == FogMode.Linear)
            {
                float clipLength = capture.Cam.farClipPlane - capture.Cam.nearClipPlane;
                RenderSettings.fogStartDistance = capture.Cam.nearClipPlane + relativeFalloff * clipLength;
                RenderSettings.fogEndDistance = capture.Cam.farClipPlane;
            }
        }

        public static void SetFogSettings()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.black;
        }
    }
}

#if UNITY_EDITOR
namespace HoloPlay.UI
{
    [CustomEditor(typeof(Extras.SimpleFog))]
    public class SimpleFogEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Setup Fog Settings"))
            {
                Extras.SimpleFog.SetFogSettings();
            }
            EditorGUILayout.HelpBox(
                "This will change your project-wide fog settings to work with the HoloPlay SimpleFog script. " +
                "Don't use this unless you want to replace your current fog settings!", MessageType.Warning
            );
        }
    }
}
#endif
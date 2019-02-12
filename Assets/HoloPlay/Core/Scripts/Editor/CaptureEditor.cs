//Copyright 2017-2018 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

//parts taken from Game Window Mover script, original notes for that here:

//Source from http://answers.unity3d.com/questions/179775/game-window-size-from-editor-window-in-editor-mode.html
//Modified by seieibob for use at the Virtual Environment and Multimodal Interaction Lab at the University of Maine.
//Use however you'd like!

using UnityEditor;
using UnityEngine;

namespace HoloPlay.UI
{
    // ? add handles
    // https://docs.unity3d.com/ScriptReference/Handles.html
    [InitializeOnLoad]
    [CustomEditor(typeof(Capture))]
    public class CaptureEditor : Editor
    {
        SerializedProperty size;
        SerializedProperty nearClipFactor;
        SerializedProperty farClipFactor;
        SerializedProperty orthographic;
        SerializedProperty fov;
        SerializedProperty advancedFoldout;
        SerializedProperty verticalAngleOffset;
        SerializedProperty horizontalAngleOffset;
        SerializedProperty viewConeFactor;
        Capture capture;
        SerializedObject serializedCam;

        void OnEnable()
        {
            size = serializedObject.FindProperty("size");
            nearClipFactor = serializedObject.FindProperty("nearClipFactor");
            farClipFactor = serializedObject.FindProperty("farClipFactor");
            fov = serializedObject.FindProperty("fov");
            advancedFoldout = serializedObject.FindProperty("advancedFoldout");
            verticalAngleOffset = serializedObject.FindProperty("verticalAngleOffset");
            horizontalAngleOffset = serializedObject.FindProperty("horizontalAngleOffset");
            viewConeFactor = serializedObject.FindProperty("viewConeFactor");

            capture = (Capture)target;
            if (capture.Cam != null)
            {
                serializedCam = new SerializedObject(capture.Cam);
                orthographic = serializedCam.FindProperty("orthographic");
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (serializedCam != null)
                serializedCam.Update();

            GUI.color = Misc.guiColorLight;
            EditorGUILayout.LabelField("HoloPlay " + Misc.version, EditorStyles.centeredGreyMiniLabel);
            GUI.color = Color.white;

            GUI.color = Misc.guiColor;
            EditorGUILayout.LabelField("- Camera -", EditorStyles.whiteMiniLabel);
            GUI.color = Color.white;

            EditorGUILayout.PropertyField(size);

            advancedFoldout.boolValue = EditorGUILayout.Foldout(advancedFoldout.boolValue, "Advanced", true);
            if (advancedFoldout.boolValue)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(nearClipFactor);
                EditorGUILayout.PropertyField(farClipFactor);

                if (orthographic != null)
                    EditorGUILayout.PropertyField(orthographic);

                if (orthographic != null)
                    GUI.enabled = !orthographic.boolValue;
                EditorGUILayout.PropertyField(fov);
                GUI.enabled = true;

                EditorGUILayout.PropertyField(verticalAngleOffset);
                EditorGUILayout.PropertyField(horizontalAngleOffset);
                EditorGUILayout.PropertyField(viewConeFactor);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
            if (serializedCam != null)
                serializedCam.ApplyModifiedProperties();
        }
        
        protected virtual void OnSceneGUI()
        {
            serializedObject.Update();
            var ct = capture.transform;

            // size handle
            Matrix4x4 handleMatrix = Handles.matrix;
            Matrix4x4 radiusMatrix = Matrix4x4.identity;
            radiusMatrix = Matrix4x4.TRS(ct.position, ct.rotation, 
                new Vector3(capture.Cam.aspect, 1f, 0.001f));
            Handles.matrix = radiusMatrix;

            var handleColor = Misc.gizmoColor1;
            handleColor.a = 0.5f;
            Handles.color = handleColor;

            float newSize = Handles.RadiusHandle(
                Quaternion.identity, Vector3.zero, size.floatValue, true
            );
            size.floatValue = Mathf.Clamp(newSize, 0.0001f, Mathf.Infinity);

            Handles.matrix = handleMatrix;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
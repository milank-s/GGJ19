using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HoloPlay.Extras
{
	[ExecuteInEditMode]
	public class SimpleDOF : MonoBehaviour 
	{
		public Capture capture;
		// start, bottom, rise, top
		public Vector4 dofParamsRelative = new Vector4(
			-2.0f, -0.75f, 1.0f, 3.0f
		);
		public float blurSize = 1.0f;
		public bool horizontalOnly = true;
		public bool testFocus;
		Material passdepthMat;
		Material boxBlurMat;
		Material finalpassMat;

		void OnEnable()
		{
			// check for capture
			if (capture == null)
			{
				capture = GetComponentInParent<Capture>();
				if (capture == null)
				{
					enabled = false;
					Debug.LogWarning(Misc.debugText + "Simple DOF needs to be on a holoplay capture's camera");
				}
			}
			capture.Cam.depthTextureMode = DepthTextureMode.Depth;

			passdepthMat = new Material(Shader.Find("HoloPlay/DOF/Pass Depth"));
			boxBlurMat   = new Material(Shader.Find("HoloPlay/DOF/Box Blur"));
			finalpassMat = new Material(Shader.Find("HoloPlay/DOF/Final Pass"));
		}

		void Update()
		{
			// passing shader vars
			Vector4 dofParamsInput = dofParamsRelative;
			dofParamsInput *= capture.Size;
			Vector4	dofParams = new Vector4(
				1.0f / (dofParamsInput.x - dofParamsInput.y),
				dofParamsInput.y,
				dofParamsInput.z,
				1.0f / (dofParamsInput.w - dofParamsInput.z)
			);
			Shader.SetGlobalVector("dofParams", dofParams);
			Shader.SetGlobalFloat("focalLength", capture.GetAdjustedDistance());
			Shader.SetGlobalInt("testFocus", testFocus ? 1 : 0);
			if (horizontalOnly)
				Shader.EnableKeyword("_HORIZONTAL_ONLY");
			else
				Shader.DisableKeyword("_HORIZONTAL_ONLY");
		}

		void OnDisable()
		{
			DestroyImmediate(passdepthMat);
			DestroyImmediate(boxBlurMat);
			DestroyImmediate(finalpassMat);
		}

		void OnRenderImage(RenderTexture src, RenderTexture dest)
		{
			// make the temporary pass rendertextures
			var fullres     = RenderTexture.GetTemporary(src.width, src.height, 0);
			var fullresDest = RenderTexture.GetTemporary(src.width, src.height, 0);
			var blur1 = RenderTexture.GetTemporary(src.width / 2, src.height / 2, 0);
			var blur2 = RenderTexture.GetTemporary(src.width / 3, src.height / 3, 0);
			var blur3 = RenderTexture.GetTemporary(src.width / 4, src.height / 4, 0);

			// passes: start with depth
			Graphics.Blit(src, fullres, passdepthMat);

			// blur 1
			boxBlurMat.SetInt("blurPassNum", 0);
			boxBlurMat.SetFloat("blurSize", blurSize * 2f);
			Graphics.Blit(fullres, blur1, boxBlurMat);

			// blur 2
			boxBlurMat.SetInt("blurPassNum", 1);
			boxBlurMat.SetFloat("blurSize", blurSize * 3f);
			Graphics.Blit(fullres, blur2, boxBlurMat);

			// blur 3
			boxBlurMat.SetInt("blurPassNum", 2);
			boxBlurMat.SetFloat("blurSize", blurSize * 4f);
			Graphics.Blit(fullres, blur3, boxBlurMat);

			// setting textures
			finalpassMat.SetTexture("blur1", blur1);
			finalpassMat.SetTexture("blur2", blur2);
			finalpassMat.SetTexture("blur3", blur3);

			// final blit for foreground
			Graphics.Blit(fullres, fullresDest, finalpassMat);
			Graphics.Blit(fullresDest, dest);

			// disposing of stuff
			RenderTexture.ReleaseTemporary(fullres);
			RenderTexture.ReleaseTemporary(fullresDest);
			RenderTexture.ReleaseTemporary(blur1);
			RenderTexture.ReleaseTemporary(blur2);
			RenderTexture.ReleaseTemporary(blur3);
		}
	}
}

#if UNITY_EDITOR
namespace HoloPlay.UI
{
    [CustomEditor(typeof(Extras.SimpleDOF))]
    public class SimpleDOFEditor : Editor
    {
        public override void OnInspectorGUI()
        {
			EditorGUILayout.Space();

            GUI.color = Misc.guiColor;
            EditorGUILayout.LabelField("- Depth of Field -", EditorStyles.whiteMiniLabel);
            GUI.color = Color.white;

            EditorGUILayout.HelpBox(
				"Simple DOF is a simple depth of field effect set up to work specifically with the HoloPlay Capture camera. ",
				MessageType.None
            );

			base.OnInspectorGUI();
        }
    }
}
#endif
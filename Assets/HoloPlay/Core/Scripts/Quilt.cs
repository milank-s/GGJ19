//Copyright 2017-2018 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Events;

namespace HoloPlay
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class Quilt : MonoBehaviour
    {
        //**********/
        //* fields */
        //**********/

        /// <summary>
        /// Static ref to the most recently active Quilt.
        /// </summary>
        private static Quilt instance;
        public static Quilt Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = FindObjectOfType<Quilt>();
                return instance;
            }
        }

        private Camera quiltCam;
        public Camera QuiltCam
        {
            get
            {
                if (quiltCam != null)
                    return quiltCam;

                quiltCam = GetComponent<Camera>();
                return quiltCam;
            }
        }

        /// <summary>
        /// The Captures this quilt will call render from
        /// </summary>
        [Tooltip("The HoloPlay Captures rendering to the QuiltRT. This Quilt calls Render on each of the Captures in this array in order")]
        public Capture[] captures;

        /// <summary>
        /// The material with the lenticular shader. The Quilt sets values for this material based on the calibration
        /// </summary>
        public Material lenticularMat;

        /// <summary>
        /// The actual rendertexture that gets drawn to the screen
        /// </summary>
        [Tooltip("The rendertexture that gets processed through the Lenticular material and spit to the screen")]
        public RenderTexture quiltRT;

        /// <summary>
        /// Useful for loading quilts directly instead of depending on a capture
        /// </summary>
        [Tooltip("Set this texture to load a quilt manually. Make sure to adjust the tiling settings to match.")]
        public Texture overrideQuilt;

        /// <summary>
        /// Used to load indivial views directly instead of depending on a capture
        /// </summary>
        public Texture[] overrideViews;

        [Tooltip("If true, the captures attached to this quilt will render on top of the override texture. " +
            "Make sure the capture camera backgrounds have an alpha value < 1.")]
        public bool renderOverrideBehind;

        private RenderTexture tileRT;

        /// <summary>
        /// Gets called for each view being rendered. Passes first the view number being rendered, then the number of views. 
        /// Gets called once per view render, then a final time after rendering is complete with viewBeingRendered equal to the number of views.
        /// </summary>
        public static Action<int, int> onViewRender;

        [Serializable]
        public struct QuiltSettings
        {
            [Range(256, 2048)]
            public int viewWidth;
            [Range(256, 2048)]
            public int viewHeight;
            [Range(1, 100)]
            public int numViews;
            public int viewsHorizontal;
            public int viewsVertical;
            public int quiltWidth;
            public int quiltHeight;
            public int paddingHorizontal;
            public int paddingVertical;
            public float viewPortionHorizontal;
            public float viewPortionVertical;
            public bool squareTiling;
            public float aspect;
            public bool overscan;

            public QuiltSettings(
                int viewWidth,
                int viewHeight,
                int numViews,
                int quiltWidth,
                int quiltHeight,
                int viewsHorizontal = 9,
                int viewsVertical = 5,
                bool squareTiling = true,
                float aspect = -1,
                bool overscan = false
            ) : this()
            {
                this.viewWidth = viewWidth;
                this.viewHeight = viewHeight;
                this.numViews = numViews;
                this.quiltWidth = quiltWidth;
                this.quiltHeight = quiltHeight;
                this.viewsHorizontal = viewsHorizontal;
                this.viewsVertical = viewsVertical;
                this.squareTiling = squareTiling;
                this.aspect = aspect;
                this.overscan = overscan;
                Setup();
            }

            public void Setup()
            {
                // enforce tiles horizontal and vertical allowing numviews
                if (squareTiling)
                {
                    float across = Mathf.Sqrt(numViews) * ((viewWidth + viewHeight) * 0.5f);
                    across /= viewWidth;
                    viewsHorizontal = Mathf.Min(Mathf.RoundToInt(across), numViews);
                    viewsVertical = Mathf.CeilToInt((float)numViews / viewsHorizontal);
                }
                else
                {
                    viewsHorizontal = Mathf.Max(viewsHorizontal, 1);
                    viewsVertical = Mathf.Max(viewsVertical, 1);
                    while (viewsHorizontal * viewsVertical < numViews)
                    {
                        viewsHorizontal++;
                    }
                }

                viewPortionHorizontal = (float)viewsHorizontal * viewWidth / quiltWidth;
                viewPortionVertical = (float)viewsVertical * viewHeight / quiltHeight;

                paddingHorizontal = quiltWidth - viewsHorizontal * viewWidth;
                paddingVertical = quiltHeight - viewsVertical * viewHeight;
            }
        }

        public QuiltSettings quiltSettings = new QuiltSettings(512, 256, 32, 2048, 2048);

        public static readonly QuiltSettings[] quiltPresets = new QuiltSettings[]{
            new QuiltSettings(512, 256, 32, 2048, 2048), // standard
            new QuiltSettings(819, 455, 45, 4096, 4096), // high res
            new QuiltSettings(400, 240, 24, 1600, 1440) // extra low
        };

        public enum QuiltPreset
        {
            Standard,
            HighRes,
            ExtraLow,
            Default,
            Custom
        }

        [SerializeField]
        private QuiltPreset currentQuiltPreset;
        public QuiltPreset CurrentQuiltPreset
        {
            get { return currentQuiltPreset; }
            set
            {
                currentQuiltPreset = value;
                ApplyPreset();
            }
        }

        // multi display settings
        [Range(0, 7)]
        public int calibrationIndex = 0;

        [Range(0, 7)]
        public int displayIndex = 0;

        ///<summary>
        /// Returns the calibration given this quilt's calibration index.
        /// Same as Calibration.Get(calibrationIndex)
        ///</summary>
        public Calibration QuiltCal 
        {
            get 
            { 
                // do some checks to make sure the calibration index isn't out of range
                if (calibrationIndex > 0 &&
                    calibrationIndex >= CalibrationManager.LoadedCalibrations.Count)
                {
                    return Calibration.Main;
                }

                return Calibration.Get(calibrationIndex);
            }
        }

        public KeyCode debugPrintoutKey = KeyCode.F8;
        public KeyCode screenshot2DKey = KeyCode.F9;
        public KeyCode screenshot3DKey = KeyCode.F10;
        public KeyCode multiDisplayKey = KeyCode.F11;

        /// <summary>
        /// Happens in OnEnable after calibration is loaded, screen is setup, material is created, and calibration is sent to shader
        /// </summary>
        public UnityEvent onQuiltSetup;

#if UNITY_EDITOR
        // for the editor script
        [SerializeField]
        bool advancedFoldout;

        [SerializeField]
        [Tooltip("Render in 2D. If set to true, the application will still render in 3D in play mode and in builds.")]
        bool renderIn2D = false;
#endif

        [SerializeField]
        [Tooltip("On startup, the resolution will automatically be set to the one read by the calibration. On by default.")]
        [FormerlySerializedAs("forceConfigResolution")]
        bool forceResolution = true;

        // used for multi display
        public string ID = "";

        //***********/
        //* methods */
        //***********/

        void OnEnable()
        {
            instance = this;

            SetupScreen();
            ApplyPreset();

            foreach (var capture in captures)
            {
                if (!capture) 
                    continue;
                capture.SetupCam(quiltSettings.aspect);
            }

            if (onQuiltSetup.GetPersistentEventCount() > 0)
                onQuiltSetup.Invoke();
            
            // multi display stuff
            if (ID == "")
                ID = Guid.NewGuid().ToString();
            else if (Application.isEditor)
            {
                // make sure it doesn't share ID with any other quilts
                var quilts = Resources.FindObjectsOfTypeAll<Quilt>();
                foreach (var q in quilts)
                {
                    if (this == q)
                        continue;
                    if (q.ID == ID)
                        ID = Guid.NewGuid().ToString();
                }
            }
            Extras.MultiDisplay.LoadAndSetupPrefs();
        }

        void OnDisable()
        {
            if (quiltRT && quiltRT.IsCreated())
            {
                quiltRT.Release();
                DestroyImmediate(quiltRT);
            }
            DestroyImmediate(lenticularMat);
        }

        void Update()
        {
            if (Input.GetKeyDown(debugPrintoutKey))
            {
                var currentDebugPrintouts = GetComponents<Extras.DebugPrintout>();
                if (currentDebugPrintouts.Length > 0)
                {
                    foreach (var c in currentDebugPrintouts)
                    {
                        Destroy(c);
                    }
                }
                else
                {
                    var printout = gameObject.AddComponent<Extras.DebugPrintout>();
                    printout.keyName = debugPrintoutKey.ToString();
                }
            }

            if (Input.GetKeyDown(multiDisplayKey))
            {
                var currentMultiDisplays = GetComponents<Extras.MultiDisplay>();
                if (currentMultiDisplays.Length > 0)
                {
                    foreach (var c in currentMultiDisplays)
                    {
                        Destroy(c);
                    }
                }
                else
                {
                    // var multiDisplay = gameObject.AddComponent<Extras.MultiDisplay>();
                    gameObject.AddComponent<Extras.MultiDisplay>();
                }
            }

            PassConfigToMaterial();

            if (Input.GetKeyDown(screenshot2DKey))
                Screenshot2D();

            if (Input.GetKeyDown(screenshot3DKey))
                StartCoroutine(StartScreenshot3D());

#if UNITY_EDITOR
            quiltCam.enabled = !renderIn2D;
            foreach (var capture in captures)
            {
                if (capture != null && capture.Cam != null)
                {
                    capture.Cam.enabled = renderIn2D;
                }
            }
#endif
        }

        void OnValidate()
        {
            ApplyPreset();
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            // clear rt
            Graphics.SetRenderTarget(quiltRT);
            GL.Clear(false, true, Color.black);
            if (overrideQuilt)
            {
                GL.PushMatrix();
                GL.LoadOrtho();
                Graphics.DrawTexture(new Rect(0, 1, 1, -1), overrideQuilt);
                GL.PopMatrix();

                if (!renderOverrideBehind)
                {
                    Graphics.Blit(quiltRT, dest, lenticularMat);
                    return;
                }
            }

            // render views
            for (int i = 0; i < quiltSettings.numViews; i++)
            {
                // broadcast the onViewRender action
                if (onViewRender != null && Application.isPlaying)
                    onViewRender(i, quiltSettings.numViews);

                int j = 0;
                foreach (var capture in captures)
                {
                    if (!capture || !capture.isActiveAndEnabled)
                        continue;
                    
                    capture.SetupCam(quiltSettings.aspect, false);
                    tileRT = RenderTexture.GetTemporary(quiltSettings.viewWidth, quiltSettings.viewHeight, 24);
                    capture.Cam.targetTexture = tileRT;
                    var bgColor = capture.Cam.backgroundColor;
                    if (j != 0)
                    {
                        capture.Cam.backgroundColor = bgColor * new Color(1, 1, 1, 0);
                    }
                    capture.RenderView(i, quiltSettings.numViews, QuiltCal.viewCone * capture.viewConeFactor);
                    CopyToQuiltRT(i, tileRT);
                    capture.Cam.targetTexture = null;
                    RenderTexture.ReleaseTemporary(tileRT);
                    capture.Cam.backgroundColor = bgColor;
                    j++;
                }
            }

            // reset cameras so they are back to center
            foreach (var capture in captures)
            {
                if (!capture)
                    continue;

                capture.HandleOffset(quiltSettings.aspect, QuiltCal.verticalAngle);
            }

            Graphics.Blit(quiltRT, dest, lenticularMat);
        }

        public void SetupQuilt()
        {
            quiltCam = GetComponent<Camera>();
            if (QuiltCam == null)
            {
                gameObject.AddComponent<Camera>();
                quiltCam = GetComponent<Camera>();
            }

            QuiltCam.enabled = true;
            QuiltCam.useOcclusionCulling = false;
            QuiltCam.cullingMask = 0;
            QuiltCam.clearFlags = CameraClearFlags.Nothing;
            QuiltCam.orthographic = true;
            QuiltCam.orthographicSize = 0.01f;
            QuiltCam.nearClipPlane = -0.01f;
            QuiltCam.farClipPlane = 0.01f;
            QuiltCam.stereoTargetEye = StereoTargetEyeMask.None;

            var shader = Shader.Find("HoloPlay/Lenticular");
            if (shader == null) return; // avoid error when first importing
            lenticularMat = new Material(shader);

            if (QuiltCal != null)
            {
                PassConfigToMaterial();
            }

            if (quiltRT != null)
                quiltRT.Release();

            quiltRT = new RenderTexture((int)quiltSettings.quiltWidth, (int)quiltSettings.quiltHeight, 0)
            {
                filterMode = FilterMode.Point,
                autoGenerateMips = false,
                useMipMap = false
            };
            quiltRT.Create();
        }

        public void CopyToQuiltRT(int view, Texture rt)
        {
            // copy to fullsize rt
            int ri = quiltSettings.viewsHorizontal * quiltSettings.viewsVertical - view - 1;
            // int ri = view;
            int x = (view % quiltSettings.viewsHorizontal) * quiltSettings.viewWidth;
            int y = (ri / quiltSettings.viewsHorizontal) * quiltSettings.viewHeight;
            // the padding is necessary because the shader takes y from the opposite spot as this does
            Rect rtRect = new Rect(x, y + quiltSettings.paddingVertical, quiltSettings.viewWidth, quiltSettings.viewHeight);

            if (quiltRT.IsCreated())
            {
                Graphics.SetRenderTarget(quiltRT);
                GL.PushMatrix();
                GL.LoadPixelMatrix(0, (int)quiltSettings.quiltWidth, (int)quiltSettings.quiltHeight, 0);
                Graphics.DrawTexture(rtRect, rt);
                GL.PopMatrix();
            }
            else
            {
                Debug.Log(Misc.debugText + "quilt not created yet");
            }
        }

        //* sending variables to the shader */
        public void PassConfigToMaterial()
        {
            float screenInches = (float)QuiltCal.screenW / QuiltCal.DPI;
            float newPitch = QuiltCal.pitch * screenInches;
            newPitch *= Mathf.Cos(Mathf.Atan(1f / QuiltCal.slope));
            lenticularMat.SetFloat("pitch", newPitch);

            float newTilt = QuiltCal.screenH / (QuiltCal.screenW * QuiltCal.slope);
            newTilt *= QuiltCal.flipImageX.asBool ? -1 : 1;
            lenticularMat.SetFloat("tilt", newTilt);

            float newCenter = QuiltCal.center;
            newCenter += QuiltCal.flipImageX.asBool ? 0.5f : 0;
            lenticularMat.SetFloat("center", newCenter);
            lenticularMat.SetFloat("invView", QuiltCal.invView);
            lenticularMat.SetFloat("flipX", QuiltCal.flipImageX);
            lenticularMat.SetFloat("flipY", QuiltCal.flipImageY);

            float subp = 1f / (QuiltCal.screenW * 3f);
            subp *= QuiltCal.flipImageX.asBool ? -1 : 1;
            lenticularMat.SetFloat("subp", subp);

            lenticularMat.SetInt("ri", !QuiltCal.flipSubp.asBool ? 0 : 2);
            lenticularMat.SetInt("bi", !QuiltCal.flipSubp.asBool ? 2 : 0);

            lenticularMat.SetVector("tile", new Vector4(
                quiltSettings.viewsHorizontal,
                quiltSettings.viewsVertical,
                quiltSettings.numViews,
                quiltSettings.viewsHorizontal * quiltSettings.viewsVertical
            ));

            lenticularMat.SetVector("viewPortion", new Vector4(
                quiltSettings.viewPortionHorizontal,
                quiltSettings.viewPortionVertical
            ));

            lenticularMat.SetVector("aspect", new Vector4(
                QuiltCal.screenW / QuiltCal.screenH,
                // if it's the default aspect (-1), just use the same aspect as the screen
                quiltSettings.aspect < 0 ? QuiltCal.screenW / QuiltCal.screenH : quiltSettings.aspect,
                quiltSettings.overscan ? 1 : 0
            ));
        }

        public void ApplyPreset()
        {
            switch (CurrentQuiltPreset)
            {
                case QuiltPreset.Standard:
                case QuiltPreset.HighRes:
                case QuiltPreset.ExtraLow:
                default:
                    quiltSettings = quiltPresets[(int)CurrentQuiltPreset];
                    break;
                case QuiltPreset.Default:
                    // if it's default (dynamic with player settings)
                    if (QualitySettings.lodBias < 0.5f)
                    {
                        quiltSettings = quiltPresets[(int)QuiltPreset.ExtraLow];
                    }
                    else if (QualitySettings.lodBias < 1)
                    {
                        quiltSettings = quiltPresets[(int)QuiltPreset.Standard];
                    }
                    else
                    {
                        quiltSettings = quiltPresets[(int)QuiltPreset.HighRes];
                    }
                    break;
                case QuiltPreset.Custom:
                    break;
            }

            quiltSettings.Setup();

            SetupQuilt();
        }

        public void SetupScreen()
        {
            // setup additional displays if necessary
            if (displayIndex > 0)
            {
                // if in playmode, activate the addl display
                if (!Application.isEditor)
                {
                    if (displayIndex >= Display.displays.Length)
                    {
                        Debug.Log(Misc.debugText + "Not enough displays connected for multi display");
                    }
                    else
                    {
                        Display.displays[displayIndex].Activate();
                        QuiltCam.targetDisplay = displayIndex;
                        Debug.Log( Misc.debugText + "Activated display " + displayIndex + " for multi display");
                    }
                }
            }

            // force screen resolution
            if (!forceResolution)
                return;

#if UNITY_EDITOR
            if (UnityEditor.PlayerSettings.defaultScreenWidth != QuiltCal.screenW.asInt ||
                UnityEditor.PlayerSettings.defaultScreenHeight != QuiltCal.screenH.asInt)
            {
                UnityEditor.PlayerSettings.defaultScreenWidth = QuiltCal.screenW.asInt;
                UnityEditor.PlayerSettings.defaultScreenHeight = QuiltCal.screenH.asInt;
            }
#endif

            // if the config is already set, return out
            if (Screen.width == QuiltCal.screenW.asInt &&
                Screen.height == QuiltCal.screenH.asInt)
            {
                return;
            }

            Screen.SetResolution(QuiltCal.screenW.asInt, QuiltCal.screenH.asInt, true);
        }

        public void Screenshot2D()
        {
            Texture2D screenTex = new Texture2D(QuiltCal.screenW.asInt, QuiltCal.screenH.asInt, TextureFormat.RGB24, false);
            RenderTexture screenRT = RenderTexture.GetTemporary(screenTex.width, screenTex.height, 24);
            // var previousRT = Capture.Instance.cam.targetTexture;
            Capture.Instance.Cam.targetTexture = screenRT;
            Capture.Instance.Cam.ResetWorldToCameraMatrix();
            Capture.Instance.Cam.ResetProjectionMatrix();
            Capture.Instance.Cam.Render();
            // Capture.Instance.cam.targetTexture = previousRT;

            RenderTexture.active = screenRT;
            screenTex.ReadPixels(new Rect(0, 0, quiltRT.width, quiltRT.height), 0, 0);
            RenderTexture.active = null;
            var bytes = screenTex.EncodeToPNG();
            string fullPath;
            string fullName;
            if (!Misc.GetNextFilename(Path.GetFullPath("."), Application.productName, ".png", out fullName, out fullPath))
            {
                Debug.LogWarning(Misc.debugText + "Couldn't save screenshot");
            }
            else
            {
                // fullFileName += DateTime.Now.ToString(" yyyy MMdd HHmmss");
                // fullFileName = fullFileName.Replace(" ", "_") + ".png";
                File.WriteAllBytes(fullPath, bytes);
                Debug.Log(Misc.debugText + "Wrote 2D screenshot to " + fullPath);
            }

            RenderTexture.ReleaseTemporary(screenRT);
            // Destroy(screenTex);
        }

        IEnumerator StartScreenshot3D()
        {
            var previousTiling = quiltSettings;
            quiltSettings = quiltPresets[1];
            SetupQuilt();

            yield return null;

            Screenshot3D();

            quiltSettings = previousTiling;
            SetupQuilt();
        }

        public void Screenshot3D()
        {
            // checking to make sure the quilt settings are correct
            if (quiltSettings.viewWidth != quiltPresets[1].viewWidth ||
                quiltSettings.viewHeight != quiltPresets[1].viewHeight ||
                quiltSettings.numViews != quiltPresets[1].numViews)
            {
                if (!Application.isPlaying)
                    Debug.LogWarning(Misc.debugText + "Cannot take screenshot, quilt settings incorrect. Try taking one in play mode!");
                else
                    Debug.LogWarning(Misc.debugText + "Cannot take screenshot, quilt settings incorrect. Shouldn't see this message! Please file a bug report on our forums");
                return;
            }

            Texture2D quiltTex = new Texture2D(quiltRT.width, quiltRT.height, TextureFormat.RGB24, false);
            RenderTexture.active = quiltRT;
            quiltTex.ReadPixels(new Rect(0, 0, quiltRT.width, quiltRT.height), 0, 0);
            RenderTexture.active = null;
            var bytes = quiltTex.EncodeToPNG();
            string fullPath;
            string fullName;
            if (!Misc.GetNextFilename(Path.GetFullPath("."), Application.productName, ".png", out fullName, out fullPath))
            {
                Debug.LogWarning(Misc.debugText + "Couldn't save screenshot");
            }
            else
            {
                // fullFileName += DateTime.Now.ToString(" yyyy MMdd HHmmss");
                // fullFileName = fullFileName.Replace(" ", "_") + ".png";
                File.WriteAllBytes(fullPath, bytes);
                Debug.Log(Misc.debugText + "Wrote 3D screenshot (quilt) to " + fullPath);
            }
        }
    }
}
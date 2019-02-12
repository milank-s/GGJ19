using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloPlay.Extras
{
	public class MultiDisplay : MonoBehaviour 
	{
		public List<Quilt> quilts;
		public List<Quilt.QuiltSettings> quiltSettings;
		public Texture2D bwTex;
		public bool bwActivated;
		public string feedbackText;

        void OnEnable()
        {
			var allMultiDisplays = FindObjectsOfType<MultiDisplay>();
			if (allMultiDisplays.Length > 1)
			{
				DestroyImmediate(this);
				return;
			}

			quilts = new List<Quilt>();
			quiltSettings = new List<Quilt.QuiltSettings>();
			var quiltArray = FindObjectsOfType<Quilt>();
			if (quiltArray.Length == 0)
			{
				Debug.Log(Misc.debugText + "Couldn't find any Quilts to MultiDisplay");
			}
			quilts.AddRange(quiltArray);

			bwTex = Resources.Load<Texture2D>("black_and_white");

			foreach (var q in quilts)
			{
				quiltSettings.Add(q.quiltSettings);
			}
		}

		void OnDisable()
		{
			if (quilts == null)
			{
				Debug.Log(Misc.debugText + "on disable found no quilts!");
				return;
			}

			int i = 0;
			foreach (var q in quilts)
			{
				if (q == null) 
				{
					Debug.Log(Misc.debugText + "on disable found null quilt!");
					continue;
				}
				q.quiltSettings = quiltSettings[i++];
				q.overrideQuilt = null;
				q.SetupQuilt();
				Resources.UnloadAsset(bwTex);
				Debug.Log(Misc.debugText + "reset quilt!");
			}
		}

		void OnGUI()
		{
			if (quilts == null)
			{
				Debug.Log(Misc.debugText + "No quilts found!");
				return;
			}

			int numCalibrations = CalibrationManager.LoadedCalibrations.Count;
			int numDisplays = Display.displays.Length;
			float sliderWidth = 400;

			GUILayout.BeginArea(new Rect(100, 100, 500, 800));
			
			int i = 1;
			foreach (var q in quilts)
			{
				GUILayout.BeginVertical(GUI.skin.box);
					GUILayout.Label("HoloPlay " + i);
					GUILayout.BeginHorizontal();
						GUILayout.Label("Calibration Index: ");
						sliderWidth = Mathf.Max(50 * (numCalibrations - 1), 10);
						q.calibrationIndex = (int)GUILayout.HorizontalSlider(
							q.calibrationIndex, 0, Mathf.Max(numCalibrations - 1, 1), 
							GUILayout.Width(sliderWidth)
						);
						GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
						GUILayout.Label("Display Index: ");
						int currDisplay = q.displayIndex;
						sliderWidth = Mathf.Max(50 * (numDisplays - 1), 10);
						q.displayIndex = (int)GUILayout.HorizontalSlider(
							q.displayIndex, 0, Mathf.Max(numDisplays - 1, 1), 
							GUILayout.Width(sliderWidth)
						);
						GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
				GUILayout.EndVertical();
				
				// if display changed, re-setup screen
				if (currDisplay != q.displayIndex)
				{
					q.SetupScreen();
				}
				i++;
			}

			bool currBwActivated = bwActivated;
			bwActivated = GUILayout.Toggle(
				bwActivated, "Check Calibration"
			);
			if (bwActivated != currBwActivated)
			{
				i = 0;
				foreach (var q in quilts)
				{
					if (bwActivated)
					{
						q.quiltSettings = new Quilt.QuiltSettings(2, 2, 16, 4, 4);
						q.overrideQuilt = bwTex;
					}
					else
					{
						q.quiltSettings = quiltSettings[i++];
						q.overrideQuilt = null;
					}
					q.SetupQuilt();
				}
			}

			if (GUILayout.Button("Save Multi Display Preferences"))
			{
				SaveToPrefs(true);
				feedbackText = "Saved!";
				StartCoroutine(TimeoutFeedbackText());
			}

			if (GUILayout.Button("Erase Preferences"))
			{
				PlayerPrefs.DeleteKey("HoloPlay-MultiDisplay");
				feedbackText = "Multi Display data erased!";
				StartCoroutine(TimeoutFeedbackText());
			}

			GUILayout.Label(feedbackText);

			GUILayout.EndArea();
		}

		IEnumerator TimeoutFeedbackText()
		{
			yield return new WaitForSeconds(1f);
			feedbackText = "";
		}

		public void SaveToPrefs(bool warnings = false)
		{
			var serialized = new MultiDisplayData();
			foreach (var q in quilts)
			{
				serialized.qID.Add(q.ID);
				serialized.calInd.Add(q.calibrationIndex);
				serialized.disInd.Add(q.displayIndex);
			}
			string json = JsonUtility.ToJson(serialized);
			if (warnings)
				Debug.Log(Misc.debugText + json);

			PlayerPrefs.SetString("HoloPlay-MultiDisplay", json);
		}

		public static void LoadAndSetupPrefs(bool warnings = false)
		{
			string json = PlayerPrefs.GetString("HoloPlay-MultiDisplay", "");

			// empty string, just return
			if (json == "")
			{
				if (warnings)
					Debug.Log(Misc.debugText + "No prefs for Multi Display found");
				return;
			}

			var serialized = JsonUtility.FromJson<MultiDisplayData>(json);

			// quick ugly check for if this is actually data
			if (serialized.qID.Count == 0 ||
				serialized.qID.Count != serialized.calInd.Count ||
				serialized.qID.Count != serialized.disInd.Count)
			{
				if (warnings)
					Debug.Log(Misc.debugText + "Improper data for Multi Display");
				return;
			}

			// this won't find disabled quilts unfortunately
			var quilts = FindObjectsOfType<Quilt>();

			int i = 0;
			foreach (var qid in serialized.qID)
			{
				// setup the quilt if the qid matches
				foreach (var q in quilts)
				{
					if (q.ID == qid)
					{
						if (warnings)
							Debug.Log(Misc.debugText + "Setting up multi display quilt!\n" + qid);
						q.calibrationIndex = serialized.calInd[i];
						q.displayIndex = serialized.disInd[i];
						q.SetupScreen();
						q.SetupQuilt();
					}
				}
				i++;
			}
		}

		[Serializable]
		public class MultiDisplayData
		{
			public List<string> qID;
			public List<int> calInd;
			public List<int> disInd;

			public MultiDisplayData()
			{
				qID = new List<string>();
				calInd = new List<int>();
				disInd = new List<int>();
			}
		}
	}
}
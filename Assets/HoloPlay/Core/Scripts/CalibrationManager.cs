//Copyright 2017-2018 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using HoloPlay.Extras;
using UnityEngine;
using UnityEngine.Events;

namespace HoloPlay
{
    /// <summary>
    /// Type for lenticular calibration
    /// </summary>
    [Serializable]
    public class Calibration
    {
        [Serializable]
        public class Parameter
        {
            public readonly bool isInt;
            [SerializeField]
            float value;
            public float Value
            {
                get { return value; }
                set
                {
                    this.value = isInt ? Mathf.Round(value) : value;
                    this.value = Mathf.Clamp(this.value, min, max);
                }
            }
            public readonly float defaultValue;
            public readonly float min;
            public readonly float max;
            public readonly string name;
            public Parameter(float defaultValue, float min, float max, string name, bool isInt = false)
            {
                this.defaultValue = defaultValue;
                this.min = min;
                this.max = max;
                this.Value = defaultValue;
                this.name = name;
                this.isInt = isInt;
            }

            // just to make life easier
            public int asInt { get { return (int)value; } }
            public bool asBool { get { return (int)value == 1; } }

            public static implicit operator float(Parameter parameter)
            {
                return parameter.Value;
            }
        }

        public string configVersion = "1.0";
        public string serial = "00000";
        public Parameter pitch = new Parameter(100f, 1f, 200, "Pitch");
        public Parameter slope = new Parameter(10f, -30, 30, "Slope");
        public Parameter center = new Parameter(0, -1, 1, "Center");
        public Parameter viewCone = new Parameter(50, 0, 180, "View Cone");
        public Parameter invView = new Parameter(0, 0, 1, "View Inversion", true);
        public Parameter verticalAngle = new Parameter(0, -20, 20, "Vert Angle");
        public Parameter DPI = new Parameter(300, 1, 1000, "DPI", true);
        public Parameter screenW = new Parameter(2560, 640, 6400, "Screen Width", true);
        public Parameter screenH = new Parameter(1600, 480, 4800, "Screen Height", true);
        public Parameter flipImageX = new Parameter(0, 0, 1, "Flip Image X", true);
        public Parameter flipImageY = new Parameter(0, 0, 1, "Flip Image Y", true);
        public Parameter flipSubp = new Parameter(0, 0, 1, "Flip Subpixels", true);
        [NonSerialized] public string loadedFrom = "not loaded -- default used";
        [NonSerialized] public bool loadedSuccess = false;

        /// <summary>
        /// Gets the calibration at index of the loaded calibrations.
        /// Will load the calibrations from flash+storage if this hasn't been done yet.
        /// This is the same as CalibrationManager.LoadedCalibrations[index]
        /// </summary>
        public static Calibration Get(int index)
        {
            return CalibrationManager.LoadedCalibrations[index];
        }

        /// <summary>
        /// Returns the 0th loaded calibration.
        /// Same as Calibration.Main
        /// </summary>
        public static Calibration Main
        {
            get { return Get(0); }
        }
    }

    public static class CalibrationManager
    {
        //**********/
        //* fields */
        //**********/

        private static List<Calibration> loadedCalibrations;
        public static List<Calibration> LoadedCalibrations
        {
            get
            {
                if (loadedCalibrations != null && loadedCalibrations.Count != 0) 
                    return loadedCalibrations;
                LoadCalibrations();
                return loadedCalibrations;
            }
        }

        //***********/
        //* methods */
        //***********/

        [DllImport("HoloPlayAPI")]
        private static extern int load_calibration(byte[] str);

        public static Calibration.Parameter[] EnumerateCalibrationFields(Calibration calibration)
        {
            System.Reflection.FieldInfo[] calibrationFields = typeof(Calibration).GetFields();
            List<Calibration.Parameter> calibrationValues = new List<Calibration.Parameter>();
            for (int i = 0; i < calibrationFields.Length; i++)
            {
                if (calibrationFields[i].FieldType == typeof(Calibration.Parameter))
                {
                    Calibration.Parameter val = (Calibration.Parameter)calibrationFields[i].GetValue(calibration);
                    calibrationValues.Add(val);
                }
            }
            return calibrationValues.ToArray();
        }

        /// <summary>
        /// Loads calibrations
        /// </summary>
        /// <returns>
        /// bitwise field:
        /// no bits: all failed
        /// bit 0: found serial flash
        /// bit 1: found local storage
        /// bit 2: loaded successfully
        /// </returns>
        public static int LoadCalibrations()
        {
            string calibrationStr;
            string[] calibrationStrs = new string[0];
            int success = 0;
            loadedCalibrations = new List<Calibration>();
            byte[] buf = new byte[8000];

            try
            {
                // Load the json string from the API.
                // Will be populated with a json str in the format {{calibration0}, {calibration1}, ...}
                success = load_calibration(buf);
                if (success == 0) Debug.Log(Misc.debugText + "No calibration found");
                calibrationStr = System.Text.Encoding.ASCII.GetString(buf);
            }
            catch
            {
                calibrationStr = "";
                Debug.LogWarning(Misc.debugText + "No calibration loaded; DLL failed");
            }

            if (calibrationStr != "")
            {
                try
                {
                    calibrationStrs = SplitJsonStrings(calibrationStr);
                }
                catch
                {
                    Debug.LogWarning(Misc.debugText + "Couldn't parse json strings from calibration loader");
                }
            }
            else 
            {
                Debug.LogWarning(Misc.debugText + "Calibration returned an empty string");
            }

            if ((success & (1<<0 | 1<<1)) != 0 && calibrationStrs.Length != 0)
            {
                foreach (var c in calibrationStrs)
                {
                    loadedCalibrations.Add(JsonUtility.FromJson<Calibration>(c));
                }
                // removed line to reduce console clutter
                // todo: make a setting for verbose output that does this
                // Debug.Log(Misc.debugText + string.Format("Calibration loaded! (Found {0})", calibrationStrs.Length));

                // set the successfully loading calibration flag
                success |= 1<<2;
            }
            else
            {
                loadedCalibrations.Add(new Calibration()); // don't let the calibration array be empty
                Debug.LogWarning(Misc.debugText + "Failed to load calibration file, initializing defaults");
            }

            loadedCalibrations[0].loadedFrom = "";
            if ((success & 1<<0) > 0)
                loadedCalibrations[0].loadedFrom += "serial flash; ";
            else
                loadedCalibrations[0].loadedFrom += "NO SERIAL FLASH CONNECTED; ";

            if ((success & 1<<1) > 0)
                loadedCalibrations[0].loadedFrom += "local storage; ";

            // make sure test value is always 0 unless specified by calibrator
            // inverted viewcone is handled separately now, so just take the abs of it
            // todo: take out once we know nothing is using 0.42
            foreach (var c in loadedCalibrations)
            {
                c.viewCone.Value = Mathf.Abs(c.viewCone.Value);
            }

            return success;
        }

        public static bool LoadFromFilepath(out Calibration loadedCalibration, string path)
        {
            bool fileExists = false;
            string calibrationStr;
            loadedCalibration = new Calibration();
            if (!File.Exists(path))
            {
                Debug.LogWarning(Misc.debugText + "Calibration file not found!");
            }
            else
            {
                calibrationStr = File.ReadAllText(path);
                if (calibrationStr.IndexOf('{') < 0 || calibrationStr.IndexOf('}') < 0)
                {
                    // if the file exists but is unpopulated by any info, don't try to parse it
                    // this is a bug with jsonUtility that it doesn't know how to handle a fully empty text file >:(
                    Debug.LogWarning(Misc.debugText + "Calibration file not found!");
                }
                else
                {
                    // if it's made it this far, just load it
                    fileExists = true;
                    // removed line to reduce console clutter
                    // todo: make a setting for verbose output that does this
                    // Debug.Log(Misc.debugText + "Calibration loaded! loaded from " + path);
                    loadedCalibration = JsonUtility.FromJson<Calibration>(calibrationStr);
                    // loadedCalibration.loadedFrom = path;
                }
            }
            // make sure test value is always 0 unless specified by calibrator
            // inverted viewcone is handled separately now, so just take the abs of it
            loadedCalibration.viewCone.Value = Mathf.Abs(loadedCalibration.viewCone.Value);

            loadedCalibrations = new List<Calibration>() { loadedCalibration };

            return fileExists;
        }

        static string[] SplitJsonStrings(string calibrationStr)
        {
            List<string> calibrationStrs = new List<string>();
            int startingBracketIndex = calibrationStr.IndexOf('{');
            int bracketCount;
            while (startingBracketIndex != -1)
            {
                bracketCount = 0;
                calibrationStr = calibrationStr.Substring(startingBracketIndex);
                int endingBracketIndex = 0;
                foreach (var c in calibrationStr)
                {
                    if ((c) == '{')
                        bracketCount++;
                    if ((c) == '}')
                        bracketCount--;

                    if (bracketCount == 0)
                    {
                        calibrationStrs.Add(calibrationStr.Substring(0, endingBracketIndex + 1));
                        break;
                    }

                    endingBracketIndex++;
                }
                calibrationStr = calibrationStr.Substring(endingBracketIndex);
                startingBracketIndex = calibrationStr.IndexOf('{');
            }

            return calibrationStrs.ToArray();
        }
    }
}
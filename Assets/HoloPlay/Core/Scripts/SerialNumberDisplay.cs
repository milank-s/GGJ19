using UnityEngine;
using UnityEngine.UI;

namespace HoloPlay.Extras
{
	public class SerialNumberDisplay : MonoBehaviour 
	{
		public void ShowSerial()
		{
			Text text = GetComponent<Text>();
			text.text = Calibration.Main.serial;
		}
	}
}
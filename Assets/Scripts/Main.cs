using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Main : MonoBehaviour {
	public Transform sunSphere;
	public Gradient sky;
	public Light sun;
	static Main _main;
	public AudioClip[][] sounds;
	Vector3 sunPos;
	public static Main main{
		get{
			return _main;
		}
	}
	float maxPlayerX;
	public Sprite[] connectors;
	public Sprite[] pickups;
	public Sprite[] shells;

	// Use this for initialization
	void Awake () {
		sounds = new AudioClip[3][];
		sounds[0] = Resources.LoadAll<AudioClip>("Sounds/harp");
		sounds[1] = Resources.LoadAll<AudioClip>("Sounds/clarinet");
		sounds[2] = Resources.LoadAll<AudioClip>("Sounds/oboe");
		sunPos = sunSphere.transform.localPosition;
		if(_main == null){
			_main = this;
		}else{
			Destroy(_main);
			_main = this;
		}
		shells = Resources.LoadAll<Sprite>("Shells");
		pickups = Resources.LoadAll<Sprite>("Pickups");
		connectors = Resources.LoadAll<Sprite>("Connectors");
	}

	public Sprite GetConnectionSprite(){
		return connectors[Random.Range(0, connectors.Length)];
	}

	public Sprite GetPickupsSprite(){
		return pickups[Random.Range(0, pickups.Length)];
	}

	public Sprite GetShellSprite(){
		return shells[Random.Range(0, shells.Length)];
	}

	public void Update(){
		if(Input.GetKey(KeyCode.R)){
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}
		EndGame();
		sun.color = sky.Evaluate(PlayerBehaviour.angle/360f);
		// sun.transform.rotation = Quaternion.Slerp(sunRot, Quaternion.Euler(3, sun.transform.eulerAngles.y, sun.transform.eulerAngles.z), Time.time/50f);
		sunSphere.transform.localPosition = Vector3.Lerp(sunPos, new Vector3(-100, -1, sunPos.z), Time.time/100f);
	}

	//if player posijtion > 1000 end gameObject
	public void EndGame(){
		// Camera.main.backgroundColor = sky.Evaluate(maxPlayerX/1000f);
		if(PlayerBehaviour.pos.x > maxPlayerX + 250){
			// PlayerBehaviour.player.GetComponent<Crab>().DropShell();
			maxPlayerX   = PlayerBehaviour.pos.x;
			// PlayerBehaviour.player.transform.position = PlayerBehaviour.player.transform.position -= Vector3.right * 110;

		}
	}

}

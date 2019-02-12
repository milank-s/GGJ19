using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour {

	public static float angle;
	public float moveDistance = 0.5f;
	private bool xbuttonDown;
	private bool zbuttonDown;
	public Transform playerTransform;
	SpriteRenderer sprite;
	Crab crab;

	public static Vector3 pos{
		get{
				return _playerInstance.position;
		}
	}

	static Transform _playerInstance;

	public static Transform player{
		get{
			return _playerInstance;
		}
	}

	void Move(){
		if(Input.GetAxis("Horizontal") > 0){
			transform.RotateAround(Vector3.zero, Vector3.up, -moveDistance * Time.deltaTime);
			angle += moveDistance * Time.deltaTime;
			xbuttonDown = true;
			// transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
		}else if(Input.GetAxis("Horizontal") < 0){
			// transform.position -= Vector3.right * moveDistance * Time.deltaTime;
			transform.RotateAround(Vector3.zero, Vector3.up, moveDistance * Time.deltaTime);
			angle -= moveDistance * Time.deltaTime;
			xbuttonDown = true;
			// transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
		}
		if(Input.GetAxis("Vertical") > 0 && playerTransform.localPosition.z < 10){
				playerTransform.localPosition += Vector3.forward * moveDistance * Time.deltaTime * 2;
				zbuttonDown = true;
		}else if(Input.GetAxis("Vertical") < 0 && playerTransform.localPosition.z > -10){
				playerTransform.localPosition -= Vector3.forward * moveDistance * Time.deltaTime * 2;
				zbuttonDown = true;
		}
		if (Input.GetAxis("Horizontal") == 0){
			xbuttonDown = false;
		}
		if(Input.GetAxis("Vertical") == 0){
		 zbuttonDown = false;
	 }

	 if(Input.GetKeyDown(KeyCode.Space) && crab.shell != null){
		 crab.shell.DropShell();
		 crab.shell = null;
		 // sprite.enabled = true;
	 }

	}

	public void Awake(){
		if(_playerInstance == null) _playerInstance = transform.GetChild(0);
		crab = GetComponentInChildren<Crab>();
		// sprite = GetComponent<SpriteRenderer>();
		// sprite.enabled = false;
		{

		}
	}
	// Update is called once per frame
	void Update () {
		Move();
		if(angle >= 360){
			angle = 0;
		}
		// angle = Mathf.Atan2(transform.position.x, transform.position.z);
		// angle = Vector3.Angle(Vector3.zero - transform.position, Vector3.forward);
	}
}

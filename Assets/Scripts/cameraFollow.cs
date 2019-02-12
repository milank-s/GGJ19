using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraFollow : MonoBehaviour {

	public Vector3 offset;
	public Transform target;
	// Use this for initialization
	void Start(){
		offset = transform.position;
		transform.position = new Vector3(target.position.x + offset.x, transform.position.y, target.position.z + offset.z);
	}
	// Update is called once per frame
	void LateUpdate () {
		transform.position = Vector3.Lerp(transform.position, new Vector3(target.position.x + offset.x, transform.position.y, target.position.z + offset.z), Time.deltaTime);

	}
}

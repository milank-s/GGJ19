using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class followTarget : MonoBehaviour {

	public Vector3 offset;
	public Transform target;
	// Use this for initialization
	void Start(){
		transform.position = target.transform.position + offset;
	}

	void Update(){
		if(target != null){
		transform.position = target.transform.position + offset;
		transform.forward = target.transform.forward;
	 }else{
		 Destroy(gameObject);
	 }
	}
}

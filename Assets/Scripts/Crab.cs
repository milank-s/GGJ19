using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crab : MonoBehaviour {

	public Shell shell;
	// Use this for initialization
	void Start () {
		if(GetComponentInChildren<Shell>() != null){
			shell = GetComponentInChildren<Shell>();
		}
	}

	public void OnTriggerEnter(Collider col){
		if(shell == null && col.tag == "Shell"){
			PickupShell(col.GetComponent<Shell>());
		}
	}

	void PickupShell(Shell s){
		if(!s.taken){
			shell = s;
			s.transform.parent = transform;
			s.transform.localPosition = Vector3.up * (s.transform.localScale.y/2);
			s.taken = true;
			if(GetComponent<SpriteRenderer>() != null){
				GetComponent<SpriteRenderer>().enabled = false;
			}
		}
	}

	public void DropShell(){
		if(shell != null){
		shell.DropShell();
		shell = null;
	}
	}
}

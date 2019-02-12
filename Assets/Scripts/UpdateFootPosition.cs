using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateFootPosition : MonoBehaviour {

	public float legLength;
	public float lerpSpeed;
	public float updateInterval;

	public Sprite footprintSprite;

	bool movingFoot = false;
	float interval;
	float distance;

	Vector3 offset;
	Vector3 lastUpdatedPosition;
	Vector3 newPosition;
	// Use this for initialization
	void Start () {
		offset = transform.parent.localPosition.normalized;
		lastUpdatedPosition = transform.position;
		interval = updateInterval;
	}

	// Update is called once per frame
	void Update () {
		Step ();

		distance = Vector3.Distance ((Vector3)transform.parent.position, lastUpdatedPosition);
		GetComponent<LineRenderer> ().SetPosition (2, transform.parent.position);
		GetComponent<LineRenderer> ().SetPosition (1, Vector3.Lerp(transform.position, transform.parent.position, 0.5f) + Vector3.up/2f);
		GetComponent<LineRenderer> ().SetPosition (0, new Vector3(transform.position.x, 0, transform.position.z));
	}

	void FindNewPosition(){
		movingFoot = true;
		Vector3 calculatedOffset = ((transform.parent.position - transform.position).normalized * legLength) + offset;
		newPosition = transform.parent.position + calculatedOffset - Vector3.up * 1.5f;
//		GameObject footprint = new GameObject ();
//		footprint.transform.position = transform.position;
//		footprint.AddComponent<SpriteRenderer> ().sprite = footprintSprite;
//		footprint.AddComponent<FadeSprite> ();
	}

	void Step(){
		if (movingFoot) {
			transform.position = newPosition;
			interval += Time.deltaTime * lerpSpeed;

			if (Vector3.Distance (transform.position, newPosition) <= 0.05f) {
				lastUpdatedPosition = newPosition;
				movingFoot = false;
				interval = updateInterval * Random.Range(0.50f, 1.50f);
			}

		} else {
			transform.position = lastUpdatedPosition;
			interval -= Time.deltaTime;

			if (Vector3.Distance(transform.position, transform.parent.position) > legLength + offset.magnitude	) {
				interval = 0;
				FindNewPosition ();
			}
		}
	}
}

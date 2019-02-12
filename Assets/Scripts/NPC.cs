using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour {

	public float moveDistance = 0.5f;
	public float xOffset;
	public float yOffset;
	public float scrollSpeed = 5;
	bool movingAway;
	public float lifeTime;
	float timer;
	public bool hasShell = true;
	void Start(){
		// moveDistance = moveDistance + Random.Range(25f, 50f);
		// transform.localScale *= (Mathf.Pow(Random.Range(0f, 1f), 5) * 10) + 1;
	 	yOffset = Random.Range(-100f, 100f);
		xOffset  = Random.Range(-100f, 100f);
	}
	void Move(){
			float perlin = Mathf.PerlinNoise(scrollSpeed * Time.time + -xOffset, Time.time + yOffset);
		  // transform.position = Vector3.MoveTowards(transform.position, PlayerBehaviour.pos + (Vector3.forward * Mathf.Sin(Time.time + xOffset) * 20) + (Vector3.right * ((perlin * 2) - 0.5f) * 100 * lifeTime * 2), Time.deltaTime * moveDistance);
			transform.RotateAround(Vector3.zero, Vector3.up, Time.deltaTime * moveDistance * ((-perlin * 2) + 0.25f));
			transform.position -= transform.forward * Mathf.Sin(Time.time * 3) * Time.deltaTime;
			// transform.position += Vector3.right * moveDistance * Time.deltaTime * Mathf.PerlinNoise(scrollSpeed * Time.deltaTime + -xOffset, Time.deltaTime + yOffset);
			//
			// if(transform.position.z < 10 && movingAway){
			// 	transform.position += Vector3.forward * moveDistance * Time.deltaTime * Mathf.PerlinNoise(scrollSpeed * Time.deltaTime + xOffset, Time.deltaTime + -yOffset);
			// }
			//
			// if(transform.position.z > -10 && !movingAway){
			// 	transform.position += Vector3.forward * moveDistance * Time.deltaTime * -Mathf.PerlinNoise(scrollSpeed * Time.deltaTime + -xOffset, Time.deltaTime + yOffset);
			// }
			//
			// if(timer < 0){
			// 	movingAway = !movingAway;
			// 	timer = Random.Range(0.25f, 3f);
			// }
			// timer -= Time.deltaTime;
	}
	// Update is called once per frame
	void Update () {
		lifeTime += Time.deltaTime;
		Move();
	}

	public void Die(){
		if(GetComponent<Crab>().shell != null){
		GetComponent<Crab>().DropShell();
		}
		Destroy(gameObject);
	}
}

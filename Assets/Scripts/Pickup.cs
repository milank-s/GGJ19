using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : MonoBehaviour{
	public float weight;
	public Sprite image;
	private Texture texture;
	public float speed;
	public Vector3 size;
	public void Start(){
		speed = Random.Range(5f, 10f);
		// image = GetComponent<SpriteRenderer>()
		image = Main.main.GetPickupsSprite();
		transform.localScale = image.bounds.size.normalized;
		transform.localScale *= (Mathf.Pow(Random.Range(0f, 1f), 3) * 10) + 4;
		transform.position += (transform.localScale.y/2) * Vector3.up;
		// if(image.bounds.extents.magnitude > 6){
		// 	transform.localScale /= image.bounds.extents.x/(Random.Range(0.5f, 3f));
		// }

		size = transform.localScale * Random.Range(1f, 2f);
		GetComponent<MeshRenderer>().material.mainTexture = image.texture;
		weight = size.magnitude;

		// GetComponent<MeshRenderer>().material.SetFloat("_ScaleY", transform.lossyScale.y);
    // GetComponent<MeshRenderer>().material.SetFloat("_ScaleX", transform.lossyScale.x);

		transform.localScale = Vector3.forward;
		StartCoroutine(Grow());
	}

	public IEnumerator Grow(){
		while(transform.localScale.magnitude < size.magnitude){
			transform.localScale += Vector3.up * Time.deltaTime * speed;
			transform.localScale += Vector3.right * Time.deltaTime * speed;
			if(transform.parent == null){
				transform.position = new Vector3(transform.position.x, transform.localScale.y/2, transform.position.z);
			}
			yield return null;
		}

	}

	public void Update(){
		if(transform.position.x - PlayerBehaviour.pos.x < -100 && GetComponent<Renderer>().isVisible == false){
			Destroy(gameObject);
		}
	}

}

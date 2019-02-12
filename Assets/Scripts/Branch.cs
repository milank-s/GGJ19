using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Branch : MonoBehaviour{

	private float scale;
	public float growTimer;
	public Branch root;
	public bool grown;
	public float speed;
	public float strength;
	public int priority;
	public bool isRoot;
	public bool broken;
	public float weight;
	public int branchDepth;
	public Transform parent;
	[HideInInspector]
	public Sprite image;
	public static int branchCount;
	public Rigidbody pickup;

	public void Initialize(){
		speed = Random.Range(10f, 15f);
		image = Main.main.GetConnectionSprite();
		transform.localScale = image.bounds.size;
		// transform.localPosition += Vector3.up * transform.localScale.y/2;
		scale = transform.localScale.y;
		scale = Mathf.Clamp(Random.Range(0.25f, 0.5f) * scale, 1, 100);
		// scale = 1 / image.bounds.extents.y;
		transform.localScale = Vector3.right * transform.localScale.x;
		GetComponent<MeshRenderer>().material.mainTexture = image.texture;

		if(!isRoot){
			// if(Vector3.Dot(transform.position - parent.transform.position, Vector3.up) > 0.75f){
			// 	transform.up = Random.Range(0, 2) == 1 ? -Vector3.right : Vector3.right;
			// 	scale /= 2;
			// }else{
			// 	transform.up = Vector3.up;
			// }
			transform.up = transform.position - parent.transform.position;
			transform.Rotate(0, 0, Random.Range(-30, 30));
			Branch b = parent.GetComponent<Branch>();
			root = b.root;
			branchDepth = b.branchDepth + 1;
			weight = b.weight;
			// transform.parent = root.parent;
		}else{
			transform.up = Vector3.Lerp(transform.position - parent.transform.position, Vector3.up, 0.85f);
			root = this;
		}
		branchCount ++;

		StartCoroutine(Grow());
	}

	public void Update(){
		if(transform.position.x - PlayerBehaviour.player.position.x < -100 && GetComponent<Renderer>().isVisible == false){
			Destroy(gameObject);
		}
	}
	public IEnumerator Grow(){
		while(transform.localScale.y < scale){
			transform.localScale += Vector3.up * Time.deltaTime * speed;
			transform.localScale += Vector3.forward * Time.deltaTime * speed;
			if(!isRoot){
				transform.position = parent.position + (parent.up * parent.localScale.y/3) + (transform.up * transform.localScale.y/2);
			}else{
				// transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
			}
			yield return null;
		}

		HingeJoint j = gameObject.AddComponent<HingeJoint>();
		j.connectedBody = parent.GetComponent<Rigidbody>();
		j.axis = transform.forward;
		JointLimits limits = j.limits;
		limits.min = -2;
		limits.max = 2;
		j.limits = limits;
		j.useLimits = true;
		j.useSpring = true;
		JointSpring spring = j.spring;
		spring.damper = 0.1f;
		spring.spring = 1f;
		j.spring = spring;
		grown = true;
	}
}

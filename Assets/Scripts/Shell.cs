using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shell : MonoBehaviour {

	public List<Branch> branches;
	public List<Branch> roots;
	private GameObject branchPrefab;
	private Branch lowestBranch;
	static int shellnumber;
	int soundbank;
	public bool taken;
	public float branchTimer = 1f;
	public float growthrate = 0.5f;
	float size;
	List<Pickup> soundsToPlay;
	Sprite shellSprite;
	void Start () {
		soundbank = shellnumber % 3;
		shellnumber++;
		soundsToPlay = new List<Pickup>();
		shellSprite = Main.main.GetShellSprite();
		// transform.localScale /= shellSprite.bounds.extents.y;
		transform.localScale = shellSprite.bounds.extents.normalized;
		transform.localScale *= (Mathf.Pow(Random.Range(0f, 1f), 3) * 25) + 10;
		transform.localScale += Vector3.forward * 3;
		if(transform.parent != null){
			transform.localPosition = Vector3.up * transform.localScale.y/2;
		}
		GetComponent<MeshRenderer>().material.mainTexture = shellSprite.texture;
		 // Mathf.Clamp(Time.time, 0, 10);
		branchPrefab = Resources.Load<GameObject>("Branch");
		branches = new List<Branch>();
		size = transform.localScale.x;
		for(int i = 0; i <= (int)(size); i+=2){

			SpawnBranch(transform, Vector3.right * (i - size/2) + (Vector3.up * transform.localScale.y * Random.Range(0.2f, 0.5f)), true);
		}
		if(transform.parent != null){
			taken = true;
		}
	}

	void SpawnBranchOnCrab(){
		Vector3 newPos = (Random.insideUnitCircle).normalized * size;
		newPos.y = Mathf.Clamp(Mathf.Abs(newPos.y), 0.5f, 100);
		SpawnBranch(transform, newPos);
	}

	public Branch SpawnBranch(Transform t, Vector3 pos, bool isRoot = false){
		Branch newBranch = Branch.Instantiate(branchPrefab, t.transform.position + pos, Quaternion.identity).GetComponent<Branch>();
		branches.Add(newBranch);
		newBranch.parent = t;
		
		if(isRoot){
			newBranch.isRoot = true;
			roots.Add(newBranch);
		}
		newBranch.Initialize();
		return newBranch;
	}

	 private Branch FindLowestBranch(){

		float priority = Mathf.Infinity;
		Branch branchToUse = null;

		foreach(Branch b in branches){
			if(b.priority < priority && b.grown){
				priority = b.priority;
				branchToUse = b;
				}
			}

		if(branchToUse == null || priority == Mathf.Infinity){
				return null;
		}else{
			lowestBranch = branchToUse;
			return lowestBranch;
		}
	}

	void AddNewBranches(){

		if(branchTimer < 0){
			if(branches.Count > 0){
				Branch branchToUse = branches[Random.Range(0, branches.Count)];
				if(!branchToUse.root.broken && branchToUse != null){
					SpawnBranch(branchToUse.transform, branchToUse.transform.localScale.y/2 * branchToUse.transform.up);
					// branches.Remove(branchToUse);
					branchTimer = 1;
			}else{
				branches.Remove(branchToUse);
				branchTimer = 1;
			}
			}else{
				branchTimer = 1;
			}
		}
			branchTimer -= Time.deltaTime * growthrate;
	}

	public void OnTriggerStay(Collider col){
		if(col.tag == "Pickup"){
			AttachPickup(col.GetComponent<Pickup>());
		}
	}

	void AttachPickup(Pickup p){

		if(FindLowestBranch() == null){
			return;
		}
		//fake parenting to end of branch
		followTarget f = p.gameObject.AddComponent<followTarget>();
		f.target = lowestBranch.transform;
		f.offset = lowestBranch.transform.localScale.y/2 * lowestBranch.transform.up + ((p.size.y/2) * Vector3.up);

		// if(p.transform.localScale.magnitude > 3){
		// 	p.transform.localScale /= 2;
		// }

		soundsToPlay.Add(p);
		p.GetComponent<Collider>().isTrigger = false;
		lowestBranch.pickup = p.GetComponent<Rigidbody>();
		lowestBranch.weight += p.weight;
		branches.Remove(lowestBranch);
		p.gameObject.tag = "Untagged";
		if(lowestBranch.branchDepth > 10f){
			// BreakBranch(lowestBranch, lowestBranch.root);
		}
		else{
			if(lowestBranch.branchDepth < 100f){
				SpawnBranch(lowestBranch.transform, lowestBranch.transform.localScale.y/2 * lowestBranch.transform.up);
			}
			FindLowestBranch();
		}
		//p.image.sortingOrde	r = -(int)p.image.sprite.bounds.extents.magnitude * 10;
		// remove collider.
		//spawn a new branch on the top of the pickup
	}
	void Update(){
		PlaySounds();
		// AddNewBranches();
	}

	void PlaySounds(){
		int time = (int)(AudioSettings.dspTime * 5);
			for(int i = 0; i < soundsToPlay.Count; i++){
				if(soundbank % 3 == 0 && time % 2 == 0){
						soundsToPlay[i].GetComponent<AudioSource>().PlayOneShot(Main.main.sounds[roots.IndexOf(lowestBranch.root) % 3][lowestBranch.branchDepth % Main.main.sounds[roots.IndexOf(lowestBranch.root) % 3].Length]);
						soundsToPlay.Remove(soundsToPlay[i]);
				}


				if(soundbank == 1 && time % 9 == 0){
						soundsToPlay[i].GetComponent<AudioSource>().PlayOneShot(Main.main.sounds[roots.IndexOf(lowestBranch.root) % 3][lowestBranch.branchDepth % Main.main.sounds[roots.IndexOf(lowestBranch.root) % 3].Length]);
						soundsToPlay.Remove(soundsToPlay[i]);
				}

				if(soundbank == 2 && time % 6 == 0){
						soundsToPlay[i].GetComponent<AudioSource>().PlayOneShot(Main.main.sounds[roots.IndexOf(lowestBranch.root) % 3][lowestBranch.branchDepth % Main.main.sounds[roots.IndexOf(lowestBranch.root) % 3].Length]);
						soundsToPlay.Remove(soundsToPlay[i]);
				}
			}
		}

	void BreakBranch(Branch top, Branch root){
		Branch b = top;
		//remove all branches from lists
		while(b != root){
			if(b.pickup != null){
				Destroy(b.pickup.GetComponent<followTarget>());
				b.GetComponent<HingeJoint>().connectedBody = b.pickup;
			}
			b.GetComponent<Rigidbody>().useGravity = true;
			Branch n = b;
			b = n.parent.GetComponent<Branch>();

		}

		root.broken = true;
		Destroy(root.GetComponent<HingeJoint>());
		roots.Remove(root);
		FindLowestBranch();
	}

	public void DropShell(){
		transform.parent = null;
		taken = false;
		transform.position = new Vector3(transform.position.x, 0, transform.position.z);
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPickups : MonoBehaviour {

	int pickupsSpawned;
	public int pickupAmount = 10;
	public float depth = 10;
	float playerXpos = 0;
	public float npcTimer = 5;
	Vector3 playerDist;
	public List<NPC> npcs;
	GameObject pickup;
	GameObject npc;
	GameObject shell;
	GameObject shellnpc;

	// Use this for initialization
	void Start () {
		playerXpos = 0;
		playerDist = PlayerBehaviour.pos;
		pickup = Resources.Load<GameObject>("QuadPickup");
		npc =  Resources.Load<GameObject>("NPC");
		shell = Resources.Load<GameObject>("Shell");
		shellnpc = Resources.Load<GameObject>("NPCshell");
		npcs = new List<NPC>();
	}

	// Update is called once per frame
	void Update () {
		if(PlayerBehaviour.angle > playerXpos){
			playerXpos = PlayerBehaviour.angle + Random.Range(0.1f, 0.75f);
			SpawnObject();
			}
		// }

		for(int i = 0; i < npcs.Count; i++){
			if(npcs[i].lifeTime > 30f || npcs[i].transform.position.x - PlayerBehaviour.pos.x > 75){
				NPC n = npcs[i];
				npcs.Remove(n);
				n.Die();
			}
		}

		if(npcTimer < 0 && npcs.Count < 2){
			if(Time.time < 15){
				SpawnNPC(true);
			}else{
				SpawnNPC(false);
			}
				npcTimer = 5;
		}
		npcTimer -= Time.deltaTime;
	}

	void SpawnNPC(bool hasShell){
		Vector3 pos = PlayerBehaviour.pos;
		Quaternion rot = PlayerBehaviour.player.rotation;

		// (Vector3.forward * Random.Range(-depth, depth)) - (Vector3.right * 100) + (Vector3.right *  PlayerBehaviour.pos.x + Vector3.up * Random.Range(1f, 3f));
		GameObject newnpc;
		// if(hasShell){
		// 	newnpc = Instantiate(shellnpc, pos, Quaternion.identity);
		// }else{
			newnpc = Instantiate(npc, pos, rot);
			newnpc.transform.RotateAround(Vector3.zero, Vector3.up, 10);
			newnpc.transform.position += newnpc.transform.forward * Random.Range(-depth, depth);
			// if(hasShell){
			// Vector3 shellpos = transform.position + (Vector3.forward * Random.Range(-depth, depth)) + ((Vector3.right * 50) + PlayerBehaviour.pos.x * Vector3.right);
			GameObject newShell = Instantiate(shell, pos, rot);
			newShell.transform.RotateAround(Vector3.zero, Vector3.up, Random.Range(-11f, -8f));
			newShell.transform.localScale *= (Mathf.Pow(Random.Range(0f, 1f), 3) * 5) + 1;
			// }
		npcs.Add(newnpc.GetComponent<NPC>());

	}

	void SpawnObject (){
		// Vector3 pos = transform.position + (Vector3.forward * Random.Range(-depth, depth)) + (Vector3.right * (Random.Range(50, 200) + PlayerBehaviour.pos.x));

		GameObject newPickup = Instantiate(pickup, PlayerBehaviour.pos, PlayerBehaviour.player.rotation);
		newPickup.transform.RotateAround(Vector3.zero, Vector3.up, Random.Range(-25f, -10f));
		// if(Random.Range(0, 100) < 2){
		// 	GameObject newShell = Instantiate(shell, pos, Quaternion.identity);
		// 	newShell.transform.localScale *= (Mathf.Pow(Random.Range(0f, 1f), 3) * 5) + 1;
		// }
	}
}

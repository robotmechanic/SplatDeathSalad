using UnityEngine;
using System.Collections;

public class BasketballScript : MonoBehaviour {
	
	//set this when the ball is picked up
	public NetworkViewID throwerID;
	
	public bool held = false;
	public float watchdog = 0f;
	public bool watchdogOn = false;
	
	public Vector3 moveVector = Vector3.zero;
	
	private Vector3 lastPos;
	
	public AudioClip sfx_bounce;
	
	private SophieNetworkScript theNetwork;
	
	private float throwTime = 0f;
	
	private float lastSync = 0f;
	
	// Use this for initialization
	void Start () {
		theNetwork = GameObject.Find("_SophieNet").GetComponent<SophieNetworkScript>();
		gameObject.layer = 13;
		ResetBall();
	}
	
	public void ResetBall(){
		for (int i=0; i<theNetwork.players.Count; i++){
			theNetwork.players[i].hasBall = false;
		}
		transform.parent = null;
		transform.position = GameObject.Find("_BasketballStart").transform.position;
		lastPos = transform.position;
		moveVector = Vector3.zero;
		watchdogOn = false;
	}
	
	public void SyncBall(Vector3 pos, Vector3 dir){
		if (!held){
			lastPos = pos;
			transform.position = pos;
			moveVector = dir;
		}
	}
	
	public void Throw(Vector3 fromPos, Vector3 direction, float strength){
		throwTime = Time.time;
	
		transform.parent = null;
		transform.position = fromPos;
		lastPos = transform.position;
		moveVector = direction * strength;
		held = false;
		
		for (int i=0; i<theNetwork.players.Count; i++){
			
				if (theNetwork.players[i].hasBall)
				{
					theNetwork.players[i].fpsEntity.PlaySound("throwBall");
				}
				
				theNetwork.players[i].hasBall = false;
		}
	}
	
	public void HoldBall(NetworkViewID viewID){
		throwerID = viewID;
		moveVector = Vector3.zero;
		held = true;
		
		for (int i=0; i<theNetwork.players.Count; i++){
			if (theNetwork.players[i].viewID == throwerID){
				theNetwork.players[i].hasBall = true;
				
				transform.parent = theNetwork.players[i].fpsEntity.gunMesh1.transform.parent;
				transform.localPosition = (-Vector3.right * 0.7f) + (Vector3.forward * 0.2f);
				
				theNetwork.players[i].fpsEntity.PlaySound("catchBall");
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (held){
			gameObject.layer = 13;	//no hit
			watchdog = 0f;
			watchdogOn = true;
		}
		else
		{
			gameObject.layer = 13;	//ball
			if (watchdogOn)
			{
				watchdog += Time.deltaTime;
			
				if (watchdog > 15f)
				{
					watchdog = 0f;
					ResetBall();
					if (theNetwork.isServer){
						theNetwork.networkView.RPC("SendChatMessage",RPCMode.All, "> ", "Ball reset.", theNetwork.ColToVec(new Color(1f,0.5f,0f,1f)));
					}
				}
			}
			
			transform.position += moveVector * Time.deltaTime;
				
			moveVector.y -= Time.deltaTime * 18f;
				
			RaycastHit hitInfo = new RaycastHit();
			int layerMask = (1<<0)|(1<<10)|(1<<11)|(1<<12);
			Vector3 rayDirection = (transform.position - lastPos).normalized;
			if (Physics.SphereCast(lastPos, 0.5f, rayDirection, out hitInfo, Vector3.Distance(transform.position, lastPos), layerMask)){
				
				if (hitInfo.collider.gameObject.layer == 11){
					//blue scores
					ResetBall();
					
					if (!SophieNetworkScript.gameOver){
						
						for (int i=0; i<theNetwork.players.Count; i++){
							theNetwork.players[i].fpsEntity.Announce("scoreblue");
						}
						
						if (theNetwork.isServer){
							theNetwork.team2Score++;
							theNetwork.networkView.RPC("AnnounceTeamScores", RPCMode.Others, theNetwork.team1Score, theNetwork.team2Score);
							theNetwork.networkView.RPC("SendChatMessage",RPCMode.All, "> ", "Blue scores.", theNetwork.ColToVec(new Color(1f,0.5f,0f,1f)));
						}
					}
				}else if (hitInfo.collider.gameObject.layer == 12){
					//red scores
					ResetBall();
					
					if (!SophieNetworkScript.gameOver){
						
						for (int i=0; i<theNetwork.players.Count; i++){
							theNetwork.players[i].fpsEntity.Announce("scorered");
						}
						
						if (theNetwork.isServer){
							theNetwork.team1Score++;
							theNetwork.networkView.RPC("AnnounceTeamScores", RPCMode.Others, theNetwork.team1Score, theNetwork.team2Score);
							theNetwork.networkView.RPC("SendChatMessage",RPCMode.All, "> ", "Red scores.", theNetwork.ColToVec(new Color(1f,0.5f,0f,1f)));
						}
					}
				}else if (hitInfo.collider.gameObject.layer == 10){
					//LAVA! :D
					ResetBall();
					
					for (int i=0; i<theNetwork.players.Count; i++){
						theNetwork.players[i].fpsEntity.Announce("balllost");
					}
					
					if (theNetwork.isServer){
						theNetwork.networkView.RPC("SendChatMessage",RPCMode.All, "> ", "Ball lost.", theNetwork.ColToVec(new Color(1f,0.5f,0f,1f)));
					}
				}else{
				
					transform.position = hitInfo.point + (hitInfo.normal*0.5f);
					moveVector = Vector3.Reflect(moveVector, hitInfo.normal);
					moveVector *= 0.7f;
						
					//Debug.Log(moveVector.magnitude);
					if (moveVector.magnitude>2f){
						audio.clip = sfx_bounce;
						audio.pitch = Random.Range(0.9f,1.1f);
						audio.Play();
					}
				}
			}
			lastPos = transform.position;
			
			if (theNetwork.isServer){
				//let's check to see if any of the players can pick up the ball
				bool captured = false;
				for (int i=0; i<theNetwork.players.Count; i++){
					if (!captured && theNetwork.players[i].health>0f && Vector3.Distance(transform.position, theNetwork.players[i].fpsEntity.transform.position)<1.5f){
						if (throwerID == null || theNetwork.players[i].viewID != throwerID || Time.time > throwTime+0.5f){
							theNetwork.AnnounceBallCapture(theNetwork.players[i].viewID);
							captured = true;
						}
					}
				}
				
				//Sync:
				
				lastSync -= Time.deltaTime;
				if (lastSync < 0)
				{
					lastSync = 5f;
					theNetwork.SyncBall(transform.position, moveVector);
				}
				
			}
		}
		
	}
}

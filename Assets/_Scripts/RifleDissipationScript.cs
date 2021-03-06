using UnityEngine;
using System.Collections;

public class RifleDissipationScript : MonoBehaviour {

	private Vector3 moveVec = Vector3.zero;
	
	private float life = 1.5f;
	public bool gravity = false;
	
	private Vector3 grav = new Vector3();
	// Use this for initialization
	void Start () {
		moveVec = new Vector3(Random.Range(-1f,1f),Random.Range(-1f,1f),Random.Range(-1f,1f));
		transform.localScale = Vector3.one * 0.3f * Random.Range(0.05f,0.08f);
		life -= Random.Range(0f,0.5f);
		life *= 2f;
	}
	
	// Update is called once per frame
	void Update () {
		transform.position += moveVec * Time.deltaTime * 0.2f;
		
		if (gravity)
		{
			grav.y -= Random.Range(0f,Time.deltaTime * 0.05f);
			transform.position += grav;
		}
		
		//moveVec.y += 8f * Time.deltaTime;
		//moveVec.x -= (0-moveVec.x) * Time.deltaTime * -3f;
		//moveVec.z -= (0-moveVec.z) * Time.deltaTime * -3f;
		
		life -= Time.deltaTime;
		if (life<=0f){
			Destroy(gameObject);
		}
	}
}

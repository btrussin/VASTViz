using UnityEngine;
using System.Collections;

public class TestSphereMovement : MonoBehaviour {

    public Vector3 move;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        transform.position += move;
	}
}

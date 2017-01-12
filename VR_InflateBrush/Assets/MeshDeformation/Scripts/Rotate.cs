using UnityEngine;
using System.Collections;

public class Rotate : MonoBehaviour {

    public float X, Y, Z;
    

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        transform.Rotate(X * Time.deltaTime, Y * Time.deltaTime, Z * Time.deltaTime);
	}
}

using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ShowFPS : MonoBehaviour {

    Text uiText;

	// Use this for initialization
	void Start () {
        uiText = GetComponent<Text>();
	}
	
	// Update is called once per frame
	void Update () {
        uiText.text = (1.0f / Time.smoothDeltaTime).ToString("0.");
	}
}

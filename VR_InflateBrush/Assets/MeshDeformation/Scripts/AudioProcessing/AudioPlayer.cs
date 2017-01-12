using UnityEngine;
using System.Collections;

public class AudioPlayer : MonoBehaviour {
    private static AudioPlayer instance;
    public static AudioPlayer Instace {
        get { return instance; }
    }

    private AudioSource audioSource;
    public AudioSource AudioSource {
        get { return audioSource; }
    }

    void Awake() {
        instance = this;
    }

	// Use this for initialization
	void Start () {
        audioSource = GetComponent<AudioSource>();

        audioSource.time = 30;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

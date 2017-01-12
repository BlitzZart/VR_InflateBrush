using UnityEngine;
using System.Collections;
using System;

public class AudioToMeshCommunicator : MonoBehaviour, AudioProcessor.AudioCallbacks {

    MeshDeformer deformer;

    public void onOnbeatDetected() {
        deformer.ApplyScaleImpulse();
    }

    public void onSpectrum(float[] spectrum) {
        deformer.ArrayOffests(spectrum);
    }

    // Use this for initialization
    void Start () {
        deformer = GetComponent<MeshDeformer>();

        // register beat detection
        AudioProcessor processor = FindObjectOfType<AudioProcessor>();
        processor.addAudioCallback(this);
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}

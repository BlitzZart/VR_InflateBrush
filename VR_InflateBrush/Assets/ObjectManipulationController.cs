using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManipulationController : MonoBehaviour {
    private SteamVR_TrackedController vrController;
    private MeshDeformer deformer;
    private Vector3 startPositionObject, startPositionController;

	// Use this for initialization
	void Start () {
        vrController = GetComponent<SteamVR_TrackedController>();
        vrController.TriggerClicked += OnTriggerClicked;
        vrController.TriggerUnclicked += OnTriggerUnclicked;
        vrController.PadClicked += OnPadClicked;
        vrController.Gripped += OnGripped;

        deformer = FindObjectOfType<MeshDeformer>();
	}

    void Update() {
        if (vrController.gripped) {
            deformer.transform.position = startPositionObject + (startPositionController - transform.position);
        }
    }

    void OnDestroy() {
        vrController.TriggerClicked -= OnTriggerClicked;
        vrController.TriggerUnclicked -= OnTriggerUnclicked;
        vrController.PadClicked -= OnPadClicked;
        vrController.Gripped -= OnGripped;
    }

    private void OnGripped(object sender, ClickedEventArgs e) {
        startPositionObject = deformer.transform.position;
        startPositionController = transform.position;
    }
    
    private void OnPadClicked(object sender, ClickedEventArgs e) {
        deformer.direction = -deformer.direction;
    }

    private void OnTriggerClicked(object sender, ClickedEventArgs e) {
        deformer.reactOnPlayerDistance = true;
    }

    private void OnTriggerUnclicked(object sender, ClickedEventArgs e) {
        deformer.reactOnPlayerDistance = false;
    }
}

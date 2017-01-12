using UnityEngine;
using System.Collections;

/// <summary>
/// this class uses the deformation values of a master deformer
/// decreases cost by taking already calulated values
/// 
/// only use if master has the same mesh (at least the same vertex count)
/// </summary>
public class MeshDeformerSlave : MonoBehaviour {

    // must have an identical mesh
    public MeshDeformer master;
    private Mesh mesh;

    private bool doUpdate = false;

    // Use this for initialization
    void Start () {
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.MarkDynamic();


        StartCoroutine(InitializeDelayed());
	}

    // delay by one frame - so master is ready
    IEnumerator InitializeDelayed() {
        yield return 0;

        if (master != null && master.Vertices.Length == mesh.vertexCount)
            doUpdate = true; // only works if vertexcount is equal
    }
	
	// Update is called once per frame
	void Update () {
        if (!doUpdate)
            return;

        //print(mesh.vertexCount + " " + master.Vertices);

        mesh.vertices = master.Vertices;
        mesh.RecalculateBounds();
    }
}

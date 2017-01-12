using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshDeformer : MonoBehaviour {
    static Vector3 unInitVec = new Vector3(-9999.1f, -9999.2f, -9999.3f); // TODO: make clean initialisation - this init value is just a hack
    class Vertex {
        public Vector3 tVertex;
        public Vector3 oVertex;
        public Vector3 normal;
        public List<int> indices;

        public Vertex() {
            indices = new List<int>();
            tVertex = new Vector3(unInitVec.x, unInitVec.y, unInitVec.z);
        }

        public Vertex(Vector3 vertex, Vector3 normal, int index) {
            this.tVertex = vertex;
            this.oVertex = vertex;
            this.normal = normal;

            indices = new List<int>();
            indices.Add(index);
        }

        public void AddIndex(int index, Vector3 normal) {
            indices.Add(index);
            this.normal = (this.normal + normal) / 2;
        }

        public override string ToString() {
            string text = "@ Vertex: ";
            text += tVertex + "\n";
            text += " Indices: ";
            foreach (int item in indices) {
                text += item + " ";
            }
            return text;
        }
    }

    class VertexList {
        public List<Vertex> vertices;
        public VertexList() {
            vertices = new List<Vertex>();
        }

        public void Add(Vertex vertex) {
            vertices.Add(vertex);
        }

        public void TryAddIndex(Vector3 vertex, Vector3 normal, int index) {
            foreach (Vertex item in vertices)
                if (item.tVertex == vertex)
                    item.AddIndex(index, normal);

        }

        public bool Contains(Vector3 vertex) {
            foreach (Vertex item in vertices)
                if (item.tVertex == vertex)
                    return true;
            return false;
        }

        public override string ToString() {
            string text = "Length " + vertices.Count + "  \n";
            foreach (Vertex item in vertices) {
                text += item.ToString();
            }
            return text;
        }
    }

    public bool solidDeformation = true;
    public bool relaxMesh = false;
    public bool reactOnPlayerDistance = false;
    public bool addFFTAndRandomToInflation = false;
    [Range(0.0f, 10)]
    public float reactionDistance = 5;

    private Vector3[] vertices;
    public Vector3[] Vertices {
        get { return vertices; }
    }

    bool positiveDeformation = true;

    Mesh mesh;
    Vector3[] targetVertices;
    Vector3[] oV;

    Vertex[] unique;
    VertexList uList;

    Vector3 initialScale;
    float scaleDownSpeed = 10; // after scale up impulse (Beat)

    float[] offsets;
    float scaleImpusleValue = 0;

    [Header("Overall power of deformation:")]
    [Range(0.0f, 50)]
    public float power = 1.0f;
    [Header("0 = both, -1 = negative only, 1 = positive only")]
    [Header("Set deformation direction:")]
    [Range(-1, 1)]
    public int direction = 0;

    [Header("Influence of FFT:")]
    [Range(0.0f, 2)]
    public float fftPower = 1.0f;
    [Header("Noise floor:")]
    [Range(0.0f, 10.0f)]
    public float randomPower = 1.0f;
    [Header("Speed of linear interpolation:")]
    [Range(0.0f, 25.0f)]
    public float speed = 1.0f;
    [Header("Update rate of animation:")]
    [Range(0.0f, 1.0f)]
    public float rate = 0.1f; // for music visualization beat detection would be nice    

    private float defaultVibration = 0.02f;

    // used for player position independend deformation
    [Header("Steady inflation: set FFT power and random power 0.")]
    [Header("Reaction distance uses approximator")]
    public Transform approximator;
    private Vector3 approxPosition;
    private float approxDistance;

    public void ResetMesh() {
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.MarkDynamic();

        // prepare datastructure for solid mesh deformation
        List<Vector3> rV = new List<Vector3>();
        rV.Add(mesh.vertices[0]);
        for (int i = 1; i < mesh.vertices.Length; i++) {
            if (!rV.Contains(mesh.vertices[i])) {
                rV.Add(mesh.vertices[i]);
            }
        }
        uList = new VertexList();
        for (int i = 0; i < mesh.vertexCount; i++) {
            if (!uList.Contains(mesh.vertices[i])) {
                uList.Add(new Vertex(mesh.vertices[i], mesh.normals[i], i));
            }
            else {
                uList.TryAddIndex(mesh.vertices[i], mesh.normals[i], i);
            }
        }
        unique = uList.vertices.ToArray();

        oV = mesh.vertices;
        targetVertices = mesh.vertices;
        offsets = new float[] { 0 };

        //playerTransform = Camera.main.transform;
    }

    // Use this for initialization
    void Start() {
        ResetMesh();

        initialScale = transform.localScale;
    }

    public void ApplyScaleImpulse() {
        //scaleImpusleValue = 1.2f;
    }

    public void ArrayOffests(float[] offsets) {
        this.offsets = offsets;
    }

    private float current = 0;
    // Update is called once per frame
    void Update() {
        if (current < rate) {
            current += Time.deltaTime;
        }
        else {
            current = 0;

            if (solidDeformation)
                UpdateMeshSolid();
            else
                UpdateMeshBreaking();
        }

        ProcessObjectScale();

        vertices = mesh.vertices;
        for (int i = 0; i < oV.Length; i++) {
            //if (positiveDeformation & targetVertices[i].sqrMagnitude < 0)
            //    targetVertices[i] *= -1;
            vertices[i] = Vector3.Lerp(vertices[i], targetVertices[i], Time.deltaTime * speed);
        }
        mesh.vertices = vertices;
        //mesh.RecalculateBounds();
    }


    private void UpdateMeshBreaking() {
        UpdatePlayerDiscance();

        for (int i = 0; i < oV.Length; i++) {
            float deformFactor = GetRamdomValue() *
                power *
                GetApproximationDeformation(oV[i]) *
                GetFFTDeformationValue(i);

            targetVertices[i] = oV[i] + mesh.normals[i] * ScaleDeformation(deformFactor);
        }
    }

    private void UpdateMeshSolid() {
        UpdatePlayerDiscance();

        int i = 0;
        foreach (Vertex item in unique) {

            float deformFactor = 
                GetRamdomValue() *
                power *
                GetApproximationDeformation(item.oVertex) *
                GetFFTDeformationValue(i++);

            // apply deformation
            item.tVertex = item.oVertex + item.normal * ScaleDeformation(deformFactor);

            if (reactOnPlayerDistance) { // vertices react on player position
                if (approxDistance < reactionDistance) {
                    UpdateTargetVertices(item, false);
                }
                else { // reset mesh if player walks away
                    UpdateTargetVertices(item, true);
                }
            }
            else { // this happens when player position check is deactivated
                UpdateTargetVertices(item, relaxMesh);
            }
        }
    }

    private void ProcessObjectScale() {
        transform.localScale = initialScale * scaleImpusleValue;

        scaleImpusleValue = Mathf.Lerp(scaleImpusleValue, 1, Time.deltaTime * scaleDownSpeed);
    }

    // calculates deforamtion factor for each vertex based on fft values
    private float GetFFTDeformationValue(int i) {
        return (defaultVibration * Mathf.Clamp01(power) + offsets[i % offsets.Length] * fftPower);
    }

    private float ScaleDeformation(float value) {
        if (direction == 0)
            return value;
        else if (direction == -1)
            return -Mathf.Abs(value);
        else
            return Mathf.Abs(value);
    }

    private float GetRamdomValue() {
        float noiseInfluence = .333f;
        if (randomPower != 0) // randomPower 0 causes a static inflation 
            if (addFFTAndRandomToInflation) // apply random noise on inflated mesh
                noiseInfluence += UnityEngine.Random.Range(-randomPower, randomPower);
            else // apply random noise on uninflated mesh
                noiseInfluence = UnityEngine.Random.Range(-randomPower, randomPower);

        return noiseInfluence;
    }

    private float GetApproximationDeformation(Vector3 v) {
        if (reactOnPlayerDistance && approximator != null && reactionDistance > 0) {
            approxDistance = Vector3.Distance(approxPosition, v);

            if (approxDistance > reactionDistance)
                return 0;

            return (reactionDistance / approxDistance);
        }

        return 1;
    }

    private void UpdatePlayerDiscance() {
        if (!reactOnPlayerDistance) {
            approxDistance = 1; // prevent division by zero
        }

        // transfrom player position in mesh(local) space
        if (approximator != null)
            approxPosition = transform.InverseTransformPoint(approximator.transform.position);
    }

    // use this to deactivate the mesh deformer delayed
    // so the mesh can relax when the player is gone
    IEnumerator DeactivateDelayed() {
        yield return new WaitForSeconds(1);
        // set mesh deformer inactive
        this.enabled = false;
    }

    // apply new position to all target vertices
    private void UpdateTargetVertices(Vertex vertex, bool relax) {
        if (relax && reactOnPlayerDistance)
            foreach (int index in vertex.indices)
                targetVertices[index] = vertex.oVertex;

        else // reset mesh if player walks away
            foreach (int index in vertex.indices)
                targetVertices[index] = vertex.tVertex;
    }
}
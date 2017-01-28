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

    public GameObject vertexMarkerPrefab;
    private List<MeshRenderer> vertexMarkerPool;
    private int vertexMarkerAmount = 23;
    private int closeToApproximatorCnt = 0;

    public bool solidDeformation = true;
    public bool relaxMesh = false;
    public bool reactOnPlayerDistance = false;
    public bool applyDeformation = true;
    [Range(0.0f, 2)]
    public float reactionDistance = 2;

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
    [Range(0, 0.1f)]
    public float power = 1.0f;
    [Header("0 = both, -1 = negative only, 1 = positive only")]
    [Header("Set deformation direction:")]
    [Range(-1, 1)]
    public int direction = 0;

    [Header("Speed of linear interpolation:")]
    [Range(0.0f, 25.0f)]
    public float speed = 1.0f;

    // used for player position independend deformation
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
        InitVertexMarker();
        initialScale = transform.localScale;
    }
    void Update()
    {
        if (solidDeformation)
            UpdateMeshSolid();
        else
            UpdateMeshBreaking();

        ProcessObjectScale();

        vertices = mesh.vertices;
        for (int i = 0; i < oV.Length; i++)
        {
            //if (positiveDeformation & targetVertices[i].sqrMagnitude < 0)
            //    targetVertices[i] *= -1;
            vertices[i] = Vector3.Lerp(vertices[i], targetVertices[i], Time.deltaTime * speed);
        }
        mesh.vertices = vertices;

        mesh.RecalculateBounds(); // important for proper cullung
    }


    public void ApplyScaleImpulse() {
        //scaleImpusleValue = 1.2f;
    }

    public void ArrayOffests(float[] offsets) {
        this.offsets = offsets;
    }

    private float current = 0;
    // Update is called once per frame


    private void InitVertexMarker()
    {
        vertexMarkerPool = new List<MeshRenderer>();
        for (int i = 0; i < vertexMarkerAmount; i++)
        {
            GameObject go = Instantiate(vertexMarkerPrefab, transform);
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.enabled = false;
            vertexMarkerPool.Add(mr);
        }
    }

    private void UpdateMeshBreaking() {
        UpdatePlayerDiscance();
        foreach (MeshRenderer item in vertexMarkerPool)
            item.enabled = false;
        for (int i = 0; i < oV.Length; i++) {
            float deformFactor =
                power *
                GetApproximationDeformation(oV[i]);

            targetVertices[i] = oV[i] + mesh.normals[i] * ScaleDeformation(deformFactor);
        }
    }

    private void UpdateMeshSolid() {
        UpdatePlayerDiscance();

        Vector3 startVector;

        foreach (MeshRenderer item in vertexMarkerPool)
            item.enabled = false;

        closeToApproximatorCnt = 0;
        foreach (Vertex item in unique) {
            if (relaxMesh)
                startVector = item.oVertex;
            else
                startVector = item.tVertex;

            float deformFactor =
                power *
                GetApproximationDeformation(startVector);

            if (applyDeformation)
            {
                // apply deformation
                item.tVertex = startVector + item.normal * ScaleDeformation(deformFactor);

                if (reactOnPlayerDistance)
                { // vertices react on player position
                    if (approxDistance < reactionDistance)
                    {
                        UpdateTargetVertices(item, relaxMesh);
                    }
                    else
                    { // reset mesh if player walks away
                        UpdateTargetVertices(item, relaxMesh);
                    }
                }
                else
                { // this happens when player position check is deactivated
                    UpdateTargetVertices(item, relaxMesh);
                }
            }
        }
    }

    private void ProcessObjectScale() {
        transform.localScale = initialScale * scaleImpusleValue;

        scaleImpusleValue = Mathf.Lerp(scaleImpusleValue, 1, Time.deltaTime * scaleDownSpeed);
    }

    private float ScaleDeformation(float value) {
        if (direction == 0)
            return value;
        else if (direction == -1)
            return -Mathf.Abs(value);
        else
            return Mathf.Abs(value);
    }

    private float GetApproximationDeformation(Vector3 v)
    {
        if (reactOnPlayerDistance && approximator != null && reactionDistance > 0)
        {
            approxDistance = Vector3.Distance(approxPosition, v);

            if (approxDistance > reactionDistance)
                return 0;

            if (closeToApproximatorCnt < vertexMarkerPool.Count)
            {
                vertexMarkerPool[closeToApproximatorCnt].transform.localPosition = v;
                vertexMarkerPool[closeToApproximatorCnt].enabled = true;
                closeToApproximatorCnt++;
            }

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
        Vector3 startVector;
        if (relaxMesh)
            startVector = vertex.oVertex;
        else
            startVector = vertex.tVertex;

        if (relax && reactOnPlayerDistance)
            foreach (int index in vertex.indices)
                targetVertices[index] = startVector;

        else // reset mesh if player walks away
            foreach (int index in vertex.indices)
                targetVertices[index] = vertex.tVertex;
    }
}
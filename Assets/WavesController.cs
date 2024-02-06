using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WavesController : MonoBehaviour
{
    [SerializeField] private int dimension = 10;
    [SerializeField] private Octave[] octaves;
    [SerializeField] private float uvScale = 2f;

    private MeshFilter meshFilter;
    private Mesh mesh;

    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        mesh.name = gameObject.name;

        mesh.vertices = GenerateVerts();
        mesh.triangles = GenerateTries();
        mesh.uv = GenerateUVs();
        mesh.RecalculateBounds();

        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
    }


    private Vector3[] GenerateVerts()
    {
        var verts = new Vector3[(dimension + 1) * (dimension + 1)];

        for (int x = 0; x <= dimension; x++)
        {
            for (int z = 0;z <= dimension; z++)
            {
                verts[index(x, z)] = new Vector3(x, 0, z);
            }
        }

        return verts;
    }

    private Vector2[] GenerateUVs()
    {
        var uvs = new Vector2[mesh.vertices.Length];

        for (int x = 0; x <= dimension; x++)
        {
            for (int z = 0; z <= dimension; z++)
            {
                uvs[index(x, z)] = new Vector2((x / uvScale) % 2, (z / uvScale) % 2);
            }
        }

        return uvs;
    }

    private int[] GenerateTries()
    {
        var tries = new int[mesh.vertices.Length * 6];

        for (int x = 0; x<dimension; x++)
        {
            for (int z = 0; z < dimension; z++)
            {
                tries[index(x, z) * 6] = index(x, z);
                tries[index(x, z) * 6 + 1] = index(x + 1, z + 1);
                tries[index(x, z) * 6 + 2] = index(x + 1, z);
                tries[index(x, z) * 6 + 3] = index(x, z);
                tries[index(x, z) * 6 + 4] = index(x, z + 1);
                tries[index(x, z) * 6 + 5] = index(x + 1, z + 1);
            }
        }

        return tries;
    }

    private int index(int x, int y)
    {
        return x * (dimension+1) + y;
    }

    private int index(float x, float y)
    {
        return (int) (x * (dimension + 1) + y);
    }

    public float GetHeight(Vector3 position)
    {
        Vector3 scale = new Vector3(1 / transform.lossyScale.x, 0, 1 / transform.lossyScale.z);
        Vector3 localPos = Vector3.Scale((position - transform.position), scale);

        Vector3 p1 = new Vector3(Mathf.Clamp(Mathf.Floor(localPos.x), 0, dimension), 0, Mathf.Clamp(Mathf.Floor(localPos.z), 0 , dimension));
        Vector3 p2 = new Vector3(Mathf.Clamp(Mathf.Floor(localPos.x), 0, dimension), 0, Mathf.Clamp(Mathf.Ceil(localPos.z), 0 , dimension));
        Vector3 p3 = new Vector3(Mathf.Clamp(Mathf.Ceil(localPos.x), 0, dimension), 0, Mathf.Clamp(Mathf.Floor(localPos.z), 0 , dimension));
        Vector3 p4 = new Vector3(Mathf.Clamp(Mathf.Ceil(localPos.x), 0, dimension), 0, Mathf.Clamp(Mathf.Ceil(localPos.z), 0 , dimension));

        float max = Mathf.Max(Vector3.Distance(p1, localPos), Vector3.Distance(p2, localPos), Vector3.Distance(p3, localPos), Vector3.Distance(p4, localPos) + Mathf.Epsilon);
        float dist = (max - Vector3.Distance(p1, localPos))
                   + (max - Vector3.Distance(p2, localPos))
                   + (max - Vector3.Distance(p3, localPos))
                   + (max - Vector3.Distance(p4, localPos)) + Mathf.Epsilon;

        float height = mesh.vertices[index(p1.x, p1.z)].y * (max - Vector3.Distance(p1, localPos))
                     + mesh.vertices[index(p2.x, p2.z)].y * (max - Vector3.Distance(p2, localPos))
                     + mesh.vertices[index(p3.x, p3.z)].y * (max - Vector3.Distance(p3, localPos))
                     + mesh.vertices[index(p4.x, p4.z)].y * (max - Vector3.Distance(p4, localPos));

        return height * transform.lossyScale.y / dist;
    }

    // Update is called once per frame
    void Update()
    {
        var verts = mesh.vertices;
        for (int x = 0; x <= dimension; x++)
        {
            for (int z = 0; z <= dimension; z++)
            {
                var y = 0.0f;
                for (int o = 0; o<octaves.Length;o++)
                {
                    if (octaves[o].alternate)
                    {
                        var perl = Mathf.PerlinNoise((x * octaves[o].scale.x) / dimension, (z * octaves[o].scale.y) / dimension) * Mathf.PI * 2.0f;
                        y += Mathf.Cos(perl + octaves[o].speed.magnitude * Time.time) * octaves[0].height;
                    }
                    else
                    {
                        var perl = Mathf.PerlinNoise((x * octaves[o].scale.x * Time.time * octaves[o].speed.x) / dimension, (z * octaves[o].scale.y * Time.time * octaves[o].speed.y) / dimension) - 0.5f;
                        y += perl * octaves[o].height;
                    }
                }

                verts[index(x,z)] = new Vector3(x,y,z);
            }
        }
        mesh.vertices = verts;
        mesh.RecalculateNormals();
    }

    [Serializable]
    public struct Octave
    {
        public Vector2 speed;
        public Vector2 scale;
        public float height;
        public bool alternate;
    }
}

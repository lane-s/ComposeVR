using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    [RequireComponent(typeof(MeshFilter))]
    public class TilitBrushLineRenderer : MonoBehaviour {

        public float stripWidth;
        public float stripHeight;
        public float numStrips;

        private Mesh mesh;
        private List<Vector3> vertices;

        // Use this for initialization
        void Awake() {
            mesh = GetComponent<MeshFilter>().mesh;
            GenerateStrips();
        }

        void GenerateStrips() {
            if(numStrips > 0) {
                vertices = new List<Vector3>();
                List<Vector2> UVs = new List<Vector2>();

                //Generate initial pair of vertices
                vertices.Add(Vector3.zero + Vector3.down * stripHeight / 2);
                UVs.Add(new Vector2(0, 0));

                vertices.Add(Vector3.zero + Vector3.up * stripHeight / 2);
                UVs.Add(new Vector2(0, 1));

                Vector3 bottomVert = vertices[0];
                Vector3 topVert = vertices[1];
                int vertIndex = 2;

                float uvIncrement = 1 / numStrips;
                float uvX = 0;

                List<int> triangles = new List<int>();

                //Generate remaining vertices, define triangles and normals
                for(int i = 0; i < numStrips; i++) {

                    bottomVert += Vector3.right * stripWidth;
                    topVert += Vector3.right * stripWidth;

                    uvX += uvIncrement;

                    vertices.Add(bottomVert);
                    vertices.Add(topVert);

                    UVs.Add(new Vector2(uvX, 0));
                    UVs.Add(new Vector2(uvX, 1));

                    vertIndex += 2;

                    int bottomLeft = vertIndex - 4;
                    int topLeft = vertIndex - 3;
                    int bottomRight = vertIndex - 2;
                    int topRight = vertIndex - 1;

                    //Define left-hand triangle
                    triangles.Add(topLeft);
                    triangles.Add(bottomLeft);
                    triangles.Add(bottomRight);

                    //Define right-hand trinagle
                    triangles.Add(topLeft);
                    triangles.Add(bottomRight);
                    triangles.Add(topRight);
                }

                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.SetUVs(0, UVs);

                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();

                
               
                GetComponent<MeshFilter>().mesh = mesh;

                Debug.Log("Mesh generated");
            }
        }

        // Update is called once per frame
        void Update() {

        }
    }
}

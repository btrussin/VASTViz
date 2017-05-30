

//--------------------------------------------------------------
// Purpose: draw a floor area for the VR chaperone area.
//--------------------------------------------------------------

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using Valve.VR;
using System.Collections.Generic;

namespace RealityCheck
{
    [ExecuteInEditMode, RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class FloorGenerator : MonoBehaviour
    {
        public enum Size
        {
            Calibrated,
            _400x300,
            _300x225,
            _200x150
        }

        public Size size;
        [Tooltip("Height of the main border bump")]
        public float borderBumpHeight = 0.05f;
        [Tooltip("Thickness of the main border")]
        public float borderThickness = 0.15f;
        [Tooltip("How far past the border the outer floor should extend")]
        public float outerPerimeterThickness = 2f;

        [Tooltip("Material shown in the main play area")]
        public Material interiorMaterial;
        [Tooltip("Material used for the sides of the boundary wall")]
        public Material boundarySidesMaterial;
        [Tooltip("Material used for the top of the boundary wall")]
        public Material boundaryTopMaterial;
        [Tooltip("Material used for the extended area past the boundary wall")]
        public Material exteriorMaterial;


        [HideInInspector]
        public Vector3[] vertices;

        public static bool GetBounds(Size size, ref HmdQuad_t pRect)
        {
            if (size == Size.Calibrated)
            {
                bool initOpenVR = (!SteamVR.active && !SteamVR.usingNativeSupport);
                if (initOpenVR)
                {
                    EVRInitError error = EVRInitError.None;
                    OpenVR.Init(ref error, EVRApplicationType.VRApplication_Other);
                }

                CVRChaperone chaperone = OpenVR.Chaperone;
                bool success = (chaperone != null) && chaperone.GetPlayAreaRect(ref pRect);
                if (!success)
                    Debug.LogWarning("Failed to get Calibrated Play Area bounds!  Make sure you have tracking first, and that your space is calibrated.");

                if (initOpenVR)
                    OpenVR.Shutdown();

                return success;
            }
            else
            {
                try
                {
                    string str = size.ToString().Substring(1);
                    string[] arr = str.Split(new char[] { 'x' }, 2);

                    // convert to half size in meters (from cm)
                    float x = float.Parse(arr[0]) / 200;
                    float z = float.Parse(arr[1]) / 200;

                    pRect.vCorners0.v0 = x;
                    pRect.vCorners0.v1 = 0;
                    pRect.vCorners0.v2 = z;

                    pRect.vCorners1.v0 = x;
                    pRect.vCorners1.v1 = 0;
                    pRect.vCorners1.v2 = -z;

                    pRect.vCorners2.v0 = -x;
                    pRect.vCorners2.v1 = 0;
                    pRect.vCorners2.v2 = -z;

                    pRect.vCorners3.v0 = -x;
                    pRect.vCorners3.v1 = 0;
                    pRect.vCorners3.v2 = z;

                    return true;
                }
                catch { }
            }

            return false;
        }

        public void BuildMesh()
        {
            HmdQuad_t rect = new HmdQuad_t();
            if (!GetBounds(size, ref rect))
                return;

            HmdVector3_t[] corners = new HmdVector3_t[] { rect.vCorners0, rect.vCorners1, rect.vCorners2, rect.vCorners3 };

            vertices = new Vector3[4];
            for (int i = 0; i < corners.Length; i++)
            {
                HmdVector3_t c = corners[i];
                vertices[i] = new Vector3(c.v0, c.v1, c.v2);
            }



            List<Vector3> verticesList = new List<Vector3>();
            List<Vector2> uvList = new List<Vector2>();
            int[] facesInterior;
            int[] facesExterior;
            int[] facesBoundaryTop;
            int[] facesBoundarySides;
            generateInterior(rect, corners, verticesList, uvList, out facesInterior);
            generateExterior(rect, corners, verticesList, uvList, out facesExterior);
            generateBoundaryTop(rect, corners, verticesList, uvList, out facesBoundaryTop);
            generateBoundarySides(rect, corners, verticesList, uvList, out facesBoundarySides);

            Mesh mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;
            mesh.vertices = verticesList.ToArray();
            mesh.uv = uvList.ToArray();
            mesh.subMeshCount = 4;
            mesh.SetTriangles(facesInterior, 0);
            mesh.SetTriangles(facesExterior, 1);
            mesh.SetTriangles(facesBoundaryTop, 2);
            mesh.SetTriangles(facesBoundarySides, 3);

            Material matDefault = null;
            if (interiorMaterial == null || exteriorMaterial == null
                || boundarySidesMaterial == null || boundaryTopMaterial == null)
            {
                matDefault = getDefaultMaterial();
            }

            Material matInterior = interiorMaterial != null ? interiorMaterial : matDefault;
            Material matExterior = exteriorMaterial != null ? exteriorMaterial : matDefault;
            Material matBoundaryTop = boundaryTopMaterial != null ? boundaryTopMaterial : matDefault;
            Material matBoundarySides = boundarySidesMaterial != null ? boundarySidesMaterial : matDefault;

            MeshRenderer renderer = GetComponent<MeshRenderer>();

            renderer.materials = new Material[] {
                matInterior,
                matExterior,
                matBoundaryTop,
                matBoundarySides
            };

        }

        private Material getDefaultMaterial()
        {
            Material result = null;
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                result = renderer.material;
            }
            if (result == null)
            {
                result = new Material(Shader.Find("Standard"));
            }
            return result;
        }

        private bool generateInterior(HmdQuad_t rect, HmdVector3_t[] corners, List<Vector3> vertices, List<Vector2> uvs, out int[] faces)
        {
            int vertexOffset = vertices.Count;

            for (int i = 0; i < corners.Length; i++)
            {
                HmdVector3_t c = corners[i];
                vertices.Add(new Vector3(c.v0, c.v1, c.v2));
            }

            faces = new int[]
            {
            vertexOffset, vertexOffset+1, vertexOffset+3,
            vertexOffset+1, vertexOffset+2, vertexOffset+3
            };

            uvs.Add(new Vector2(rect.vCorners0.v0, rect.vCorners0.v2));
            uvs.Add(new Vector2(rect.vCorners1.v0, rect.vCorners1.v2));
            uvs.Add(new Vector2(rect.vCorners2.v0, rect.vCorners2.v2));
            uvs.Add(new Vector2(rect.vCorners3.v0, rect.vCorners3.v2));

            return true;
        }

        private bool generateExterior(HmdQuad_t rect, HmdVector3_t[] corners, List<Vector3> vertices, List<Vector2> uvs, out int[] faces)
        {
            float r1 = borderThickness;
            float r2 = outerPerimeterThickness;
            return generatePerimeter(rect, corners, 0f, r1, r2, vertices, uvs, out faces);
        }


        private bool generatePerimeter(HmdQuad_t rect, HmdVector3_t[] corners, float height, float r1, float r2, List<Vector3> vertices, List<Vector2> uvs, out int[] faces)
        {
            int vertexOffset = vertices.Count;

            Vector3 a = new Vector3(corners[0].v0 + r1, height, corners[0].v2 - r1);
            Vector3 b = new Vector3(corners[1].v0 - r1, height, corners[1].v2 - r1);
            Vector3 c = new Vector3(corners[2].v0 - r1, height, corners[2].v2 + r1);
            Vector3 d = new Vector3(corners[3].v0 + r1, height, corners[3].v2 + r1);
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);

            vertices.Add(new Vector3(a.x + r2, height, a.z));
            vertices.Add(new Vector3(a.x + r2, height, a.z - r2));
            vertices.Add(new Vector3(a.x, height, a.z - r2));

            vertices.Add(new Vector3(b.x, height, b.z - r2));
            vertices.Add(new Vector3(b.x - r2, height, b.z - r2));
            vertices.Add(new Vector3(b.x - r2, height, b.z));

            vertices.Add(new Vector3(c.x - r2, height, c.z));
            vertices.Add(new Vector3(c.x - r2, height, c.z + r2));
            vertices.Add(new Vector3(c.x, height, c.z + r2));

            vertices.Add(new Vector3(d.x, height, d.z + r2));
            vertices.Add(new Vector3(d.x + r2, height, d.z + r2));
            vertices.Add(new Vector3(d.x + r2, height, d.z));

            for (int i = vertexOffset; i < vertices.Count; i++)
            {
                uvs.Add(new Vector2(vertices[i].x, vertices[i].z));
            }

            int[] facesTemp = new int[]
            {
                0, 5, 6,
                0, 4, 5,
                0, 6, 7,
                0, 7, 1,
                1, 7, 8,
                1, 8, 9,
                1, 9, 10,
                1, 10, 2,
                2, 10, 11,
                2, 11, 12,
                2, 12, 13,
                2, 13, 3,
                3, 13, 14,
                3, 14, 15,
                3, 15, 4,
                3, 4, 0
            };
            faces = new int[facesTemp.Length];
            for (int i = 0; i < facesTemp.Length; i++)
            {
                faces[i] = facesTemp[i] + vertexOffset;
            }

            return true;
        }



        private bool generateBoundaryTop(HmdQuad_t rect, HmdVector3_t[] corners, List<Vector3> vertices, List<Vector2> uvs, out int[] faces)
        {
            float r1 = 0f;
            float r2 = borderThickness;
            return generatePerimeter(rect, corners, this.borderBumpHeight, r1, r2, vertices, uvs, out faces);
        }
        private bool generateBoundarySides(HmdQuad_t rect, HmdVector3_t[] corners, List<Vector3> vertices, List<Vector2> uvs, out int[] faces)
        {
            int vertexOffset = vertices.Count;

            float r = borderThickness;

            Vector3 a = new Vector3(corners[0].v0, 0f, corners[0].v2);
            Vector3 b = new Vector3(corners[1].v0, 0f, corners[1].v2);
            Vector3 c = new Vector3(corners[2].v0, 0f, corners[2].v2);
            Vector3 d = new Vector3(corners[3].v0, 0f, corners[3].v2);

            Vector3 a1 = new Vector3(corners[0].v0, borderBumpHeight, corners[0].v2);
            Vector3 b1 = new Vector3(corners[1].v0, borderBumpHeight, corners[1].v2);
            Vector3 c1 = new Vector3(corners[2].v0, borderBumpHeight, corners[2].v2);
            Vector3 d1 = new Vector3(corners[3].v0, borderBumpHeight, corners[3].v2);

            float heightUVOffset = borderBumpHeight;
            for (int i = 0; i < 2; i++)
            {
                vertices.Add(a);
                uvs.Add(new Vector2(a.x, a.z - heightUVOffset));
                vertices.Add(b);
                uvs.Add(new Vector2(b.x, b.z - heightUVOffset));
                vertices.Add(a1);
                uvs.Add(new Vector2(a1.x, a1.z));
                vertices.Add(b1);
                uvs.Add(new Vector2(b1.x, b1.z));

                vertices.Add(b);
                uvs.Add(new Vector2(b.x + heightUVOffset, b.z));
                vertices.Add(c);
                uvs.Add(new Vector2(c.x + heightUVOffset, c.z));
                vertices.Add(b1);
                uvs.Add(new Vector2(b1.x, b1.z));
                vertices.Add(c1);
                uvs.Add(new Vector2(c1.x, c1.z));

                vertices.Add(c);
                uvs.Add(new Vector2(c.x, c.z + heightUVOffset));
                vertices.Add(d);
                uvs.Add(new Vector2(d.x, d.z + heightUVOffset));
                vertices.Add(c1);
                uvs.Add(new Vector2(c1.x, c1.z));
                vertices.Add(d1);
                uvs.Add(new Vector2(d1.x, d1.z));

                vertices.Add(d);
                uvs.Add(new Vector2(d.x - heightUVOffset, d.z));
                vertices.Add(a);
                uvs.Add(new Vector2(a.x - heightUVOffset, a.z));
                vertices.Add(d1);
                uvs.Add(new Vector2(d1.x, d1.z));
                vertices.Add(a1);
                uvs.Add(new Vector2(a1.x, a1.z));

                // For the second round, adjust the coordinates to the outside 
                a = new Vector3(corners[0].v0 + r, 0f, corners[0].v2 - r);
                b = new Vector3(corners[1].v0 - r, 0f, corners[1].v2 - r);
                c = new Vector3(corners[2].v0 - r, 0f, corners[2].v2 + r);
                d = new Vector3(corners[3].v0 + r, 0f, corners[3].v2 + r);

                a1 = new Vector3(corners[0].v0 + r, borderBumpHeight, corners[0].v2 - r);
                b1 = new Vector3(corners[1].v0 - r, borderBumpHeight, corners[1].v2 - r);
                c1 = new Vector3(corners[2].v0 - r, borderBumpHeight, corners[2].v2 + r);
                d1 = new Vector3(corners[3].v0 + r, borderBumpHeight, corners[3].v2 + r);

                heightUVOffset = -borderBumpHeight;
            }

            int[] facesTemp = new int[]
            {
                0, 2, 3,
                0, 3, 1,
                4, 6, 7,
                4, 7, 5,
                8, 10, 11,
                8, 11, 9,
                12, 14, 15,
                12, 15, 13,

                16, 17, 18,
                17, 19, 18,

                20, 21, 22,
                21, 23, 22,

                24, 25, 26,
                25, 27, 26,

                28, 29, 30,
                29, 31, 30
            };
            faces = new int[facesTemp.Length];
            for (int i = 0; i < facesTemp.Length; i++)
            {
                faces[i] = facesTemp[i] + vertexOffset;
            }
            return true;
        }



        void OnDrawGizmos()
        {
            DrawWireframe();
        }

        public void DrawWireframe()
        {
            if (vertices == null || vertices.Length == 0)
                return;

            Vector3 offset = transform.TransformVector(Vector3.up * 2.0f);
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;

                Vector3 a = transform.TransformPoint(vertices[i]);
                Vector3 b = a + offset;
                Vector3 c = transform.TransformPoint(vertices[next]);
                Vector3 d = c + offset;
                Gizmos.DrawLine(a, b);
                Gizmos.DrawLine(a, c);
                Gizmos.DrawLine(b, d);
            }
        }

        public void OnEnable()
        {
            if (Application.isPlaying)
            {
                GetComponent<MeshRenderer>().enabled = true;

                // No need to remain enabled at runtime.
                // Anyone that wants to change properties at runtime
                // should call BuildMesh themselves.
                enabled = false;

                // If we want the configured bounds of the user,
                // we need to wait for tracking.
                if (size == Size.Calibrated)
                    StartCoroutine("UpdateBounds");
            }
        }

        IEnumerator UpdateBounds()
        {
            GetComponent<MeshFilter>().mesh = null; // clear existing

            CVRChaperone chaperone = OpenVR.Chaperone;
            if (chaperone == null)
                yield break;

            while (chaperone.GetCalibrationState() != ChaperoneCalibrationState.OK)
                yield return null;

            BuildMesh();
        }
    }
}
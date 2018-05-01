using Steamworks;
using UnityEngine;

namespace Oxide.GettingOverItMP.Debugging
{
    [RequireComponent(typeof(PolygonCollider2D))]
    public class PolygonColliderVisualizer : MonoBehaviour
    {
        private PolygonCollider2D collider;
        private GameObject visualizeObject;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh mesh;

        private void Awake()
        {
            collider = GetComponent<PolygonCollider2D>();

            RebuildMesh();

            visualizeObject = new GameObject("PolygonColliderVisualizer");
            visualizeObject.transform.SetParent(transform, false);

            var pos = visualizeObject.transform.localPosition;
            visualizeObject.transform.localPosition = new Vector3(pos.x, pos.y, -10);
            visualizeObject.transform.Translate(collider.offset);

            meshFilter = visualizeObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            meshRenderer = visualizeObject.AddComponent<MeshRenderer>();
        }

        private void OnDisable()
        {
            visualizeObject.SetActive(false);
        }

        private void OnEnable()
        {
            visualizeObject.SetActive(true);
        }

        private void OnDestroy()
        {
            Destroy(visualizeObject);
            Destroy(mesh);
        }

        public void RebuildMesh()
        {
            var triangulator = new Triangulator(collider.points);
            int[] indices = triangulator.Triangulate();

            Vector3[] vertices = new Vector3[collider.points.Length];

            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = new Vector3(collider.points[i].x, collider.points[i].y, 0);
            }

            if (!mesh)
                mesh = new Mesh();

            mesh.vertices = vertices;
            mesh.triangles = indices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}

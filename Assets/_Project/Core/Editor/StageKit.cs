using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Building blocks for the greybox sets: lathe meshes (tanks, nozzles, hulls), primitives without
    /// colliders, beams between two points, and lights. Everything takes a material name from
    /// SetupVisuals.Materials so every surface stays on the palette.
    /// </summary>
    public static class StageKit
    {
        const string MeshDir = "Assets/_Project/Art/Generated/Meshes";

        // ---------------------------------------------------------------- meshes

        /// <summary>
        /// Surface of revolution around Y. Profile points are (radius, y), listed bottom to top for an
        /// outward-facing surface. Neighbouring segments closer than smoothAngle are shaded smooth.
        /// </summary>
        public static Mesh Lathe(string name, Vector2[] p, int seg = 40, float arcDegrees = 360f, bool twoSided = false, float smoothAngle = 40f)
        {
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            int n = p.Length;
            var sn = new Vector2[n - 1];
            for (int i = 0; i < n - 1; i++)
            {
                Vector2 d = p[i + 1] - p[i];
                sn[i] = new Vector2(d.y, -d.x).normalized;
            }
            float cosT = Mathf.Cos(smoothAngle * Mathf.Deg2Rad);
            float arc = arcDegrees * Mathf.Deg2Rad;
            for (int i = 0; i < n - 1; i++)
            {
                Vector2 n0 = sn[i], n1 = sn[i];
                if (i > 0 && Vector2.Dot(sn[i - 1], sn[i]) > cosT) n0 = (sn[i - 1] + sn[i]).normalized;
                if (i < n - 2 && Vector2.Dot(sn[i + 1], sn[i]) > cosT) n1 = (sn[i + 1] + sn[i]).normalized;
                int b = verts.Count;
                for (int k = 0; k <= seg; k++)
                {
                    float a = arc * k / seg, c = Mathf.Cos(a), s = Mathf.Sin(a);
                    verts.Add(new Vector3(p[i].x * c, p[i].y, p[i].x * s)); norms.Add(new Vector3(n0.x * c, n0.y, n0.x * s));
                    verts.Add(new Vector3(p[i + 1].x * c, p[i + 1].y, p[i + 1].x * s)); norms.Add(new Vector3(n1.x * c, n1.y, n1.x * s));
                    uvs.Add(new Vector2(k / (float)seg, i / (float)(n - 1))); uvs.Add(new Vector2(k / (float)seg, (i + 1) / (float)(n - 1)));
                }
                for (int k = 0; k < seg; k++)
                {
                    int A = b + 2 * k, C = A + 1, B = b + 2 * (k + 1), D = B + 1;
                    tris.Add(A); tris.Add(C); tris.Add(B);
                    tris.Add(B); tris.Add(C); tris.Add(D);
                }
            }
            if (twoSided)
            {
                int count = verts.Count, tcount = tris.Count;
                for (int i = 0; i < count; i++) { verts.Add(verts[i]); norms.Add(-norms[i]); uvs.Add(uvs[i]); }
                for (int i = 0; i < tcount; i += 3) { tris.Add(tris[i] + count); tris.Add(tris[i + 2] + count); tris.Add(tris[i + 1] + count); }
            }
            return Save(name, verts, norms, uvs, tris);
        }

        static Mesh Save(string name, List<Vector3> v, List<Vector3> nrm, List<Vector2> uv, List<int> t)
        {
            Directory.CreateDirectory(MeshDir);
            string path = MeshDir + "/" + name + ".asset";
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = m == null;
            if (isNew) m = new Mesh();
            m.Clear();
            m.name = name;
            m.indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            m.SetVertices(v); m.SetNormals(nrm); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateBounds();
            if (isNew) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
            return m;
        }

        // Shared shapes, built once per setup run.
        static readonly Dictionary<string, Mesh> Shapes = new Dictionary<string, Mesh>();

        public static void BuildShapes()
        {
            Shapes.Clear();
            Shapes["tank"] = Lathe("tank", new[] {
                V(0f, -1f), V(0.25f, -0.97f), V(0.42f, -0.88f), V(0.5f, -0.72f),
                V(0.5f, 0.72f), V(0.42f, 0.88f), V(0.25f, 0.97f), V(0f, 1f) });
            Shapes["nozzle"] = Lathe("nozzle", new[] {
                V(0.34f, -0.60f), V(0.32f, -0.50f), V(0.27f, -0.36f), V(0.20f, -0.20f), V(0.13f, -0.06f), V(0.10f, 0f) }, 40, 360f, true);
            Shapes["cone"] = Lathe("cone", new[] { V(0f, 0f), V(0.5f, 0f), V(0f, 1f) }, 24);
            Shapes["plume"] = Lathe("plume", new[] { V(0f, -1.6f), V(0.2f, -0.85f), V(0.36f, -0.25f), V(0.28f, 0f) }, 28, 360f, true, 80f);
            Shapes["dish"] = Lathe("dish", new[] { V(0f, 0f), V(0.2f, 0.02f), V(0.4f, 0.08f), V(0.6f, 0.19f) }, 32, 360f, true, 80f);
            Shapes["fragment"] = Lathe("fragment", new[] { V(1.7f, 0f), V(1.7f, 0.85f), V(1.62f, 1.25f), V(1.48f, 1.55f) }, 14, 46f, true);
            Shapes["ring"] = Lathe("ring", new[] { V(0.5f, -0.5f), V(0.5f, 0.5f) }, 40, 360f, true);
        }

        public static Mesh Shape(string name) => Shapes[name];
        public static Vector2 V(float r, float y) => new Vector2(r, y);

        // ---------------------------------------------------------------- objects

        public static Material Mat(string name)
        {
            if (SetupVisuals.Materials.TryGetValue(name, out var m) && m != null) return m;
            Debug.LogError("[StageKit] Material missing: " + name);
            return null;
        }

        public static Transform Group(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, string mat, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return Place(go, name, parent, pos, scale, mat, euler);
        }

        public static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 size, string mat, Vector3? euler = null) =>
            Prim(PrimitiveType.Cube, name, parent, pos, size, mat, euler);

        /// <summary>Cylinder with the given diameter and full height (Unity's primitive is 2 units tall).</summary>
        public static GameObject Cyl(string name, Transform parent, Vector3 pos, float diameter, float height, string mat, Vector3? euler = null) =>
            Prim(PrimitiveType.Cylinder, name, parent, pos, new Vector3(diameter, height * 0.5f, diameter), mat, euler);

        public static GameObject Ball(string name, Transform parent, Vector3 pos, float diameter, string mat) =>
            Prim(PrimitiveType.Sphere, name, parent, pos, Vector3.one * diameter, mat);

        public static GameObject MeshObj(Mesh mesh, string name, Transform parent, Vector3 pos, Vector3 scale, string mat, Vector3? euler = null)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            return Place(go, name, parent, pos, scale, mat, euler);
        }

        static GameObject Place(GameObject go, string name, Transform parent, Vector3 pos, Vector3 scale, string mat, Vector3? euler)
        {
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Mat(mat);
            if (mat.StartsWith("st_glow") || mat.StartsWith("st_plume") || mat == "st_screen")
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            return go;
        }

        /// <summary>A square bar stretched between two local points.</summary>
        public static GameObject Beam(string name, Transform parent, Vector3 a, Vector3 b, float thickness, string mat)
        {
            var go = Cube(name, parent, (a + b) * 0.5f, new Vector3(thickness, thickness, (b - a).magnitude), mat);
            go.transform.localRotation = Quaternion.LookRotation((b - a).normalized, Mathf.Abs(Vector3.Dot((b - a).normalized, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up);
            return go;
        }

        public static void Recolor(GameObject go, string mat)
        {
            var m = Mat(mat);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var arr = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < arr.Length; i++) arr[i] = m;
                r.sharedMaterials = arr;
            }
        }

        // ---------------------------------------------------------------- lights

        public static Light Point(string name, Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        public static Light Spot(string name, Transform parent, Vector3 pos, Vector3 lookAt, Color color, float intensity, float range, float angle, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.rotation = Quaternion.LookRotation((lookAt - go.transform.position).normalized);
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot; l.color = color; l.intensity = intensity; l.range = range;
            l.spotAngle = angle; l.innerSpotAngle = angle * 0.6f;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            l.shadowStrength = 0.85f;
            return l;
        }

        /// <summary>Clamp block plus a red and a green lamp. Missions switch the lamps when a module is secured.</summary>
        public static void ClampLamps(Transform module, Vector3 pos)
        {
            Cube("ClampBlock", module, pos, new Vector3(0.16f, 0.1f, 0.12f), "st_hazard");
            Ball("LedRed", module, pos + new Vector3(0f, 0.09f, 0f), 0.075f, "st_glow_red");
            Ball("LedGreen", module, pos + new Vector3(0f, 0.09f, 0f), 0.075f, "st_glow_green").SetActive(false);
        }
    }
}

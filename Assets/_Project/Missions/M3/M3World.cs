using LastSignal.Ship;
using UnityEngine;

namespace LastSignal.M3
{
    /// <summary>
    /// Greybox deep-space backdrop for M3: stars, dust (to feel speed), Earth, the Moon, the Sun, the departure dock,
    /// and the uncharted planet (hidden until the scanner finds it). Earth, the Moon and the distance between them use one
    /// scale (SectorLayout.KmPerUnit, game balance) with JPL Horizons directions and NSSDC radii; the Sun is drawn in its
    /// real direction as a backdrop, not at its real distance.
    /// </summary>
    public class M3World : MonoBehaviour
    {
        public Transform Earth { get; private set; }
        public Transform Planet { get; private set; }
        public Transform Dock { get; private set; }
        public Transform Moon { get; private set; }
        public Transform Sun { get; private set; }
        /// <summary>Unit vector toward the Sun (Horizons snapshot).</summary>
        public Vector3 SunDirection { get; private set; }
        /// <summary>False if a NASA CSV was missing and fallback layout values were used.</summary>
        public bool LayoutFromCsv { get; private set; }

        /// <summary>Earth's place in the sector (sector coordinates, game layout). The Moon is placed from Horizons data.</summary>
        public static readonly Vector3 EarthPosition = new Vector3(-6000f, 1500f, -8000f);
        const float SunBackdropDistance = 60000f;
        const float SunSizeBoost = 4f;   // [game balance] Sun drawn 4x its real angular size so it reads on screen
        public static readonly Vector3 PlanetPosition = new Vector3(14000f, -2500f, 26000f);

        Transform _stars;

        public static M3World Build(Transform parent)
        {
            GameObject go = new GameObject("M3World");
            go.transform.SetParent(parent, false);
            M3World w = go.AddComponent<M3World>();
            w.BuildAll();
            return w;
        }

        void BuildAll()
        {
            _stars = PointCloud("Stars", 2500, 40000f, 40000f, true, new Color(0.9f, 0.95f, 1f)).transform;
            PointCloud("Dust", 1500, 1800f, 0f, false, new Color(0.55f, 0.6f, 0.7f)).transform.localPosition = new Vector3(0f, 0f, 400f);

            CsvTable horizons = CsvTable.Load(SectorLayout.HorizonsCsv);
            CsvTable radii = CsvTable.Load(SectorLayout.RadiiCsv);
            Vector3 moonOffset;
            Vector3 sunDir;
            double earthSunKm, earthR, moonR, sunR;
            bool ok = SectorLayout.TryMoonOffset(horizons, out moonOffset);
            ok &= SectorLayout.TrySunDirection(horizons, out sunDir, out earthSunKm);
            ok &= SectorLayout.TryRadiusKm(radii, "Earth", out earthR);
            ok &= SectorLayout.TryRadiusKm(radii, "Moon", out moonR);
            ok &= SectorLayout.TryRadiusKm(radii, "Sun", out sunR);
            LayoutFromCsv = ok;
            if (ok)
            {
                NasaData.Mark("horizons_positions_2026-11-13.csv", MissionIds.M3);
                NasaData.Mark("body_radii.csv", MissionIds.M3);
            }
            else
            {
                NasaData.ReportFallback("horizons_positions_2026-11-13.csv", "M3 sector layout CSV unreadable, built-in layout used");
                NasaData.ReportFallback("body_radii.csv", "M3 body radii CSV unreadable, built-in radii used");
            }
            if (!ok)
            {
                // Fallback only if the CSVs are missing from the build; same numbers the CSVs give today.
                Debug.LogWarning("[M3] Sector CSVs missing; using fallback layout.");
                moonOffset = new Vector3(386f, -1141f, -16169f);
                sunDir = new Vector3(-0.64f, 0f, -0.77f).normalized;
                earthSunKm = 1.4806e8; earthR = 6371.0; moonR = 1737.4; sunR = 695700.0;
            }
            SunDirection = sunDir;

            Earth = GreyboxKit.Part(PrimitiveType.Sphere, "Earth", transform, EarthPosition, Vector3.one * SectorLayout.KmToUnits(earthR) * 2f, new Color(0.15f, 0.40f, 0.85f), true).transform;
            Moon = GreyboxKit.Part(PrimitiveType.Sphere, "Moon", transform, EarthPosition + moonOffset, Vector3.one * SectorLayout.KmToUnits(moonR) * 2f, new Color(0.6f, 0.6f, 0.62f), true).transform;
            float sunDeg = SectorLayout.AngularDiameterDeg(sunR, earthSunKm) * SunSizeBoost;
            float sunSize = 2f * SunBackdropDistance * Mathf.Tan(sunDeg * 0.5f * Mathf.Deg2Rad);
            Sun = GreyboxKit.Part(PrimitiveType.Sphere, "Sun", transform, sunDir * SunBackdropDistance, Vector3.one * sunSize, new Color(1f, 0.85f, 0.55f), true).transform;

            Planet = GreyboxKit.Part(PrimitiveType.Sphere, "UnchartedPlanet", transform, PlanetPosition, Vector3.one * 6000f, new Color(0.70f, 0.42f, 0.28f), true).transform;
            GreyboxKit.Part(PrimitiveType.Sphere, "PlanetCloudBand", Planet, Vector3.zero, Vector3.one * 1.015f, new Color(0.85f, 0.75f, 0.65f));
            Planet.gameObject.SetActive(false);

            Dock = BuildDock();
        }

        Transform BuildDock()
        {
            Transform dock = new GameObject("DepartureDock").transform;
            dock.SetParent(transform, false);
            Color frame = new Color(0.45f, 0.48f, 0.55f);
            GreyboxKit.Part(PrimitiveType.Cube, "Spine", dock, new Vector3(0f, -6f, -4f), new Vector3(3f, 2f, 40f), frame);
            GreyboxKit.Part(PrimitiveType.Cube, "ArmL", dock, new Vector3(-7f, -2f, 0f), new Vector3(1.2f, 9f, 1.2f), frame);
            GreyboxKit.Part(PrimitiveType.Cube, "ArmR", dock, new Vector3(7f, -2f, 0f), new Vector3(1.2f, 9f, 1.2f), frame);
            GreyboxKit.Part(PrimitiveType.Cube, "Gantry", dock, new Vector3(0f, 2.8f, 0f), new Vector3(15f, 0.8f, 1.2f), frame);
            GreyboxKit.Part(PrimitiveType.Sphere, "LightL", dock, new Vector3(-7f, 3.2f, 0f), Vector3.one * 0.6f, new Color(0.3f, 1f, 0.5f), true);
            GreyboxKit.Part(PrimitiveType.Sphere, "LightR", dock, new Vector3(7f, 3.2f, 0f), Vector3.one * 0.6f, new Color(0.3f, 1f, 0.5f), true);
            GreyboxKit.Part(PrimitiveType.Cylinder, "Habitat", dock, new Vector3(0f, -6f, -30f), new Vector3(14f, 6f, 14f), new Vector3(90f, 0f, 0f), frame * 0.8f);
            return dock;
        }

        public void SetupCamera(Camera cam)
        {
            if (cam == null) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.005f, 0.008f, 0.02f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 90000f;
            RenderSettings.ambientLight = new Color(0.12f, 0.13f, 0.16f);
            Light sun = FindDirectionalLight();
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.LookRotation(-SunDirection);
                sun.intensity = 1.3f;
                sun.color = new Color(1f, 0.95f, 0.88f);
            }
        }

        static Light FindDirectionalLight()
        {
            if (RenderSettings.sun != null) return RenderSettings.sun;
            GameObject existing = GameObject.Find("Directional Light");
            Light found = existing != null ? existing.GetComponent<Light>() : null;
            if (found != null && found.type == LightType.Directional) return found;
            GameObject go = new GameObject("Sun Light");
            Light l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            return l;
        }

        public void RevealPlanet()
        {
            Planet.gameObject.SetActive(true);
        }

        void LateUpdate()
        {
            // Stars stay around the camera so they always look infinitely far away.
            Camera cam = Camera.main;
            if (cam != null && _stars != null) _stars.position = cam.transform.position;
            if (cam != null && Sun != null) Sun.position = cam.transform.position + SunDirection * SunBackdropDistance;
        }

        /// <summary>A mesh of single-pixel points. shell = true puts them on a sphere of the given radius.</summary>
        GameObject PointCloud(string name, int count, float size, float shellRadius, bool shell, Color tint)
        {
            System.Random rng = new System.Random(name.GetHashCode());
            Vector3[] verts = new Vector3[count];
            Color[] cols = new Color[count];
            int[] idx = new int[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 p = new Vector3((float)rng.NextDouble() * 2f - 1f, (float)rng.NextDouble() * 2f - 1f, (float)rng.NextDouble() * 2f - 1f);
                if (shell) p = p.normalized * shellRadius;
                else p *= size * 0.5f;
                verts[i] = p;
                float b = 0.4f + (float)rng.NextDouble() * 0.6f;
                cols[i] = new Color(tint.r * b, tint.g * b, tint.b * b, 1f);
                idx[i] = i;
            }
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = verts;
            mesh.colors = cols;
            mesh.SetIndices(idx, MeshTopology.Points, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * (shell ? shellRadius * 2.2f : size * 1.2f));

            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = GreyboxKit.LineMaterial();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }
    }
}

using LastSignal.Ship;
using UnityEngine;

namespace LastSignal.M3
{
    /// <summary>Relay R-7: drifts slowly (so targeting takes a little skill), blinks, and breaks apart when hit.</summary>
    public class RelaySatellite : MonoBehaviour
    {
        public float driftAmplitude = 18f;
        public bool IsDestroyed { get; private set; }

        Vector3 _anchor;
        Transform _body;
        GreyboxTint _beacon;
        float _t;

        public static RelaySatellite Build(Vector3 position, Transform parent)
        {
            GameObject go = new GameObject("Relay_R7");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            RelaySatellite r = go.AddComponent<RelaySatellite>();
            r._anchor = position;
            r.BuildParts();
            return r;
        }

        void BuildParts()
        {
            _body = new GameObject("Body").transform;
            _body.SetParent(transform, false);
            Color metal = new Color(0.75f, 0.70f, 0.55f);
            Color panel = new Color(0.12f, 0.20f, 0.45f);
            GreyboxKit.Part(PrimitiveType.Cube, "Bus", _body, Vector3.zero, new Vector3(4f, 4f, 5f), metal);
            GreyboxKit.Part(PrimitiveType.Cube, "PanelL", _body, new Vector3(-9f, 0f, 0f), new Vector3(12f, 0.2f, 4f), panel, true);
            GreyboxKit.Part(PrimitiveType.Cube, "PanelR", _body, new Vector3(9f, 0f, 0f), new Vector3(12f, 0.2f, 4f), panel, true);
            GreyboxKit.Part(PrimitiveType.Cylinder, "Dish", _body, new Vector3(0f, 0f, -4f), new Vector3(7f, 0.3f, 7f), new Vector3(90f, 0f, 0f), new Color(0.9f, 0.9f, 0.9f));
            GreyboxKit.Part(PrimitiveType.Cylinder, "Feed", _body, new Vector3(0f, 0f, -6f), new Vector3(0.4f, 1.6f, 0.4f), new Vector3(90f, 0f, 0f), metal);
            _beacon = GreyboxKit.Part(PrimitiveType.Sphere, "Beacon", _body, new Vector3(0f, 2.6f, 0f), Vector3.one * 0.9f, new Color(1f, 0.3f, 0.2f), true).GetComponent<GreyboxTint>();
            // Dish points back toward Earth.
            transform.rotation = Quaternion.LookRotation(transform.position - M3World.EarthPosition);
        }

        void Update()
        {
            if (IsDestroyed) return;
            _t += Time.deltaTime;
            transform.position = _anchor + new Vector3(Mathf.Sin(_t * 0.35f) * driftAmplitude, Mathf.Sin(_t * 0.5f) * driftAmplitude * 0.5f, 0f);
            _body.Rotate(0f, 0f, 6f * Time.deltaTime, Space.Self);
            if (_beacon != null) _beacon.gameObject.SetActive(Mathf.Repeat(_t, 1.2f) < 0.3f);
        }

        /// <summary>Breaks the relay into drifting pieces. flash = false for a quiet wreck (checkpoint restore).</summary>
        public void Explode(bool flash = true)
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            if (flash) GreyboxKit.Flash(transform.position, new Color(1f, 0.6f, 0.25f), 45f, 1.2f);

            System.Random rng = new System.Random(7);
            Transform[] parts = new Transform[_body.childCount];
            for (int i = 0; i < parts.Length; i++) parts[i] = _body.GetChild(i);
            for (int i = 0; i < parts.Length; i++)
            {
                Transform p = parts[i];
                if (p == _beacon.transform) { p.gameObject.SetActive(false); continue; }
                p.SetParent(transform, true);
                Rigidbody rb = p.gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.linearDamping = 0f;
                rb.angularDamping = 0f;
                Vector3 dir = (p.position - transform.position).normalized;
                if (dir.sqrMagnitude < 0.01f) dir = Random.onUnitSphere;
                Vector3 jitter = new Vector3((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f);
                rb.AddForce((dir + jitter) * (flash ? 10f : 2f), ForceMode.VelocityChange);
                rb.AddTorque(jitter * 3f, ForceMode.VelocityChange);
                if (!flash) p.position += dir * 8f;
            }
        }
    }
}

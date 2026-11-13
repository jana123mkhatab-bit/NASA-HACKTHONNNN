using UnityEngine;

namespace LastSignal.Ship
{
    /// <summary>
    /// Greybox helpers: coloured primitives, line materials and simple flashes.
    /// Colours are applied at runtime by GreyboxTint, so prefabs saved in the editor need no material assets.
    /// Person 5 can swap any greybox part for real art later.
    /// </summary>
    public static class GreyboxKit
    {
        public static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale,
            Color color, bool emissive = false, bool keepCollider = false)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (!keepCollider)
            {
                Collider c = go.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
            }
            GreyboxTint tint = go.AddComponent<GreyboxTint>();
            tint.color = color;
            tint.emissive = emissive;
            if (Application.isPlaying) tint.Apply();
            return go;
        }

        public static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale,
            Vector3 localEuler, Color color, bool emissive = false)
        {
            GameObject go = Part(type, name, parent, localPos, localScale, color, emissive);
            go.transform.localRotation = Quaternion.Euler(localEuler);
            return go;
        }

        static Material _lineMat;

        /// <summary>An unlit material for LineRenderers (Sprites/Default is always included in builds).</summary>
        public static Material LineMaterial()
        {
            if (_lineMat == null)
            {
                Shader s = Shader.Find("Sprites/Default");
                if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
                _lineMat = new Material(s);
                _lineMat.name = "GreyboxLine (runtime)";
            }
            return _lineMat;
        }

        public static LineRenderer Line(string name, Transform parent, Color color, float width, int points)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = LineMaterial();
            lr.startColor = color;
            lr.endColor = color;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.positionCount = points;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        /// <summary>A glowing sphere that grows and disappears, with a light pulse. For explosions and hits.</summary>
        public static void Flash(Vector3 position, Color color, float maxScale, float duration)
        {
            GameObject go = Part(PrimitiveType.Sphere, "Flash", null, position, Vector3.one, color, true);
            go.transform.position = position;
            FxFlash fx = go.AddComponent<FxFlash>();
            fx.maxScale = maxScale;
            fx.duration = duration;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = maxScale * 6f;
            l.intensity = 8f;
            fx.pulse = l;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _lineMat = null;
        }
    }
}

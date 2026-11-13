using System;
using System.Collections;
using LastSignal.Ship;
using UnityEngine;

namespace LastSignal.M3
{
    /// <summary>The repaired scanner sends out an expanding ring; when it finishes, the planet is revealed.</summary>
    public class ScannerSweep : MonoBehaviour
    {
        public float duration = 3.5f;
        public float maxRadius = 2500f;
        const int Points = 96;

        public void Run(Transform origin, Action onDone)
        {
            StartCoroutine(Sweep(origin, onDone));
        }

        IEnumerator Sweep(Transform origin, Action onDone)
        {
            LineRenderer[] rings = new LineRenderer[3];
            for (int r = 0; r < rings.Length; r++)
            {
                rings[r] = GreyboxKit.Line("ScanRing", null, new Color(0.3f, 0.9f, 0.8f, 0.9f), 1f, Points);
                rings[r].loop = true;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                for (int r = 0; r < rings.Length; r++)
                {
                    float k = Mathf.Clamp01(t / duration - r * 0.15f);
                    float radius = Mathf.Lerp(5f, maxRadius, k * k);
                    float width = Mathf.Lerp(0.4f, 25f, k);
                    rings[r].startWidth = width;
                    rings[r].endWidth = width;
                    Vector3 c = origin.position;
                    Quaternion tilt = Quaternion.Euler(0f, 0f, r * 25f);
                    for (int i = 0; i < Points; i++)
                    {
                        float a = i / (float)Points * Mathf.PI * 2f;
                        rings[r].SetPosition(i, c + tilt * new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
                    }
                }
                yield return null;
            }
            for (int r = 0; r < rings.Length; r++) Destroy(rings[r].gameObject);
            if (onDone != null) onDone();
        }
    }
}

using UnityEngine;

/// <summary>Drifting debris that gives a sense of speed in empty space. The ship is the render origin, so dust moves against its velocity.</summary>
public class SpaceDust : MonoBehaviour
{
    Transform[] p; Material mat; const int N = 150; const float BOX = 140f;
    public void Init()
    {
        mat = Mats.Unlit(new Color(0.7f, 0.85f, 1.4f));
        p = new Transform[N];
        for (int i = 0; i < N; i++)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(transform, false);
            g.GetComponent<Renderer>().sharedMaterial = mat; g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            g.transform.localPosition = new Vector3(Random.Range(-BOX, BOX), Random.Range(-BOX, BOX), Random.Range(-BOX, BOX)); p[i] = g.transform;
        }
    }
    public void SetActive(bool a) { gameObject.SetActive(a); }

    public void Tick(Vector3 camPos, Vector3 relVel, float dt)
    {
        float sp = relVel.magnitude;
        Vector3 dir = sp > 0.1f ? relVel / sp : Vector3.forward;
        float len = Mathf.Clamp(sp * 0.012f, 0.05f, 40f);
        Quaternion q = Quaternion.LookRotation(dir);
        for (int i = 0; i < N; i++)
        {
            var t = p[i]; Vector3 w = t.position - camPos - relVel * dt;
            w.x = Wrap(w.x); w.y = Wrap(w.y); w.z = Wrap(w.z);
            t.position = camPos + w; t.rotation = q; t.localScale = new Vector3(0.05f, 0.05f, len);
        }
    }
    static float Wrap(float v) { if (v > BOX) v -= 2 * BOX; else if (v < -BOX) v += 2 * BOX; return v; }
}

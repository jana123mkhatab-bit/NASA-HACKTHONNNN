using UnityEngine;

/// <summary>Mission Control hall: a procedural interior far above the surface world. Walk in through the glowing arch at the spaceport.</summary>
public class InteriorHall : MonoBehaviour
{
    public static InteriorHall I;
    GameObject root; public Vector3 origin = new Vector3(0, 4000, 0);
    public Vector3 spawn, exitPos; Transform globe;

    public void Build()
    {
        I = this; root = new GameObject("MissionControlHall"); root.transform.position = origin; root.SetActive(false);
        Material wall = Mats.Lit(new Color(0.09f, 0.1f, 0.13f), 0.5f, 0.55f), floor = Mats.Lit(new Color(0.05f, 0.06f, 0.08f), 0.6f, 0.9f);
        Material panel = Mats.Lit(new Color(0.16f, 0.18f, 0.22f), 0.7f, 0.5f), glow = Mats.Unlit(new Color(0.2f, 2.2f, 2.6f)), amber = Mats.Unlit(new Color(2.6f, 1.2f, 0.2f));
        float W = 44, L = 34, Hh = 13;
        Box("Floor", new Vector3(0, -0.25f, 0), new Vector3(W, 0.5f, L), floor);
        Box("Ceiling", new Vector3(0, Hh + 0.25f, 0), new Vector3(W, 0.5f, L), wall);
        Box("WallN", new Vector3(0, Hh / 2, L / 2 + 0.25f), new Vector3(W, Hh, 0.5f), wall);
        Box("WallS", new Vector3(0, Hh / 2, -L / 2 - 0.25f), new Vector3(W, Hh, 0.5f), wall);
        Box("WallE", new Vector3(W / 2 + 0.25f, Hh / 2, 0), new Vector3(0.5f, Hh, L), wall);
        Box("WallW", new Vector3(-W / 2 - 0.25f, Hh / 2, 0), new Vector3(0.5f, Hh, L), wall);
        // big screen on the north wall showing Earth
        var scr = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(scr.GetComponent<Collider>()); scr.transform.SetParent(root.transform, false);
        scr.transform.localPosition = new Vector3(0, 7f, L / 2 - 0.02f); scr.transform.localRotation = Quaternion.Euler(0, 180, 0); scr.transform.localScale = new Vector3(34, 8.5f, 1);
        var sm = Mats.Unlit(Color.white); sm.SetTexture("_BaseMap", Mats.Tex("earth_daymap")); sm.SetColor("_BaseColor", new Color(1.1f, 1.1f, 1.15f)); scr.GetComponent<Renderer>().sharedMaterial = sm;
        Box("ScreenFrame", new Vector3(0, 7f, L / 2 - 0.05f), new Vector3(35, 9.5f, 0.1f), glow, false);
        // light strips
        for (int i = -3; i <= 3; i++) Box("Strip" + i, new Vector3(i * 6f, Hh - 0.05f, 0), new Vector3(0.4f, 0.1f, L - 4), glow, false);
        // console rows
        for (int r = 0; r < 3; r++)
            for (int c = -4; c <= 4; c++)
            {
                if (c == 0) continue;
                Vector3 p = new Vector3(c * 3.8f, 0.55f, -4f + r * 5.5f);
                Box("Desk", p, new Vector3(3.1f, 1.1f, 1.7f), panel);
                Box("Screen", p + new Vector3(0, 1.05f, 0.45f), new Vector3(2.4f, 0.9f, 0.08f), (c + r) % 2 == 0 ? glow : amber, false);
            }
        // holo globe in the middle aisle
        var gp = new GameObject("GlobePlinth"); gp.transform.SetParent(root.transform, false); gp.transform.localPosition = new Vector3(0, 0, 8f);
        Box("Plinth", new Vector3(0, 0.6f, 8f), new Vector3(3.2f, 1.2f, 3.2f), panel);
        var g = Mats.Model("Celestial/earth", root.transform, false); globe = g.transform; globe.localPosition = new Vector3(0, 3.4f, 8f);
        var gm = new Material(Mats.Sh("Space/Planet")); gm.SetTexture("_MainTex", Mats.Tex("earth_daymap")); gm.SetTexture("_NightTex", Mats.Tex("earth_nightmap"));
        gm.SetVector("_SunDir", new Vector4(0.5f, 0.3f, -0.8f, 0)); gm.SetFloat("_Ambient", 0.12f); gm.SetFloat("_DetailFar", 0); gm.SetFloat("_Haze", 0.8f);
        foreach (var r in g.GetComponentsInChildren<Renderer>()) r.sharedMaterial = gm;
        globe.localScale = Vector3.one * 0.17f;
        // exit door (south wall)
        Box("ExitFrame", new Vector3(0, 2.2f, -L / 2 + 0.05f), new Vector3(4.6f, 4.4f, 0.15f), glow, false);
        var l1 = new GameObject("L1"); l1.transform.SetParent(root.transform, false); l1.transform.localPosition = new Vector3(0, 9, 0);
        var li = l1.AddComponent<Light>(); li.type = LightType.Point; li.range = 45; li.intensity = 3.2f; li.color = new Color(0.7f, 0.85f, 1f);
        var l2 = new GameObject("L2"); l2.transform.SetParent(root.transform, false); l2.transform.localPosition = new Vector3(0, 4, 6);
        var li2 = l2.AddComponent<Light>(); li2.type = LightType.Point; li2.range = 30; li2.intensity = 3f; li2.color = new Color(0.4f, 1f, 1f);
        var tm = new GameObject("Title"); tm.transform.SetParent(root.transform, false); tm.transform.localPosition = new Vector3(0, 11.2f, L / 2 - 0.3f); tm.transform.localRotation = Quaternion.Euler(0, 180, 0);
        var t = tm.AddComponent<TextMesh>(); t.text = "MISSION CONTROL  -  The Last Horizon"; t.fontSize = 60; t.characterSize = 0.25f; t.anchor = TextAnchor.MiddleCenter; t.color = new Color(0.6f, 1f, 1f);
        spawn = origin + new Vector3(0, 0.1f, -L / 2 + 3.5f); exitPos = origin + new Vector3(0, 0, -L / 2 + 1.2f);
    }

    void Box(string n, Vector3 p, Vector3 s, Material m, bool collider = true)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Cube); b.name = n; b.transform.SetParent(root.transform, false);
        b.transform.localPosition = p; b.transform.localScale = s; b.GetComponent<Renderer>().sharedMaterial = m;
        if (!collider) Destroy(b.GetComponent<Collider>());
    }

    public void SetActive(bool a) { root.SetActive(a); }
    void Update() { if (globe != null && root.activeSelf) globe.Rotate(0, -6f * Time.deltaTime, 0, Space.Self); }
}

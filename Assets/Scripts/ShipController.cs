using System;
using UnityEngine;

/// <summary>The player's ship "Aurora". Two physics regimes: Space (double precision, N-body gravity, floating origin) and
/// Surface (Rigidbody in a local flat world, 1 unit = 1 m).</summary>
public class ShipController : MonoBehaviour
{
    public static ShipController I;

    // ---- state
    public V3d pos, vel;                 // space state (world, sun-centred)
    public Quaternion rot = Quaternion.identity;
    public float hull = 100, shield = 100, energy = 100;
    public bool assist = true, supercruise, gearDown, lightsOn = true, inSurfaceMode, landed, alive = true, controlsEnabled = true;
    public float cruiseSpeed = 2500, throttleVis, heat, damageFlash;
    public Vector3 localVelHud;          // ship-local velocity relative to reference frame (space)
    public float groundDist = 999f;
    public string notice = ""; public float noticeT;

    // ---- parts
    public Transform visual;
    Transform[] gear = new Transform[3]; float gearT = 0f;
    Transform[] flames = new Transform[3]; Material flameMat; Light tailLight, headLight;
    GameObject colliders; public Rigidbody rb;
    Vector2 lookBuf; float rollBuf;
    public float sensitivity = 0.12f; public bool invertY = false;
    public float spaceScale = 0.35f;

    void Awake()
    {
        I = this;
        visual = new GameObject("Visual").transform; visual.SetParent(transform, false);
        var model = Mats.Model("Custom/ship_aurora", visual);
        model.transform.localRotation = Quaternion.identity;

        Vector3[] gp = { new Vector3(0, -0.55f, 4.0f), new Vector3(2.6f, -0.35f, -2.7f), new Vector3(-2.6f, -0.35f, -2.7f) };
        for (int i = 0; i < 3; i++)
        {
            var g = Mats.Model("Custom/ship_gear", visual); g.transform.localPosition = gp[i]; gear[i] = g.transform;
        }

        flameMat = Mats.Glow(new Color(0.25f, 0.85f, 1.6f), 0.55f, 2.6f, true, 0.5f);
        Vector3[] ep = { new Vector3(2.45f, 0.02f, -5.85f), new Vector3(-2.45f, 0.02f, -5.85f), new Vector3(0, 0.02f, -6.55f) };
        float[] er = { 0.45f, 0.45f, 0.58f };
        for (int i = 0; i < 3; i++)
        {
            var f = new GameObject("Flame" + i); f.transform.SetParent(visual, false); f.transform.localPosition = ep[i];
            var mf = f.AddComponent<MeshFilter>(); mf.sharedMesh = ConeMesh(er[i], 1f, 18);
            var mr = f.AddComponent<MeshRenderer>(); mr.sharedMaterial = flameMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            flames[i] = f.transform;
        }
        var tl = new GameObject("TailLight"); tl.transform.SetParent(visual, false); tl.transform.localPosition = new Vector3(0, 0.3f, -7.5f);
        tailLight = tl.AddComponent<Light>(); tailLight.type = LightType.Point; tailLight.color = new Color(0.3f, 0.8f, 1f); tailLight.range = 40; tailLight.intensity = 0; tailLight.shadows = LightShadows.None;
        var hl = new GameObject("HeadLight"); hl.transform.SetParent(visual, false); hl.transform.localPosition = new Vector3(0, 0.1f, 6.5f);
        headLight = hl.AddComponent<Light>(); headLight.type = LightType.Spot; headLight.spotAngle = 55; headLight.range = 220; headLight.intensity = 0; headLight.color = new Color(0.9f, 0.95f, 1f);

        // surface colliders (disabled in space)
        colliders = new GameObject("Colliders"); colliders.transform.SetParent(transform, false); colliders.layer = 2;   // Ignore Raycast layer: ground probes skip the ship itself
        var b1 = colliders.AddComponent<BoxCollider>(); b1.center = new Vector3(0, 0, 0.3f); b1.size = new Vector3(3.0f, 1.4f, 11.5f);
        var b2 = colliders.AddComponent<BoxCollider>(); b2.center = new Vector3(0, -0.1f, -1.8f); b2.size = new Vector3(14.6f, 0.35f, 6.5f);
        foreach (var p in gp) { var sc = colliders.AddComponent<SphereCollider>(); sc.center = p + Vector3.down * 0.75f; sc.radius = 0.5f; }
        rb = gameObject.AddComponent<Rigidbody>(); rb.mass = 6000; rb.useGravity = false; rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.freezeRotation = true;
        colliders.SetActive(false);
        SetSpaceMode();
    }

    static Mesh ConeMesh(float r, float len, int n)
    {
        var m = new Mesh(); var v = new Vector3[n + 2]; var t = new int[n * 6];
        for (int i = 0; i < n; i++) { float a = Mathf.PI * 2 * i / n; v[i] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0); }
        v[n] = new Vector3(0, 0, -len); v[n + 1] = Vector3.zero;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            t[i * 6] = i; t[i * 6 + 1] = n; t[i * 6 + 2] = j;          // outer
            t[i * 6 + 3] = i; t[i * 6 + 4] = j; t[i * 6 + 5] = n + 1;   // base (hidden)
        }
        m.vertices = v; m.triangles = t; m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    public void Notice(string s, float t = 3f) { notice = s; noticeT = t; }

    // =====================================================================  MODE SWITCHING
    public void SetSpaceMode()
    {
        inSurfaceMode = false; landed = false;
        rb.isKinematic = true; colliders.SetActive(false);
        visual.localScale = Vector3.one * spaceScale;
        transform.localScale = Vector3.one;
        SetGear(false, true);
    }

    public void SetSurfaceMode(Vector3 p, float yawDeg, Vector3 v)
    {
        inSurfaceMode = true; landed = false;
        visual.localScale = Vector3.one;
        colliders.SetActive(true); rb.isKinematic = false;
        transform.position = p; transform.rotation = Quaternion.Euler(0, yawDeg, 0);
        rb.position = p; rb.rotation = transform.rotation; rb.linearVelocity = v; rb.angularVelocity = Vector3.zero;
        SetGear(true, false);
    }

    public void SetGear(bool down, bool instant)
    {
        gearDown = down; if (instant) { gearT = down ? 1f : 0f; ApplyGear(); }
    }
    void ApplyGear() { foreach (var g in gear) { float s = Mathf.Max(0.001f, gearT); g.localScale = new Vector3(1, s, 1); } }

    public void ResetSpace(V3d p, V3d v, Quaternion q)
    {
        pos = p; vel = v; rot = q; hull = 100; shield = 100; energy = 100; alive = true; heat = 0; supercruise = false;
        lookBuf = Vector2.zero; transform.rotation = q; SetSpaceMode();
    }

    public void Damage(float amount)
    {
        if (amount <= 0 || !alive) return;
        damageFlash = 1f;
        float s = Mathf.Min(shield, amount); shield -= s; amount -= s * 0.8f;
        hull = Mathf.Max(0, hull - Mathf.Max(0, amount) * 0.5f);
        if (hull <= 0) alive = false;
    }

    // =====================================================================  SPACE FLIGHT
    public void SpaceStep(float dt, SolarSystem ss, V3d carry)
    {
        bool input = controlsEnabled && alive;
        // ---- rotation: mouse + arrows + Q/E (smoothed)
        Vector2 md = input && Cursor.lockState == CursorLockMode.Locked ? Inp.MouseDelta : Vector2.zero;
        float kx = Inp.Axis(K.Left, K.Right), ky = Inp.Axis(K.Down, K.Up);
        lookBuf += new Vector2(md.x * sensitivity + kx * 90f * dt, md.y * sensitivity * (invertY ? -1f : 1f) + ky * 70f * dt);
        rollBuf += input ? Inp.Axis(K.E, K.Q) * 100f * dt : 0f;
        float k = 1f - Mathf.Exp(-16f * dt);
        Vector2 ap = lookBuf * k; lookBuf -= ap; float ar = rollBuf * k; rollBuf -= ar;
        rot = rot * Quaternion.Euler(-ap.y, ap.x, ar);
        rot.Normalize();

        Body rb0 = ss.ReferenceBody(pos); V3d refVel = rb0.vel;
        pos = pos + carry;
        V3d vRel = vel - refVel;
        Vector3 tin = input ? new Vector3(Inp.Axis(K.A, K.D), (Inp.Held(K.Space) ? 1f : 0f) - (Inp.Held(K.Ctrl) ? 1f : 0f), Inp.Axis(K.S, K.W)) : Vector3.zero;
        bool boosting = input && Inp.Held(K.Shift) && energy > 2f && !supercruise && tin.z > 0;
        energy = Mathf.Clamp(energy + (boosting ? -28f : 9f) * dt, 0, 100);
        throttleVis = U.Damp(throttleVis, supercruise ? 1f : Mathf.Clamp01(Mathf.Abs(tin.z) + Mathf.Abs(tin.x) * 0.3f + Mathf.Abs(tin.y) * 0.3f) * (boosting ? 1.6f : 1f), 6f, dt);

        if (supercruise)
        {
            double alt; Body nb = ss.Nearest(pos, out alt);
            double drop = nb.radius * 0.6 + 90.0;
            if (alt < drop) { supercruise = false; Notice("SUPERCRUISE DISENGAGED - MASS LOCK (" + nb.name + ")", 4f); vel = refVel + (vel - refVel).norm * Math.Min((vel - refVel).mag, 300.0); }
            else
            {
                cruiseSpeed = Mathf.Clamp(cruiseSpeed + tin.z * 2500f * dt, 300f, 8000f);
                V3d target = refVel + new V3d(rot * Vector3.forward) * cruiseSpeed;
                V3d dv = target - vel; double m = dv.mag, lim = 3000.0 * dt;
                vel = m <= lim ? target : vel + dv * (lim / m);
                pos = pos + vel * dt;
                localVelHud = Quaternion.Inverse(rot) * (vel - refVel).f;
                return;
            }
        }

        float fwdA = 150f * (boosting ? 3.2f : 1f), sideA = 85f, vertA = 85f;
        Vector3 aLoc = new Vector3(tin.x * sideA, tin.y * vertA, tin.z * (tin.z > 0 ? fwdA : fwdA * 0.55f));
        Vector3 vLoc = Quaternion.Inverse(rot) * vRel.f;
        if (assist)
        {
            float[] lim = { sideA * 1.3f, vertA * 1.3f, fwdA * 1.1f };
            for (int i = 0; i < 3; i++) if (Mathf.Abs(tin[i]) < 0.05f) aLoc[i] += -Mathf.Clamp(vLoc[i] / Mathf.Max(dt, 0.001f), -lim[i], lim[i]);
            if (input && Inp.Held(K.X)) aLoc += -Vector3.ClampMagnitude(vLoc / Mathf.Max(dt, 0.001f), 400f);
            float cap = boosting ? 900f : 350f, sp = vLoc.magnitude;
            if (sp > cap) aLoc += -vLoc.normalized * Mathf.Min((sp - cap) * 3f, 600f);
        }
        V3d aT = new V3d(rot * aLoc);
        int n = Mathf.Clamp(Mathf.CeilToInt(dt / 0.01f), 1, 30); float h = dt / n;
        bool grav = !assist;
        for (int i = 0; i < n; i++)
        {
            V3d a = aT; if (grav) a = a + ss.Gravity(pos);
            vel = vel + a * h; pos = pos + vel * h;
        }
        localVelHud = Quaternion.Inverse(rot) * (vel - refVel).f;

        // ---- hazards: sun heat, re-entry heat
        double d2s = (pos - ss.sun.pos).mag - ss.sun.radius;
        float sunHeat = (float)Math.Max(0, 1.0 - d2s / (ss.sun.radius * 1.4));
        double altE = (pos - ss.earth.pos).mag - ss.earth.radius;
        double spE = (vel - ss.earth.vel).mag;
        float entry = altE < 9.0 ? (float)Math.Max(0, (spE - 90.0) / 260.0) * (float)(1.0 - altE / 9.0) : 0f;
        heat = U.Damp(heat, Mathf.Clamp01(Mathf.Max(sunHeat, entry * 2f)), 3f, dt);
        if (altE < 9.0 && !assist) vel = vel - (vel - ss.earth.vel) * (0.04 * (1.0 - altE / 9.0)) * dt;   // thin-air drag when coasting
        if (heat > 0.35f) { Damage((heat - 0.35f) * 9f * dt); if (noticeT <= 0) Notice(sunHeat > entry ? "THERMAL OVERLOAD - MOVE AWAY FROM THE SUN" : "RE-ENTRY HEAT - REDUCE SPEED", 1.5f); }
        shield = Mathf.Min(100, shield + (heat > 0.2f ? 0f : 4f) * dt);
    }

    // =====================================================================  SURFACE FLIGHT
    float yaw, pitchLean, rollLean;
    public void SurfaceStep(float dt, float gravity, bool atmosphere)
    {
        bool input = controlsEnabled && alive;
        Vector2 md = input && Cursor.lockState == CursorLockMode.Locked ? Inp.MouseDelta : Vector2.zero;
        float kx = Inp.Axis(K.Left, K.Right);
        yaw += md.x * sensitivity * 1.2f + kx * 80f * dt;
        Vector3 tin = input ? new Vector3(Inp.Axis(K.A, K.D), (Inp.Held(K.Space) ? 1f : 0f) - (Inp.Held(K.Ctrl) ? 1f : 0f), Inp.Axis(K.S, K.W)) : Vector3.zero;
        bool boost = input && Inp.Held(K.Shift) && energy > 2f;
        energy = Mathf.Clamp(energy + (boost ? -18f : 10f) * dt, 0, 100);
        float hMax = boost ? 160f : 48f;
        Quaternion yq = Quaternion.Euler(0, yaw, 0);
        Vector3 desH = (yq * Vector3.forward * tin.z + yq * Vector3.right * tin.x);
        if (desH.sqrMagnitude > 1) desH.Normalize(); desH *= hMax;
        Vector3 v = rb.linearVelocity;
        Vector3 vh = new Vector3(v.x, 0, v.z);
        float acc = (desH.sqrMagnitude > 0.01f ? 38f : 22f) * (boost ? 3f : 1f);
        vh = Vector3.MoveTowards(vh, desH, acc * dt);
        float vyT = tin.y * (boost ? 70f : 22f);
        if (landed && tin.y <= 0) vyT = -2f;
        float vy = v.y;
        if (assist || tin.y != 0) vy = Mathf.MoveTowards(vy, vyT, 28f * dt);
        else vy -= gravity * dt;
        rb.linearVelocity = new Vector3(vh.x, vy, vh.z);
        pitchLean = U.Damp(pitchLean, -tin.z * 11f + Mathf.Clamp(vy * -0.12f, -10f, 10f) * 0.4f, 5f, dt);
        rollLean = U.Damp(rollLean, -tin.x * 16f - md.x * 0.6f, 5f, dt);
        rb.MoveRotation(yq * Quaternion.Euler(-pitchLean, 0, rollLean));
        throttleVis = U.Damp(throttleVis, landed ? 0.05f : Mathf.Clamp01(0.35f + Mathf.Abs(tin.z) * 0.4f + Mathf.Abs(tin.x) * 0.2f + Mathf.Abs(tin.y) * 0.4f) * (boost ? 1.5f : 1f), 5f, dt);
        shield = Mathf.Min(100, shield + 4f * dt);

        RaycastHit hit;
        groundDist = Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 3000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? hit.distance - 0.5f : 999f;
        // gear: auto-deploy low, hold with G
        bool want = gearDown;
        if (groundDist < 70f && vy < 1f) want = true;
        if (Inp.Down(K.G) && input) { gearDown = !gearDown; want = gearDown; }
        gearDown = want;
        landed = groundDist < 2.2f && rb.linearVelocity.magnitude < 1.6f && tin.y <= 0;
    }

    public void SetYaw(float y) { yaw = y; }
    public float Yaw { get { return yaw; } }

    void OnCollisionEnter(Collision c)
    {
        if (!inSurfaceMode) return;
        float s = c.relativeVelocity.magnitude;
        if (s > 12f) { Damage((s - 12f) * 4f); Notice("IMPACT!", 1.2f); }
    }

    // =====================================================================  VISUAL UPDATE (both modes)
    void Update()
    {
        if (noticeT > 0) noticeT -= Time.deltaTime;
        damageFlash = Mathf.MoveTowards(damageFlash, 0, Time.deltaTime * 2f);
        gearT = Mathf.MoveTowards(gearT, gearDown && inSurfaceMode ? 1f : 0f, Time.deltaTime * 0.8f);
        ApplyGear();
        float th = alive ? throttleVis : 0f;
        for (int i = 0; i < 3; i++)
        {
            float flick = 1f + Mathf.Sin(Time.time * 41f + i * 2f) * 0.08f;
            flames[i].localScale = new Vector3(1, 1, Mathf.Lerp(0.15f, 2.6f, Mathf.Clamp01(th * 0.8f)) * flick * (supercruise ? 2.4f : 1f));
            flames[i].gameObject.SetActive(alive);
        }
        tailLight.intensity = th * 4f * (inSurfaceMode ? 6f : 1f);
        if (Inp.Down(K.L) && controlsEnabled) lightsOn = !lightsOn;
        headLight.intensity = lightsOn && inSurfaceMode ? 6f : 0f;
        if (!inSurfaceMode) { transform.rotation = rot; }
    }
}

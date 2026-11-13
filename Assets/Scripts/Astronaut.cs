using UnityEngine;

/// <summary>Procedurally animated astronaut built from the generated OBJ parts. Walk/run/jump, low-gravity bounding, jetpack.</summary>
public class Astronaut : MonoBehaviour
{
    public static Astronaut I;
    public CharacterController cc;
    public float gravity = 9.81f;
    public bool lowG;
    public bool firstPerson;
    public float yaw, pitch;
    public Transform eye, body;
    Transform torso, head, armL, armR, legL, legR, pack;
    Vector3 velocity; float walkPhase; float speedSm; public bool grounded; public float jetFuel = 100f; public bool jetting;
    Light flash;
    public bool flashlightOn;
    public float walkSpeed = 3.4f, runSpeed = 7.5f;

    void Awake()
    {
        I = this;
        cc = gameObject.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.32f; cc.center = new Vector3(0, 0.9f, 0); cc.stepOffset = 0.4f; cc.skinWidth = 0.04f;
        body = new GameObject("Body").transform; body.SetParent(transform, false);
        torso = Part("astro_torso", new Vector3(0, 0.92f, 0), body);
        head = Part("astro_head", new Vector3(0, 0.62f, 0), torso);
        pack = Part("astro_pack", Vector3.zero, torso);
        armL = Part("astro_arm", new Vector3(-0.35f, 0.56f, 0), torso);
        armR = Part("astro_arm", new Vector3(0.35f, 0.56f, 0), torso);
        legL = Part("astro_leg", new Vector3(-0.14f, 0f, 0), torso);
        legR = Part("astro_leg", new Vector3(0.14f, 0f, 0), torso);
        // legs hang from hip; they are children of torso for convenience, so counter the torso pitch in Animate()
        eye = new GameObject("Eye").transform; eye.SetParent(transform, false); eye.localPosition = new Vector3(0, 1.62f, 0.12f);
        var fl = new GameObject("Flashlight"); fl.transform.SetParent(head, false); fl.transform.localPosition = new Vector3(0, 0.2f, 0.2f);
        flash = fl.AddComponent<Light>(); flash.type = LightType.Spot; flash.spotAngle = 60; flash.range = 40; flash.intensity = 0; flash.shadows = LightShadows.None;
    }

    Transform Part(string n, Vector3 localPos, Transform parent)
    {
        var g = Mats.Model("Custom/" + n, parent); g.transform.localPosition = localPos; return g.transform;
    }

    public void Spawn(Vector3 p, float y, float g, bool low)
    {
        cc.enabled = false; transform.position = p; yaw = y; transform.rotation = Quaternion.Euler(0, y, 0); cc.enabled = true;
        gravity = g; lowG = low; velocity = Vector3.zero; jetFuel = 100;
    }

    public void Tick(float dt, bool input, Transform camTransform)
    {
        Vector2 md = input && Cursor.lockState == CursorLockMode.Locked ? Inp.MouseDelta : Vector2.zero;
        yaw += md.x * 0.12f; pitch = Mathf.Clamp(pitch - md.y * 0.12f, -80f, 80f);

        Vector3 mv = input ? new Vector3(Inp.Axis(K.A, K.D), 0, Inp.Axis(K.S, K.W)) : Vector3.zero;
        if (mv.sqrMagnitude > 1) mv.Normalize();
        bool sprint = input && Inp.Held(K.Shift);
        float spd = (sprint ? runSpeed : walkSpeed) * (lowG ? 1.25f : 1f);
        Quaternion yq = Quaternion.Euler(0, yaw, 0);
        Vector3 wish = yq * mv * spd;

        grounded = cc.isGrounded;
        Vector3 hv = new Vector3(velocity.x, 0, velocity.z);
        float ctl = grounded ? 14f : (lowG ? 2.2f : 3f);
        hv = Vector3.MoveTowards(hv, wish, ctl * dt * (lowG && !grounded ? 1f : 1f) * 2f);
        velocity.x = hv.x; velocity.z = hv.z;

        if (grounded && velocity.y < 0) velocity.y = -2f;
        if (input && grounded && Inp.Down(K.Space)) velocity.y = lowG ? 4.2f : 5.2f;
        // jetpack (hold Space in the air)
        jetting = input && !grounded && Inp.Held(K.Space) && jetFuel > 0 && velocity.y < (lowG ? 9f : 7f);
        if (jetting) { velocity.y += (lowG ? 9f : 16f) * dt; jetFuel -= 22f * dt; }
        else if (grounded) jetFuel = Mathf.Min(100, jetFuel + 35f * dt);
        velocity.y -= gravity * dt;
        cc.Move(velocity * dt);
        if (cc.isGrounded && velocity.y < 0) velocity.y = Mathf.Max(velocity.y, -2f);

        transform.rotation = Quaternion.Euler(0, yaw, 0);
        if (input && Inp.Down(K.F)) { flashlightOn = !flashlightOn; flash.intensity = flashlightOn ? 5f : 0f; }

        // ---- procedural animation
        float hs = new Vector3(cc.velocity.x, 0, cc.velocity.z).magnitude;
        speedSm = U.Damp(speedSm, hs, 10f, dt);
        walkPhase += speedSm * dt * (lowG ? 1.1f : 1.9f);
        float amp = Mathf.Clamp01(speedSm / 4f) * (sprint ? 45f : 32f);
        float s = Mathf.Sin(walkPhase * 2f);
        bool air = !grounded;
        float airPose = air ? 1f : 0f;
        torso.localRotation = U.Damp(torso.localRotation, Quaternion.Euler(Mathf.Clamp01(speedSm / 7f) * 9f + airPose * 6f, 0, s * amp * 0.06f), 12f, dt);
        torso.localPosition = new Vector3(0, 0.92f + Mathf.Abs(Mathf.Cos(walkPhase * 2f)) * 0.04f * Mathf.Clamp01(speedSm / 4f) * (lowG ? 2.5f : 1f), 0);
        // legs are children of the torso: counter-rotate by the torso pitch so they swing from the hips
        Quaternion inv = Quaternion.Inverse(torso.localRotation);
        legL.localRotation = inv * Quaternion.Euler(air ? 25f : s * amp, 0, 0);
        legR.localRotation = inv * Quaternion.Euler(air ? -15f : -s * amp, 0, 0);
        legL.localPosition = inv * new Vector3(-0.14f, 0, 0) + new Vector3(0, -0.0f, 0);
        legR.localPosition = inv * new Vector3(0.14f, 0, 0);
        armL.localRotation = Quaternion.Euler(air ? -60f : -s * amp * 0.9f, 0, 8f);
        armR.localRotation = Quaternion.Euler(air ? -60f : s * amp * 0.9f, 0, -8f);
        head.localRotation = Quaternion.Euler(Mathf.Clamp(pitch * 0.6f, -35f, 35f) - torso.localEulerAngles.x * 0.0f, 0, 0);
        if (firstPerson) { eye.localRotation = Quaternion.Euler(pitch, 0, 0); }
        // hide the head in first person so the helmet doesn't block the view
        head.gameObject.SetActive(!firstPerson);
        pack.gameObject.SetActive(true);
        flash.transform.rotation = Quaternion.LookRotation(yq * Quaternion.Euler(pitch, 0, 0) * Vector3.forward);
    }

    public void SetVisible(bool v) { body.gameObject.SetActive(v); }
}

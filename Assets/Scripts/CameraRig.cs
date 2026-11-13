using UnityEngine;

public class CameraRig : MonoBehaviour
{
    public Camera cam;
    public enum View { Chase, Cockpit, Far }
    public View view = View.Chase;
    Quaternion smoothRot = Quaternion.identity; Vector3 smoothPos;
    float freeYaw, freePitch, zoom = 1f, fov = 62f, shake;
    public bool firstPersonAstro;
    float astroDist = 4.2f; bool inited;

    public void Init(Camera c) { cam = c; fov = 62f; }

    public void CycleView() { view = (View)(((int)view + 1) % 3); }

    // ------------------------------------------------------------ SPACE: ship sits at the render origin
    public void SpaceUpdate(ShipController ship, float dt, float speedFrac)
    {
        var t = cam.transform;
        bool look = Inp.MouseHeld(1) && Cursor.lockState == CursorLockMode.Locked;
        if (look) { Vector2 md = Inp.MouseDelta * 0.15f; freeYaw += md.x; freePitch = Mathf.Clamp(freePitch - md.y, -80f, 80f); }
        else { freeYaw = U.Damp(freeYaw, 0f, 6f, dt); freePitch = U.Damp(freePitch, 0f, 6f, dt); }
        zoom = Mathf.Clamp(zoom - Inp.Scroll * 0.1f, 0.5f, 3f);
        if (!inited) { smoothRot = ship.rot; inited = true; }
        smoothRot = U.Damp(smoothRot, ship.rot, view == View.Cockpit ? 60f : 7f, dt);
        Quaternion q = smoothRot * Quaternion.Euler(freePitch, freeYaw, 0);
        float s = ship.spaceScale;
        Vector3 off; float k = s / 0.35f;
        switch (view)
        {
            case View.Cockpit: off = new Vector3(0, 0.55f * s, 2.2f * s); break;
            case View.Far: off = new Vector3(0, 3.2f, -17f) * zoom * k; break;
            default: off = new Vector3(0, 1.5f, -8.6f) * zoom * k; break;
        }
        t.rotation = view == View.Cockpit && !look ? ship.rot : q;
        t.position = ship.transform.position + (view == View.Cockpit ? ship.rot : q) * off;
        shake = Mathf.MoveTowards(shake, 0f, dt * 2f);
        if (ship.damageFlash > 0.4f) shake = 0.12f;
        t.position += Random.insideUnitSphere * shake * 0.3f;
        float tf = 62f + speedFrac * 22f + (ship.supercruise ? 26f : 0f);
        fov = U.Damp(fov, tf, 3f, dt); cam.fieldOfView = fov;
        cam.nearClipPlane = view == View.Cockpit ? 0.04f : 0.1f;
    }

    // ------------------------------------------------------------ SURFACE: chase the ship
    Vector3 shipDir = Vector3.forward;
    public void SurfaceShipUpdate(ShipController ship, float dt)
    {
        var t = cam.transform;
        bool look = Inp.MouseHeld(1) && Cursor.lockState == CursorLockMode.Locked;
        if (look) { Vector2 md = Inp.MouseDelta * 0.15f; freeYaw += md.x; freePitch = Mathf.Clamp(freePitch - md.y, -60f, 70f); }
        else { freeYaw = U.Damp(freeYaw, 0, 4f, dt); freePitch = U.Damp(freePitch, 0, 4f, dt); }
        zoom = Mathf.Clamp(zoom - Inp.Scroll * 0.1f, 0.5f, 2.5f);
        Quaternion yq = Quaternion.Euler(14f + freePitch, ship.Yaw + freeYaw, 0);
        Vector3 target = ship.transform.position + Vector3.up * 2.5f;
        Vector3 pos = target + yq * new Vector3(0, 0, -(view == View.Far ? 55f : 26f) * zoom);
        if (view == View.Cockpit) { pos = ship.transform.position + ship.transform.rotation * new Vector3(0, 0.75f, 2.8f); t.rotation = ship.transform.rotation * Quaternion.Euler(freePitch, freeYaw, 0); t.position = pos; }
        else { t.position = pos; t.rotation = Quaternion.LookRotation(target - pos); }
        fov = U.Damp(fov, 66f, 3f, dt); cam.fieldOfView = fov; cam.nearClipPlane = 0.1f;
    }

    // ------------------------------------------------------------ SURFACE: astronaut
    public void AstroUpdate(Astronaut a, float dt)
    {
        var t = cam.transform;
        if (Inp.Down(K.V)) a.firstPerson = !a.firstPerson;
        if (a.firstPerson)
        {
            t.position = a.eye.position; t.rotation = Quaternion.Euler(a.pitch, a.yaw, 0); cam.nearClipPlane = 0.05f;
        }
        else
        {
            astroDist = Mathf.Clamp(astroDist - Inp.Scroll * 0.4f, 1.8f, 9f);
            Vector3 pivot = a.transform.position + Vector3.up * 1.55f;
            Quaternion q = Quaternion.Euler(a.pitch, a.yaw, 0);
            Vector3 dir = q * Vector3.back; float d = astroDist;
            RaycastHit hit;
            if (Physics.SphereCast(pivot, 0.25f, dir, out hit, d, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) d = Mathf.Max(0.6f, hit.distance - 0.05f);
            Vector3 p = pivot + dir * d + q * (Vector3.right * 0.45f);
            t.position = Vector3.Lerp(t.position, p, 1f - Mathf.Exp(-22f * dt)); t.rotation = Quaternion.Euler(a.pitch, a.yaw, 0); cam.nearClipPlane = 0.1f;
        }
        fov = U.Damp(fov, Inp.Held(K.Shift) ? 72f : 66f, 4f, dt); cam.fieldOfView = fov;
    }

    public void Fixed(Vector3 pos, Quaternion rot) { cam.transform.SetPositionAndRotation(pos, rot); }
    public void Reset() { inited = false; freeYaw = freePitch = 0; }
}

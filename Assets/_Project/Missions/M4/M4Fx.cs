using UnityEngine;

namespace LastSignal.M4
{
    /// <summary>Code-built particle effects (built-in Particle System, no VFX Graph package needed).</summary>
    public static class M4Fx
    {
        static ParticleSystem Make(string name, Transform parent, Material mat, bool world)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = world ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.playOnAwake = true;
            main.loop = true;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        static Gradient Grad(Color a, Color b, Color c, float a0, float a1, float a2)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.35f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(a0, 0f), new GradientAlphaKey(a1, 0.3f), new GradientAlphaKey(a2, 1f) });
            return g;
        }

        /// <summary>Main engine plume pointing down from the socket. Drive with SetRate(ps, throttle * max).</summary>
        public static ParticleSystem EnginePlume(Transform socket)
        {
            var ps = Make("Engine Plume", socket, M4Materials.Particle("Flame", true), false);
            ps.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // emit along -Y
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(16f, 22f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.85f, 1f));
            main.maxParticles = 400;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 7f; sh.radius = 0.35f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Grad(new Color(0.8f, 0.9f, 1f), new Color(1f, 0.6f, 0.25f), new Color(0.6f, 0.3f, 0.2f), 0.9f, 0.6f, 0f));
            var sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.7f, 1f, 2.4f));
            SetRate(ps, 0f);
            ps.Play();
            return ps;
        }

        /// <summary>Dust blown out radially where the plume hits the ground. Place at the ground point every frame.</summary>
        public static ParticleSystem GroundDust(Transform parent)
        {
            var ps = Make("Ground Dust", parent, M4Materials.Particle("Dust", false), true);
            ps.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // circle in the XZ plane
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 3.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 13f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 3.6f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.42f, 0.32f, 0.55f), new Color(0.42f, 0.32f, 0.25f, 0.4f));
            main.gravityModifier = -0.02f;
            main.maxParticles = 900;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.8f; sh.radiusThickness = 0.2f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, Color.white, 0f, 0.8f, 0f));
            var sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.4f, 1f, 2.2f));
            var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.limit = new ParticleSystem.MinMaxCurve(2f); lim.dampen = 0.08f;
            SetRate(ps, 0f);
            ps.Play();
            return ps;
        }

        /// <summary>Slow floating dust motes around the camera (environmental depth cue).</summary>
        public static ParticleSystem AmbientDust(Transform cam)
        {
            var ps = Make("M4 Ambient Dust", cam, M4Materials.Particle("Mote", false), true);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.9f, 0.75f, 0.6f, 0.35f));
            main.maxParticles = 500;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(36f, 14f, 36f);
            var vol = ps.velocityOverLifetime; vol.enabled = true; vol.space = ParticleSystemSimulationSpace.World;
            vol.x = new ParticleSystem.MinMaxCurve(0.35f); vol.y = new ParticleSystem.MinMaxCurve(0.02f); vol.z = new ParticleSystem.MinMaxCurve(0.12f);
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, Color.white, 0f, 1f, 0f));
            SetRate(ps, 45f);
            ps.Play();
            return ps;
        }

        /// <summary>One-shot puff (touchdown, footsteps).</summary>
        public static void Puff(ParticleSystem dust, Vector3 at, int count)
        {
            if (dust == null) return;
            var p = new ParticleSystem.EmitParams { position = at, applyShapeToPosition = true };
            dust.Emit(p, count);
        }

        public static void SetRate(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            var em = ps.emission;
            em.rateOverTime = rate;
        }
    }
}

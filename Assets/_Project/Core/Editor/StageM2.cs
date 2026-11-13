using UnityEngine;
using static LastSignal.EditorTools.StageKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// M2 set: the launch pad at dusk with the father's nuclear-powered ship in its gantry.
    /// Names under "Ship/Systems" (LifeSupport, Comms, Navigation, Radio) are lit by M2PreflightMission.
    /// </summary>
    public static class StageM2
    {
        public static void Build()
        {
            var root = new GameObject("M2 Stage").transform;
            Pad(root);
            Ship(root);
            Gantry(root);
            Lighting(root);
            Camera(root);
        }

        static void Pad(Transform root)
        {
            var env = Group("Pad", root, Vector3.zero);
            Prim(PrimitiveType.Plane, "Desert", env, new Vector3(0, -0.06f, 0), Vector3.one * 80f, "st_sand");
            Cyl("PadDeck", env, new Vector3(0, 0.4f, 0), 46f, 0.8f, "st_floor");
            Cyl("PadRing", env, new Vector3(0, 0.81f, 0), 15f, 0.02f, "st_hazard");
            Cyl("PadInner", env, new Vector3(0, 0.83f, 0), 14.2f, 0.02f, "st_floor");
            Cube("FlameTrench", env, new Vector3(0, 0.42f, 12f), new Vector3(7f, 0.8f, 10f), "st_scorch");
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(a) * 21f, 0.95f, Mathf.Sin(a) * 21f);
                Cube("EdgeLight" + i, env, p, new Vector3(0.5f, 0.2f, 0.5f), i % 2 == 0 ? "st_glow_amber" : "st_glow_cyan");
            }
            // Comms dish on the pad edge: the long-range radio the emergency test cuts.
            var dishBase = Group("CommsDish", env, new Vector3(-17f, 0.8f, 9f));
            Cyl("Mast", dishBase, new Vector3(0, 2f, 0), 0.5f, 4f, "st_steel");
            MeshObj(Shape("dish"), "Dish", dishBase, new Vector3(0, 4.2f, 0), Vector3.one * 7f, "st_ceramic", new Vector3(-55, 30, 0));
            Ball("Feed", dishBase, new Vector3(-0.9f, 5.8f, -1.3f), 0.35f, "st_glow_cyan");

            var h1 = SetupScenes.Model("Buildings/warehouse_01.obj", "Assembly Hall", new Vector3(-55, 0, 70), 1.4f, new Vector3(0, 20, 0));
            var tower = SetupScenes.Model("Buildings/tower_antenna.obj", "Telemetry Tower", new Vector3(38, 0, 52), 1.6f);
            Recolor(h1, "st_hangar"); Recolor(tower, "st_steel_dark");
            h1.transform.SetParent(env, true); tower.transform.SetParent(env, true);
            var beacon = Ball("TowerBeacon", env, new Vector3(38, 49.3f, 52), 1f, "st_glow_red");
            var blink = env.gameObject.AddComponent<LightFlicker>();
            blink.mode = LightFlicker.Mode.Blink; blink.speed = 0.6f; blink.blinkTarget = beacon;

            float[] mx = { -260, -150, -40, 80, 190, 300 };
            float[] ms = { 120, 90, 150, 110, 140, 90 };
            for (int i = 0; i < mx.Length; i++)
                MeshObj(Shape("cone"), "Mesa" + i, env, new Vector3(mx[i], -2f, 300 + (i % 2) * 60), new Vector3(ms[i] * 1.8f, ms[i] * 0.3f, ms[i]), "st_mountain");
        }

        static void Ship(Transform root)
        {
            var ship = Group("Ship", root, new Vector3(0, 0.82f, 0));
            // Landing legs and engine section.
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + i * 90f) * Mathf.Deg2Rad;
                Vector3 foot = new Vector3(Mathf.Cos(a) * 4.6f, 0.15f, Mathf.Sin(a) * 4.6f);
                Vector3 hip = new Vector3(Mathf.Cos(a) * 2.2f, 5.5f, Mathf.Sin(a) * 2.2f);
                Beam("Leg" + i, ship, foot, hip, 0.35f, "st_steel");
                Cyl("Foot" + i, ship, foot, 1.1f, 0.3f, "st_steel_dark");
            }
            MeshObj(Shape("nozzle"), "Nozzle", ship, new Vector3(0, 3.1f, 0), new Vector3(7f, 5f, 7f), "st_nozzle");
            Cyl("Thrust", ship, new Vector3(0, 4.6f, 0), 2.6f, 1.6f, "st_steel_dark");

            // Reactor, shadow shield and radiators: the nuclear-powered stage (fiction).
            Cyl("Reactor", ship, new Vector3(0, 6.6f, 0), 3.2f, 2.4f, "st_steel");
            Cyl("ReactorGlowRing", ship, new Vector3(0, 6.6f, 0), 3.26f, 0.18f, "st_glow_cyan");
            MeshObj(Shape("cone"), "Shield", ship, new Vector3(0, 7.8f, 0), new Vector3(5.2f, 1.4f, 5.2f), "st_steel_dark");
            Cyl("Truss", ship, new Vector3(0, 11f, 0), 1.2f, 5f, "st_steel_dark");
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f;
                var fin = Group("Radiator" + i, ship, new Vector3(0, 11f, 0));
                fin.localEulerAngles = new Vector3(0, a, 0);
                Cube("Panel", fin, new Vector3(2.6f, 0, 0), new Vector3(3.6f, 4.4f, 0.08f), "st_foil");
                Cube("Spine", fin, new Vector3(0.9f, 0, 0), new Vector3(0.25f, 4.6f, 0.18f), "st_steel");
            }

            // Propellant tank, habitat and crew capsule.
            MeshObj(Shape("tank"), "Tank", ship, new Vector3(0, 18.4f, 0), new Vector3(4.6f, 4.8f, 4.6f), "st_ceramic");
            MeshObj(Shape("ring"), "TankBand", ship, new Vector3(0, 18.4f, 0), new Vector3(4.68f, 0.5f, 4.68f), "st_rust");
            Cyl("Habitat", ship, new Vector3(0, 25.2f, 0), 4.2f, 3.8f, "st_ceramic");
            Cyl("HabBand", ship, new Vector3(0, 23.6f, 0), 4.26f, 0.35f, "st_hazard");
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                Cube("Window" + i, ship, new Vector3(Mathf.Cos(a) * 2.09f, 25.9f, Mathf.Sin(a) * 2.09f), new Vector3(0.6f, 0.35f, 0.6f), "st_glow_warm",
                    new Vector3(0, -i * 60f, 0));
            }
            MeshObj(Shape("cone"), "Capsule", ship, new Vector3(0, 27.1f, 0), new Vector3(4.2f, 4.2f, 4.2f), "st_ceramic");
            Cyl("NoseCap", ship, new Vector3(0, 31.2f, 0), 0.5f, 0.3f, "st_steel");

            // Subsystem status lamps. M2 turns them from amber to green as checks pass.
            var sys = Group("Systems", ship, Vector3.zero);
            SystemLamp(sys, "LifeSupport", new Vector3(0, 24.4f, -2.12f));
            SystemLamp(sys, "Comms", new Vector3(1.5f, 24.4f, -1.5f));
            SystemLamp(sys, "Navigation", new Vector3(-1.5f, 24.4f, -1.5f));
            // High-gain antenna on the habitat: the radio the emergency test cuts.
            var radio = Group("Radio", sys, new Vector3(2.2f, 28f, 0));
            Cyl("Boom", radio, new Vector3(0.6f, 0, 0), 0.12f, 1.4f, "st_steel", new Vector3(0, 0, 90));
            MeshObj(Shape("dish"), "Dish", radio, new Vector3(1.4f, 0, 0), Vector3.one * 2.2f, "st_ceramic", new Vector3(0, 0, -90));
            Ball("Live", radio, new Vector3(1.3f, 0, 0), 0.25f, "st_glow_cyan");
        }

        static void SystemLamp(Transform sys, string name, Vector3 pos)
        {
            var g = Group(name, sys, pos);
            Cube("Pending", g, Vector3.zero, new Vector3(0.7f, 0.25f, 0.12f), "st_glow_amber");
            Cube("Pass", g, Vector3.zero, new Vector3(0.7f, 0.25f, 0.12f), "st_glow_green").SetActive(false);
        }

        static void Gantry(Transform root)
        {
            var g = Group("Gantry", root, new Vector3(-6.4f, 0.82f, 0));
            const float w = 3f, hgt = 34f;
            foreach (float x in new[] { -w / 2, w / 2 })
                foreach (float z in new[] { -w / 2, w / 2 })
                    Beam("Column", g, new Vector3(x, 0, z), new Vector3(x, hgt, z), 0.3f, "st_rust");
            for (float y = 3f; y < hgt; y += 3.4f)
            {
                Beam("Tie", g, new Vector3(-w / 2, y, -w / 2), new Vector3(w / 2, y, -w / 2), 0.18f, "st_rust");
                Beam("Tie", g, new Vector3(-w / 2, y, w / 2), new Vector3(w / 2, y, w / 2), 0.18f, "st_rust");
                Beam("Tie", g, new Vector3(-w / 2, y, -w / 2), new Vector3(-w / 2, y, w / 2), 0.18f, "st_rust");
                Beam("Tie", g, new Vector3(w / 2, y, -w / 2), new Vector3(w / 2, y, w / 2), 0.18f, "st_rust");
                Beam("X", g, new Vector3(-w / 2, y, -w / 2), new Vector3(w / 2, y + 3.4f, -w / 2), 0.1f, "st_steel_dark");
            }
            // Access arms to the habitat and the tank.
            Cube("CrewArm", g, new Vector3(3.2f, 24.4f, 0), new Vector3(4.2f, 0.5f, 1.4f), "st_steel_dark");
            Cube("CrewArmLight", g, new Vector3(3.2f, 24.1f, -0.72f), new Vector3(3.8f, 0.08f, 0.04f), "st_glow_warm");
            Cube("FuelArm", g, new Vector3(3.3f, 17f, 0), new Vector3(4.4f, 0.4f, 0.9f), "st_steel_dark");
            var top = Ball("Beacon", g, new Vector3(0, hgt + 0.4f, 0), 0.6f, "st_glow_red");
            var blink = g.gameObject.AddComponent<LightFlicker>();
            blink.mode = LightFlicker.Mode.Blink; blink.speed = 0.75f; blink.phase = 0.4f; blink.blinkTarget = top;
        }

        static void Lighting(Transform root)
        {
            var rig = Group("Lighting", root, Vector3.zero);
            var sun = SetupScenes.AddSun(new Vector3(140, 30, 600), Vector3.zero, 1.4f, Palette.Horizon);
            sun.transform.SetParent(rig, true);
            SetupScenes.DuskAtmosphere(0.0028f);
            // Pad floodlights: warm from the front left, cool rim from behind, and a wash up the hull.
            Spot("FloodA", rig, new Vector3(-26f, 14f, -26f), new Vector3(0, 14f, 0), Palette.LampWarm, 3000f, 70f, 34f, true);
            Spot("FloodB", rig, new Vector3(24f, 10f, -20f), new Vector3(0, 20f, 0), Palette.KeyCool, 3400f, 60f, 32f, false);
            Spot("Rim", rig, new Vector3(10f, 26f, 26f), new Vector3(0, 16f, 0), Palette.KeyCool, 2600f, 60f, 40f, false);
            Point("PadGlow", rig, new Vector3(0, 2f, -6f), Palette.LampWarm, 40f, 16f);
            foreach (var p in new[] { new Vector3(-26f, 0.8f, -26f), new Vector3(24f, 0.8f, -20f) })
            {
                Cube("FloodMast", rig, p + new Vector3(0, 6.5f, 0), new Vector3(0.4f, 13f, 0.4f), "st_steel_dark");
                Cube("FloodHead", rig, p + new Vector3(0, 13.2f, 0), new Vector3(2.2f, 1.1f, 0.4f), "st_glow_warm");
            }
        }

        static void Camera(Transform root)
        {
            var cam = SetupScenes.AddStageCamera(40f);
            var rig = cam.gameObject.AddComponent<CameraRig>();
            rig.shots.Add(new CameraRig.Shot { name = "wide", position = new Vector3(14f, 7f, -48f), lookAt = new Vector3(-1f, 15f, 0), fov = 42f, orbit = 0.6f });
            rig.shots.Add(new CameraRig.Shot { name = "ship", position = new Vector3(9f, 25f, -16f), lookAt = new Vector3(0, 24.6f, 0), fov = 38f });
            rig.shots.Add(new CameraRig.Shot { name = "comms", position = new Vector3(-8f, 6.5f, -10f), lookAt = new Vector3(-17f, 4.5f, 9f), fov = 40f });
            rig.startShot = "wide";
            rig.sway = 0.12f;
        }
    }
}

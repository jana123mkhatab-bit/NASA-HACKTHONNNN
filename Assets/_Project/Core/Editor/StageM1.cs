using UnityEngine;
using UnityEngine.Rendering;
using static LastSignal.EditorTools.StageKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// M1 set: the ground test range at dusk. A rocket sled on a rail carries the test frame; the
    /// recovered fragment sits on a bench to the left and the test archive console to the right.
    /// Object names under "Sled/Modules" and the camera shots are read by M1GroundMission.
    /// </summary>
    public static class StageM1
    {
        public static void Build()
        {
            var root = new GameObject("M1 Stage").transform;
            Range(root);
            Sled(root);
            FragmentBench(root);
            ArchiveConsole(root);
            Lighting(root);
            Camera(root);
        }

        static void Range(Transform root)
        {
            var env = Group("Range", root, Vector3.zero);
            Prim(PrimitiveType.Plane, "Desert", env, new Vector3(0, -0.06f, 0), Vector3.one * 80f, "st_sand");
            Cube("Apron", env, new Vector3(24, -0.1f, 1), new Vector3(96, 0.2f, 18), "st_floor");
            Cube("TrackBed", env, new Vector3(26, 0.05f, 0), new Vector3(70, 0.1f, 2.3f), "st_track");
            Cube("RailNear", env, new Vector3(26, 0.15f, -0.55f), new Vector3(70, 0.1f, 0.08f), "st_steel");
            Cube("RailFar", env, new Vector3(26, 0.15f, 0.55f), new Vector3(70, 0.1f, 0.08f), "st_steel");
            Cube("HazardNear", env, new Vector3(26, 0.004f, -1.55f), new Vector3(70, 0.012f, 0.14f), "st_hazard");
            Cube("HazardFar", env, new Vector3(26, 0.004f, 1.55f), new Vector3(70, 0.012f, 0.14f), "st_hazard");
            for (int i = 0; i < 35; i++)
                Cube("Sleeper" + i, env, new Vector3(-8 + i * 2f, 0.105f, 0), new Vector3(0.24f, 0.03f, 1.7f), "st_steel_dark");

            // Distance posts along the far side: depth at rest, speed during the run.
            for (int i = 0; i <= 11; i++)
            {
                float x = i * 5f;
                Cube("Post" + i, env, new Vector3(x, 0.5f, 2.1f), new Vector3(0.08f, 1f, 0.08f), "st_steel_dark");
                Cube("PostCap" + i, env, new Vector3(x, 1.03f, 2.1f), new Vector3(0.12f, 0.07f, 0.12f), i % 2 == 0 ? "st_glow_cyan" : "st_glow_amber");
            }

            // Blast wall behind the sled and the water brake at the far end.
            Cube("BlastWall", env, new Vector3(-6.5f, 1.6f, 0), new Vector3(0.7f, 3.2f, 6.5f), "st_floor");
            Cube("BlastStripe", env, new Vector3(-6.13f, 0.5f, 0), new Vector3(0.02f, 0.35f, 6.5f), "st_hazard");
            Cube("Brake", env, new Vector3(57, 0.5f, 0), new Vector3(5f, 1f, 2.6f), "st_steel_dark");

            // Control bunker with a lit window strip.
            Cube("Bunker", env, new Vector3(9, 1.6f, 9.5f), new Vector3(8, 3.2f, 4.5f), "st_hangar");
            Cube("BunkerWindow", env, new Vector3(9, 2.1f, 7.23f), new Vector3(6.2f, 0.7f, 0.05f), "st_screen");
            Cube("BunkerRoof", env, new Vector3(9, 3.3f, 9.5f), new Vector3(8.6f, 0.2f, 5.1f), "st_steel_dark");

            // The team's model library, recoloured to the palette, as the far background.
            var h1 = SetupScenes.Model("Buildings/warehouse_01.obj", "Hangar A", new Vector3(-34, 0, 62), 0.8f, new Vector3(0, 12, 0));
            var h2 = SetupScenes.Model("Buildings/warehouse_01.obj", "Hangar B", new Vector3(52, 0, 74), 0.9f, new Vector3(0, -8, 0));
            var tower = SetupScenes.Model("Buildings/tower_antenna.obj", "Telemetry Tower", new Vector3(22, 0, 24), 1.4f);
            Recolor(h1, "st_hangar"); Recolor(h2, "st_hangar"); Recolor(tower, "st_steel_dark");
            h1.transform.SetParent(env, true); h2.transform.SetParent(env, true); tower.transform.SetParent(env, true);
            var beacon = Ball("TowerBeacon", env, new Vector3(22, 43.2f, 24), 0.9f, "st_glow_red");
            var blink = beacon.transform.parent.gameObject.AddComponent<LightFlicker>();
            blink.mode = LightFlicker.Mode.Blink; blink.speed = 0.6f; blink.blinkTarget = beacon;

            for (int i = 0; i < 7; i++)
            {
                float lx = -17 + i * 11f;
                Cyl("LampPole" + i, env, new Vector3(lx, 3.3f, 7.2f), 0.16f, 6.6f, "st_steel_dark");
                Cube("LampArm" + i, env, new Vector3(lx, 6.6f, 6.8f), new Vector3(0.12f, 0.12f, 0.9f), "st_steel_dark");
                Cube("LampHead" + i, env, new Vector3(lx, 6.5f, 6.4f), new Vector3(0.5f, 0.12f, 0.3f), "st_glow_warm");
            }

            // Mesas on the horizon, layered by the fog.
            float[] mx = { -150, -70, 10, 95, 170, 250 };
            float[] ms = { 90, 130, 80, 150, 100, 120 };
            for (int i = 0; i < mx.Length; i++)
                MeshObj(Shape("cone"), "Mesa" + i, env, new Vector3(mx[i], -2f, 210 + (i % 2) * 50), new Vector3(ms[i] * 1.8f, ms[i] * 0.32f, ms[i]), "st_mountain");
        }

        static void Sled(Transform root)
        {
            var sled = Group("Sled", root, Vector3.zero);
            Cube("Chassis", sled, new Vector3(0, 0.36f, 0), new Vector3(2.9f, 0.16f, 1.3f), "st_steel_dark");
            Cube("Deck", sled, new Vector3(0, 0.46f, 0), new Vector3(2.7f, 0.04f, 1.12f), "st_ceramic");
            Cube("Nose", sled, new Vector3(1.5f, 0.5f, 0), new Vector3(0.18f, 0.32f, 1.1f), "st_rust");
            foreach (float x in new[] { -0.95f, 0.95f })
                foreach (float z in new[] { -0.55f, 0.55f })
                    Cube("Shoe", sled, new Vector3(x, 0.25f, z), new Vector3(0.55f, 0.12f, 0.18f), "st_steel");

            // The test frame: four posts, a top ring and diagonal braces.
            var frame = Group("Frame", sled, Vector3.zero);
            const float fx = 1.22f, fz = 0.5f, y0 = 0.48f, y1 = 1.98f;
            foreach (float x in new[] { -fx, fx })
                foreach (float z in new[] { -fz, fz })
                {
                    Beam("Post", frame, new Vector3(x, y0, z), new Vector3(x, y1, z), 0.07f, "st_ceramic");
                    Cube("Foot", frame, new Vector3(x, y0 + 0.05f, z), new Vector3(0.16f, 0.1f, 0.16f), "st_hazard");
                }
            foreach (float z in new[] { -fz, fz })
            {
                Beam("TopRail", frame, new Vector3(-fx, y1, z), new Vector3(fx, y1, z), 0.07f, "st_ceramic");
                Beam("Brace", frame, new Vector3(-fx, y0, z), new Vector3(-0.2f, y1, z), 0.04f, "st_steel");
                Beam("Brace", frame, new Vector3(fx, y0, z), new Vector3(0.2f, y1, z), 0.04f, "st_steel");
            }
            foreach (float x in new[] { -fx, fx })
                Beam("TopCross", frame, new Vector3(x, y1, -fz), new Vector3(x, y1, fz), 0.07f, "st_ceramic");

            var mods = Group("Modules", sled, Vector3.zero);
            Engine(mods, "E20", 0.26f, 0.9f, 0.72f, 0.96f);
            Engine(mods, "E35", 0.33f, 1.1f, 0.92f, 1.02f);
            Engine(mods, "E50", 0.41f, 1.2f, 1.14f, 1.08f);

            var rec = Module(mods, "REC", new Vector3(0.32f, 0.53f, -0.62f));
            Cube("Box", rec, new Vector3(0.32f, 0.67f, -0.24f), new Vector3(0.42f, 0.38f, 0.42f), "st_rust");
            Cube("Band", rec, new Vector3(0.32f, 0.67f, -0.24f), new Vector3(0.44f, 0.08f, 0.44f), "st_ceramic");
            Cube("Handle", rec, new Vector3(0.32f, 0.89f, -0.24f), new Vector3(0.3f, 0.03f, 0.05f), "st_steel");

            var tel = Module(mods, "TEL", new Vector3(0.32f, 0.53f, 0.62f));
            Cube("Box", tel, new Vector3(0.32f, 0.64f, 0.24f), new Vector3(0.4f, 0.32f, 0.38f), "st_ceramic");
            Cube("Face", tel, new Vector3(0.32f, 0.66f, 0.045f), new Vector3(0.3f, 0.12f, 0.01f), "st_screen");
            Cyl("Antenna", tel, new Vector3(0.44f, 1.08f, 0.32f), 0.025f, 0.56f, "st_steel");
            Ball("AntennaTip", tel, new Vector3(0.44f, 1.38f, 0.32f), 0.07f, "st_glow_cyan");

            var scn = Module(mods, "SCN", new Vector3(0.92f, 0.53f, -0.62f));
            Cyl("Body", scn, new Vector3(0.92f, 0.77f, 0), 0.48f, 0.58f, "st_steel");
            Cyl("Collar", scn, new Vector3(0.92f, 1.07f, 0), 0.52f, 0.05f, "st_hazard");
            Ball("Dome", scn, new Vector3(0.92f, 1.1f, 0), 0.42f, "st_glass");
            Ball("Lens", scn, new Vector3(0.92f, 1.2f, -0.12f), 0.1f, "st_glow_cyan");

            var shd = Module(mods, "SHD", new Vector3(-0.2f, 2.08f, -0.56f));
            Cube("Slab", shd, new Vector3(0, 2.07f, 0), new Vector3(2.5f, 0.1f, 1.02f), "st_steel_dark");
            Cube("Tile", shd, new Vector3(0, 2.13f, 0), new Vector3(2.3f, 0.03f, 0.86f), "st_scorch");
            foreach (float x in new[] { -1.1f, 1.1f })
                foreach (float z in new[] { -0.42f, 0.42f })
                    Cyl("Bolt", shd, new Vector3(x, 2.15f, z), 0.07f, 0.06f, "st_hazard");

            var tnk = Module(mods, "TNK", new Vector3(0.5f, 1.27f, -0.56f));
            Ball("Sphere", tnk, new Vector3(0.5f, 1.52f, 0), 0.62f, "st_foil");
            Cyl("Cradle", tnk, new Vector3(0.5f, 1.26f, 0), 0.44f, 0.04f, "st_steel");
            Beam("StrapA", tnk, new Vector3(0.3f, 1.24f, -fz), new Vector3(0.3f, 1.24f, fz), 0.05f, "st_steel");
            Beam("StrapB", tnk, new Vector3(0.7f, 1.24f, -fz), new Vector3(0.7f, 1.24f, fz), 0.05f, "st_steel");
            Cyl("Band", tnk, new Vector3(0.5f, 1.52f, 0), 0.64f, 0.05f, "st_steel_dark");
        }

        static Transform Module(Transform mods, string id, Vector3 lampPos)
        {
            var m = Group("Mod_" + id, mods, Vector3.zero);
            ClampLamps(m, lampPos);
            m.gameObject.SetActive(false);
            return m;
        }

        // Engines lie along X with the nozzle pointing back (-X), so thrust pushes the sled toward +X.
        static void Engine(Transform mods, string id, float radius, float length, float nozzle, float y)
        {
            var e = Module(mods, id, new Vector3(-0.62f, 0.53f, -0.62f));
            const float cx = -0.6f;
            var lie = new Vector3(0, 0, -90);
            MeshObj(Shape("tank"), "Tank", e, new Vector3(cx, y, 0), new Vector3(radius * 2f, length * 0.5f, radius * 2f), "st_ceramic", lie);
            MeshObj(Shape("ring"), "Band", e, new Vector3(cx + 0.12f, y, 0), new Vector3(radius * 2f + 0.02f, 0.1f, radius * 2f + 0.02f), "st_rust", lie);
            MeshObj(Shape("ring"), "BandRear", e, new Vector3(cx - length * 0.28f, y, 0), new Vector3(radius * 2f + 0.02f, 0.04f, radius * 2f + 0.02f), "st_steel_dark", lie);
            float rear = cx - length * 0.5f + 0.06f;
            MeshObj(Shape("nozzle"), "Nozzle", e, new Vector3(rear, y, 0), Vector3.one * nozzle, "st_nozzle", lie);
            foreach (float x in new[] { cx - length * 0.25f, cx + length * 0.25f })
                Cube("Saddle", e, new Vector3(x, (0.48f + y - radius * 0.7f) * 0.5f, 0), new Vector3(0.1f, y - radius * 0.7f - 0.48f, radius * 1.7f), "st_steel");

            var plume = Group("Plume", e, new Vector3(rear - 0.6f * nozzle + 0.05f, y, 0));
            MeshObj(Shape("plume"), "PlumeOuter", plume, Vector3.zero, new Vector3(nozzle * 1.5f, nozzle * 2.2f, nozzle * 1.5f), "st_plume_outer", lie);
            MeshObj(Shape("plume"), "PlumeCore", plume, Vector3.zero, new Vector3(nozzle * 0.75f, nozzle * 1.5f, nozzle * 0.75f), "st_plume_core", lie);
            Point("PlumeLight", plume, new Vector3(-0.8f, 0.2f, 0), Palette.Horizon, 60f, 16f);
            plume.gameObject.SetActive(false);
        }

        static void FragmentBench(Transform root)
        {
            var b = Group("FragmentBench", root, new Vector3(-4.3f, 0, -2.4f));
            Cube("Top", b, new Vector3(0, 0.9f, 0), new Vector3(2.2f, 0.07f, 1.1f), "st_steel_dark");
            Cube("Mat", b, new Vector3(0, 0.94f, 0), new Vector3(1.9f, 0.012f, 0.9f), "st_ceramic");
            foreach (float x in new[] { -1f, 1f })
                foreach (float z in new[] { -0.45f, 0.45f })
                    Cube("Leg", b, new Vector3(x, 0.45f, z), new Vector3(0.07f, 0.9f, 0.07f), "st_steel");
            // The recovered fragment: a scorched arc of outer skin lying concave side up.
            var frag = Group("Fragment", b, new Vector3(-0.1f, 0.97f, 0.05f));
            MeshObj(Shape("fragment"), "Skin", frag, new Vector3(0.75f, -1.55f, -0.55f), Vector3.one, "st_scorch", new Vector3(0, 0, 90));
            Cube("Stringer", frag, new Vector3(-0.1f, 0.05f, 0), new Vector3(1.3f, 0.06f, 0.05f), "st_steel", new Vector3(0, 8, 0));
            Cube("Bracket", frag, new Vector3(0.42f, 0.06f, 0.18f), new Vector3(0.2f, 0.12f, 0.14f), "st_steel");
            Cube("SerialPlate", frag, new Vector3(0.42f, 0.125f, 0.18f), new Vector3(0.16f, 0.012f, 0.1f), "st_ceramic");
            // World anchors for the three inspection markers the mission draws on screen.
            Group("Hot_Edge", frag, new Vector3(-0.62f, 0.06f, -0.12f));
            Group("Hot_Bolt", frag, new Vector3(-0.05f, 0.1f, 0.02f));
            Group("Hot_Bracket", frag, new Vector3(0.42f, 0.15f, 0.18f));
            // Work lamp.
            Cyl("LampPost", b, new Vector3(-0.95f, 1.35f, 0.42f), 0.04f, 0.9f, "st_steel");
            Cube("LampHead", b, new Vector3(-0.72f, 1.8f, 0.3f), new Vector3(0.5f, 0.06f, 0.16f), "st_steel_dark", new Vector3(0, 25, -12));
            Cube("LampTube", b, new Vector3(-0.72f, 1.765f, 0.3f), new Vector3(0.44f, 0.02f, 0.1f), "st_glow_warm", new Vector3(0, 25, -12));
            Spot("BenchLamp", b, new Vector3(-0.7f, 1.75f, 0.3f), b.position + new Vector3(0.1f, 0.95f, 0), Palette.LampWarm, 5f, 5f, 95f, false);
        }

        static void ArchiveConsole(Transform root)
        {
            var c = Group("ArchiveConsole", root, new Vector3(4.7f, 0, -2.5f));
            Cube("Base", c, new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.9f, 0.7f), "st_hangar");
            Cube("Trim", c, new Vector3(0, 0.92f, 0), new Vector3(1.66f, 0.05f, 0.76f), "st_steel");
            Cube("Keys", c, new Vector3(0, 0.96f, -0.14f), new Vector3(1.1f, 0.03f, 0.3f), "st_steel_dark", new Vector3(8, 0, 0));
            Cube("Monitor", c, new Vector3(0, 1.45f, 0.22f), new Vector3(1.4f, 0.85f, 0.06f), "st_steel_dark", new Vector3(-14, 0, 0));
            Cube("Screen", c, new Vector3(0, 1.45f, 0.185f), new Vector3(1.28f, 0.73f, 0.01f), "st_screen", new Vector3(-14, 0, 0));
            Cube("StatusBar", c, new Vector3(0, 1.75f, 0.11f), new Vector3(1.28f, 0.04f, 0.012f), "st_glow_cyan", new Vector3(-14, 0, 0));
            Cube("Rack", c, new Vector3(1.25f, 1f, 0.1f), new Vector3(0.6f, 2f, 0.6f), "st_hangar");
            for (int i = 0; i < 5; i++)
                Cube("RackLed" + i, c, new Vector3(1.1f + (i % 2) * 0.2f, 0.5f + i * 0.3f, -0.205f), new Vector3(0.1f, 0.03f, 0.01f), i == 2 ? "st_glow_amber" : "st_glow_green");
            Point("ScreenGlow", c, new Vector3(0, 1.5f, -0.6f), Palette.Accent, 4f, 4.5f);
        }

        static void Lighting(Transform root)
        {
            var rig = Group("Lighting", root, Vector3.zero);
            // The sun is on the horizon behind the set: it lights the sky and rims the hardware.
            var sun = SetupScenes.AddSun(new Vector3(60, 14, 300), Vector3.zero, 1.5f, Palette.Horizon);
            sun.transform.SetParent(rig, true);
            sun.shadowStrength = 0.8f;
            SetupScenes.DuskAtmosphere(0.0045f);

            // Key: a floodlight tower off-camera left. Rim: cool light from behind the far rail.
            var key = Spot("KeyFlood", rig, new Vector3(-6.5f, 8.5f, -7.5f), new Vector3(0.2f, 1f, 0), Palette.LampWarm, 520f, 30f, 46f, true);
            key.shadowBias = 0.03f;
            Spot("RimFlood", rig, new Vector3(7.5f, 6.5f, 8f), new Vector3(0, 1.2f, 0), Palette.KeyCool, 300f, 26f, 50f, false);
            Spot("TrackFlood", rig, new Vector3(24, 10f, -9f), new Vector3(24, 0, 0), Palette.KeyCool, 420f, 36f, 80f, false);
            var work = Point("SledWork", rig, new Vector3(0.4f, 2.9f, -1.8f), Palette.LampWarm, 9f, 6f);
            var breathe = work.gameObject.AddComponent<LightFlicker>();
            breathe.mode = LightFlicker.Mode.Noise; breathe.speed = 0.3f; breathe.depth = 0.12f;
        }

        static void Camera(Transform root)
        {
            var cam = SetupScenes.AddStageCamera(34f);
            var rig = cam.gameObject.AddComponent<CameraRig>();
            rig.shots.Add(new CameraRig.Shot { name = "title", position = new Vector3(-7.5f, 2.4f, -11f), lookAt = new Vector3(2.5f, 1.5f, 0), fov = 40f, orbit = 0.8f });
            rig.shots.Add(new CameraRig.Shot { name = "bench", position = new Vector3(0.9f, 2.25f, -8.2f), lookAt = new Vector3(0.05f, 1.02f, 0), fov = 33f });
            rig.shots.Add(new CameraRig.Shot { name = "run", position = new Vector3(3.6f, 1.5f, -9.5f), lookAt = new Vector3(1.4f, 1.15f, 0), fov = 46f });
            rig.shots.Add(new CameraRig.Shot { name = "fragment", position = new Vector3(-4.3f, 2.7f, -5.0f), lookAt = new Vector3(-4.35f, 0.98f, -2.35f), fov = 34f });
            rig.shots.Add(new CameraRig.Shot { name = "archive", position = new Vector3(4.2f, 1.75f, -5.2f), lookAt = new Vector3(4.75f, 1.35f, -2.4f), fov = 34f });
            rig.startShot = "title";
            rig.sway = 0.05f;
        }
    }
}

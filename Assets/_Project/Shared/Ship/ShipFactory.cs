using UnityEngine;

namespace LastSignal.Ship
{
    /// <summary>
    /// Builds the greybox ship. The editor menu "Last Signal > Ship - Build Ship prefab" saves the result as Ship.prefab.
    /// Scenes can also call ShipFactory.Build() at runtime if the prefab is missing, so a scene never breaks.
    /// The nose points along +Z. About 12 m long.
    /// </summary>
    public static class ShipFactory
    {
        public static readonly Color HullColor = new Color(0.78f, 0.80f, 0.84f);
        public static readonly Color TrimColor = new Color(0.20f, 0.45f, 0.75f);
        public static readonly Color GlassColor = new Color(0.10f, 0.25f, 0.40f);
        public static readonly Color GlowColor = new Color(0.30f, 0.85f, 1.00f);
        public static readonly Color SupplyColor = new Color(0.95f, 0.60f, 0.20f);

        /// <summary>Point on top of the ship where another craft can dock (local space).</summary>
        public static readonly Vector3 DockingPortLocal = new Vector3(0f, 1.6f, 0.5f);

        public static GameObject Build(string name = "Ship")
        {
            GameObject root = new GameObject(name);
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.mass = 18000f;

            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            Transform body = new GameObject("GreyboxBody").transform;
            body.SetParent(visual, false);

            GreyboxKit.Part(PrimitiveType.Capsule, "Fuselage", body, new Vector3(0f, 0f, 0f), new Vector3(2.4f, 5.2f, 2.4f), new Vector3(90f, 0f, 0f), HullColor);
            GreyboxKit.Part(PrimitiveType.Sphere, "Cockpit", body, new Vector3(0f, 0.75f, 2.9f), new Vector3(1.5f, 1.0f, 2.4f), GlassColor, true);
            GreyboxKit.Part(PrimitiveType.Cube, "Wings", body, new Vector3(0f, -0.25f, -1.2f), new Vector3(11f, 0.3f, 3.4f), HullColor);
            GreyboxKit.Part(PrimitiveType.Cube, "WingTipL", body, new Vector3(-5.4f, 0.1f, -1.6f), new Vector3(0.3f, 1.0f, 2.4f), TrimColor);
            GreyboxKit.Part(PrimitiveType.Cube, "WingTipR", body, new Vector3(5.4f, 0.1f, -1.6f), new Vector3(0.3f, 1.0f, 2.4f), TrimColor);
            GreyboxKit.Part(PrimitiveType.Cube, "Fin", body, new Vector3(0f, 1.5f, -3.6f), new Vector3(0.3f, 2.2f, 2.2f), TrimColor);
            GreyboxKit.Part(PrimitiveType.Cube, "SupplyPodL", body, new Vector3(-1.3f, -1.05f, 0.2f), new Vector3(0.9f, 0.8f, 3.2f), SupplyColor);
            GreyboxKit.Part(PrimitiveType.Cube, "SupplyPodR", body, new Vector3(1.3f, -1.05f, 0.2f), new Vector3(0.9f, 0.8f, 3.2f), SupplyColor);
            GreyboxKit.Part(PrimitiveType.Cylinder, "DockingPort", body, DockingPortLocal - new Vector3(0f, 0.35f, 0f), new Vector3(1.4f, 0.25f, 1.4f), TrimColor);
            GreyboxKit.Part(PrimitiveType.Cylinder, "EngineL", body, new Vector3(-2.2f, 0f, -4.4f), new Vector3(1.1f, 1.2f, 1.1f), new Vector3(90f, 0f, 0f), HullColor * 0.7f);
            GreyboxKit.Part(PrimitiveType.Cylinder, "EngineR", body, new Vector3(2.2f, 0f, -4.4f), new Vector3(1.1f, 1.2f, 1.1f), new Vector3(90f, 0f, 0f), HullColor * 0.7f);

            // Engine glows are not part of the body so they stay when a real model replaces the greybox.
            Transform glowL = GreyboxKit.Part(PrimitiveType.Sphere, "GlowL", visual, new Vector3(-2.2f, 0f, -5.8f), new Vector3(0.9f, 0.9f, 0.2f), GlowColor, true).transform;
            Transform glowR = GreyboxKit.Part(PrimitiveType.Sphere, "GlowR", visual, new Vector3(2.2f, 0f, -5.8f), new Vector3(0.9f, 0.9f, 0.2f), GlowColor, true).transform;

            GameObject lightGo = new GameObject("EngineLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0f, -7.5f);
            Light engineLight = lightGo.AddComponent<Light>();
            engineLight.type = LightType.Point;
            engineLight.color = GlowColor;
            engineLight.range = 30f;
            engineLight.intensity = 0f;
            engineLight.shadows = LightShadows.None;

            // Landing gear for M4 (hidden by default).
            GameObject gear = new GameObject("Gear");
            gear.transform.SetParent(root.transform, false);
            GreyboxKit.Part(PrimitiveType.Cylinder, "LegFront", gear.transform, new Vector3(0f, -1.9f, 3.2f), new Vector3(0.3f, 0.8f, 0.3f), TrimColor);
            GreyboxKit.Part(PrimitiveType.Cylinder, "LegL", gear.transform, new Vector3(-2.6f, -1.6f, -2.4f), new Vector3(0.3f, 0.7f, 0.3f), TrimColor);
            GreyboxKit.Part(PrimitiveType.Cylinder, "LegR", gear.transform, new Vector3(2.6f, -1.6f, -2.4f), new Vector3(0.3f, 0.7f, 0.3f), TrimColor);
            gear.SetActive(false);

            // Physics colliders on the root's children (the Rigidbody uses them as a compound collider).
            GameObject colliders = new GameObject("Colliders");
            colliders.transform.SetParent(root.transform, false);
            BoxCollider fuselage = colliders.AddComponent<BoxCollider>();
            fuselage.center = new Vector3(0f, 0f, 0f);
            fuselage.size = new Vector3(2.6f, 2.6f, 11.5f);
            BoxCollider wings = colliders.AddComponent<BoxCollider>();
            wings.center = new Vector3(0f, -0.25f, -1.2f);
            wings.size = new Vector3(11f, 0.6f, 3.4f);
            BoxCollider legs = colliders.AddComponent<BoxCollider>();
            legs.center = new Vector3(0f, -2.0f, 0f);
            legs.size = new Vector3(5.6f, 0.4f, 7f);
            legs.enabled = false;   // the controller's gear hook enables it via GearColliderToggle

            ShipController controller = root.AddComponent<ShipController>();
            controller.engineGlows = new Transform[] { glowL, glowR };
            controller.engineLight = engineLight;
            controller.gearRoot = gear;
            root.AddComponent<GearColliderToggle>().legCollider = legs;

            root.AddComponent<LaunchSequence>();
            return root;
        }

        /// <summary>Hides the greybox body (used when a real model is placed under Visual).</summary>
        public static void HideGreyboxBody(GameObject ship)
        {
            Transform body = ship.transform.Find("Visual/GreyboxBody");
            if (body != null) body.gameObject.SetActive(false);
        }
    }
}

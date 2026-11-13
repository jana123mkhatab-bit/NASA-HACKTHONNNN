namespace LastSignal.Core
{
    /// <summary>
    /// The agreed mission ids and scene names. This is part of the shared contract.
    /// Flow: Boot -> M1 -> M2 -> M3 -> M4 -> Final -> DesignMode.
    /// </summary>
    public static class MissionIds
    {
        public const string Boot = "Boot";
        public const string M1 = "M1";
        public const string M2 = "M2";
        public const string M3 = "M3";
        public const string M4 = "M4";
        public const string Final = "Final";
        public const string DesignMode = "DesignMode";

        /// <summary>Playable order after the title screen.</summary>
        public static readonly string[] Order = { M1, M2, M3, M4, Final, DesignMode };

        static readonly string[] AllIds = { Boot, M1, M2, M3, M4, Final, DesignMode };

        public static string SceneFor(string id)
        {
            switch (id)
            {
                case Boot: return "Boot";
                case M1: return "M1_Ground";
                case M2: return "M2_Preflight";
                case M3: return "M3_DeepSpace";
                case M4: return "M4_Planet";
                case Final: return "Final";
                case DesignMode: return "DesignMode";
                default: return null;
            }
        }

        /// <summary>Returns the mission id for a scene name, or null if the scene is not part of the flow.</summary>
        public static string IdForScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return null;
            for (int i = 0; i < AllIds.Length; i++)
            {
                if (SceneFor(AllIds[i]) == sceneName) return AllIds[i];
            }
            return null;
        }

        /// <summary>The mission that follows this one, or null after DesignMode.</summary>
        public static string Next(string id)
        {
            if (id == Boot) return M1;
            for (int i = 0; i < Order.Length; i++)
            {
                if (Order[i] == id) return i + 1 < Order.Length ? Order[i + 1] : null;
            }
            return null;
        }

        public static string Title(string id)
        {
            switch (id)
            {
                case Boot: return "The Last Known Signal";
                case M1: return "What Fell to Earth";
                case M2: return "Ten Percent";
                case M3: return "The Ship That Came Out of Nowhere";
                case M4: return "Someone's Been Living Here";
                case Final: return "The Call Home";
                case DesignMode: return "Mission Design Mode";
                default: return id;
            }
        }
    }
}

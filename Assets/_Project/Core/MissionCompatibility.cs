namespace LastSignal
{
    public static class Pip
    {
        public static void Say(string line)
        {
            if (MissionManager.Instance != null && MissionManager.Instance.pipUI != null)
                MissionManager.Instance.pipUI.Say(line);
        }
    }

    public static class FatherLog
    {
        public static void Add(string text)
        {
            if (MissionManager.Instance == null) return;
            var log = MissionManager.Instance.GetComponentInChildren<FatherLogUI>();
            if (log != null) log.AddEntry(text);
        }
    }
}

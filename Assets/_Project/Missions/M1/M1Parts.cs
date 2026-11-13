namespace LastSignal
{
    /// <summary>
    /// M1 parts library. ALL VALUES ARE GAME-BALANCE PLACEHOLDERS, not NASA data.
    /// Sprint 3: person 6's parts CSV (NASA vehicle and engine fact sheets) replaces these numbers.
    /// The balance is chosen so the lesson lands: the biggest engine is not automatically the best,
    /// and every extra kilogram of payload costs acceleration (a = F / m).
    /// </summary>
    public static class M1Parts
    {
        public class Part
        {
            public string id, name, note;
            public float massKg, thrustN;
            public bool required;
        }

        public const float FrameMassKg = 120f;   // sled, rails shoes and test frame
        public const float MassBudgetKg = 270f;  // the rail brake is rated for this
        public const float TargetAccel = 16f;    // m/s^2 the abort profile needs

        public static readonly Part[] Engines =
        {
            new Part { id = "E20", name = "E-20 Kestrel", massKg = 35f, thrustN = 2400f, note = "Light, weak" },
            new Part { id = "E35", name = "E-35 Osprey", massKg = 60f, thrustN = 3800f, note = "Balanced" },
            new Part { id = "E50", name = "E-50 Condor", massKg = 100f, thrustN = 5200f, note = "Strong, heavy" },
        };

        public static readonly Part[] Payloads =
        {
            new Part { id = "REC", name = "Flight recorder", massKg = 30f, required = true, note = "Writes the archive record" },
            new Part { id = "TEL", name = "Telemetry pack", massKg = 15f, required = true, note = "Streams the run to the bunker" },
            new Part { id = "SCN", name = "Survey scanner", massKg = 25f, note = "Not needed for this test" },
            new Part { id = "SHD", name = "Heat-shield slab", massKg = 45f, note = "Not needed for this test" },
            new Part { id = "TNK", name = "Spare propellant", massKg = 35f, note = "Not needed for this test" },
        };
    }
}

namespace LastSignal.M3
{
    /// <summary>
    /// Every line of M3 text in one place, so the story team (persons 4 and 6) can edit wording without touching logic.
    /// Keep the stranger's identity hidden: M4 reveals that the stranger is the son.
    /// </summary>
    public static class M3Lines
    {
        public const string Control = "MISSION CONTROL";
        public const string Unknown = "UNKNOWN CRAFT";
        public const string Ship = "SHIP";

        // ---- launch and relay
        public static string ControlOrder(string delay)
        {
            return "(sent " + delay + " ago) Relay R-7 is broadcasting corrupted navigation data. Ships that trust it fly blind. Take it offline.";
        }
        public const string ObjFlyToRelay = "Fly to relay R-7";
        public const string ObjTarget = "Hold the nose on R-7 to lock, then fire";
        public const string PipLaunch = "Departure burn done. W and S for thrust, A and D to turn. The relay is ahead.";
        public const string PipTargeting = "Keep the relay inside the circle until the lock bar fills.";
        public const string PipRelayDown = "That relay was our only link to Earth. Nothing we send will get home now.";
        public const string LogRelayDown = "I took out the relay. Mission Control's order arrived hours after they sent it, and now no answer can reach them.";

        // ---- stranger
        public const string UnknownWarning = "Cut your engines. Hold position. Do not run.";
        public const string ObjIntercept = "Unknown craft closing";
        public const string PipIntercept = "It's much faster than us. Brace!";
        public const string UnknownDocking = "Stay still. I'm only taking what I need.";
        public const string ShipSupplyBay = "SUPPLY BAY OPENED FROM OUTSIDE";
        public const string PipLeaving = "They took half our supplies. The ship is lighter now, so it will accelerate faster.";

        // ---- tumble
        public const string TumbleTitle = "LOSS OF CONTROL. STABILIZER OFFLINE";
        public const string TumbleHelp = "Counter the spin by hand. Push the opposite way on each axis.";
        public const string PipTumble = "Look at the bars and push against the spin!";
        public const string FailTumble = "Lost control after the collision. The ship tumbled out of reach of the unknown craft.";
        public const string RetryImpact = "Restarted from the impact. Counter the spin.";

        // ---- triage and scanner
        public const string ObjTriage = "Choose what to repair with the power you have left";
        public const string CommsUseless = "Long-range comms online. Calling Earth... no relay in range. Nobody can hear us.";
        public const string ObjScan = "Scanner sweep in progress";
        public const string PipPlanet = "Scanner found a planet nobody has charted. It's close enough... maybe.";
        public const string LogPlanet = "The stranger left toward the same patch of sky where the scanner found a planet.";

        // ---- reroute and ending
        public const string ObjReroute = "Plan a new route with the supplies you have left";
        public const string PlannerNote = "The stranger left you just enough for one place. Supply costs are GAME-BALANCE values.";
        public const string ObjDepart = "Course set";
        public const string FailHull = "The hull failed.";
        public const string FailFuel = "Out of fuel before reaching the relay.";

        public static string FailSupplies(string destination, float shortBy)
        {
            return "Supplies ran out on the way to " + destination + ". About " + UnityEngine.Mathf.CeilToInt(shortBy) + " supply points short.";
        }
    }
}

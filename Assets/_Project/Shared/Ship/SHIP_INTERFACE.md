# Ship prefab interface (person 3 and person 4 agreement, S0)

Owner: person 3. Person 4 uses the same prefab in M4. Ask person 3 before you change anything in `Shared/Ship`.

* Prefab: `Assets/_Project/Shared/Ship/Ship.prefab`. Build or rebuild it with the menu **Last Signal > Ship - Build Ship prefab**.
* Assembly: `LastSignal.Ship` (namespace `LastSignal.Ship`). Add it to your mission asmdef references.
* Units: 1 unit = 1 metre, mass in kg, thrust in newtons. The ship's nose points along +Z.
* All thrust, mass and consumption numbers are **GAME-BALANCE values**, not NASA data.

## Input scheme (keyboard / gamepad)

| Action | Keyboard | Gamepad |
|---|---|---|
| Main thrust forward / reverse | W / S | Right trigger / Left trigger |
| Yaw left / right | A / D | Left stick X |
| Pitch nose up / down | Up / Down arrow | Left stick Y |
| Roll left / right | Q / E | LB / RB |
| Lift up / down (landing thrusters) | Space / Left Ctrl (or C) | Y / B |
| Stabilize (kill spin, costs fuel) | X | X (west) |
| Boost (more thrust, more fuel) | Left Shift | Left stick press |
| Fire / interact | F (or left mouse) | A (south) |

Read it anywhere with `ShipInput.Read()` (returns a `ShipInputFrame`) and `ShipInput.FirePressed()`.

## Physics

`acceleration = thrust / current mass`. The mass is recomputed every physics step from `PlayerState`:

```
mass = dryMass + supplies x kgPerSupplyPoint + fuel x kgPerFuelPoint     (ShipMassModel)
```

So when supplies drop (for example M3's stranger takes half), the ship gets lighter and accelerates harder.
Fuel burns with throttle and supplies drain over time through `PlayerState.SetFuel / SetSupplies`.

## ShipController API

```csharp
ShipController ship = ShipController.Active;      // the ship in the current scene

// state (read)
ship.Mode                 // ShipMode.Space | Atmosphere | Landed
ship.CurrentMass          // kg
ship.LastAcceleration     // m/s^2 from main + lift thrust this step
ship.CurrentThrust        // N
ship.Velocity, ship.Speed
ship.AltitudeAboveGround  // metres (raycast down, groundMask), +inf if nothing below
ship.VerticalSpeed        // m/s, + is up (world)
ship.IsGrounded

// control (write)
ship.ControlEnabled = false;            // ignore the player (cutscenes)
ship.InputOverride = () => frame;       // autopilot: return a ShipInputFrame each step; null = player
ship.FlightAssist = true;               // gentle damping when no input (on by default)
ship.MainEngineOnline / StabilizerOnline / RcsAuthority / ThrustMultiplier
ship.DrainSupplies, ship.SuppliesDrainMultiplier
ship.SetMode(ShipMode.Atmosphere);      // M4: enables planet gravity (planetGravity m/s^2) and drag
ship.planetGravity = 3.7f;
ship.SetGear(true);                     // shows the landing legs
ship.Hold(true);                        // freeze in place (docked, launch clamp)
ship.Teleport(pos, rot, vel, angVel);
ship.ApplyImpact(deltaV, deltaAngularV, hullDamage);

// events (landing hooks for M4)
ship.Touchdown += info => { info.impactSpeed; info.uprightDot; info.safe; info.point; info.ground; };
ship.Landed    += () => { };   // grounded and still for landedSettleTime seconds
ship.Liftoff   += () => { };
ship.FuelEmpty += () => { };
ship.HullDepleted += () => { };
ship.Collided  += collision => { };
ship.GearChanged += down => { };
```

`info.safe` is true when `impactSpeed <= maxSafeTouchdownSpeed` (default 3 m/s) and the ship is upright (`uprightDot >= 0.8`).
Person 4 decides what an unsafe touchdown does (fail, damage, retry).

## Launch sequence

```csharp
LaunchSequence launch = ship.GetComponent<LaunchSequence>();
launch.burnDirection = LaunchSequence.BurnDirection.Up;   // M4 takeoff (Forward for M3 departure)
launch.Begin(() => { /* control handed to the player */ });
```
It holds the ship, counts down 3-2-1 on screen, releases it, burns for `burnDuration`, then gives control back.

## Camera and telemetry

* `ChaseCamera.Attach(Camera.main, ship.transform)` gives a chase camera. `SetStable(true)` keeps the camera level while the ship spins. `Shake(amount, seconds)`.
* `ShipTelemetryHud.Create(ship)` shows mass, speed and `a = F / m` on screen (bottom right).

# Space Exploration Simulation - 3D Models Library

This directory contains high-detail 3D models specifically tailored for a realistic space exploration and career simulation platform.

---

## 📁 Directory Structure & Generated Assets

```
d:/3d models/
├── space/
│   ├── space_skybox.obj        # Inverted sphere (r=500, 64x128 segs, 8,192 faces)
│   └── space_skybox.mtl        # Skybox material mapping (Ka 0.01 0.01 0.02)
├── sun/
│   ├── sun.obj                 # High-detail solar body (r=20, 96x192 segs, 18,432 faces)
│   ├── sun_corona.obj          # Outer glowing atmosphere/corona (r=22, 48x96 segs)
│   └── sun.mtl                 # Emissive solar surface (Ke 10 8 3) and transparent corona
├── earth/
│   ├── earth.obj               # Ultra-detail terrestrial body (r=10, 128x256 segs, 32,768 faces)
│   ├── earth_atmosphere.obj    # Atmospheric Rayleigh scatter shell (r=10.15, d=0.12 blue)
│   ├── earth_clouds.obj        # Independent rotational cloud layer (r=10.08, d=0.5 white)
│   └── earth.mtl               # Multi-layer material definitions for Blue Marble
├── moon/
│   ├── moon.obj                # Procedurally cratered lunar sphere (r=3, 96x192 segs, 18,432 faces)
│   └── moon.mtl                # Regolith lunar material
├── buildings/
│   ├── skyscraper_01.obj       # Modern stepped glass skyscraper (80m high, 15x15m base)
│   ├── skyscraper_02.obj       # Cylindrical tower with observation dome (60m high, R=8m)
│   ├── office_building_01.obj  # Mid-rise corporate office with recessed entrance (30x25x25m)
│   ├── residential_01.obj      # Apartment complex with balconies and gabled roof (20x15x15m)
│   ├── residential_02.obj      # Suburban / outpost residential home with chimney (10x8x8.5m)
│   ├── warehouse_01.obj        # Industrial spaceport hangar / logistics depot (40x20x10m + arch)
│   ├── tower_antenna.obj       # Communication / telemetry lattice tower (30m high)
│   ├── street_lamp.obj         # Urban street lighting post
│   ├── tree_01.obj             # Urban vegetation element
│   ├── bench.obj               # Ground facility bench
│   ├── city_block.obj          # Pre-assembled 200x200m urban block with roads and walkways
│   ├── city_district.obj       # Large 630x630m 3x3 urban district (6,860 vertices, 1,886 faces)
│   └── buildings.mtl           # Unified PBR-ready material definitions for all structures
├── generate_celestial.js       # Node.js generator script for celestial bodies
└── generate_buildings.js       # Node.js generator script for architecture and city layouts
```

---

## 🪐 1. Celestial Bodies Details

### Space Skybox (`space/`)
* **Mesh**: Inverted UV sphere normals pointing inward toward the scene origin (0, 0, 0).
* **Radius**: 500 units.
* **Usage**: Place at camera origin or center of system. Disable depth writing in your shader or place on background layer.
* **Recommended Texture**: NASA/ESA deep space panorama or Tycho 8K/16K star map equirectangular projection.

### The Sun (`sun/`)
* **Two-Layer Construction**:
  1. `sun.obj`: Core sphere with high emissive values (`Ke 10.0 8.0 3.0`) for HDR bloom and volumetric sun shaft generation.
  2. `sun_corona.obj`: Outer translucent envelope (`d 0.15`) that rotates slightly or pulses using a vertex/fragment shader.
* **Recommended Texture**: NASA SDO (Solar Dynamics Observatory) 4K/8K AIA 171/304 ultraviolet or optical composite.

### Earth System (`earth/`)
* **Three-Layer Realism Hierarchy**:
  1. `earth.obj`: Base planetary surface with full equirectangular UV mapping compatible with NASA Blue Marble Next Generation.
  2. `earth_clouds.obj`: Sits at `r = 10.08` (just above terrain). Rotate this layer independently by a small angular velocity (e.g., 1.05x planetary spin) to simulate dynamic weather patterns.
  3. `earth_atmosphere.obj`: Sits at `r = 10.15`. Has a translucent cyan-blue Fresnel shader configuration (`d 0.12`).
* **Recommended Textures (NASA Visible Earth / Blue Marble)**:
  - Albedo: `earth_daymap_8k.jpg` (NASA Blue Marble)
  - Night Lights: `earth_nightmap_8k.jpg` (NASA Black Marble for the unlit hemisphere)
  - Specular Map: `earth_specular_map.png` (oceans reflect, continents matte)
  - Normal / Elevation Map: `earth_elevation_normal_8k.png`
  - Clouds: `earth_clouds_8k.png` (8K cloud mask with alpha transparency)

### The Moon (`moon/`)
* **Mesh & Topography**: Procedural cratering algorithm applied to all 18,721 vertices. Generates 40 primary impact craters with Gaussian rim elevation and smooth floor depression profiles, plus multi-frequency fractal noise for regolith roughness.
* **Recommended Texture**: NASA LRO (Lunar Reconnaissance Orbiter) LOLA elevation & LROC 8K albedo map.

---

## 🏙️ 2. Earth Surface & Building System (`buildings/`)

To fulfill the requirement that **players can travel down to Earth and see actual buildings**, both modular individual buildings and pre-assembled districts are provided:

### Modular Architecture
1. **Skyscraper 01** (`skyscraper_01.obj`): Three-tiered setback modern skyscraper (15x15 -> 12x12 -> 8x8 -> 80m height) with rooftop communication mast and helipad geometry.
2. **Skyscraper 02** (`skyscraper_02.obj`): Cylindrical glass tower with observation dome penthouse (R=8m, 60m height).
3. **Office Building 01** (`office_building_01.obj`): Corporate headquarters / NASA administration hub with recessed ground lobby and window grid.
4. **Residential 01 & 02** (`residential_01.obj`, `residential_02.obj`): Multi-unit apartment complex with protruding balconies and single-family / outpost habitation units.
5. **Industrial Warehouse / Spaceport Facility** (`warehouse_01.obj`): Vaulted-roof hangar (40x20x10m) suitable for rover storage, rocket assembly, or payload logistics.
6. **Telemetry & Communication Tower** (`tower_antenna.obj`): 4-leg tapered lattice tower (30m height) with satellite dishes.

### Assembled Environments
* **`city_block.obj`**: A 200m x 200m urban street block with sidewalks, roads, and placed buildings.
* **`city_district.obj`**: A 630m x 630m 3x3 district featuring varied building layouts, street lanes, and plazas.

---

## 🚀 3. Seamless Space-to-Surface Transition Architecture

For your game engine (Unreal Engine 5, Unity, Godot 4, or custom WebGL/Three.js):

1. **Orbital Phase (Altitude > 200 km)**:
   - Render `earth.obj` + `earth_clouds.obj` + `earth_atmosphere.obj`.
   - Scale factor: 1 unit = ~637 km (or your game's celestial scale).

2. **Re-entry / Atmospheric Descent Phase (Altitude 200 km -> 10 km)**:
   - Interpolate camera exposure and atmospheric Rayleigh scattering.
   - Cloud layer alpha fades as camera passes through `earth_clouds.obj` (r = 10.08).

3. **Surface / Landing Phase (Altitude < 10 km)**:
   - Switch or stream in `city_district.obj` or localized `city_block.obj` at the landing coordinates (e.g. Kennedy Space Center / Spaceport area).
   - Use Level-of-Detail (LOD) streaming: individual buildings `skyscraper_01.obj`, `office_building_01.obj`, etc., load higher resolution colliders and interior props.


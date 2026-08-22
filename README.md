# Shape Guard

A minimal, top-down 2D tower-defense prototype for Unity 6.

## Play

Open `Assets/Scenes/SampleScene.unity` and press Play. Runtime bootstrap creates the game automatically.

- Buy Triangle Defenses with ore and Ore Collectors with gold.
- Triangle Defenses attack automatically. Killing red-circle enemies awards gold.
- Ore Collectors generate ore. Defense upgrades cost ore; collector upgrades cost gold.
- Press **Start Wave** to begin progression. Wins automatically continue to the next wave until the core falls.
- After a progression loss, the last cleared wave repeats as a farming wave. Start Wave queues another progression run after the current farm wave.
- All ten future paths are visible. Nearby entrances merge at shared junctions instead of overlapping, forming four clean trunks into the core. One path starts open, and another opens after every 10 cleared waves.
- Hold left-click and drag to move the camera. Use the mouse wheel to zoom around the cursor.
- Each scroll-wheel step changes zoom noticeably, with a range from close-up building views to a full-map overview.
- Use the HUD speed button to cycle the game between 1x, 2x, and 3x speed.
- Right-click to cancel building placement.

## Structure

- `Assets/Scripts/Core` - bootstrap, balance, large-map data, economy, waves, placement, and camera
- `Assets/Scripts/Gameplay` - triangle defenses, ore collectors, enemies, and smooth runtime shapes
- `Assets/Scripts/UI` - minimal HUD, build controls, and upgrade panels

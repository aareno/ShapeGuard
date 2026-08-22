# Shape Guard

A minimal, top-down 2D tower-defense prototype for Unity 6.

## Play

Open `Assets/Scenes/SampleScene.unity` and press Play. Runtime bootstrap creates the game automatically.

- Buy Triangle Defenses with ore and Ore Collectors with gold.
- Triangle Defenses attack automatically. Killing red-circle enemies awards gold.
- Ore Collectors generate ore. Defense upgrades cost ore; collector upgrades cost gold.
- Press **Start Wave** to begin progression. Wins automatically continue to the next wave until the core falls.
- After a progression loss, the last cleared wave repeats as a farming wave. Start Wave queues another progression run after the current farm wave.
- The core sits at the center of a large 60-path skill tree: four primary branches radiate outward, each splits into two outer branches, every outer branch splits into two end branches, and each end branch splits into two final leaves. Every 10 cleared waves grants a path unlock, and the player chooses an available connected branch and its permanent bonus.
- Selectable path segments turn yellow. Hover over any path at any time to see its bonus and unlock requirements, then click a yellow path to unlock it.
- Hold left-click and drag to move the camera. Use the mouse wheel to zoom around the cursor.
- Each scroll-wheel step changes zoom noticeably, with a range from close-up building views to a full-map overview.
- Use the HUD speed button to cycle the game between 1x, 2x, and 3x speed.
- Right-click to cancel building placement.
- Progress is saved automatically. Gold, ore, cleared waves, game speed, placed buildings, and upgrades are restored the next time the game starts.
- Loading an existing save immediately starts a farming wave. Only a brand-new game begins at Wave 1 without spawning enemies.

## Structure

- `Assets/Scripts/Core` - bootstrap, balance, large-map data, economy, waves, placement, and camera
- `Assets/Scripts/Gameplay` - triangle defenses, ore collectors, enemies, and smooth runtime shapes
- `Assets/Scripts/UI` - minimal HUD, build controls, and upgrade panels

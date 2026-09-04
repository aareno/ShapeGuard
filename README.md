# Shape Guard

A minimal, top-down 2D tower-defense prototype for Unity 6.

## Requirements

- Unity `6000.4.6f1` (Unity 6.4)
- Git LFS, installed before cloning

## Getting Started

```bash
git lfs install
git clone https://github.com/aareno/Game.git
```

Open the cloned folder from Unity Hub using Unity `6000.4.6f1`. Unity restores the packages listed in `Packages/manifest.json` automatically. Then open `Assets/Scenes/SampleScene.unity` and press Play.

## Play

Runtime bootstrap creates the game automatically.

- Buy Triangle Defenses with ore and Ore Collectors with gold.
- Triangle Defenses attack automatically. Killing red-circle enemies awards gold.
- Ore Collectors generate ore. Defense upgrades cost ore; collector upgrades cost gold.
- Press **Start Wave** to begin progression. Wins automatically continue to the next wave until the core falls.
- After a progression loss, the last cleared wave repeats as a farming wave. Start Wave queues another progression run after the current farm wave.
- The core sits at the center of a large 124-path skill tree: four primary branches radiate outward through multiple split tiers, and each of the previous 32 end branches now splits into two final leaves. Every 10 cleared waves grants a path unlock, and the player chooses an available connected branch and its permanent bonus.
- The outer edge tier uses a wide 15-degree split, and each of those endpoints fans into a distinct 5-degree terminal pair.
- Enemies emerge from red portals at the outermost unlocked endpoints. Extending a branch moves its portal outward, while splitting a branch creates multiple fronts; deeper and additional fronts increase wave pressure, and longer routes make enemies faster.
- Selectable path segments turn yellow. Hover over any path at any time to see its bonus and unlock requirements, then click a yellow path to unlock it.
- Progression uses idle-game scaling: building and upgrade prices grow exponentially, collectors provide steady but controlled ore growth, enemy health outpaces gold rewards, and farming waves supply the resources needed to break later progression walls.
- Hold left-click and drag to move the camera. Use the mouse wheel to zoom around the cursor.
- Each scroll-wheel step changes zoom noticeably, with a range from close-up building views to a full-map overview.
- Use the HUD speed button to cycle the game between 1x, 2x, and 3x speed.
- Open **Settings** from the game controls to adjust master volume, display mode, resolution, V-Sync, and the frame-rate limit. Settings are saved separately from game progress.
- Right-click to cancel building placement.
- Click a defense or collector to select it. Hold it briefly to reposition it, then release on a valid location; invalid drops return it to its original position.
- Progress is saved automatically. Gold, ore, cleared waves, game speed, placed buildings, and upgrades are restored the next time the game starts.
- Loading an existing save immediately starts a farming wave. Only a brand-new game begins at Wave 1 without spawning enemies.

## Structure

- `Assets/Scripts/Core` - bootstrap, balance, large-map data, economy, waves, placement, and camera
- `Assets/Scripts/Gameplay` - triangle defenses, ore collectors, enemies, and smooth runtime shapes
- `Assets/Scripts/UI` - minimal HUD, build controls, and upgrade panels

## Development Notes

- Commit Unity `.meta` files alongside their matching assets.
- Do not commit generated folders such as `Library`, `Temp`, `Logs`, or `UserSettings`.
- Large binary asset types are configured for Git LFS in `.gitattributes`.
- Player progress and settings are stored locally and are not part of the repository.

## License

No license has been selected yet. The source is publicly viewable, but no permission to copy, modify, or redistribute it is granted. Add a license before accepting outside contributions or allowing reuse.

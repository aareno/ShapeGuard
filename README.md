# Meadow Guard

A self-contained, top-down 2D tower-defense prototype for Unity 6.

## Play

Open `Assets/Scenes/SampleScene.unity` and press Play. The game bootstraps itself, so no scene setup is required.

- Wave 1 starts automatically and repeats as a gold-farming wave.
- Build cannons, gold collectors, and ore drills from the bottom bar.
- Click a building to upgrade it. Cannons use ore; collectors use gold.
- **Start Wave** queues the next challenge after the current farm wave ends.
- Clear a challenge to promote it to the new farm wave. If the core falls, the last cleared wave resumes.
- Right-click while placing to cancel.

## Structure

- `Assets/Scripts/Core` — bootstrap, balance, and wave/resource state
- `Assets/Scripts/Gameplay` — buildings, enemies, and runtime visuals
- `Assets/Scripts/UI` — HUD and player controls
- `Assets/Resources/Art` — generated battlefield art

Most tuning values are centralized in `Balance` or grouped near the top of `GameController`.

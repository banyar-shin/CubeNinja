# CubeNinja

Unity 6 arcade starter where cubes launch from the bottom of the screen, fall under gravity, and are cleared by clicking them.

## Quick Start

1. Open this folder in Unity 6.0. The project was last exercised on `6000.0.72f1`.
2. Open `Assets/_Game/Scenes/CubeNinja.unity`.
3. Press Play.
4. Click `START RUN`.
5. Hit green and red cubes before they fall below the screen. Avoid clicking black danger cubes.

If Unity opens the TextMesh Pro importer, exit Play Mode before importing TMP essentials.

## Current Gameplay

- Green regular cubes score 1 point before combo.
- Red bonus cubes score 2 points before combo.
- Black danger cubes cost 1 life when clicked, but are safe when missed.
- Scoring hits inside the 0.5 second combo window increase the multiplier.
- Missed scoring cubes cost 1 life. The run ends at 0 lives.
- High score is saved locally with `PlayerPrefs` under `CubeNinja.HighScore`.
- Score, combo, miss, start, and game-over sounds are generated at runtime.

## Presentation

- The shrine background is loaded from `Assets/_Game/Resources/Backgrounds/shrine_background.jpg`.
- Cubes use smooth translucent unlit materials with no reflection.
- Cube outlines are built from runtime black edge geometry on each cube target.
- Cubes reflect off the left and right screen bounds to stay in view.
- The UI is generated at runtime with UGUI and TextMesh Pro.
- Menu, HUD, game-over, button, and popup text use synchronized black backing text layers for readable borders.
- The red edge glow flashes on life loss and remains visible on game over.
- Clicks spawn a small white pixel burst at the cursor.

## Project Structure

```
Assets/_Game/
|-- Core/       Pure runtime primitives such as combo scoring
|-- Data/       ScriptableObject tuning data and cube type definitions
|-- Editor/     Validation and starter content builder tools
|-- Gameplay/   Cube target behaviour, pooling helpers, and audio feedback
|-- Resources/  Runtime-loaded visual assets
|-- Run/        Spawning, score, lives, high score, background, and run state
|-- Scenes/     Playable scene
|-- Tests/      Focused EditMode tests
`-- UI/         Runtime UGUI/TMP HUD, menus, popups, and effects
```

The project keeps the useful starter conventions: module asmdefs, explicit dependency edges, small scenes, ScriptableObject tuning, and testable pure C# scoring logic.

## Tuning

Cube values, spawn weights, launch velocity, spawn interval, lives, cube scale, miss padding, and combo window are in:

`Assets/_Game/Data/Configs/DefaultCubeSpawnSettings.asset`

Use `CubeNinja > Validate Definitions` to check config assets, or `CubeNinja > Rebuild Starter Content` to regenerate the scene/config/prefab from editor code.

## Tests

EditMode tests cover combo scoring in `Assets/_Game/Tests/EditMode/ScoreComboTrackerTests.cs`.

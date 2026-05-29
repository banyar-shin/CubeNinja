# Architecture

## Runtime Split

- `CubeNinja.Core`
  - combo scoring and pure run math primitives
- `CubeNinja.Data`
  - ScriptableObject definitions for cube types and spawn tuning
- `CubeNinja.Gameplay`
  - cube target behaviour, click/miss callbacks, and component pooling
- `CubeNinja.Run`
  - run lifecycle, spawning, score, lives, and game over handling
- `CubeNinja.UI`
  - IMGUI HUD, life cubes, and combo popups

## Rules

- Keep tuning in ScriptableObjects rather than hard-coded scene state.
- Keep score/combo math testable outside Unity scene objects.
- Keep scenes small; the run director creates default services when references are missing.
- Use additive component behaviour instead of inheritance-heavy target types.
- Keep assembly definition references explicit and one-directional.

## Dependency Graph

```
Core
Data
Gameplay -> Data
UI
Run -> Core, Data, Gameplay, UI
Editor -> Data, Gameplay, Run, UI
Tests.EditMode -> Core
```

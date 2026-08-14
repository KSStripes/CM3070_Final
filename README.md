# End of Shift

CM3070 Final Project  
University of London  
Author: Kristin Schumann

## Overview

End of Shift is a Unity 6.3 procedural 3D isometric roguelike prototype. The player experiences a one-week probation period in a generated office, completing daily workplace tasks while managing Resolve and avoiding stressful NPC interruptions.

The project adapts an earlier dungeon roguelike prototype into an office/workday setting. The focus is procedural content generation, game-system integration, and evaluation rather than a full commercial game.

## How To Run

Open the project in Unity 6.3.

Main scene:

- `Assets/Scenes/OfficeScene.unity`

Supporting scenes:

- `Assets/Scenes/ProtoScene.unity`
- `Assets/Scenes/Dungeon1.unity`

Controls:

- Move with `WASD` or arrow keys.

## Core Features

- Hybrid procedural generation using BSP and cellular automata.
- Flood-fill validation for reachable layouts.
- Procedural room role assignment for office areas.
- Procedural placement of props, NPCs, pickups, quest items, and task markers.
- Monday-to-Saturday workday loop.
- Quest system with delivery, marker, and collect-only tasks.
- Resolve system with damage, recovery pickups, low-Resolve slowdown, and feedback warnings.
- Six workplace NPC types with simple LongLine/Wander patrol behaviour.
- Role-specific NPC pressure comments shown through the feedback panel.
- Office-themed UI/HUD with task list, inventory, Resolve bar, feedback, and PCG report stats.
- UI/gameplay sound effects and day-specific low-stress background music.

## Project Structure

- `Assets/Scripts/PCG`  
  Shared procedural generation algorithms and layout data.

- `Assets/Scripts/Core`  
  Shared game flow, player, camera, UI, health/Resolve, and audio systems.

- `Assets/Scripts/Office`  
  Office-specific generation wrapper, HUD, layout planning, prop placement, NPCs, quests, and report stats.

- `Assets/Scripts/Dungeon1`  
  Earlier dungeon gameplay reference scene.

- `Assets/Scripts/ProtoRuntime`  
  Lightweight PCG inspection scene runtime.

- `Assets/Prefabs/EndOfShift`  
  Runtime prefabs for the office prototype.

- `Assets/Data/OfficeQuests`  
  ScriptableObject quest definitions and quest database.

- `Assets/ThirdParty`  
  Curated runtime third-party assets required by the Unity project.

## Design Scope

The final game intentionally uses simple NPC patrol and proximity pressure rather than NavMesh pathfinding, raycast chasing, or full dialogue AI. This keeps the implementation appropriate for a third-year prototype and keeps the report focus on procedural generation and integrated gameplay systems.

## Report Evidence

The in-game report panel shows useful PCG metrics during play, including seed, room counts, reachable/walkable floor tiles, room role distribution, prop counts, quest counts, quest item/marker counts, and NPC role distribution.

Recommended evidence screenshots:

- `ProtoScene` showing PCG algorithm comparison.
- `OfficeScene` showing generated layouts and report stats.
- Quest/objective flow during a workday.
- NPC pressure and Resolve feedback.
- Day complete, game over, and final win states.

## Asset Credits

This project uses third-party Unity Asset Store assets, including Synty office/UI assets, Human Basic Motions FREE, Yughues materials, Simple Game UI Sounds, and downloaded background music packs. Full citation details are included in the final report.

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

## Design Scope

The project uses a hybrid PCG approach: BSP algorithms create readable office-room structure, cellular automata add more organic variation, and flood-fill validation ensures each generated layout remains reachable and playable. Office-specific systems then layer room roles, props, quests, pickups, NPCs, and exit placement onto the generated layout so each run changes while still supporting the same workday gameplay loop. 
The final game intentionally uses simple NPC patrol and proximity pressure rather than NavMesh pathfinding, raycast chasing, or full dialogue AI. This keeps the implementation focussed on procedural generation. With more time an actual NPC chasing behaviour or dialogue system could have been implemented. Similarly the quests are kept simple for this prototype, but could be extended with more time.

The in-game report panel shows useful PCG metrics during play, including seed, room counts, reachable/walkable floor tiles, room role distribution, prop counts, quest counts, quest item/marker counts, and NPC role distribution. This was added for scientific reporting.

## Asset Credits

This project uses third-party Unity Asset Store assets, including Synty office/UI assets, Human Basic Motions FREE, Yughues materials, Simple Game UI Sounds, and downloaded background music packs. 

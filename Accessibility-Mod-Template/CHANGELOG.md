# Changelog

## 1.0.1 - 2026-02-27

### Fixed

- Main menu prompt now correctly says "Press 1 for keyboard" instead of "Press a key"
- Suppressed control scheme tooltip ("alternate movements rotate camera...") from being announced on main menu transition

## 1.0.0 - 2026-02-27

Initial release.

### Features

- Room announcements: on entering a room, hear contents (enemies, NPCs, containers, traps), exits with relative directions, and nearby points of interest up to 3 steps away
- Rotation feedback: facing direction and what's ahead (open, blocked, enemy)
- Interaction hints: type-to-interact prompts for NPCs and objects
- Visited/new room tracking: each room tagged as "Visited" or "New" on entry
- Menu accessibility: main menu, pause menu, and settings fully navigable with announcements for items, tabs, option values, and toggles
- Combat: combat start/end, enemy info, spell casting, damage dealt/received, party HP, turn timer warnings, game over
- Inventory: tab cycling, item navigation with descriptions, quantities, new item flags, potion crafting mode
- Brain/spell screen: character switching, spell info with descriptions, skill/memory distinction
- NPC subtitles and tooltip/notification intercept: routes game text (subtitles, loot, location changes, tooltips) to screen reader
- Category-based navigation: cycle through Exits, NPCs, Enemies, Interactables, Scenery, and All Nearby with Ctrl+PgUp/PgDn and PgUp/PgDn, with BFS directions via End key
- Pathfinding: BFS scan up to 100 steps, cycle reachable targets (NPCs, containers, enemies, shops, fishing, cards), get turn-by-turn route descriptions
- GPS mode: step-by-step navigation guidance with automatic recalculation on deviation
- Breadcrumb backtracking: retrace to last junction (room with 3+ exits)
- Scenery filtering: decorative objects (floormist, hanginglight, etc.) separated from interactable containers
- Debug mode: toggle with F11 for detailed logging

### Controls

- F1: Help
- F2: Repeat room info
- F3: Repeat menu info
- F4: Repeat last combat announcement
- F5: Party HP status
- F6: Turn timer
- F7: Enemy info
- F8: Repeat inventory or brain info
- F9: Repeat last notification
- F10: Pathfind (scan/cycle targets, Enter for route)
- Shift+F10: Toggle GPS mode
- Ctrl+F10: Retrace to last junction
- F11: Toggle debug mode
- Ctrl+PgUp/PgDn: Cycle navigation categories
- PgUp/PgDn: Cycle items within category
- End: Get directions to selected item

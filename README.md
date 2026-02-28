# CryptmasterAccess

Accessibility mod for Cryptmaster -- makes the game playable with screen readers (NVDA, JAWS).

## Status

Version 1.3.0 -- all core features implemented. Covers menus, room exploration, combat, inventory, NPC text, pathfinding, and word puzzle accessibility.

## Features

- Room announcements with contents, exits, and nearby points of interest
- Full menu accessibility (main menu, pause, settings)
- Combat: enemy info, spell casting, damage, party HP, turn timer
- Inventory and brain/spell screen navigation
- NPC subtitle and notification intercept
- Word puzzle accessibility: letter count, revealed/blank positions, auto-updates on reveals
- BFS pathfinding with GPS-guided navigation and breadcrumb backtracking
- Visited/new room tracking
- Category-based navigation (Exits, NPCs, Enemies, Interactables, Scenery)
- Debug logging (F11)

## Installation

1. Install MelonLoader v0.7.1 for Cryptmaster
   - Download: https://github.com/LavaGang/MelonLoader.Installer/releases
2. Run the game once to create the Mods folder, then close it
3. Download the latest release ZIP from the Releases page
4. Copy all files from the ZIP into your Cryptmaster game folder:
   - `CryptmasterAccess.dll` goes into the `Mods` subfolder
   - `Tolk.dll` and `nvdaControllerClient64.dll` go into the main game folder (next to the .exe)
5. Start the game -- you should hear "Cryptmaster Access loaded. F1 for help."

## Controls

- F1: Help (lists all key bindings)
- F2: Repeat room info
- Ctrl+F2: Repeat word puzzle state
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

## Building from Source

Requirements:
- .NET SDK (targeting net472)
- Cryptmaster installed via Steam (references game DLLs)

```
dotnet build CryptmasterAccess.csproj
```

The build auto-copies the DLL to the game's Mods folder.

## Third-Party Components

- MelonLoader (Apache 2.0) -- mod loader
- Harmony (MIT) -- runtime patching, included with MelonLoader
- Tolk (LGPL 2.1) -- screen reader communication library

## License

MIT -- see LICENSE file.

CryptmasterAccess - Accessibility Mod for Cryptmaster
Version: 1.3.0
Author: Zersiax (Florian Beijers)

WHAT THIS MOD DOES
==================
Makes Cryptmaster playable with a screen reader (NVDA, JAWS).
- Room announcements: hear contents, exits, and nearby objects
- Menu accessibility: navigate all menus with keyboard
- Combat: enemy info, spell casting, damage, HP, timer warnings
- Inventory and spell screen navigation
- NPC subtitles and game notifications routed to screen reader
- Pathfinding: find routes to NPCs, containers, shops, and more
- GPS mode: step-by-step navigation with automatic recalculation
- Auto-walk to nearest unexplored room
- Visited/new room tracking
- Word puzzle accessibility: hear letter count, revealed/blank positions, auto-updates
- Scenery separated from interactable objects

REQUIREMENTS
============
- Cryptmaster (Steam version tested)
- MelonLoader v0.7.1 (https://github.com/LavaGang/MelonLoader.Installer/releases)
- A screen reader (NVDA recommended, JAWS also works)

INSTALLATION
============
1. Install MelonLoader for Cryptmaster (run the installer, select your game)
2. Start the game once to create the Mods folder, then close it
3. Copy ALL files from this ZIP into your Cryptmaster game folder:
   - CryptmasterAccess.dll goes into the Mods subfolder
   - Tolk.dll goes into the main game folder (where CryptMaster.exe is)
   - nvdaControllerClient64.dll goes into the main game folder
4. Start the game - you should hear "Cryptmaster Access loaded. F1 for help."

CONTROLS
========
F1:           Help (lists all bindings)
F2:           Repeat room info
Ctrl+F2:      Repeat word puzzle state (letter count + revealed/blank)
F3:           Repeat menu info
F4:           Repeat last combat announcement
F5:           Party HP status
F6:           Turn timer
F7:           Enemy info
F8:           Repeat inventory or brain info
F9:           Repeat last notification
Ctrl+F9:      Auto-walk to nearest unexplored room (any key cancels)
F10:          Pathfind - scan for targets, press again to cycle, Enter for route
Shift+F10:    Toggle GPS mode (step-by-step navigation)
Ctrl+F10:     Retrace to last junction
F11:          Toggle debug mode
Ctrl+PgUp/PgDn: Cycle navigation categories
PgUp/PgDn:    Cycle items within category
End:          Get directions to selected item

TROUBLESHOOTING
===============
- No speech from screen reader: Make sure Tolk.dll is in the main game folder
  (next to CryptMaster.exe), not in the Mods folder.
- Mod doesn't load: Check MelonLoader\Latest.log in the game folder for errors.
  Make sure MelonLoader v0.7.1 is installed.
- Game crashes on startup: Remove the mod DLL from the Mods folder and report
  the issue with your MelonLoader\Latest.log file.

KNOWN ISSUES
============
- Some features are still being tested. Please report bugs!
- F12 is used by the game for a special reset function in menus.

SOURCE CODE AND BUG REPORTS
============================
https://github.com/zersiax/CryptmasterAccess

LICENSE
=======
MIT License - see LICENSE.txt in the source repository.
Tolk library is LGPL 2.1.
MelonLoader is Apache 2.0.

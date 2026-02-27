# Cryptmaster - Game API Documentation

## Overview

- **Game:** Cryptmaster
- **Engine:** Unity 2021.3.39f1
- **Runtime:** net35 (from MelonLoader log)
- **Architecture:** 64-bit
- **Developer:** PaulHartandLeeWilliams (exact from MelonLoader log)
- **Game Version:** 1.116

---

## 1. Singleton Access Points

### Architecture: Hub-and-Spoke

GameManager is the central hub — NOT a singleton. Other managers hold a `myGameManager` back-reference. Access subsystems through GameManager.

### GameManager (Master Controller)

- **File:** GameManager.cs
- **Type:** MonoBehaviour (scene-loaded, not singleton)
- **Access:** Find via `GameObject.FindObjectOfType<GameManager>()` or cached reference
- **Manager references:**
  - `myEventManager` (EventManager)
  - `myAudioManager` (AudioManager)
  - `myMenuManager` (MenuManager)
  - `mySaveManager` (SaveManager)
  - `mySpellManager` (SpellManager)
  - `myWorldManager` (WorldManager)
  - `myTextManager` (TextManager)
  - `myEditManager` (EditManager)
  - `myMiniGameManager` (MiniGameManager)
- **Game objects:**
  - `myCryptMaster` (CryptMaster) — main NPC
  - `myInventory` (Inventory)
  - `myOverworld` (Overworld)
  - `myBrainController` (BrainController)
  - `myVirtualKeyboard` (VirtualKeyboard)
  - `myTwitchUI` (TwitchUI)
- **Input:**
  - `rewiredControl` (Rewired Player)
  - `myRewiredManager` (InputManager)
- **UI containers:** `inventoryContainer`, `enemyBattleContainer`, `worldContainer`, `optionsContainer`

### True Singletons

- `AGSaveLoadManager.Instance` — platform save/load (persists across scenes)
- `SteamManager.Instance` / `SteamManager.Initialized` — Steam API (persists)
- `AGClientPerformance.Instance` — screen resolution cycling (persists)

### DontDestroyOnLoad Objects

These persist across scene loads:
- SaveManager
- AudioManager
- SpellManager
- SteamManager
- TwitchIRC (optional)

### Scene Management

- Bootstrap: `BootStrap.cs` — loads "Loading" scene via Addressables
- Scene loading: `Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Single)`
- Loading screen: `LoadManager` with `sceneToLoad`, `percentLoaded` (TextMeshProUGUI)

---

## 2. Game Key Bindings (DO NOT override in mod!)

**CRITICAL: These keys are used by the game. The mod MUST NOT bind them!**

### Rewired Action Names (Primary Input System)

**Movement:**
- `"moveUp"` — forward
- `"moveDown"` — backward
- `"moveLeft"` — left
- `"moveRight"` — right

**Menu/Confirm:**
- `"enter"` — confirm/activate
- `"enterSingle"` — single press enter
- `"escape"` — exit/cancel
- `"backspace"` — delete

**Screen Toggles:**
- `"mapScreen"` — toggle map
- `"macroMapScreen"` — alternative map toggle
- `"inventoryScreen"` — toggle inventory
- `"hear"` — audio detection action

**Character Selection:**
- `"brainScreenJoro"` — character 0 (Joro)
- `"brainScreenSyn"` — character 1 (Syn)
- `"brainScreenMaz"` — character 2 (Maz)
- `"brainScreenNix"` — character 3 (Nix)

**Joypad-Specific:**
- `"joypadBrainLeft"` / `"joypadBrainRight"` — brain screen nav
- `"joypadviewX"` / `"joypadviewy"` — camera rotation
- `"startjoypad"` / `"startkeyboard"` — input mode
- `"twitchShow"` — Twitch chat toggle

### Keyboard Fallback Keys (always blocked)

**Movement:** Arrow keys, WASD+Shift
**Numbers:** Alpha1-4, Keypad1-4 (character selection), Alpha5
**Confirm/Cancel:** Return, KeypadEnter, Space, Escape, Tab
**Edit:** Backspace, Delete
**Modifiers:** LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt
**Mouse:** Right mouse button (camera rotation)

### Input Blocking Patterns

- `blockRot` (float) — blocks rotation when > 0
- `blockSpellStart` (float) — blocks spell initiation
- `blockActions` (bool) — general action blocking
- `blockEnter` (float, VirtualKeyboard) — blocks enter key
- `NoBlockedKeys()` — returns true when no game input keys pressed (used for character typing)

### ControlManager Helper Methods

Wrapper methods checking both Rewired and keyboard:
- `Enter(mode)`, `EnterSingle(mode)`
- `UpArrow(keyboardOnly, isHeld, mode)`, `DownArrow(...)`, `LeftArrow(...)`, `RightArrow(...)`
- `MapButton()`, `ShowMenuExit(action, mode)`
- `BrainLeft()`, `BrainRight()`
- `ShiftHold()` — keyboard only

---

## 3. Safe Keys for Mod

**F-keys (all free):**
- F1: Help (mod)
- F2-F11: Available for mod features
- F11: Debug toggle (mod)
- F12: RESERVED by game (menu reset)

**Letter keys (not blocked by game):**
- B, C, F, G, H, I, J, K, L, M, N, O, P, Q, R, T, U, V, X, Y, Z
- WARNING: These overlap with the virtual keyboard typing system! Only use when virtual keyboard is NOT active.

**Numeric keys:**
- Alpha5-9, Alpha0: Partially available (5 has some use, 6-0 appear free)
- Keypad5-9, Keypad0: Available

**Other:**
- Home, End, PageUp, PageDown: Not used by game
- Insert: Not used
- Numpad operators (+, -, *, /): Not used

**CAUTION — context-dependent:**
- Letter keys during gameplay typing — game uses them for word input
- Any key when `NoBlockedKeys()` returns false

---

## 4. UI System

### Canvas Architecture

- Two main canvases: `gameplayCanvas` and `menuCanvas` (in MenuManager)
- Canvas controllers: `mainMenuCanvasController`, `tooltipCanvasController`
- All text uses **TextMeshPro** (TMP)

### Text Components

- **TextMeshProUGUI** — canvas/UI text
- **TextMeshPro** — world/3D text
- **TMP_InputField** — text input fields

### Menu System (MenuManager)

- **State tracking:** `mainMenuState` (int)
- **Menu opening:** `GameObject.SetActive(true/false)` + canvas toggling
- **Navigation:** Arrow-based via ControlManager with `"menu"` mode
- **Selection tracking:** `currentMount` (int) in selector objects

**Selector objects:**
- `optionsSelector` — options menu
- `mainMenuAdditionalSelector` — additional menu
- `exitMenuSelector` — exit confirmation
- `debugSelector` — debug menu

**Navigation pattern:**
```csharp
myControlManager.LeftArrow(_keyboardOnly: false, isHeld: false, "menu")
myControlManager.RightArrow(_keyboardOnly: false, isHeld: false, "menu")
myControlManager.MenuEnter("menu")
```

### MenuBacker (Menu Item)

- `baseName` / `translatedBaseName` (string) — item text
- `myText` (TextMeshProUGUI) — main text
- `myTopText` (TextMeshProUGUI) — header text
- `outerText` (TextMeshProUGUI) — secondary text
- `isActive`, `isDisabled`, `isRecommended` — state flags
- `isToggle` + `myToggleBox` — toggle options
- `glowImage` (Image) — selection highlight

### GameOption (Selectable Option)

- `myBacker` (MenuBacker) — visual container
- `mySlider` (OptionsSlider) — multi-choice selector
- `myOptionText` (TextMeshProUGUI) — description
- `myInput` (TMP_InputField) — text input
- `currentlySelectedOption` (int) — selection index

### OptionsSlider (Multi-Choice)

- `allSliderOptions` (List<string>) — raw options
- `allTranslatedOptions` (List<string>) — translated options
- `allOptionsNodes` (List<optionsNode>) — visual nodes with `isSelected`

### Text Display (TextManager)

- `locationText` (TextMeshProUGUI) — location name
- `topText` (TextMeshPro) — top screen text
- `bottomText` (TextMeshPro) — bottom screen text
- `middleText` (TextMeshPro) — center screen text
- `memoryText` (TextMeshPro) — memory/status
- `lootTextContainer` (GameObject) — loot display

### World Text System

- `worldTextContainer` (GameObject) — 3D text parent
- `allWords` (List<TextWord>) — active words
- `TextWord.myTextMesh` (TextMeshProUGUI) — word text
- Material states: `worldTextMat`, `worldTextHighlightMat`, `worldTextSelectedMat`

### Localization: ReturnLookup

```csharp
SaveManager.ReturnLookup(string lookupType, string ENGLookup)
```
- Lookup types: `"sentence"`, `"word"`, `"minijoy"`, `"tooltip"`, `"quote"`
- Text loaded from external language files at `Application.persistentDataPath`
- 30+ tag types in language files

### Virtual Keyboard (Word Input)

- `VirtualKeyboard` — 3D on-screen keyboard
  - `allVKeys` (List<VirtualKey>) — all keys
  - `allVSkillKeys` (List<VirtualKey>) — skill keys
  - `doesUseAZERTY` — layout support
  - `isActiveDetecting` — blocks movement during input
- `VirtualKey` — individual key
  - `myChar` (string) — character
  - `myText` (TextMeshPro) — display
  - `isEnter`, `isActive` — state
  - `Push()` — press animation

### Tooltip System

- `Tooltip` class with `SubToolTip` inner class
- `SetTooltipState(string stateName)` — activate by name
- Named states: "controls", "exit", "loot", "levelup", "map", "fishingtutorial", "cards", "cyclecards", "tumbling", "outofsouls", "firstAbility"
- `allTranslateableText` (List<TextMeshProUGUI>) — localized text
- Auto-timeout via `doesKill` + `killTime`
- Separate keyboard/joypad control displays

### Subtitle System

- `SubtitleManager` — manages subtitle display for dialogue
- Subtitles from `VO_<LANG>.txt` files, keyed by audio clip name

---

## 5. Game Mechanics - Feature Catalog

### Notable Libraries

- **Rewired** — input (Rewired_Core.dll, Rewired_Windows.dll)
- **Cinemachine** — camera
- **Heathen Steamworks** — Steam integration
- **Addressables** — asset/scene loading

### Core Gameplay Systems

- **CryptMaster** — main NPC, speech/dialog system
- **Inventory** — item management
- **BrainController** — spell/ability menu per character (Joro, Syn, Maz, Nix)
- **CombatManager** — battle system
- **SpellManager** — magic/ability system
- **MiniGameManager** — fishing, cards, tumbling mini-games
- **EventManager** — game event processing via `ProcessEvent(string)`
- **WorldManager** — map chunks, walls
- **OverworldManager** — overworld map with chunks

### Event Types (EventManager.ProcessEvent)

- text, print, iprint, logprint, emote, emoteevent

---

## 6. Status and Notifications

### Text Display

- Via TextManager fields (locationText, topText, bottomText, middleText, memoryText)
- Loot notifications via `lootTextContainer`
- Warning messages via `warningMessages` (TextMeshProUGUI)

### Tooltip Notifications

- Contextual tooltips via `Tooltip.SetTooltipState(string)`
- 13+ named tooltip states for different game events

---

## 7. Audio System

- `AudioManager` (via `GameManager.myAudioManager`)
- `allAudioFolders` (List<AudioGroup>) — audio groups by language/context
- `allFootstepTypes` (List<FootstepController>) — surface footsteps
- `allDictionaryAudio` (List<DictionaryAudio>) — word/item audio clips
- Persists across scenes (DontDestroyOnLoad)

---

## 8. Save and Load

- `SaveManager` (via `GameManager.mySaveManager`) — game state, flags
- `AGSaveLoadManager.Instance` — platform-level save I/O
- `allSaveFlags` (List<SaveFlag>) — state tracking flags
- `allDynamicChecks` (List<DynamicCheck>) — conditional checks
- `allPredictiveTextStart` (List<string>) — autocomplete predictions
- Speech recognition: uses `KeywordRecognizer`

---

## 9. Event Hooks for Harmony Patches

### Best Patch Points (to be refined during feature implementation)

- `MenuManager` navigation methods — intercept menu selection changes
- `SaveManager.ReturnLookup()` — intercept all localized text
- `Tooltip.SetTooltipState()` — intercept tooltip displays
- `TextManager` text property setters — intercept text display changes
- `EventManager.ProcessEvent()` — intercept game events
- `SubtitleManager` — intercept subtitle display
- `VirtualKeyboard` — intercept typing state changes

### Update Loops

- `GameManager.Update()` — main game loop
- `MenuManager.Update()` — menu input processing

---

## 10. Localization

- All text via `SaveManager.ReturnLookup(lookupType, ENGKey)`
- External language files at `Application.persistentDataPath`
- 30+ tag types: `[WORDLOOKUP]`, `[SENTENCELOOKUP]`, `[TOOLTIPWORDLOOKUP]`, `[MENUOPTION]`, `[SPELLLOOKUP]`, `[HINT]`, `[QUESTHINT]`, etc.
- Subtitle files: `VO_<LANG>.txt`
- 9 lookup categories in `ReturnLookup()`

---

## 11. Code Examples

### Access GameManager

```csharp
// Find GameManager (cache this!)
var gm = UnityEngine.Object.FindObjectOfType<GameManager>();
if (gm == null) return;

// Access subsystems
var menu = gm.myMenuManager;
var save = gm.mySaveManager;
var text = gm.myTextManager;
var inventory = gm.myInventory;
```

### Read Menu Selection

```csharp
// Check current menu selection (pattern)
var selector = menuManager.optionsSelector;
int currentItem = selector.currentMount;
var backers = selector.allBackers;
if (currentItem >= 0 && currentItem < backers.Count)
{
    string itemText = backers[currentItem].myText.text;
}
```

### Harmony Patch Example

```csharp
// Patch ReturnLookup to intercept all localized text
[HarmonyPatch(typeof(SaveManager), "ReturnLookup")]
class ReturnLookupPatch
{
    static void Postfix(string __result, string _lookupType, string _ENGLookup)
    {
        // __result contains the localized text
        DebugLogger.Log(LogCategory.Game, $"Lookup [{_lookupType}]: {_ENGLookup} -> {__result}");
    }
}
```

---

## 12. Known Issues and Workarounds

- GameManager is NOT a singleton — must be found via FindObjectOfType or cached from scene
- F12 used by game for menu reset — mod debug toggle moved to F11
- `isExitMenuAcitve` — typo is in original game code, keep as-is

### Game State Flags (on GameManager)

Key boolean flags for handler state checks:
- `isInSpellMode` — typing/spell mode
- `isExitMenuAcitve` — exit menu open (typo in game code)
- `isLooting` — looting state
- `isLevelingUp` — level up screen
- `isAtoZActive` — A-to-Z mode
- `isGetActive` — get/pickup mode
- `isCraftingActive` — crafting mode
- `isSpecialTextEntry` — special text entry mode
- `isInSpellCreationMode` — spell creation
- `isBlockingTyping` / `isScriptisBlockingTyping` — typing blocked
- `isAtTarget` — player at movement target
- `isQueingMovement` — queued movement
- `isWarping` — warp in progress
- `isBlinded` — blinded state
- `blockRot` (float, <= 0 means unblocked) — rotation blocking
- `blockActions` (bool) — general action blocking

Key methods:
- `isIngamePaused(string ignoreCheck)` — checks if game is paused (pass context string)
- `IsBlockedCheck(bool isCheckingBattle, bool isCheckingEditor)` — general block check

Key UI state:
- `myVirtualKeyboard.isActiveDetecting` — keyboard typing active
- `myMenuManager.gameplayCanvas.enabled` — gameplay canvas visible
- `myMenuManager.mainMenuState` — menu state int

---

## 13. Combat System (CombatManager)

### Key Fields

- `CombatManager.battleCounter` (float 0-100) — turn timer; at 100 enemy attacks
- `CombatManager.enemyTargetPlayer` (CharacterHUD) — which character enemy targets
- `GameManager.currentlyLoadedEnemy` (Enemy) — active enemy, null = no combat
- `GameManager.isInSpellMode` (bool) — player is typing a spell
- `GameManager.allCharacterUI` (List<CharacterHUD>) — 4 characters: Joro(0), Syn(1), Maz(2), Nix(3)
- `GameManager.allEnemyLettersActive` (List<NameDefinition>) — enemy HP = count of .isActive entries

### CharacterHUD Fields

- `myName` (string) — character name
- `nameHP` (int) — current HP (letters remaining)
- `totalNameHP` (int) — max HP (total letters in name)

### Enemy Fields

- `enemyName` (string) — display name
- `attackDamage` (int) — damage per attack
- `doesAttackAll` (bool) — AoE attack hits all characters (0.2s delay between)

### Key Methods (Patch Points)

- `CharacterHUD.RecieveDamage(int _damage)` — character takes damage (note: game typo "Recieve")
- `CombatManager.DamageEnemyWord(int _currentHealth, int _damage, string _element)` — enemy takes damage
- `SpellManager.CastSpell(string _spellName, CharacterHUD _optionalChar, Enemy _currEnemy, bool _isDiscovered)` — spell cast
- `CombatManager.KillEnemy(Enemy _enem, bool _doesKill)` — enemy defeated (only if _doesKill)
- `CombatManager.GameOver()` — party wipe

### Notes

- `RecieveDamage` starts a coroutine that decrements `nameHP` before first yield — HP is already updated in postfix
- Enemy HP = letters in name; damage removes letters via `DamageEnemyWord`
- `doesAttackAll` enemies hit all characters with 0.2s delay between hits

---

## 14. Inventory System (Inventory)

### Access

- `GameManager.myInventory` (Inventory) — inventory controller

### Key Fields (Inventory)

- `isShopping` (bool) — true when inventory screen is open
- `currentShopItemY` (int) — current tab index (Up/Down arrows)
- `currentShopItemX` (int) — current item index within tab (Left/Right arrows)
- `currentInventoryScroll` (InventoryScroll) — currently active tab object
- `isPotionCreationActive` (bool) — potion crafting mode active

### InventoryScroll (Inner Class — Tab)

- `inventoryDisplayNameTranslated` (string) — translated tab name
- `inventoryDisplayName` (string) — raw tab name
- `inventoryScrollName` (string) — internal name
- `currentShopContainer` (ShopItemContainer) — currently highlighted item
- `allShopContainers` (List<ShopItemContainer>) — all items in this tab
- `allCollectableItems` (List<CollectableItem>) — all collectable item data
- `displayItemCounts` (bool) — whether to show item counts

Tab names: "warps", "quests", "hints", "ingredients", "potions", "cards"

### ShopItemContainer (Item Display)

- `myNameText` (string) — raw item name
- `myTranslatedNameText` (string) — translated item name
- `myDescText` (string) — item description
- `isHighlighted` (bool) — currently selected
- `yPosition` (int) — position in list
- `myCollectableItem` (Inventory.CollectableItem) — backing data
- `isVisible` (bool) — visible on screen
- `hasRevealed` (bool) — has been revealed

### CollectableItem (Inner Class — Item Data)

- `collectableDisplayName` (string) — human-readable name
- `CollectableDescription` (string) — description (note: capital C)
- `ingredientCount` (int) — quantity (ingredients)
- `isCastable` (bool) — can be used/cast
- `castContext` (string) — action label (e.g., "use", "add")
- `isNewItem` (bool) — marked as new
- `isWarp` (bool) — is a warp item
- `hasCollected` (bool) — has been collected

### Navigation

- Up/Down arrows change tabs (currentShopItemY)
- Left/Right arrows change items within tab (currentShopItemX)
- `UpdateShopPos()` sets currentShopContainer and updates header/description text on MenuManager

---

## 15. Brain Screen (BrainController)

### Access

- `GameManager.myBrainController` (BrainController) — brain screen controller
- `GameManager.isInSpellCreationMode` (bool) — also set when brain opens

### Key Fields (BrainController)

- `isActive` (bool) — brain screen is open
- `currentCharacter` (int) — character index: 0=Joro, 1=Syn, 2=Maz, 3=Nix
- `xPos` (int) — grid column position
- `yPos` (int) — grid row position
- `totalX` (int) — grid width
- `totalY` (int) — grid height
- `allSpellBoxes` (List<SpellBox>) — all grid cells
- `myFlavorText` (TextMeshProUGUI) — flavor text display
- `myDescriptionText` (TextMeshProUGUI) — description text display

### SpellBox (Inner Class — Grid Cell)

- `xPos` (int) — column position in grid
- `yPos` (int) — row position in grid
- `isActive` (bool) — has a spell (false = empty cell)
- `isSelected` (bool) — currently highlighted
- `myText` (TextMeshPro) — spell name display
- `description` (string) — spell description ("???" if not revealed)
- `flavor` (string) — spell flavor text
- `myBrainNode` (BrainNode) — backing node data

### BrainNode

- `isSkill` (bool) — true = skill, false = memory
- `isActive` (bool) — node active state
- `hasRevealed` (bool) — spell has been revealed
- `mySkillNameText` (TextMeshPro) — skill name display

### Navigation

- Arrow keys move in grid (xPos/yPos)
- Keys 1-4 switch characters (currentCharacter)
- `CheckNewPos()` validates position and calls `UpdateColors()` which sets header/description text

---

## 16. Subtitle System (SubtitleManager)

### Access

- `GameManager.mySubtitleManager` (SubtitleManager) — subtitle controller

### Key Fields

- `mySubtitleText` (TextMeshProUGUI) — the displayed subtitle text element
- `isRunning` (bool) — subtitle currently being displayed
- `isActive` (bool) — subtitles enabled in settings
- `currentText` (string) — stored current text
- `unloadTimer` (float) — countdown to hide subtitle
- `allSubtitles` (List<SubtitleText>) — all loaded subtitle entries

### SubtitleText (Inner Class)

- `audioName` (string) — audio clip name key
- `voText` (string) — the subtitle text content
- `voDelay` (float) — delay before displaying

### Key Methods

- `LoadSubtitle(string _subtitleName, string endText, string startText)` — loads and displays subtitle for an audio clip; starts `SetSubtitles` coroutine
- `SetSubtitles(SubtitleText t, string endText, string startText)` — coroutine: waits `voDelay`, then sets `mySubtitleText.text` to `(startText + voText + endText).ToUpper()`, sets `isRunning = true`
- `UnloadSubtitles()` — hides subtitle, sets `isRunning = false`
- `ReturnSubtitle(string _subtitleName)` — returns subtitle text without displaying

### Notes

- Subtitle text is assembled as `startText + voText + endText` and uppercased
- Display has a delay (`voDelay`) before text appears — polling catches it after the coroutine yields
- Subtitles auto-hide after `unloadTime * voText.Length + additionalTimer` seconds

---

## 17. Tooltip System (Tooltip)

### Access

- `GameManager.myToolTip` (Tooltip) — tooltip controller

### Key Fields

- `currentTooltip` (int) — index of active tooltip in `allToolTips` (-1 = none)
- `allToolTips` (List<SubToolTip>) — all tooltip definitions

### SubToolTip (Inner Class)

- `toolTipName` (string) — identifier (e.g., "controls", "exit", "loot", "levelup", "map", "fishingtutorial", "cards", "cyclecards", "tumbling", "outofsouls", "firstAbility")
- `isActive` (bool) — currently visible on screen
- `hasCompleted` (bool) — tooltip has been shown and completed
- `hasBeenSet` (bool) — tooltip has been activated at least once
- `allTranslateableText` (List<TextMeshProUGUI>) — localized text content
- `allFlipBookObjects` (List<TextMeshProUGUI>) — flipbook-style animated text
- `doesKill` (bool) — auto-hides after timeout
- `killTime` / `baseKillTime` (int) — timeout countdown
- `blockInCombat` (bool) — hidden during combat
- `IgnoreCryptMaster` (bool) — shown even when CryptMaster is speaking

### Key Methods

- `SetTooltipState(string _stateName)` — activates tooltip by name, sets `currentTooltip` to its index
- `HideAll(bool isHiding)` — deactivates all, sets `currentTooltip = -1`
- `HideSpecificTooltip(string _targname)` — hides a specific tooltip by name

### Notes

- Tooltip text is pre-translated via `ReturnLookup("tooltip", ...)` in `Start()`
- Visibility is controlled by `isActive` flag, which depends on CryptMaster not speaking and not being in combat (for relevant tooltips)
- `SlowLoop()` runs every 1s to decrement `killTime` for auto-hiding tooltips

---

## 18. Loot Text (GameManager + TextManager)

### Access

- `GameManager.lootPromptText` (TextMeshProUGUI) — loot prompt display text

### Key Methods (TextManager)

- `SetLootText(string _lootText, float _hideDelay, bool _doesAutoHide, int _lootLength, bool _isLooting)` — sets loot prompt text and shows loot UI
- Loot text examples: "ENEMY has Dropped...", "CHARACTER adds a letter to their name!"

---

## 19. Location Text (TextManager)

### Access

- `TextManager.locationText` (TextMeshProUGUI) — location overlay text ("TO LOCATIONNAME")
- `TextManager.locationTextHeader` (TextMeshPro) — location header ("-HEADERNAME-")
- `TextManager.isLocationOn` (bool) — location text currently visible
- `TextManager.currMapLoc` (string) — current map location name

### Key Methods

- `SetLocationText(string _mapName)` — looks up level display name, sets `locationText.text` to "TO " + name
- `ClearLocationTextOverlay()` — clears location text and hides header

---

## 20. Not Yet Analyzed

- [ ] Mini-game mechanics (fishing, cards, tumbling)
- [ ] Overworld navigation mechanics
- [ ] Dialog/speech system (CryptMaster character)

---

## Change History

- **2026-02-24**: Initial creation during setup
- **2026-02-24**: Tier 1 analysis — structure, input system, UI system documented
- **2026-02-25**: Combat system documented (CombatManager, CharacterHUD, Enemy, SpellManager patch points)
- **2026-02-25**: Inventory system documented (Inventory, InventoryScroll, ShopItemContainer, CollectableItem)
- **2026-02-25**: Brain screen documented (BrainController, SpellBox, BrainNode)
- **2026-02-25**: Subtitle, tooltip, loot text, location text systems documented (SubtitleManager, Tooltip, TextManager)

# Cryptmaster -- Extracted Tutorial, Help, and Instructional Texts

> **NOTE:** These are extracted game texts from decompiled Cryptmaster source code, NOT mod documentation. Text content referenced here is largely defined in external language/data files loaded at runtime, not hardcoded in the source. The decompiled code reveals the *systems* and *keys* used for tutorials, hints, tooltips, and descriptions, along with some hardcoded string identifiers. Actual player-facing text is stored in localization files at `Application.persistentDataPath`.

---

## Table of Contents

- 1. Tutorial System
- 2. Tooltip System (In-World UI)
- 3. Hint System
- 4. Spell/Ability Descriptions
- 5. Inventory and Item Descriptions
- 6. Quest and Warp Descriptions
- 7. Menu Option Descriptions
- 8. Subtitle / Voice-Over System
- 9. Localization Architecture
- 10. Warning Texts
- 11. Game Data File Locations

---

## 1. Tutorial System

**Source files:** `GameManager.cs`, `SaveManager.cs`, `EventManager.cs`, `MiniGameManager.cs`, `Tooltip.cs`, `TestBot.cs`

### Tutorial Settings (Player-Configurable)

The game has two separate tutorial toggle settings, saved as options:

- `[TUTORIAL]` -- Controls display of gameplay tutorial tooltips. Stored in `SaveManager.tutorialOn` (bool). Toggled On/Off in the options menu.
- `[TUTORIALCONTROL]` -- Controls display of control-specific tutorial tooltips. Stored in `SaveManager.tutorialControlOn` (bool). Toggled On/Off in the options menu.

When either setting is OFF, the corresponding tooltips are hidden (slid off-screen) regardless of game state.

**Source:** `SaveManager.cs` lines 669-671, 2529-2538, 3744-3762, 4024-4025

### Tutorial Counter and Flags

- `GameManager.tutorialCounter` -- Incremented by the event `"enabletutorialflags"`. Tracks tutorial progression.
- `GameManager.tutorialWordCollection` -- Tracks word collection during tutorial.
- `GameManager.tutorialBlocker` -- Float value controlling tutorial blocking behavior.
- `GameManager.tutorialVeil` / `currentTutorialVeil` -- GameObjects for visual tutorial veil overlay.
- `GameManager.tutorialEffects` -- Bool toggled by the `"tutorialeffects"` event (triggers after 8-second delay, also unhides character bars).
- `GameManager.allTutorialCryptScriptOverrides` -- List of CryptmasterScript overrides active during tutorials.
- `TestBot.blocktutorial` -- Debug flag to block tutorials entirely.

**Source:** `GameManager.cs` lines 545-558, 770; `EventManager.cs` lines 194-195, 898, 1403-1404, 1527-1531; `TestBot.cs` line 17

### Tutorial Events (Triggered by Event System)

- `"enabletutorialflags"` -- Increments `tutorialCounter` (EventManager.cs line 194)
- `"disabletutorialmode"` -- Calls `myToolTip.ClearAll()` to dismiss all tooltips (EventManager.cs line 898)
- `"tutorialeffects"` -- Invoked with 8-second delay, sets `tutorialEffects = true` and unhides character bars (EventManager.cs lines 1403-1404, 1527-1531)

### Mini-Game Tutorials

#### Fishing Tutorial
- Flag: `"fishingTutorial"` (external flag, checked via `CheckFlag`)
- First time fishing: tooltip `"fishingtutorial"` shown, flag set so subsequent times show `"fishing"` tooltip instead
- **Source:** `MiniGameManager.cs` lines 1452-1465

#### Card Game Tutorial
- Flag: `"cardTutorial"` (external flag)
- Bool: `MiniGameManager.isInCardTutorialMode`
- Int: `MiniGameManager.cardTutorialState` (progresses through states 0, 1, then exits)
- Tutorial audio clips stored in `currentCardPlayer.allTutorialAudio` (list of audio clip names)
- State 0: Plays `allTutorialAudio[0]`
- State 1: Plays `allTutorialAudio[1]`
- After state 1: exits tutorial mode
- During tutorial mode, quip audio is suppressed
- **Source:** `MiniGameManager.cs` lines 72-74, 402-404, 1899-1904, 2091-2101, 2156, 2395

#### Card Cycle Tutorial
- Flag: `"cardDownTutorial"` (external flag)
- When the player has enough down-cards and the timer expires, tooltip `"cyclecards"` is shown and the flag is set
- **Source:** `MiniGameManager.cs` lines 1942, 3921-3932

#### Tumbling Mini-Game
- Tooltip `"tumbling"` shown when tumbling starts
- **Source:** `MiniGameManager.cs` line 1024

### Mini-Game Tutorial Data Structure
```
MiniGameManager.CardPlayer:
  bool hasTutorial
  List<string> allTutorialAudio  -- audio clip names for tutorial narration
```
**Source:** `MiniGameManager.cs` lines 72-74

---

## 2. Tooltip System (In-World UI)

**Source file:** `Tooltip.cs`

Tooltips are in-world UI elements that slide in/out based on game state. They are managed by the `Tooltip` class attached to a `GameManager.myToolTip` reference.

### Tooltip Data Structure (SubToolTip)
```
string toolTipName          -- identifier used by SetTooltipState()
GameObject tooltipContainer -- the UI container
bool isLowPriority
bool isActive
bool isControlTooltip       -- if true, governed by tutorialControlOn setting
bool hasCompleted
bool blockMovement
bool blockInCombat          -- hide during combat
bool IgnoreCryptMaster      -- show even when CryptMaster is speaking
float blockTime             -- delay before showing
bool doesKill               -- auto-hide after killTime seconds
int killTime / baseKillTime
bool customSpeechBlock
string customSpeechBlockType
List<TextMeshProUGUI> allTranslateableText  -- localized via ReturnLookup("tooltip", ...)
List<TextMeshProUGUI> allFlipBookObjects    -- animated text, also localized
List<GameObject> keyboardOnlyControls       -- shown only for keyboard
List<GameObject> joypadOnlyControls         -- shown only for gamepad
List<TextMeshProUGUI> characterDyanmicText  -- dynamically filled with loot name
```
**Source:** `Tooltip.cs` lines 8-62

### All Known Tooltip Names (SetTooltipState Calls)

These are the tooltip identifiers triggered throughout the codebase:

- `"controls"` -- Shown when entering the game/controls setup (MenuManager.cs line 2441)
- `"exit"` -- Shown near dungeon exits (GameManager.cs lines 5370, 5430)
- `"outofsouls"` -- Shown when player runs out of souls (GameManager.cs line 3945)
- `"loot"` -- Shown when loot is collected (TextManager.cs line 1869)
- `"levelup"` -- Shown when a character levels up (TextManager.cs line 2010)
- `"map"` -- Shown when opening the overworld map (Overworld.cs line 453)
- `"tumbling"` -- Shown when entering tumbling mini-game (MiniGameManager.cs line 1024)
- `"fishingtutorial"` -- Shown on first fishing attempt (MiniGameManager.cs lines 1454, 1464)
- `"fishing"` -- Shown on subsequent fishing attempts (MiniGameManager.cs line 1459)
- `"cards"` -- Shown when starting card game (MiniGameManager.cs line 1944)
- `"cyclecards"` -- Shown when card cycling becomes available (MiniGameManager.cs line 3930)
- `"firstAbility"` -- Hidden when brain/spell menu is opened (BrainController.cs line 175)
- Dynamic names from events: `EventManager.cs` line 918 passes `array[1]` directly

### Tooltip Text Localization

All tooltip text goes through `ReturnLookup("tooltip", text)` which looks up the English text in the `allWordTooltipLookup` list (loaded from `[TOOLTIPWORDLOOKUP]` entries in language files).

**Source:** `Tooltip.cs` lines 86-93

### Tooltip Behavior

- Tooltips slide in from an offset position when active
- They slide back out when inactive
- When fully off-screen (within 15 units), the container is deactivated entirely
- Flip-book tooltips animate through multiple text frames with fade effects
- Dynamic text (characterDyanmicText) updates with current loot name during level-up sequences
- Control-specific tooltips switch between keyboard/joypad graphics based on input mode

**Source:** `Tooltip.cs` lines 157-287

---

## 3. Hint System

**Source files:** `SaveManager.cs`, `CryptMaster.cs`, `EventManager.cs`, `Inventory.cs`

### Hint Settings

- `SaveManager.hints` -- Bool, toggleable option `[HINTS]`. When ON (easy mode), the first letter of items is auto-filled.
- `SaveManager.skullHints` -- Bool, toggleable option `[SKULLHINTS]`. Additional skull-related hints.

**Source:** `SaveManager.cs` lines 613-615, 1762-1766, 3638-3656

### Hint Behavior

When `hints` is true and a tarot card is collected, `GameManager.AutoFillFirstItemLetter()` is called to reveal the first letter of the current item.

**Source:** `CryptMaster.cs` lines 2970-2974

### Difficulty-Hint Relationship

- **Normal difficulty:** `cards = 0`, `hints = false`
- **Easy difficulty:** `cards = 1`, `hints = true`

**Source:** `EventManager.cs` lines 324-333

### Riddle Hint System

```
SaveManager.RiddleCheck:
  string riddleScriptName
  int numberOfTimesRun
```
- Stored in `SaveManager.allRiddleChecks` list
- Tracks how many times each riddle script has been run (for progressive hints)

**Source:** `SaveManager.cs` lines 350-354, 835-836

### Hint/Quest Lists (from Language Files)

Hints and quests are loaded from language files using these tags:

#### `[HINT]` entries (stored in `allHintLists`)
```
Format: [HINT]/baseName/displayName/optionsCount/optionalDesc/optionalSubText/prefabName/flagCheck/optionalScript/optionalUIUseContext
Group: "hints"
```

#### `[QUESTHINT]` entries (stored in `allQuestLists`)
```
Format: [QUESTHINT]/baseName/displayName/optionsCount/optionalDesc/optionalSubText/prefabName/flagCheck/optionalScript/optionalUIUseContext
Group: "quests"
```

These populate the inventory scrolls for hints and quests tabs.

**Source:** `SaveManager.cs` lines 2227-2256, 2718-2756

### Hint Flag Check in SaveManager

The `"hintson"` flag check evaluates whether hints are enabled:
```
case "hintson":
    if ((num == 1f && hints) || (num == 0f && !hints))
        result = true;
```
**Source:** `SaveManager.cs` lines 5639-5642

---

## 4. Spell/Ability Descriptions

**Source files:** `SpellManager.cs`, `BrainController.cs`, `CharacterHUD.cs`, `GameManager.cs`, `SaveManager.cs`

### Spell Data Structure
```
SpellManager.SpellStats:
  string spellName             -- display name
  string spellDetectionName    -- internal identifier
  string spellDescription      -- description text (localized)
  string engDescription        -- English override
  string spaDescription        -- Spanish override
  string spellFlavor           -- flavor text
  string engFlavor             -- English flavor override
  string spaFlavor             -- Spanish flavor override
  string optionalElement
  string hitEffect
  bool isMemory                -- memory (passive) vs skill (active)
  bool isFreeCast              -- free to cast (no soul cost)
  int assignedLevel            -- level at which unlocked
  bool isLevelBreak
  bool doesGainSkillLetter
```
**Source:** `SpellManager.cs` lines 86-122

### Spell Description Loading

Spell descriptions are loaded from language files via `[SPELLLOOKUP]`:
```
allSpell.spellDetectionName = array[2]
allSpell.spellDescription = array[3]  (lowercased)
allSpell.spellFlavor = array[4]       (lowercased)
```
**Source:** `SaveManager.cs` lines 2293-2300

When switching languages, descriptions are overridden:
- English: `allSpell.spellDescription = allSpell.engDescription`
- Spanish: `allSpell2.spellDescription = allSpell2.spaDescription`

**Source:** `SaveManager.cs` lines 3407-3410, 3472-3475

### Spell Description Display

**Brain Controller (Spell Menu):**
- Header shows: `spellName + ": " + description` (or just `"???"` if unrevealed)
- Description area shows: `flavor` text
- Free-cast spells prepend `"[FREE]"` (localized) before the description
- **Source:** `BrainController.cs` lines 345-356, 565-578

**Character HUD (Skill Discovery):**
- Shows `ReturnLookup("word", spellDescription)` when a skill is first discovered
- **Source:** `CharacterHUD.cs` line 1653

**GameManager (Skill Display):**
- Sets `skillDescription.text = allSpell.spellDescription`
- Sets `skillName.text = allSpell.spellName`
- **Source:** `GameManager.cs` lines 4977-4980

---

## 5. Inventory and Item Descriptions

**Source file:** `Inventory.cs`, `ShopItemContainer.cs`

### Inventory Item Structure
```
Inventory.CollectableItem:
  string collectableDetectionName
  string collectableDisplayName
  string CollectableDescription
  string CollectablePrefabName
  bool hasCollected
  int ingredientCount
  bool isCastable
  List<string> allRevealedPotionLetters
  bool isWarp  -- [Header("Null Flag Check To Show for Hints")]
```
**Source:** `Inventory.cs` lines 65-101

### Inventory Tabs (InventoryScroll)
```
Inventory.InventoryScroll:
  string inventoryScrollName      -- internal name
  string inventoryDisplayName     -- display name (English)
  string inventoryDisplayNameTranslated  -- localized display name
  string collectionContext
```
**Source:** `Inventory.cs` lines 12-21

Known inventory scroll names (from code references):
- `"warps"` -- warp/teleport locations
- `"hints"` -- hint items
- `"quests"` -- quest items
- `"cards"` -- collected cards
- `"potions"` -- potion items

### Shop Item Display
```
ShopItemContainer:
  TextMeshPro myName           -- item name display
  TextMeshPro myDescription    -- item description display
  TextMeshPro myUseText        -- "use" button text
  TextMeshPro myQuantity       -- quantity display
  string myDescText            -- description text content
  string myNameText            -- name text content
  string myTranslatedNameText  -- localized name
  TextMeshPro cardDamageText   -- damage display for cards
  string cardTag               -- card type tag
```
**Source:** `ShopItemContainer.cs` lines 1-53

### Inventory Bottom Panel

When an inventory item is highlighted:
- `MenuManager.headerText` shows the item name or spell name + description
- `MenuManager.descriptionText` shows the flavor text
- These are cleared when no item is selected and brain controller is not active

**Source:** `Inventory.cs` lines 560-563; `BrainController.cs` lines 565-578

---

## 6. Quest and Warp Descriptions

**Source file:** `SaveManager.cs`, `Inventory.cs`

### Quest List Structure
```
SaveManager.QuestList:
  string baseName
  string groupName             -- "quests" or "hints"
  string displayName
  int optionsCount
  string optionalDesc          -- long description
  string optionalSubText       -- subtitle text
  string prefabName            -- icon prefab
  string flagCheck             -- condition flag
  string optionalScript        -- associated script
  string optionalUIUseContext  -- UI context for "use" action
```
**Source:** `SaveManager.cs` lines 256-278

### Warp List Structure
```
SaveManager.WarpList:
  string baseWarpName
  string warpName
  string locationText
  string locationHeaderText
  string warpDescriptionLong
```
Loaded from `[WARP]` tag: `[WARP]/baseWarpName/warpName/locationText/locationHeaderText/warpDescriptionLong`

**Source:** `SaveManager.cs` lines 242-253, 2216-2225

### Inventory Population

Warps, quests, and hints are populated into inventory scrolls at startup:
```csharp
// Warps
FillInventoryScroll("warps", baseWarpName, warpName, 0,
    warpDescriptionLong, warpDescriptionLong, "warp01", "",
    _isWarp: true, "", ReturnLookup("word", "warp"));

// Quests
FillInventoryScroll(groupName, baseName, displayName, optionsCount,
    optionalSubText, optionalDesc, prefabName, flagCheck,
    _isWarp: false, optionalScript, optionalUIUseContext);

// Hints
FillInventoryScroll(groupName, baseName, displayName, optionsCount,
    optionalSubText, optionalDesc, prefabName, flagCheck,
    _isWarp: false, optionalScript, optionalUIUseContext);
```
**Source:** `Inventory.cs` lines 295-306

---

## 7. Menu Option Descriptions

**Source files:** `SaveManager.cs`, `MenuManager.cs`, `GameOption.cs`

### Menu Option Data (from Language Files)

Menu option descriptions are loaded from `[MENUOPTION]` tags in language files:
```
Format: [MENUOPTION]/baseName/description/optionWord1,optionWord2,...
```

Stored in `SaveManager.OptionsLookup`:
```
OptionsLookup:
  string baseName           -- matches GameOption.optionName (e.g. "[TUTORIAL]")
  string description        -- localized description text
  List<string> allOptionsWords  -- localized option values (e.g. "On", "Off")
```
**Source:** `SaveManager.cs` lines 197-205, 2379-2390

### Known Menu Option Names

From the save system and option processing code:
- `[RESOLUTION]` -- Screen resolution
- `[QUALITY]` -- Graphics quality
- `[DRAWDISTANCE]` -- Draw distance
- `[FOV]` -- Field of view
- `[HEADBOB]` -- Head bobbing
- `[MAPROTATION]` -- Map rotation
- `[POINTER]` -- Mouse pointer
- `[TUTORIAL]` -- Gameplay tutorials (On/Off)
- `[TUTORIALCONTROL]` -- Control tutorials (On/Off)
- `[HINTS]` -- Letter hints (On/Off, tied to easy mode)
- `[SKULLHINTS]` -- Skull hints (On/Off)
- `[SKIPDIALOG]` -- Skip dialog
- `[MOVESPEED]` -- Movement speed
- `[SPEECHAUTO]` -- Auto speech
- `[ENTERTOCOLLECT]` -- Enter to collect items
- `[LANGUAGE]` -- Language selection
- `[TWITCH]` -- Twitch integration
- `[TWITCHSPEED]` -- Twitch speed
- `[TWITCHSAMEVOTE]` -- Twitch same vote
- `[TWITCHROOM]` -- Twitch room
- `[BLOCKSPEECH]` -- Block speech
- `[SUBTITLES]` -- Subtitles
- `[MINIMAPFADE]` -- Minimap fade
- `[CARDS]` -- Card difficulty
- `[BACK]` -- Back button (special)
- `[CLEARSAVES]` -- Clear saves (special)
- `[VIEWCREDITS]` -- View credits (special)
- `[TOPHEADER]` -- Section headers in options menu

**Source:** `SaveManager.cs` lines 4060-4090, various

### Menu Headers (Main Menu)

Main menu headers are localized via `ReturnLookup("sentence", headerText)`.

```
MenuManager.MenuHeader:
  string headerText     -- English base text
  TextMeshProUGUI myText -- display element
  float fader           -- animation state
```
**Source:** `MenuManager.cs` lines 24-28, 633-637

### Game Mode Selection

Mode backers are localized: `ReturnLookup("word", baseName)`
- Known mode-related words: `"Continue"`, `"Begin"`, `"voice"`, `"keyboard"`, `"joypad"`
- Recommended text also localized

**Source:** `MenuManager.cs` lines 728-734, 2507-2513

---

## 8. Subtitle / Voice-Over System

**Source file:** `SubtitleManager.cs`

### Subtitle Data
```
SubtitleManager.SubtitleText:
  string audioName   -- matches audio clip name (lowercased)
  string voText      -- the subtitle text to display
  float voDelay      -- delay before showing
```

### Subtitle Loading

Subtitles are loaded from a text file at:
```
Application.persistentDataPath + "/VO_" + language.ToUpper() + ".txt"
```
Format per line: `audioName/voDelay/voText`

Example: `cm_greeting_01/0.5/Welcome to the crypt, mortal.`

**Source:** `SubtitleManager.cs` lines 46-77

### Subtitle Display

- Text shown in UPPERCASE
- Fades in with white text (0.7 alpha) on black backing (0.5 alpha)
- Duration: `unloadTime * textLength + additionalTimer`
- Prepends `startText` and appends `endText` if provided
- Active only when `isActive` is true (subtitle setting enabled)

**Source:** `SubtitleManager.cs` lines 462-545

---

## 9. Localization Architecture

**Source file:** `SaveManager.cs`

### Language File Location

Language data is loaded from `Application.persistentDataPath` with a language suffix:
```
Application.persistentDataPath + forwardString + currLang + ".txt"
```
The `persistentDataPath` on Windows is typically:
`C:\Users\<user>\AppData\LocalLow\<Company>\Cryptmaster\`

### Lookup Types (ReturnLookup Categories)

The `ReturnLookup(type, key)` function supports these lookup types:
- `"worldtext"` -- In-world 3D text (signs, etc.)
- `"payntext"` -- Payn-specific text
- `"rhyme"` -- Rhyming word translations
- `"name"` -- Character/item name translations
- `"murdername"` -- Murder word name translations (uses same name lookup)
- `"tooltip"` -- Tooltip UI text
- `"minijoy"` -- Mini joypad button labels (uses tooltip lookup)
- `"sentence"` -- Full sentence translations (menu headers, warnings, etc.)
- `"word"` -- Single word translations (general purpose)

**Source:** `SaveManager.cs` lines 1485-1570

### Language File Tags (Data Sections)

The language files contain these tagged data sections:

**Localization lookups:**
- `[WORDLOOKUP]` -- Single word translations
- `[SENTENCELOOKUP]` -- Full sentence translations
- `[NAMELOOKUP]` -- Name translations
- `[RHYMINGLOOKUP]` -- Rhyming word translations
- `[TOOLTIPWORDLOOKUP]` -- Tooltip text translations
- `[MINIJOYWORDLOOKUP]` -- Joypad button label translations
- `[WORLDTEXTLOOKUP]` -- In-world 3D text translations
- `[QUOTELOOKUP]` -- Quote translations
- `[PAYNTEXT]` -- Payn text translations
- `[CODELOOKUP]` -- Code translations

**Game content:**
- `[MENUOPTION]` -- Menu option descriptions and values
- `[SPELLLOOKUP]` -- Spell names, descriptions, and flavors
- `[HINT]` -- Hint list entries
- `[QUESTHINT]` -- Quest hint entries
- `[WARP]` -- Warp point data
- `[CHEST]` -- Chest content data
- `[FOOD]` -- Food item data
- `[POTION]` -- Potion data
- `[MONSTERCARDS]` -- Monster card names
- `[RESPONSE]` -- Response data
- `[TUMBLER]` -- Tumbler puzzle data
- `[BANNED]` -- Banned words

**Audio/speech:**
- `[AUDIOPOOL]` -- Audio pool definitions
- `[POOL]` -- Pool definitions
- `[POOLPROPERTY]` -- Pool property settings
- `[SFX]` -- Sound effects
- `[WORDSOUND]` -- Word-triggered sounds
- `[SPEECHLOOKUP]` -- Speech lookup data
- `[NAMELIST]` -- Name list data
- `[STARTPRED]` -- Starting predictive text

**Predictive text:**
- `[PREDBASE]` -- Baseline predictive phrases
- `[PREDCHEST]` -- Chest-related predictive phrases
- `[PREDCONVERSATION]` -- Conversation predictive phrases

**Rap/rhyme mini-game:**
- `[RAPWORD]` -- Rap words
- `[RAPLINE]` -- Rap lines
- `[RAPPER]` -- Rapper definitions

**Settings/state:**
- `[NODETAG]` -- Map node tags
- `[ALTSCRIPT]` -- Alternative scripts
- `[PREDICTIVE_ENT]` -- Predictive entities

**Source:** `SaveManager.cs` lines 2259-2660 (primary language file), 2700-2960 (English backup), 3080-3250 (Spanish)

---

## 10. Warning Texts

**Source files:** `GameOption.cs`, `MenuManager.cs`, `ControllerDisconnect.cs`

### Option Warning Texts

Game options can have associated warning text (shown when changing the option):
```
GameOption.warningText -- lookup key for ReturnLookup("sentence", ...)
```

Known warning text keys:
- `"clearsavewarning01"` -- Warning before clearing saves (changed to `"clearsavewarningconsole"` on console)
- `"speechwarning01"` -- Warning about speech settings (changed to `"speechwarningconsole"` on console)

**Source:** `GameOption.cs` lines 61-84; `MenuManager.cs` line 652

### Controller Disconnect Warning

Shown when a controller is disconnected:
- Key: `"controllerdisconnect"` (sentence lookup)

**Source:** `ControllerDisconnect.cs` lines 18-19

### Joypad Warning

Platform-specific joypad warning shown on certain platforms:
- Text localized via `ReturnLookup("sentence", joypadWarningText.text)`

**Source:** `MenuManager.cs` lines 2487-2488

---

## 11. Game Data File Locations

### Localization/String Files

Language files are loaded from `Application.persistentDataPath` (Windows: `%APPDATA%\..\LocalLow\<Company>\Cryptmaster\`):
- Main language file: `persistentDataPath + "/" + language + ".txt"` (e.g., `ENG.txt`)
- English backup: loaded separately for fallback
- Spanish: loaded separately
- Subtitles: `persistentDataPath + "/VO_" + language.ToUpper() + ".txt"`
- Predictive text: `persistentDataPath + "/Predictive_UNI.txt"`

### Game Installation Data Paths

- `C:\Program Files (x86)\Steam\steamapps\common\Cryptmaster\CryptMaster_Data\StreamingAssets\` -- Streaming assets (could not verify contents due to permission restrictions)
- `C:\Program Files (x86)\Steam\steamapps\common\Cryptmaster\CryptMaster_Data\Resources\` -- Unity resources (could not verify contents due to permission restrictions)
- `C:\Program Files (x86)\Steam\steamapps\common\Cryptmaster\CryptMaster_Data\Managed\` -- Managed DLLs (confirmed via Assembly-CSharp.csproj references)

### Export/Debug Output

The game has a debug export feature (`SubtitleManager.ExportLanguageVO()`) that outputs:
- `OUTPUT_ENG.txt` -- All translatable text with tagged warnings
- Subtitle text files

Export categories include:
- `[WORDLOOKUP]` -- All unique words from scripts
- `[RHYMINGLOOKUP]` -- Rhyming words
- `[TOOLTIP WARNING]` -- Tooltip texts needing translation
- `[ENEMY WARNING]` -- Enemy-related texts
- `[MINIJOY WARNING]` -- Joypad label texts
- `[* WARNING]` -- Script word references
- `[AUDIOPOOL WARNING]` -- Missing audio pool references
- `[SCRIPT_ERROR]` -- Script flag errors
- `[TILE]` -- Map tile data (levelName, textLine, activeWord, enemyDisplayName, hiddenWord)
- `[INVENTORYHEADER_WORDLOOKUP]` -- Inventory tab names
- `[MAINMENU_SENTENCELOOKUP]` -- Main menu header texts
- `[CREDITS_SENTENCELOOKUP]` -- Credits text
- `[CREDITSSKIP_SENTENCELOOKUP]` -- Credits skip prompt
- `[NAMELOOKUP]` -- Enemy names
- `[SENTENCELOOKUP]` -- Enemy special text
- `[ITEM_SENTENCELOOKUP]` -- Item text objects

**Source:** `SubtitleManager.cs` lines 79-383

---

## Summary of Key Accessibility-Relevant Systems

For a screen reader mod, the most important text systems to intercept are:

1. **Tooltip text** -- `Tooltip.SubToolTip.allTranslateableText` and `allFlipBookObjects`, localized via `ReturnLookup("tooltip", ...)`
2. **Subtitle text** -- `SubtitleManager.mySubtitleText.text` (already displayed as text)
3. **Menu descriptions** -- `MenuManager.descriptionText.text` and `headerText.text`
4. **Spell descriptions** -- `SpellManager.SpellStats.spellDescription` and `spellFlavor`
5. **Item descriptions** -- `ShopItemContainer.myDescription.text` and `myName.text`
6. **Quest/hint descriptions** -- Via inventory scroll system, `optionalDesc` and `optionalSubText`
7. **Warning texts** -- `GameOption.myWarningText.text`
8. **Skill discovery text** -- `CharacterHUD.mySkillDiscoveredDesc.text`
9. **Controller disconnect** -- `ControllerDisconnect.warningText.text`

All player-facing text is ultimately stored in external language files and loaded at runtime, with the English text serving as the lookup key for translations.

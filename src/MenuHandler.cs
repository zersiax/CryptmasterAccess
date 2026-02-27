using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace CryptmasterAccess
{
    /// <summary>
    /// Provides screen reader announcements for all game menus.
    /// Uses per-frame polling to detect state changes — no Harmony patches needed.
    /// Handles: main menu input select, main menu navigation, exit/pause menu, options/settings.
    /// </summary>
    public class MenuHandler
    {
        #region Enums

        /// <summary>
        /// Which menu context is currently active.
        /// </summary>
        private enum MenuContext
        {
            /// <summary>No menu active (gameplay or loading).</summary>
            None,
            /// <summary>Main menu state 0: waiting for keyboard/controller input selection.</summary>
            MainMenuInput,
            /// <summary>Main menu state 1: navigating menu items.</summary>
            MainMenu,
            /// <summary>Exit/pause menu is open.</summary>
            ExitMenu,
            /// <summary>Options/settings screen is open.</summary>
            Options
        }

        #endregion

        #region Fields

        private GameManager _gameManager;
        private MenuManager _menuManager;

        // Previous-frame state for change detection
        private MenuContext _lastContext = MenuContext.None;
        private int _lastMainMenuMount = -1;
        private int _lastExitMenuMount = -1;
        private int _lastOptionsSubMenu = -1;
        private int _lastOptionsMount = -1;
        private int _lastOptionValue = -1;

        // Cached announcement for F3 repeat
        private string _lastAnnouncement = "";

        #endregion

        #region Public Methods

        /// <summary>
        /// Called every frame from Main.UpdateHandlers(). Detects menu state changes and announces them.
        /// </summary>
        public void Update()
        {
            if (_gameManager == null) return;

            if (_menuManager == null)
            {
                _menuManager = _gameManager.myMenuManager;
                if (_menuManager == null) return;
            }

            MenuContext currentContext = DetermineContext();

            if (currentContext != _lastContext)
            {
                HandleContextChange(_lastContext, currentContext);
                _lastContext = currentContext;
            }

            // Update within the active context
            switch (currentContext)
            {
                case MenuContext.MainMenu:
                    UpdateMainMenu();
                    break;
                case MenuContext.ExitMenu:
                    UpdateExitMenu();
                    break;
                case MenuContext.Options:
                    UpdateOptions();
                    break;
            }
        }

        /// <summary>
        /// Receives cached GameManager from Main.
        /// </summary>
        public void SetGameManager(GameManager gm)
        {
            _gameManager = gm;
            _menuManager = gm != null ? gm.myMenuManager : null;
        }

        /// <summary>
        /// Clears all state. Called on scene change.
        /// </summary>
        public void Reset()
        {
            _gameManager = null;
            _menuManager = null;
            _lastContext = MenuContext.None;
            _lastMainMenuMount = -1;
            _lastExitMenuMount = -1;
            _lastOptionsSubMenu = -1;
            _lastOptionsMount = -1;
            _lastOptionValue = -1;
            _lastAnnouncement = "";
            DebugLogger.LogState("MenuHandler reset");
        }

        /// <summary>
        /// Re-announces the current menu state. Triggered by F3.
        /// </summary>
        public void RepeatCurrentMenu()
        {
            DebugLogger.LogInput("F3", "Repeat menu");

            if (string.IsNullOrEmpty(_lastAnnouncement))
            {
                // Build fresh announcement for current context
                string fresh = BuildContextAnnouncement(DetermineContext());
                if (!string.IsNullOrEmpty(fresh))
                {
                    Announce(fresh);
                }
                return;
            }

            ScreenReader.Say(_lastAnnouncement);
        }

        #endregion

        #region Context Detection

        /// <summary>
        /// Determines the current menu context based on game state.
        /// Priority: Options > ExitMenu > MainMenuInput > MainMenu > None.
        /// </summary>
        private MenuContext DetermineContext()
        {
            if (_menuManager == null || _gameManager == null) return MenuContext.None;

            // Options takes priority (it overlays other menus)
            if (_menuManager.isOptionsActive)
                return MenuContext.Options;

            // Exit menu (typo is in game code: isExitMenuAcitve)
            if (_gameManager.isExitMenuAcitve)
                return MenuContext.ExitMenu;

            // Main menu states (only when MainGameState == 0)
            if (_gameManager.MainGameState == 0)
            {
                if (_menuManager.mainMenuState == 0)
                    return MenuContext.MainMenuInput;

                if (_menuManager.mainMenuState == 1)
                    return MenuContext.MainMenu;
            }

            return MenuContext.None;
        }

        #endregion

        #region Context Changes

        /// <summary>
        /// Announces menu transitions (open/close).
        /// </summary>
        private void HandleContextChange(MenuContext oldContext, MenuContext newContext)
        {
            DebugLogger.LogState($"Menu context: {oldContext} -> {newContext}");

            // Announce closing of certain menus
            if (oldContext == MenuContext.Options && newContext != MenuContext.Options)
            {
                Announce(Loc.Get("menu_options_closed"));
            }
            else if (oldContext == MenuContext.ExitMenu && newContext == MenuContext.None)
            {
                Announce(Loc.Get("menu_resumed"));
            }

            // Announce the new context
            switch (newContext)
            {
                case MenuContext.MainMenuInput:
                    Announce(Loc.Get("menu_main_input"));
                    break;

                case MenuContext.MainMenu:
                    ResetMainMenuState();
                    AnnounceMainMenuFull();
                    break;

                case MenuContext.ExitMenu:
                    ResetExitMenuState();
                    AnnounceExitMenuFull();
                    break;

                case MenuContext.Options:
                    ResetOptionsState();
                    AnnounceOptionsFull();
                    break;
            }
        }

        /// <summary>
        /// Builds a full announcement for the given context (used by RepeatCurrentMenu).
        /// </summary>
        private string BuildContextAnnouncement(MenuContext context)
        {
            switch (context)
            {
                case MenuContext.MainMenuInput:
                    return Loc.Get("menu_main_input");

                case MenuContext.MainMenu:
                    var mainSelector = _menuManager.mainMenuAdditionalSelector;
                    if (mainSelector == null) return null;
                    string mainItem = GetSelectorItemName(mainSelector, mainSelector.currentMount);
                    int mainTotal = mainSelector.allBackers != null ? mainSelector.allBackers.Count : 0;
                    return Loc.Get("menu_main") + ". " +
                           Loc.Get("menu_item", mainItem, mainSelector.currentMount + 1, mainTotal);

                case MenuContext.ExitMenu:
                    var exitSelector = _menuManager.exitMenuSelector;
                    if (exitSelector == null) return null;
                    string exitItem = GetSelectorItemName(exitSelector, exitSelector.currentMount);
                    int exitTotal = exitSelector.allBackers != null ? exitSelector.allBackers.Count : 0;
                    return Loc.Get("menu_exit") + ". " +
                           Loc.Get("menu_item", exitItem, exitSelector.currentMount + 1, exitTotal);

                case MenuContext.Options:
                    return BuildOptionsFullAnnouncement();

                default:
                    return null;
            }
        }

        #endregion

        #region Main Menu

        private void ResetMainMenuState()
        {
            var selector = _menuManager.mainMenuAdditionalSelector;
            _lastMainMenuMount = selector != null ? selector.currentMount : -1;
        }

        private void AnnounceMainMenuFull()
        {
            var selector = _menuManager.mainMenuAdditionalSelector;
            if (selector == null) return;

            string itemName = GetSelectorItemName(selector, selector.currentMount);
            int total = selector.allBackers != null ? selector.allBackers.Count : 0;

            string msg = Loc.Get("menu_main") + ". " +
                         Loc.Get("menu_item", itemName, selector.currentMount + 1, total);
            Announce(msg);
        }

        private void UpdateMainMenu()
        {
            var selector = _menuManager.mainMenuAdditionalSelector;
            if (selector == null) return;

            int mount = selector.currentMount;
            if (mount != _lastMainMenuMount)
            {
                _lastMainMenuMount = mount;
                string itemName = GetSelectorItemName(selector, mount);
                int total = selector.allBackers != null ? selector.allBackers.Count : 0;
                Announce(Loc.Get("menu_item", itemName, mount + 1, total));
            }
        }

        #endregion

        #region Exit Menu

        private void ResetExitMenuState()
        {
            var selector = _menuManager.exitMenuSelector;
            _lastExitMenuMount = selector != null ? selector.currentMount : -1;
        }

        private void AnnounceExitMenuFull()
        {
            var selector = _menuManager.exitMenuSelector;
            if (selector == null) return;

            string itemName = GetSelectorItemName(selector, selector.currentMount);
            int total = selector.allBackers != null ? selector.allBackers.Count : 0;

            string msg = Loc.Get("menu_exit") + ". " +
                         Loc.Get("menu_item", itemName, selector.currentMount + 1, total);
            Announce(msg);
        }

        private void UpdateExitMenu()
        {
            var selector = _menuManager.exitMenuSelector;
            if (selector == null) return;

            int mount = selector.currentMount;
            if (mount != _lastExitMenuMount)
            {
                _lastExitMenuMount = mount;
                string itemName = GetSelectorItemName(selector, mount);
                int total = selector.allBackers != null ? selector.allBackers.Count : 0;
                Announce(Loc.Get("menu_item", itemName, mount + 1, total));
            }
        }

        #endregion

        #region Options Menu

        private void ResetOptionsState()
        {
            _lastOptionsSubMenu = _menuManager.optionsSubMenu;
            var selector = _menuManager.optionsSelector;
            _lastOptionsMount = selector != null ? selector.currentMount : -1;
            _lastOptionValue = GetCurrentOptionValueHash();
        }

        private void AnnounceOptionsFull()
        {
            string msg = BuildOptionsFullAnnouncement();
            if (!string.IsNullOrEmpty(msg))
            {
                Announce(msg);
            }
        }

        private string BuildOptionsFullAnnouncement()
        {
            string tabName = GetOptionsTabName();
            GameOption option = GetCurrentGameOption();

            string result = Loc.Get("menu_options") + ". " + Loc.Get("menu_tab", tabName);

            if (option != null)
            {
                string optName = GetOptionDisplayName(option);
                string optVal = GetOptionValue(option);

                if (!string.IsNullOrEmpty(optVal))
                {
                    result += ". " + Loc.Get("menu_option_value", optName, optVal);
                }
                else
                {
                    result += ". " + Loc.Get("menu_option_novalue", optName);
                }
            }

            return result;
        }

        private void UpdateOptions()
        {
            // Check tab change
            int subMenu = _menuManager.optionsSubMenu;
            if (subMenu != _lastOptionsSubMenu)
            {
                _lastOptionsSubMenu = subMenu;
                string tabName = GetOptionsTabName();

                // Also announce first option in new tab
                var selector = _menuManager.optionsSelector;
                _lastOptionsMount = selector != null ? selector.currentMount : -1;

                GameOption option = GetCurrentGameOption();
                string msg = Loc.Get("menu_tab", tabName);
                if (option != null)
                {
                    string optName = GetOptionDisplayName(option);
                    string optVal = GetOptionValue(option);
                    if (!string.IsNullOrEmpty(optVal))
                    {
                        msg += ". " + Loc.Get("menu_option_value", optName, optVal);
                    }
                    else
                    {
                        msg += ". " + Loc.Get("menu_option_novalue", optName);
                    }
                }
                _lastOptionValue = GetCurrentOptionValueHash();
                Announce(msg);
                return;
            }

            // Check item change
            var optSelector = _menuManager.optionsSelector;
            if (optSelector == null) return;

            int mount = optSelector.currentMount;
            if (mount != _lastOptionsMount)
            {
                _lastOptionsMount = mount;
                GameOption option = GetCurrentGameOption();
                if (option != null)
                {
                    string optName = GetOptionDisplayName(option);
                    string optVal = GetOptionValue(option);
                    if (!string.IsNullOrEmpty(optVal))
                    {
                        Announce(Loc.Get("menu_option_value", optName, optVal));
                    }
                    else
                    {
                        Announce(Loc.Get("menu_option_novalue", optName));
                    }
                }
                _lastOptionValue = GetCurrentOptionValueHash();
                return;
            }

            // Check value change (same item, value changed by left/right)
            int currentValueHash = GetCurrentOptionValueHash();
            if (currentValueHash != _lastOptionValue)
            {
                _lastOptionValue = currentValueHash;
                GameOption option = GetCurrentGameOption();
                if (option != null)
                {
                    string optVal = GetOptionValue(option);
                    if (!string.IsNullOrEmpty(optVal))
                    {
                        Announce(Loc.Get("menu_value_changed", optVal));
                    }
                }
            }
        }

        #endregion

        #region Item Name Reading

        /// <summary>
        /// Reads the display name of a menu item from a JoypadSelector.
        /// For main menu items (isMainMenu): uses outerText.text.
        /// For other items: combines myText.text + outerText.text.
        /// Falls back to baseName.
        /// </summary>
        private string GetSelectorItemName(JoypadSelector selector, int index)
        {
            if (selector == null || selector.allBackers == null) return "?";
            if (index < 0 || index >= selector.allBackers.Count) return "?";

            MenuBacker backer = selector.allBackers[index];
            if (backer == null) return "?";

            try
            {
                if (backer.isMainMenu)
                {
                    // Main menu items: full text in outerText
                    if (backer.outerText != null && !string.IsNullOrEmpty(backer.outerText.text))
                        return backer.outerText.text.Trim();
                }
                else
                {
                    // Non-main menu items: combine myText + outerText
                    string inner = backer.myText != null ? backer.myText.text : "";
                    string outer = backer.outerText != null ? backer.outerText.text : "";

                    string combined = (inner + " " + outer).Trim();
                    if (!string.IsNullOrEmpty(combined))
                        return combined;
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.Log(LogCategory.Handler, "MenuHandler",
                    $"Error reading item name at index {index}: {ex.Message}");
            }

            // Fallback
            return !string.IsNullOrEmpty(backer.baseName) ? backer.baseName : "?";
        }

        #endregion

        #region Options Reading

        /// <summary>
        /// Finds the currently focused GameOption, matching game logic (MenuManager.cs:2118-2135).
        /// Skips items in the currentBannedList to match visible index to selector mount.
        /// </summary>
        private GameOption GetCurrentGameOption()
        {
            if (_menuManager.currOptionsList == null) return null;

            var selector = _menuManager.optionsSelector;
            if (selector == null) return null;

            int targetMount = selector.currentMount;
            int visibleIndex = 0;

            foreach (GameOption option in _menuManager.currOptionsList.allMenuOptions)
            {
                if (_menuManager.currentBannedList != null &&
                    _menuManager.currentBannedList.Contains(option.optionName.ToLower()))
                {
                    continue;
                }

                if (visibleIndex == targetMount)
                    return option;

                visibleIndex++;
            }

            return null;
        }

        /// <summary>
        /// Gets the display name of a GameOption from its MenuBacker.
        /// </summary>
        private string GetOptionDisplayName(GameOption option)
        {
            if (option == null) return "?";

            try
            {
                // Options use myOptionText for the display name/description,
                // set by LoadNamesAndLinks from localization data.
                // The backer's myText/outerText are unreliable for options.
                if (option.myOptionText != null && !string.IsNullOrEmpty(option.myOptionText.text))
                    return option.myOptionText.text.Trim();

                // Fallback to backer baseName (editor-set English name)
                if (option.myBacker != null && !string.IsNullOrEmpty(option.myBacker.baseName))
                    return option.myBacker.baseName;
            }
            catch (System.Exception ex)
            {
                DebugLogger.Log(LogCategory.Handler, "MenuHandler",
                    $"Error reading option name: {ex.Message}");
            }

            // Fallback to optionName (internal key like "[VOLUME]")
            return !string.IsNullOrEmpty(option.optionName) ? option.optionName : "?";
        }

        /// <summary>
        /// Gets the current value of a GameOption.
        /// Returns null for buttons/headers that have no value.
        /// </summary>
        private string GetOptionValue(GameOption option)
        {
            if (option == null) return null;

            try
            {
                // Top headers and buttons have no value
                if (option.isTopHeader) return null;
                if (option.optionName == "[BACK]" ||
                    option.optionName == "[CLEARSAVES]" ||
                    option.optionName == "[VIEWCREDITS]")
                    return null;

                // Toggle: only check if the backer is actually a toggle item
                if (option.myBacker != null && option.myBacker.isToggle && option.myBacker.myToggleBox != null)
                {
                    bool isOn = option.myBacker.myToggleBox.isToggleActive;
                    return isOn ? Loc.Get("menu_toggle_on") : Loc.Get("menu_toggle_off");
                }

                // Slider: read from allOptionsNodes (most options use this, including on/off)
                if (option.mySlider != null && option.mySlider.allOptionsNodes != null)
                {
                    int idx = option.currentlySelectedOption;
                    if (idx >= 0 && idx < option.mySlider.allOptionsNodes.Count)
                    {
                        string nodeName = option.mySlider.allOptionsNodes[idx].optionsNodeName;
                        if (!string.IsNullOrEmpty(nodeName))
                            return nodeName;
                    }
                }

                // String input field
                if (option.myInput != null && !string.IsNullOrEmpty(option.myInput.text))
                {
                    return option.myInput.text;
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.Log(LogCategory.Handler, "MenuHandler",
                    $"Error reading option value: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Gets the name of the currently active options tab.
        /// Uses optionsSubMenu as index into allOptionsLists.
        /// </summary>
        private string GetOptionsTabName()
        {
            try
            {
                int subMenu = _menuManager.optionsSubMenu;
                if (_menuManager.allOptionsLists != null &&
                    subMenu >= 0 && subMenu < _menuManager.allOptionsLists.Count)
                {
                    var list = _menuManager.allOptionsLists[subMenu];

                    // Prefer translated name
                    if (!string.IsNullOrEmpty(list.translatedListName))
                        return list.translatedListName;

                    if (!string.IsNullOrEmpty(list.optionListName))
                        return list.optionListName;
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.Log(LogCategory.Handler, "MenuHandler",
                    $"Error reading tab name: {ex.Message}");
            }

            return "?";
        }

        /// <summary>
        /// Returns a hash of the current option value for change detection.
        /// </summary>
        private int GetCurrentOptionValueHash()
        {
            GameOption option = GetCurrentGameOption();
            if (option == null) return -1;

            string val = GetOptionValue(option);
            return val != null ? val.GetHashCode() : -1;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Announces text and caches it for F3 repeat.
        /// </summary>
        private void Announce(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            _lastAnnouncement = text;
            DebugLogger.Log(LogCategory.Handler, "MenuHandler", $"Announce: {text}");
            ScreenReader.Say(text);
        }

        #endregion
    }
}

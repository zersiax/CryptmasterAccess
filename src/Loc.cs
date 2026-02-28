using System.Collections.Generic;

namespace CryptmasterAccess
{
    /// <summary>
    /// Central localization for the accessibility mod.
    /// English only for now, but all strings go through here from day one.
    ///
    /// Usage:
    ///   Loc.Get("key")              — get string
    ///   Loc.Get("key", arg1, arg2)  — string with placeholders {0}, {1}
    /// </summary>
    public static class Loc
    {
        #region Fields

        private static bool _initialized = false;
        private static readonly Dictionary<string, string> _strings = new Dictionary<string, string>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes localization. Call once at mod startup.
        /// </summary>
        public static void Initialize()
        {
            InitializeStrings();
            _initialized = true;
        }

        /// <summary>
        /// Gets a localized string.
        /// </summary>
        public static string Get(string key)
        {
            if (!_initialized) Initialize();

            if (_strings.TryGetValue(key, out string value))
                return value;

            // Fallback: key itself (helps with debugging)
            return key;
        }

        /// <summary>
        /// Gets a localized string with placeholders {0}, {1}, etc.
        /// </summary>
        public static string Get(string key, params object[] args)
        {
            string template = Get(key);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// All mod strings defined here.
        /// Naming convention: [handler]_[action]
        /// </summary>
        private static void InitializeStrings()
        {
            // ===== GENERAL =====
            _strings["mod_loaded"] = "Cryptmaster Access loaded. F1 for help.";
            _strings["help_text"] = "Key bindings: F1 Help. F2 Repeat room info. Ctrl+F2 Repeat word puzzle. F3 Repeat menu info. F4 Repeat combat. F5 Party HP. F6 Turn timer. F7 Enemy info. F8 Repeat inventory or brain info. F9 Repeat last notification. Ctrl+F9 Auto-walk to nearest unexplored room. F10 Pathfind, cycle targets, Enter for route. Shift+F10 Toggle GPS. Ctrl+F10 Retrace to junction. Ctrl+PgUp/PgDn cycle categories. PgUp/PgDn cycle items. End for directions. F11 Toggle debug mode.";
            _strings["debug_toggle"] = "Debug mode {0}";

            // ===== DIRECTIONS (RELATIVE) =====
            _strings["dir_ahead"] = "ahead";
            _strings["dir_right"] = "right";
            _strings["dir_behind"] = "behind";
            _strings["dir_left"] = "left";
            _strings["dir_unknown"] = "unknown";

            // ===== DIRECTIONS (ABSOLUTE/CARDINAL) =====
            _strings["dir_north"] = "north";
            _strings["dir_south"] = "south";
            _strings["dir_east"] = "east";
            _strings["dir_west"] = "west";

            // ===== ROOM CONTENTS =====
            _strings["room_enemy"] = "Enemy: {0}";
            _strings["room_npc"] = "NPC: {0}";
            _strings["room_container"] = "Container";
            _strings["room_trap"] = "Trap";
            _strings["room_scenery"] = "Scenery: {0}";
            _strings["room_fishing"] = "Fishing spot";
            _strings["room_cards"] = "Card game";
            _strings["room_shop"] = "Shop";

            // ===== EXITS =====
            _strings["room_exits"] = "Exits:";
            _strings["room_no_exits"] = "No exits";
            _strings["room_all_blocked"] = "all blocked";

            // ===== FACING / ROTATION =====
            _strings["facing_format"] = "Facing {0}. {1}";
            _strings["facing_wall"] = "Wall ahead";
            _strings["facing_blocked"] = "Blocked ahead";
            _strings["facing_open"] = "Open ahead";
            _strings["facing_enemy"] = "Enemy ahead: {0}";

            // ===== POINTS OF INTEREST =====
            _strings["poi_enemy"] = "Enemy";
            _strings["poi_npc"] = "NPC";
            _strings["poi_container"] = "Container";
            _strings["poi_trap"] = "Trap";
            _strings["poi_shop"] = "Shop";
            _strings["poi_fishing"] = "Fishing";
            _strings["poi_cards"] = "Cards";
            _strings["poi_scenery"] = "Scenery";
            _strings["poi_door"] = "Door";
            _strings["poi_named_format"] = "{0}: {1}, {2} {3}";
            _strings["poi_format"] = "{0}: {1} {2}";

            // ===== NAVIGATION CATEGORIES =====
            _strings["nav_cat_exits"] = "Exits";
            _strings["nav_cat_npcs"] = "NPCs";
            _strings["nav_cat_enemies"] = "Enemies";
            _strings["nav_cat_interactables"] = "Interactables";
            _strings["nav_cat_pois"] = "All nearby";
            _strings["nav_cat_scenery"] = "Scenery";
            _strings["nav_no_items"] = "None";
            _strings["nav_item"] = "{0}, {1} {2}";
            _strings["nav_item_here"] = "{0}, here";
            _strings["nav_category_announce"] = "{0}: {1} of {2}";
            _strings["nav_exit_item"] = "Exit {0}";

            // ===== DIRECTIONS / PATHFINDING =====
            _strings["nav_go_ahead"] = "{0} ahead";
            _strings["nav_turn_left"] = "turn left";
            _strings["nav_turn_right"] = "turn right";
            _strings["nav_turn_around"] = "turn around";
            _strings["nav_unreachable"] = "No path found";
            _strings["nav_here"] = "Here";

            // ===== INTERACTION HINTS =====
            _strings["interact_hint"] = "Type {0} to interact";
            _strings["interact_npc"] = "{0}. Type {1} to talk";

            // ===== MENUS =====
            _strings["menu_main_input"] = "Main menu. Press 1 for keyboard, or Start for controller.";
            _strings["menu_main"] = "Main menu";
            _strings["menu_exit"] = "Pause menu";
            _strings["menu_options"] = "Settings";
            _strings["menu_options_closed"] = "Settings closed";
            _strings["menu_resumed"] = "Resumed";
            _strings["menu_item"] = "{0}, {1} of {2}";
            _strings["menu_tab"] = "{0} tab";
            _strings["menu_option_value"] = "{0}: {1}";
            _strings["menu_option_novalue"] = "{0}";
            _strings["menu_value_changed"] = "{0}";
            _strings["menu_toggle_on"] = "On";
            _strings["menu_toggle_off"] = "Off";

            // ===== COMBAT =====
            _strings["combat_start"] = "Combat! {0}, attack {1}. {2}";
            _strings["combat_end"] = "Combat over.";
            _strings["combat_enemy_info"] = "{0}, {1} of {2} HP, attack {3}, targeting {4}";
            _strings["combat_not_in_combat"] = "Not in combat.";
            _strings["combat_spell_mode_on"] = "Spell mode.";
            _strings["combat_spell_mode_off"] = "Spell mode ended.";
            _strings["combat_spell_cast"] = "{0} casts {1}.";
            _strings["combat_enemy_damaged"] = "{0} damage to enemy. {1} of {2} HP left.";
            _strings["combat_enemy_defeated"] = "{0} defeated!";
            _strings["combat_char_hit"] = "{0} hit! {1} of {2} HP.";
            _strings["combat_char_dead"] = "{0} is dead!";
            _strings["combat_game_over"] = "Game over. All characters dead.";
            _strings["combat_timer"] = "Timer {0} percent.";
            _strings["combat_timer_warning"] = "Warning! Timer at {0} percent.";
            _strings["combat_party_status"] = "Party: {0}";
            _strings["combat_char_hp"] = "{0} {1} of {2}";
            _strings["combat_char_hp_dead"] = "{0} dead";
            _strings["combat_targeting"] = "targeting {0}";
            _strings["combat_targeting_all"] = "targeting all";

            // ===== LOOT / LEVEL-UP =====
            _strings["loot_start"] = "Loot! {0}";
            _strings["loot_assignment"] = "{0} gets {1}";
            _strings["loot_levelup_start"] = "{0} levels up! Select a letter. Current: {1}.";
            _strings["loot_levelup_letter"] = "{0}.";

            // ===== INVENTORY =====
            _strings["inv_opened"] = "Inventory. {0} tab, {1} items.";
            _strings["inv_closed"] = "Inventory closed.";
            _strings["inv_tab_changed"] = "{0} tab, {1} items.";
            _strings["inv_item"] = "{0}, {1} of {2}.";
            _strings["inv_item_desc"] = "{0}, {1} of {2}. {3}";
            _strings["inv_item_quantity"] = "{0}, quantity {1}, {2} of {3}.";
            _strings["inv_item_new"] = "New: {0}, {1} of {2}.";
            _strings["inv_item_usable"] = "{0}, {1}. {2} of {3}.";
            _strings["inv_no_items"] = "No items.";
            _strings["inv_potion_on"] = "Potion crafting.";
            _strings["inv_potion_off"] = "Potion crafting closed.";

            // ===== BRAIN =====
            _strings["brain_opened"] = "{0} brain. {1}";
            _strings["brain_closed"] = "Brain closed.";
            _strings["brain_char_changed"] = "{0} brain. {1}";
            _strings["brain_spell"] = "{0}, {1}.";
            _strings["brain_spell_desc"] = "{0}, {1}. {2}";
            _strings["brain_spell_unknown"] = "Unknown spell.";
            _strings["brain_skill"] = "skill";
            _strings["brain_memory"] = "memory";

            // ===== PATHFINDING =====
            _strings["path_moved"] = "Moved {0}.";
            _strings["path_scanning"] = "Scanning...";
            _strings["path_targets_summary"] = "{0} targets found.";
            _strings["path_target_item"] = "{0}: {1}, {2} steps.";
            _strings["path_target_unnamed"] = "{0}, {1} steps.";
            _strings["path_no_targets"] = "No targets in current area.";
            _strings["path_route"] = "Route to {0}: {1}";
            _strings["path_no_route"] = "No path found.";
            _strings["path_arrived"] = "Arrived at {0}.";
            _strings["path_gps_next"] = "{0}.";
            _strings["path_gps_on"] = "GPS on.";
            _strings["path_gps_off"] = "GPS off.";
            _strings["path_off_route"] = "Off route. Recalculating.";
            _strings["path_retrace"] = "Back to junction: {0}";
            _strings["path_no_breadcrumbs"] = "No breadcrumb trail.";
            _strings["path_room_visited"] = "Visited.";
            _strings["path_room_new"] = "New.";
            _strings["path_go"] = "go {0} {1}";
            _strings["path_turn_left"] = "turn left";
            _strings["path_turn_right"] = "turn right";
            _strings["path_turn_around"] = "turn around";
            _strings["path_no_unexplored"] = "No unexplored rooms in current area. Try a door to reach other areas.";
            _strings["path_autowalk_start"] = "Auto-walking {0} steps.";
            _strings["path_autowalk_cancelled"] = "Auto-walk cancelled.";
            _strings["path_autowalk_blocked"] = "Auto-walk blocked.";
            _strings["path_autowalk_arrived"] = "Auto-walk complete.";

            // ===== WORD PUZZLE =====
            _strings["word_puzzle_start"] = "Word puzzle, {0} letters.";
            _strings["word_puzzle_letters"] = "{0} letters. {1}";
            _strings["word_puzzle_solved"] = "Word solved.";
            _strings["word_puzzle_blank"] = "blank";
            _strings["word_puzzle_no_active"] = "No word puzzle.";
            _strings["word_world_start"] = "World word, {0} letters.";
            _strings["word_world_solved"] = "World word solved.";

            // ===== TEXT INTERCEPT =====
            _strings["text_subtitle"] = "{0}";
            _strings["text_tooltip"] = "{0}";
            _strings["text_loot"] = "{0}";
            _strings["text_location"] = "{0}";
            _strings["text_no_recent"] = "No recent notification.";
        }

        #endregion
    }
}

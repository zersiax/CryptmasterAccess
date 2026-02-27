using MelonLoader;
using UnityEngine;
using System.Collections;
using HarmonyLib;

// ============================================================================
// CRITICAL: Accessing Game Code
// ============================================================================
// Any access to game classes BEFORE fully loaded will crash!
//
// FORBIDDEN in OnInitializeMelon() or earlier:
//   - Game manager singletons (GameManager.i, AudioManager.instance, etc.)
//   - typeof(GameClass) in Harmony attributes
//
// ALLOWED only from OnSceneWasLoaded() / when CheckGameReady() is true.
//
// For crashes or silent failures:
//   See docs/technical-reference.md section "CRITICAL: Accessing Game Code"
// ============================================================================

[assembly: MelonInfo(typeof(CryptmasterAccess.Main), "CryptmasterAccess", "1.1.0", "Zersiax")]
[assembly: MelonGame("PaulHartandLeeWilliams", "CryptMaster")]

namespace CryptmasterAccess
{
    /// <summary>
    /// Main mod entry point. Coordinates all handlers and processes global hotkeys.
    /// Keep this class SMALL — put ALL feature logic in separate Handler classes.
    /// </summary>
    public class Main : MelonMod
    {
        #region Fields

        private bool _gameReady = false;
        private bool _patchesApplied = false;
        private GameManager _cachedGameManager;
        private HarmonyLib.Harmony _harmony;

        /// <summary>
        /// Debug mode — when true, logs all screenreader output and detailed game state.
        /// Toggle with F11.
        /// </summary>
        public static bool DebugMode = false;

        // Handlers — one per feature/screen
        private MenuHandler _menuHandler;
        private RoomHandler _roomHandler;
        private NavigationHandler _navigationHandler;
        private CombatHandler _combatHandler;
        private InventoryHandler _inventoryHandler;
        private BrainHandler _brainHandler;
        private TextInterceptHandler _textInterceptHandler;
        private PathfindHandler _pathfindHandler;

        /// <summary>
        /// Static reference for Harmony patch callbacks (room).
        /// </summary>
        internal static RoomHandler RoomHandlerInstance;

        /// <summary>
        /// Static reference for Harmony patch callbacks (combat).
        /// </summary>
        internal static CombatHandler CombatHandlerInstance;

        /// <summary>
        /// Static reference for visited room checks (used by RoomHandler).
        /// </summary>
        internal static PathfindHandler PathfindHandlerInstance;

        #endregion

        #region Lifecycle

        public override void OnInitializeMelon()
        {
            ScreenReader.Initialize();
            Loc.Initialize();
            _harmony = new HarmonyLib.Harmony("com.zersiax.cryptmasteraccess");
            InitializeHandlers();
            MelonCoroutines.Start(AnnounceStartupDelayed());
        }

        private void InitializeHandlers()
        {
            _menuHandler = new MenuHandler();
            _roomHandler = new RoomHandler();
            _navigationHandler = new NavigationHandler();
            _combatHandler = new CombatHandler();
            _inventoryHandler = new InventoryHandler();
            _brainHandler = new BrainHandler();
            _textInterceptHandler = new TextInterceptHandler();
            _pathfindHandler = new PathfindHandler();
            RoomHandlerInstance = _roomHandler;
            CombatHandlerInstance = _combatHandler;
            PathfindHandlerInstance = _pathfindHandler;
        }

        private IEnumerator AnnounceStartupDelayed()
        {
            yield return new WaitForSeconds(1f);
            ScreenReader.Say(Loc.Get("mod_loaded"));
        }

        public override void OnUpdate()
        {
            if (!CheckGameReady()) return;

            if (ProcessHotkeys()) return;

            UpdateHandlers();
        }

        private bool CheckGameReady()
        {
            if (_gameReady) return true;

            // Not ready until first scene load
            return _gameReady;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            MelonLogger.Msg($"Scene loaded: {sceneName}");
            DebugLogger.LogState($"Scene changed to: {sceneName}");

            // Reset handlers on scene change
            _menuHandler.Reset();
            _roomHandler.Reset();
            _navigationHandler.Reset();
            _combatHandler.Reset();
            _inventoryHandler.Reset();
            _brainHandler.Reset();
            _textInterceptHandler.Reset();
            _pathfindHandler.Reset();
            _patchesApplied = false;
            _cachedGameManager = null;

            // Mark game as ready on first scene load
            if (!_gameReady)
            {
                _gameReady = true;
                MelonLogger.Msg("Game ready");
            }
        }

        public override void OnApplicationQuit()
        {
            ScreenReader.Shutdown();
        }

        #endregion

        #region Harmony Patches

        /// <summary>
        /// Applies Harmony patches once GameManager is found.
        /// Uses manual patching to avoid referencing game types too early.
        /// </summary>
        private void ApplyHarmonyPatches()
        {
            if (_patchesApplied) return;

            try
            {
                _harmony.PatchAll(typeof(Patches.UpdateToNewPositionPatch).Assembly);
                _patchesApplied = true;
                MelonLogger.Msg("Harmony patches applied");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Failed to apply Harmony patches: {ex.Message}");
            }
        }

        #endregion

        #region Hotkeys

        /// <summary>
        /// Processes global hotkeys. Returns true if a key was handled.
        /// </summary>
        private bool ProcessHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.F11))
            {
                DebugMode = !DebugMode;
                var status = DebugMode ? "enabled" : "disabled";
                MelonLogger.Msg($"Debug mode {status}");
                ScreenReader.Say(Loc.Get("debug_toggle", status));
                return true;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                DebugLogger.LogInput("F1", "Help");
                AnnounceHelp();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                _roomHandler.RepeatLastAnnouncement();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                _menuHandler.RepeatCurrentMenu();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                DebugLogger.LogInput("F4", "Repeat combat");
                _combatHandler.RepeatLastCombatInfo();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                DebugLogger.LogInput("F5", "Party HP");
                _combatHandler.AnnouncePartyStatus();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                DebugLogger.LogInput("F6", "Turn timer");
                _combatHandler.AnnounceTurnTimer();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                DebugLogger.LogInput("F7", "Enemy info");
                _combatHandler.AnnounceEnemyInfo();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                DebugLogger.LogInput("F8", "Repeat inventory/brain");
                if (_inventoryHandler.IsOpen())
                    _inventoryHandler.RepeatCurrentInfo();
                else if (_brainHandler.IsOpen())
                    _brainHandler.RepeatCurrentInfo();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                if (ctrl)
                {
                    DebugLogger.LogInput("Ctrl+F9", "Auto-walk to unexplored");
                    _pathfindHandler.AutoWalkToUnexplored();
                }
                else
                {
                    _textInterceptHandler.RepeatLastNotification();
                }
                return true;
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                if (ctrl)
                {
                    DebugLogger.LogInput("Ctrl+F10", "Retrace");
                    _pathfindHandler.Retrace();
                }
                else if (shift)
                {
                    DebugLogger.LogInput("Shift+F10", "Toggle GPS");
                    _pathfindHandler.ToggleGPS();
                }
                else
                {
                    DebugLogger.LogInput("F10", "Pathfind");
                    _pathfindHandler.HandlePathfindKey();
                }
                return true;
            }

            return false;
        }

        #endregion

        #region Handler Updates

        private void UpdateHandlers()
        {
            // Find GameManager if not cached
            if (_cachedGameManager == null)
            {
                _cachedGameManager = Object.FindObjectOfType<GameManager>();
                if (_cachedGameManager != null)
                {
                    _menuHandler.SetGameManager(_cachedGameManager);
                    _navigationHandler.SetGameManager(_cachedGameManager);
                    _combatHandler.SetGameManager(_cachedGameManager);
                    _inventoryHandler.SetGameManager(_cachedGameManager);
                    _brainHandler.SetGameManager(_cachedGameManager);
                    _textInterceptHandler.SetGameManager(_cachedGameManager);
                    _pathfindHandler.SetGameManager(_cachedGameManager);
                }
            }

            // Menu handler runs first (menus block gameplay)
            _menuHandler.Update();
            _roomHandler.Update();
            _navigationHandler.Update();
            _combatHandler.Update();
            _inventoryHandler.Update();
            _brainHandler.Update();
            _textInterceptHandler.Update();
            _pathfindHandler.Update();

            // Apply Harmony patches once GameManager is available
            if (!_patchesApplied && _cachedGameManager != null)
            {
                ApplyHarmonyPatches();
            }
        }

        #endregion

        #region Help

        private void AnnounceHelp()
        {
            ScreenReader.Say(Loc.Get("help_text"));
        }

        #endregion
    }
}

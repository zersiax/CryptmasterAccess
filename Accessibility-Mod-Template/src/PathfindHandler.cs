using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

namespace CryptmasterAccess
{
    /// <summary>
    /// User-facing pathfinding handler. Provides movement confirmation, target cycling,
    /// GPS-guided navigation, and breadcrumb backtracking.
    ///
    /// Controls:
    ///   Ctrl+F9      — Auto-walk to nearest unexplored room
    ///   F10          — Scan/cycle targets, Enter to get route
    ///   Shift+F10    — Toggle GPS mode
    ///   Ctrl+F10     — Retrace to last junction
    /// </summary>
    public class PathfindHandler
    {
        #region Constants

        private const int MaxBreadcrumbs = 200;
        private const int JunctionExitThreshold = 3;

        #endregion

        #region Fields

        private GameManager _gameManager;
        private readonly RoomScanner _scanner = new RoomScanner();
        private readonly Pathfinder _pathfinder = new Pathfinder();

        // Visited room tracking
        private readonly HashSet<MapPiece> _visitedRooms = new HashSet<MapPiece>();

        // Movement detection
        private MapPiece _lastMapPiece;
        private bool _hasInitialPiece;

        // Target cycling (F10)
        private List<Pathfinder.ReachableTarget> _targets;
        private int _targetIndex;
        private bool _pathfindActive;

        // Active route + GPS
        private List<int> _currentRoute;
        private int _routeStepIndex;
        private MapPiece _routeTargetPiece;
        private string _routeTargetName;
        private bool _gpsActive;

        // Breadcrumbs
        private readonly List<MapPiece> _breadcrumbs = new List<MapPiece>();
        private MapPiece _lastJunction;

        // Auto-walk
        private bool _autoWalking;
        private bool _autoWalkCancelRequested;

        // Repeat
        private string _lastAnnouncement = "";

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the GameManager reference. Called from Main when GM is found.
        /// </summary>
        public void SetGameManager(GameManager gm)
        {
            _gameManager = gm;
        }

        /// <summary>
        /// Called every frame. Handles movement detection and GPS announcements.
        /// </summary>
        public void Update()
        {
            if (_gameManager == null) return;

            MapPiece currentPiece = _gameManager.myCurrentMapPiece;
            if (currentPiece == null) return;

            // Initialize tracking on first frame
            if (!_hasInitialPiece)
            {
                _lastMapPiece = currentPiece;
                _hasInitialPiece = true;
                AddBreadcrumb(currentPiece);
                return;
            }

            // Movement detection
            if (currentPiece != _lastMapPiece)
            {
                OnPlayerMoved(currentPiece);
                _lastMapPiece = currentPiece;
            }

            // Cancel auto-walk on any key press
            if (_autoWalking && Input.anyKeyDown)
            {
                _autoWalkCancelRequested = true;
                return;
            }

            // Enter key while target selection is active — get route
            if (_pathfindActive && Input.GetKeyDown(KeyCode.Return))
            {
                DebugLogger.LogInput("Enter", "Get route to target");
                SelectCurrentTarget();
                return;
            }

            // Escape or movement keys deactivate target cycling
            if (_pathfindActive && Input.GetKeyDown(KeyCode.Escape))
            {
                _pathfindActive = false;
                DebugLogger.LogState("Pathfind target selection cancelled");
            }
        }

        /// <summary>
        /// Handles F10 press: first press scans, subsequent presses cycle targets.
        /// </summary>
        public void HandlePathfindKey()
        {
            if (_gameManager == null) return;

            MapPiece currentPiece = _gameManager.myCurrentMapPiece;
            if (currentPiece == null) return;

            if (!_pathfindActive)
            {
                // First press: scan
                DebugLogger.LogInput("F10", "Scan for targets");
                ScanForTargets(currentPiece);
            }
            else
            {
                // Subsequent presses: cycle
                DebugLogger.LogInput("F10", "Cycle target");
                CycleTarget();
            }
        }

        /// <summary>
        /// Toggles GPS mode on/off for the current route (Shift+F10).
        /// </summary>
        public void ToggleGPS()
        {
            _gpsActive = !_gpsActive;

            if (_gpsActive)
            {
                Announce(Loc.Get("path_gps_on"));
                DebugLogger.LogState("GPS mode enabled");
            }
            else
            {
                Announce(Loc.Get("path_gps_off"));
                DebugLogger.LogState("GPS mode disabled");
            }
        }

        /// <summary>
        /// Gets directions back to the last junction or entry point (Ctrl+F10).
        /// </summary>
        public void Retrace()
        {
            if (_gameManager == null) return;

            MapPiece currentPiece = _gameManager.myCurrentMapPiece;
            if (currentPiece == null) return;

            if (_lastJunction == null)
            {
                Announce(Loc.Get("path_no_breadcrumbs"));
                return;
            }

            if (currentPiece == _lastJunction)
            {
                Announce(Loc.Get("nav_here"));
                return;
            }

            List<int> path = _pathfinder.FindPath(currentPiece, _lastJunction, _gameManager, _scanner);
            if (path == null)
            {
                Announce(Loc.Get("path_no_route"));
                return;
            }

            int lookDir = _gameManager.lookDirection;
            string route = _pathfinder.BuildRouteDescription(path, lookDir);
            Announce(Loc.Get("path_retrace", route));
        }

        /// <summary>
        /// Resets handler state. Called on scene change.
        /// </summary>
        public void Reset()
        {
            _gameManager = null;
            _lastMapPiece = null;
            _hasInitialPiece = false;
            _targets = null;
            _targetIndex = 0;
            _pathfindActive = false;
            _currentRoute = null;
            _routeStepIndex = 0;
            _routeTargetPiece = null;
            _routeTargetName = null;
            _gpsActive = false;
            _autoWalking = false;
            _autoWalkCancelRequested = true;
            _breadcrumbs.Clear();
            _visitedRooms.Clear();
            _lastJunction = null;
            _lastAnnouncement = "";
            DebugLogger.LogState("PathfindHandler reset");
        }

        /// <summary>
        /// Returns true if the given MapPiece has been visited this scene.
        /// Called by RoomHandler to tag room announcements.
        /// </summary>
        public bool IsVisited(MapPiece piece)
        {
            return piece != null && _visitedRooms.Contains(piece);
        }

        /// <summary>
        /// Repeats the last pathfinding announcement.
        /// </summary>
        public void RepeatLastAnnouncement()
        {
            if (!string.IsNullOrEmpty(_lastAnnouncement))
                ScreenReader.Say(_lastAnnouncement);
        }

        #endregion

        #region Private Methods — Movement Detection

        /// <summary>
        /// Called when the player moves to a new MapPiece.
        /// Updates breadcrumbs and handles GPS.
        /// </summary>
        private void OnPlayerMoved(MapPiece newPiece)
        {
            DebugLogger.Log(LogCategory.Handler, "PathfindHandler", "Player moved to new room");

            // Update breadcrumbs
            AddBreadcrumb(newPiece);

            // Deactivate target selection on movement
            if (_pathfindActive)
            {
                _pathfindActive = false;
            }

            // GPS mode: announce next step or arrival
            if (_gpsActive && _currentRoute != null)
            {
                HandleGPSMovement(newPiece);
            }
        }

        #endregion

        #region Private Methods — Target Scanning & Cycling

        /// <summary>
        /// Scans for all reachable targets and announces the summary + first target.
        /// </summary>
        private void ScanForTargets(MapPiece currentPiece)
        {
            Announce(Loc.Get("path_scanning"));

            _targets = _pathfinder.FindAllReachable(currentPiece, _gameManager, _scanner);
            _targetIndex = 0;

            if (_targets.Count == 0)
            {
                Announce(Loc.Get("path_no_targets"));
                _pathfindActive = false;
                return;
            }

            _pathfindActive = true;

            // Announce summary + first target
            string summary = Loc.Get("path_targets_summary", _targets.Count);
            string firstTarget = FormatTarget(_targets[0]);
            Announce($"{summary} {firstTarget}");
        }

        /// <summary>
        /// Cycles to the next target in the list and announces it.
        /// </summary>
        private void CycleTarget()
        {
            if (_targets == null || _targets.Count == 0) return;

            _targetIndex = (_targetIndex + 1) % _targets.Count;
            Announce(FormatTarget(_targets[_targetIndex]));
        }

        /// <summary>
        /// Selects the current target and announces the full route.
        /// </summary>
        private void SelectCurrentTarget()
        {
            if (_targets == null || _targets.Count == 0) return;

            var target = _targets[_targetIndex];
            _pathfindActive = false;

            MapPiece currentPiece = _gameManager.myCurrentMapPiece;
            if (currentPiece == null) return;

            if (target.Distance == 0)
            {
                Announce(Loc.Get("nav_here"));
                return;
            }

            // Get fresh path from current position
            List<int> path = _pathfinder.FindPath(currentPiece, target.Piece, _gameManager, _scanner);
            if (path == null)
            {
                Announce(Loc.Get("path_no_route"));
                return;
            }

            // Store route for GPS
            _currentRoute = path;
            _routeStepIndex = 0;
            _routeTargetPiece = target.Piece;
            _routeTargetName = FormatTargetName(target);

            int lookDir = _gameManager.lookDirection;
            string route = _pathfinder.BuildRouteDescription(path, lookDir);
            Announce(Loc.Get("path_route", _routeTargetName, route));
        }

        /// <summary>
        /// Formats a target for announcement.
        /// </summary>
        private string FormatTarget(Pathfinder.ReachableTarget target)
        {
            string typeName = Loc.Get(target.TypeKey);

            if (!string.IsNullOrEmpty(target.Name))
                return Loc.Get("path_target_item", typeName, target.Name, target.Distance);
            else
                return Loc.Get("path_target_unnamed", typeName, target.Distance);
        }

        /// <summary>
        /// Gets a short display name for a target (for route/arrival announcements).
        /// </summary>
        private string FormatTargetName(Pathfinder.ReachableTarget target)
        {
            if (!string.IsNullOrEmpty(target.Name))
                return target.Name;
            return Loc.Get(target.TypeKey);
        }

        #endregion

        #region Private Methods — GPS

        /// <summary>
        /// Handles GPS navigation after the player moves.
        /// Announces next step, arrival, or recalculates if off-route.
        /// </summary>
        private void HandleGPSMovement(MapPiece newPiece)
        {
            // Check for arrival
            if (newPiece == _routeTargetPiece)
            {
                ScreenReader.SayQueued(Loc.Get("path_arrived", _routeTargetName));
                _currentRoute = null;
                _routeStepIndex = 0;
                return;
            }

            // Check if player is on-route
            if (_routeStepIndex < _currentRoute.Count)
            {
                // Verify the move matches the expected direction
                int expectedDir = _currentRoute[_routeStepIndex];
                MapPiece expectedPiece = DirectionHelper.GetAdjacentPiece(_lastMapPiece, expectedDir);

                if (expectedPiece == newPiece)
                {
                    // On track — advance and announce next
                    _routeStepIndex++;

                    if (_routeStepIndex >= _currentRoute.Count)
                    {
                        // Should be at destination
                        ScreenReader.SayQueued(Loc.Get("path_arrived", _routeTargetName));
                        _currentRoute = null;
                        _routeStepIndex = 0;
                    }
                    else
                    {
                        // Announce next instruction
                        int nextDir = _currentRoute[_routeStepIndex];
                        int lookDir = _gameManager.lookDirection;
                        string instruction = _pathfinder.GetNextGPSInstruction(nextDir, lookDir);
                        ScreenReader.SayQueued(Loc.Get("path_gps_next", instruction));
                    }
                    return;
                }
            }

            // Off route — recalculate
            RecalculateRoute(newPiece);
        }

        /// <summary>
        /// Recalculates the route from the player's new position to the target.
        /// </summary>
        private void RecalculateRoute(MapPiece fromPiece)
        {
            if (_routeTargetPiece == null)
            {
                _currentRoute = null;
                return;
            }

            ScreenReader.SayQueued(Loc.Get("path_off_route"));

            List<int> newPath = _pathfinder.FindPath(fromPiece, _routeTargetPiece, _gameManager, _scanner);
            if (newPath == null)
            {
                _currentRoute = null;
                ScreenReader.SayQueued(Loc.Get("path_no_route"));
                return;
            }

            _currentRoute = newPath;
            _routeStepIndex = 0;

            // Announce the new route
            int lookDir = _gameManager.lookDirection;
            string route = _pathfinder.BuildRouteDescription(newPath, lookDir);
            ScreenReader.SayQueued(Loc.Get("path_route", _routeTargetName, route));
        }

        #endregion

        #region Private Methods — Breadcrumbs

        /// <summary>
        /// Adds a room to the breadcrumb trail and updates junction tracking.
        /// </summary>
        private void AddBreadcrumb(MapPiece piece)
        {
            if (piece == null) return;

            _visitedRooms.Add(piece);
            _breadcrumbs.Add(piece);

            // Cap breadcrumb list
            if (_breadcrumbs.Count > MaxBreadcrumbs)
                _breadcrumbs.RemoveAt(0);

            // Check if this room is a junction (3+ open exits)
            if (_gameManager != null)
            {
                int openExits = CountOpenExits(piece);
                if (openExits >= JunctionExitThreshold)
                {
                    _lastJunction = piece;
                    DebugLogger.Log(LogCategory.Handler, "PathfindHandler",
                        $"Junction detected: {openExits} exits");
                }
            }
        }

        /// <summary>
        /// Counts the number of open (unblocked) exits from a room.
        /// </summary>
        private int CountOpenExits(MapPiece piece)
        {
            int count = 0;
            for (int dir = 0; dir < 4; dir++)
            {
                MapPiece adjacent = DirectionHelper.GetAdjacentPiece(piece, dir);
                if (adjacent == null) continue;

                if (!_scanner.IsDirectionBlocked(piece, adjacent, dir, _gameManager))
                    count++;
            }
            return count;
        }

        #endregion

        #region Auto-Walk

        /// <summary>
        /// Starts auto-walking to the nearest unvisited room (Ctrl+F9).
        /// </summary>
        public void AutoWalkToUnexplored()
        {
            if (_gameManager == null) return;

            if (_autoWalking)
            {
                _autoWalkCancelRequested = true;
                return;
            }

            MapPiece currentPiece = _gameManager.myCurrentMapPiece;
            if (currentPiece == null) return;

            // Find nearest unvisited room
            MapPiece target = FindNearestUnvisited(currentPiece);
            if (target == null)
            {
                Announce(Loc.Get("path_no_unexplored"));
                return;
            }

            List<int> path = _pathfinder.FindPath(currentPiece, target, _gameManager, _scanner);
            if (path == null || path.Count == 0)
            {
                Announce(Loc.Get("path_no_route"));
                return;
            }

            Announce(Loc.Get("path_autowalk_start", path.Count));
            MelonCoroutines.Start(AutoWalkCoroutine(path));
        }

        /// <summary>
        /// BFS to find the nearest unvisited, reachable room.
        /// </summary>
        private MapPiece FindNearestUnvisited(MapPiece start)
        {
            var visited = new HashSet<MapPiece>();
            var queue = new Queue<MapPiece>();

            visited.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                MapPiece current = queue.Dequeue();

                for (int dir = 0; dir < 4; dir++)
                {
                    MapPiece next = DirectionHelper.GetAdjacentPiece(current, dir);
                    if (next == null || visited.Contains(next)) continue;

                    if (_scanner.IsDirectionBlocked(current, next, dir, _gameManager))
                        continue;

                    // Skip rooms with alive enemies (can't walk through)
                    if (next.myMapEnemy != null && !next.myMapEnemy.isDead)
                        continue;

                    visited.Add(next);

                    // Found an unvisited room
                    if (!_visitedRooms.Contains(next))
                        return next;

                    queue.Enqueue(next);
                }
            }

            return null;
        }

        /// <summary>
        /// Coroutine that walks the player along a path by calling game movement methods.
        /// Cancels on any key press.
        /// </summary>
        private IEnumerator AutoWalkCoroutine(List<int> path)
        {
            _autoWalking = true;
            _autoWalkCancelRequested = false;

            for (int i = 0; i < path.Count; i++)
            {
                if (_autoWalkCancelRequested || _gameManager == null)
                {
                    Announce(Loc.Get("path_autowalk_cancelled"));
                    break;
                }

                int targetDir = path[i];
                int currentFacing = _gameManager.lookDirection;

                // Rotate to face the correct direction
                int relDir = DirectionHelper.AbsoluteToRelative(targetDir, currentFacing);

                if (relDir == 1) // need to turn right
                {
                    _gameManager.RotateRight();
                    yield return new WaitForSeconds(0.2f);
                }
                else if (relDir == 3) // need to turn left
                {
                    _gameManager.RotateLeft();
                    yield return new WaitForSeconds(0.2f);
                }
                else if (relDir == 2) // need to turn around
                {
                    _gameManager.RotateRight();
                    yield return new WaitForSeconds(0.2f);
                    if (_autoWalkCancelRequested) break;
                    _gameManager.RotateRight();
                    yield return new WaitForSeconds(0.2f);
                }
                // relDir == 0: already facing ahead

                if (_autoWalkCancelRequested) break;

                // Wait until the game is ready for movement
                float waitTimer = 0f;
                while (!_gameManager.isAtTarget && waitTimer < 3f)
                {
                    yield return null;
                    waitTimer += Time.deltaTime;
                }

                if (_autoWalkCancelRequested) break;

                // Move forward
                MapPiece beforeMove = _gameManager.myCurrentMapPiece;
                _gameManager.ProcessCenterClick();

                // Wait for movement to complete (position changes)
                waitTimer = 0f;
                while (_gameManager.myCurrentMapPiece == beforeMove && waitTimer < 3f)
                {
                    yield return null;
                    waitTimer += Time.deltaTime;
                }

                if (_gameManager.myCurrentMapPiece == beforeMove)
                {
                    // Movement didn't happen — blocked
                    Announce(Loc.Get("path_autowalk_blocked"));
                    break;
                }

                // Wait for isAtTarget to settle
                waitTimer = 0f;
                while (!_gameManager.isAtTarget && waitTimer < 3f)
                {
                    yield return null;
                    waitTimer += Time.deltaTime;
                }

                // Small delay between steps for stability
                yield return new WaitForSeconds(0.1f);
            }

            if (!_autoWalkCancelRequested && _gameManager != null)
            {
                Announce(Loc.Get("path_autowalk_arrived"));
            }

            _autoWalking = false;
            _autoWalkCancelRequested = false;
        }

        #endregion

        #region Private Methods — Utility

        /// <summary>
        /// Announces text via screenreader and stores for repeat.
        /// </summary>
        private void Announce(string text)
        {
            _lastAnnouncement = text;
            ScreenReader.Say(text);
        }

        #endregion
    }
}

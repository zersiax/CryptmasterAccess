using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CryptmasterAccess
{
    /// <summary>
    /// Category-based navigation query system.
    /// Lets the player cycle through categories (Exits, NPCs, Enemies, Interactables, POIs)
    /// and items within each category, with BFS pathfinding directions to selected items.
    /// </summary>
    public class NavigationHandler
    {
        #region Types

        /// <summary>
        /// Navigation categories for grouping nearby items.
        /// </summary>
        public enum NavCategory
        {
            Exits,
            NPCs,
            Enemies,
            Interactables,
            Scenery,
            POIs
        }

        /// <summary>
        /// A single navigable item within a category.
        /// </summary>
        public struct NavItem
        {
            /// <summary>Display name (e.g. "Goblin", "Container", "Exit ahead")</summary>
            public string Name;

            /// <summary>Localization type key (e.g. "poi_enemy")</summary>
            public string TypeKey;

            /// <summary>Absolute grid direction from player (0=west, 1=north, 2=east, 3=south)</summary>
            public int AbsoluteDirection;

            /// <summary>Steps away from player (0 = current room)</summary>
            public int Distance;

            /// <summary>Target map piece for pathfinding</summary>
            public MapPiece Piece;
        }

        #endregion

        #region Constants

        private static readonly NavCategory[] AllCategories = new NavCategory[]
        {
            NavCategory.Exits,
            NavCategory.NPCs,
            NavCategory.Enemies,
            NavCategory.Interactables,
            NavCategory.Scenery,
            NavCategory.POIs
        };

        private const int MaxPathfindDepth = 10;

        #endregion

        #region Fields

        private GameManager _gameManager;
        private readonly RoomScanner _scanner = new RoomScanner();

        private int _currentCategoryIndex;
        private int _currentItemIndex;
        private MapPiece _lastPiece;
        private int _lastLookDir = -1;

        /// <summary>Cached items per category, rebuilt on room/rotation change.</summary>
        private readonly Dictionary<NavCategory, List<NavItem>> _cachedItems
            = new Dictionary<NavCategory, List<NavItem>>();

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
        /// Called every frame. Checks for navigation key presses and invalidates cache on movement/rotation.
        /// </summary>
        public void Update()
        {
            if (_gameManager == null)
            {
                _gameManager = Object.FindObjectOfType<GameManager>();
                if (_gameManager == null) return;
            }

            MapPiece currentPiece = _gameManager.myCurrentMapPiece;
            if (currentPiece == null) return;

            // Invalidate cache on room or rotation change
            int lookDir = _gameManager.lookDirection;
            if (currentPiece != _lastPiece || lookDir != _lastLookDir)
            {
                InvalidateCache();
                _lastPiece = currentPiece;
                _lastLookDir = lookDir;
            }

            // Ctrl+PgUp / Ctrl+PgDn — cycle categories
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (ctrl && Input.GetKeyDown(KeyCode.PageUp))
            {
                DebugLogger.LogInput("Ctrl+PgUp", "Previous category");
                CycleCategory(-1);
                return;
            }
            if (ctrl && Input.GetKeyDown(KeyCode.PageDown))
            {
                DebugLogger.LogInput("Ctrl+PgDn", "Next category");
                CycleCategory(1);
                return;
            }

            // PgUp / PgDn without Ctrl — cycle items
            if (!ctrl && Input.GetKeyDown(KeyCode.PageUp))
            {
                DebugLogger.LogInput("PgUp", "Previous item");
                CycleItem(-1);
                return;
            }
            if (!ctrl && Input.GetKeyDown(KeyCode.PageDown))
            {
                DebugLogger.LogInput("PgDn", "Next item");
                CycleItem(1);
                return;
            }

            // End — announce directions to selected item
            if (Input.GetKeyDown(KeyCode.End))
            {
                DebugLogger.LogInput("End", "Directions to item");
                AnnounceDirections();
                return;
            }
        }

        /// <summary>
        /// Resets handler state. Called on scene change.
        /// </summary>
        public void Reset()
        {
            _gameManager = null;
            _lastPiece = null;
            _lastLookDir = -1;
            _currentCategoryIndex = 0;
            _currentItemIndex = 0;
            _cachedItems.Clear();
            DebugLogger.LogState("NavigationHandler reset");
        }

        #endregion

        #region Private Methods — Category/Item Cycling

        /// <summary>
        /// Cycles through navigation categories. Announces category name and first item.
        /// </summary>
        private void CycleCategory(int direction)
        {
            EnsureCache();

            _currentCategoryIndex = Wrap(_currentCategoryIndex + direction, AllCategories.Length);
            _currentItemIndex = 0;

            NavCategory cat = AllCategories[_currentCategoryIndex];
            List<NavItem> items = GetCategoryItems(cat);

            string catName = GetCategoryName(cat);

            if (items.Count == 0)
            {
                ScreenReader.Say(Loc.Get("nav_category_announce", catName, Loc.Get("nav_no_items"), ""));
            }
            else
            {
                string itemDesc = FormatNavItem(items[0]);
                string position = Loc.Get("nav_category_announce", catName, "1", items.Count.ToString());
                ScreenReader.Say($"{position}. {itemDesc}");
            }
        }

        /// <summary>
        /// Cycles through items within the current category. Announces item with position.
        /// </summary>
        private void CycleItem(int direction)
        {
            EnsureCache();

            NavCategory cat = AllCategories[_currentCategoryIndex];
            List<NavItem> items = GetCategoryItems(cat);

            if (items.Count == 0)
            {
                string catName = GetCategoryName(cat);
                ScreenReader.Say(Loc.Get("nav_category_announce", catName, Loc.Get("nav_no_items"), ""));
                return;
            }

            _currentItemIndex = Wrap(_currentItemIndex + direction, items.Count);

            string itemDesc = FormatNavItem(items[_currentItemIndex]);
            string position = Loc.Get("nav_category_announce",
                GetCategoryName(cat),
                (_currentItemIndex + 1).ToString(),
                items.Count.ToString());
            ScreenReader.Say($"{position}. {itemDesc}");
        }

        /// <summary>
        /// Announces BFS pathfinding directions to the currently selected item.
        /// </summary>
        private void AnnounceDirections()
        {
            EnsureCache();

            NavCategory cat = AllCategories[_currentCategoryIndex];
            List<NavItem> items = GetCategoryItems(cat);

            if (items.Count == 0)
            {
                ScreenReader.Say(Loc.Get("nav_unreachable"));
                return;
            }

            NavItem item = items[_currentItemIndex];

            if (item.Distance == 0)
            {
                ScreenReader.Say(Loc.Get("nav_here"));
                return;
            }

            MapPiece currentPiece = _gameManager.myCurrentMapPiece;
            if (currentPiece == null || item.Piece == null)
            {
                ScreenReader.Say(Loc.Get("nav_unreachable"));
                return;
            }

            string directions = BuildDirections(currentPiece, _gameManager.lookDirection, item.Piece);
            ScreenReader.Say(directions);
        }

        #endregion

        #region Private Methods — Cache

        /// <summary>
        /// Ensures the navigation cache is populated.
        /// </summary>
        private void EnsureCache()
        {
            if (_cachedItems.Count == 0)
                RebuildCache();
        }

        /// <summary>
        /// Invalidates the navigation cache, forcing a rebuild on next access.
        /// </summary>
        private void InvalidateCache()
        {
            _cachedItems.Clear();
            _currentCategoryIndex = 0;
            _currentItemIndex = 0;
        }

        /// <summary>
        /// Scans current and nearby rooms, populates cached items per category.
        /// </summary>
        private void RebuildCache()
        {
            _cachedItems.Clear();

            // Initialize empty lists for all categories
            foreach (var cat in AllCategories)
                _cachedItems[cat] = new List<NavItem>();

            if (_gameManager == null) return;
            MapPiece currentPiece = _gameManager.myCurrentMapPiece;
            if (currentPiece == null) return;

            int lookDir = _gameManager.lookDirection;

            // === Exits ===
            List<RoomScanner.Exit> exits = _scanner.ScanExits(currentPiece, _gameManager);
            foreach (var exit in exits)
            {
                if (exit.IsBlocked) continue;
                int relDir = DirectionHelper.AbsoluteToRelative(exit.AbsoluteDirection, lookDir);
                string dirName = DirectionHelper.GetRelativeName(relDir);
                MapPiece exitPiece = DirectionHelper.GetAdjacentPiece(currentPiece, exit.AbsoluteDirection);

                string visitedTag = Main.PathfindHandlerInstance != null && Main.PathfindHandlerInstance.IsVisited(exitPiece)
                    ? Loc.Get("path_room_visited")
                    : Loc.Get("path_room_new");

                _cachedItems[NavCategory.Exits].Add(new NavItem
                {
                    Name = Loc.Get("nav_exit_item", dirName) + ", " + visitedTag,
                    TypeKey = "nav_cat_exits",
                    AbsoluteDirection = exit.AbsoluteDirection,
                    Distance = 1,
                    Piece = exitPiece
                });
            }

            // === Current room items (distance 0) ===
            AddCurrentRoomItems(currentPiece);

            // === Nearby POIs (distance 1+) ===
            List<RoomScanner.PointOfInterest> pois = _scanner.ScanNearbyPOIs(currentPiece, _gameManager);
            foreach (var poi in pois)
            {
                NavItem item = new NavItem
                {
                    Name = !string.IsNullOrEmpty(poi.Name) ? poi.Name : Loc.Get(poi.TypeKey),
                    TypeKey = poi.TypeKey,
                    AbsoluteDirection = poi.AbsoluteDirection,
                    Distance = poi.Distance,
                    Piece = GetPieceAtDistance(currentPiece, poi.AbsoluteDirection, poi.Distance)
                };

                NavCategory targetCat = ClassifyPOI(poi.TypeKey);
                _cachedItems[targetCat].Add(item);

                // Also add to POIs (all) category
                if (targetCat != NavCategory.POIs)
                    _cachedItems[NavCategory.POIs].Add(item);
            }

            DebugLogger.Log(LogCategory.Handler, "NavigationHandler",
                $"Cache rebuilt: Exits={_cachedItems[NavCategory.Exits].Count}, " +
                $"NPCs={_cachedItems[NavCategory.NPCs].Count}, " +
                $"Enemies={_cachedItems[NavCategory.Enemies].Count}, " +
                $"Interactables={_cachedItems[NavCategory.Interactables].Count}, " +
                $"POIs={_cachedItems[NavCategory.POIs].Count}");
        }

        /// <summary>
        /// Adds items from the current room (distance 0) to appropriate categories.
        /// </summary>
        private void AddCurrentRoomItems(MapPiece piece)
        {
            // Enemy in current room
            if (piece.myMapEnemy != null && !piece.myMapEnemy.isDead)
            {
                var item = new NavItem
                {
                    Name = piece.myMapEnemy.enemyName ?? Loc.Get("poi_enemy"),
                    TypeKey = "poi_enemy",
                    AbsoluteDirection = -1,
                    Distance = 0,
                    Piece = piece
                };
                _cachedItems[NavCategory.Enemies].Add(item);
                _cachedItems[NavCategory.POIs].Add(item);
            }

            // NPC in current room
            if (piece.myMapNPC != null)
            {
                var item = new NavItem
                {
                    Name = piece.myMapNPC.npcName ?? Loc.Get("poi_npc"),
                    TypeKey = "poi_npc",
                    AbsoluteDirection = -1,
                    Distance = 0,
                    Piece = piece
                };
                _cachedItems[NavCategory.NPCs].Add(item);
                _cachedItems[NavCategory.POIs].Add(item);
            }

            // Container — interactive vs scenery
            if (piece.myCollectionContainer != null && !piece.myCollectionContainer.hasOpened)
            {
                if (RoomScanner.IsInteractableContainer(piece.myCollectionContainer))
                {
                    var item = new NavItem
                    {
                        Name = Loc.Get("poi_container"),
                        TypeKey = "poi_container",
                        AbsoluteDirection = -1,
                        Distance = 0,
                        Piece = piece
                    };
                    _cachedItems[NavCategory.Interactables].Add(item);
                    _cachedItems[NavCategory.POIs].Add(item);
                }
                else
                {
                    var item = new NavItem
                    {
                        Name = piece.myCollectionContainer.containerBaseName ?? Loc.Get("poi_scenery"),
                        TypeKey = "poi_scenery",
                        AbsoluteDirection = -1,
                        Distance = 0,
                        Piece = piece
                    };
                    _cachedItems[NavCategory.Scenery].Add(item);
                    _cachedItems[NavCategory.POIs].Add(item);
                }
            }

            // Trap
            if (piece.myWorldTrap != null && piece.myWorldTrap.isActive)
            {
                var item = new NavItem
                {
                    Name = Loc.Get("poi_trap"),
                    TypeKey = "poi_trap",
                    AbsoluteDirection = -1,
                    Distance = 0,
                    Piece = piece
                };
                _cachedItems[NavCategory.Interactables].Add(item);
                _cachedItems[NavCategory.POIs].Add(item);
            }

            // Shop
            if (piece.canShop)
            {
                var item = new NavItem
                {
                    Name = Loc.Get("poi_shop"),
                    TypeKey = "poi_shop",
                    AbsoluteDirection = -1,
                    Distance = 0,
                    Piece = piece
                };
                _cachedItems[NavCategory.Interactables].Add(item);
                _cachedItems[NavCategory.POIs].Add(item);
            }

            // Fishing
            if (piece.canFish)
            {
                var item = new NavItem
                {
                    Name = Loc.Get("poi_fishing"),
                    TypeKey = "poi_fishing",
                    AbsoluteDirection = -1,
                    Distance = 0,
                    Piece = piece
                };
                _cachedItems[NavCategory.Interactables].Add(item);
                _cachedItems[NavCategory.POIs].Add(item);
            }

            // Cards
            if (piece.canPlayCards)
            {
                var item = new NavItem
                {
                    Name = Loc.Get("poi_cards"),
                    TypeKey = "poi_cards",
                    AbsoluteDirection = -1,
                    Distance = 0,
                    Piece = piece
                };
                _cachedItems[NavCategory.Interactables].Add(item);
                _cachedItems[NavCategory.POIs].Add(item);
            }
        }

        /// <summary>
        /// Classifies a POI type key into a navigation category.
        /// </summary>
        private NavCategory ClassifyPOI(string typeKey)
        {
            switch (typeKey)
            {
                case "poi_enemy": return NavCategory.Enemies;
                case "poi_npc": return NavCategory.NPCs;
                case "poi_scenery": return NavCategory.Scenery;
                case "poi_container":
                case "poi_trap":
                case "poi_shop":
                case "poi_fishing":
                case "poi_cards":
                    return NavCategory.Interactables;
                default:
                    return NavCategory.POIs;
            }
        }

        /// <summary>
        /// Gets the items list for a given category from cache.
        /// </summary>
        private List<NavItem> GetCategoryItems(NavCategory category)
        {
            if (_cachedItems.TryGetValue(category, out var items))
                return items;
            return new List<NavItem>();
        }

        /// <summary>
        /// Walks a number of steps in a direction from a starting piece.
        /// Returns the piece at that distance, or null if path is broken.
        /// </summary>
        private MapPiece GetPieceAtDistance(MapPiece start, int absoluteDir, int distance)
        {
            MapPiece current = start;
            for (int i = 0; i < distance; i++)
            {
                MapPiece next = DirectionHelper.GetAdjacentPiece(current, absoluteDir);
                if (next == null) return null;
                current = next;
            }
            return current;
        }

        #endregion

        #region Private Methods — Pathfinding

        /// <summary>
        /// BFS pathfinding from player's current piece to target.
        /// Returns relative turn-by-turn directions from the player's perspective.
        /// </summary>
        private string BuildDirections(MapPiece from, int lookDir, MapPiece target)
        {
            if (from == target)
                return Loc.Get("nav_here");

            // BFS to find path
            var visited = new HashSet<MapPiece>();
            var parent = new Dictionary<MapPiece, MapPiece>();
            var directionTo = new Dictionary<MapPiece, int>(); // absolute direction used to reach this piece
            var queue = new Queue<MapPiece>();

            visited.Add(from);
            queue.Enqueue(from);

            bool found = false;
            int depth = 0;

            while (queue.Count > 0 && depth < MaxPathfindDepth)
            {
                int levelSize = queue.Count;
                for (int i = 0; i < levelSize; i++)
                {
                    MapPiece current = queue.Dequeue();

                    for (int dir = 0; dir < 4; dir++)
                    {
                        MapPiece next = DirectionHelper.GetAdjacentPiece(current, dir);
                        if (next == null || visited.Contains(next)) continue;

                        // Check if path is passable
                        if (_scanner.IsDirectionBlocked(current, next, dir, _gameManager))
                            continue;

                        visited.Add(next);
                        parent[next] = current;
                        directionTo[next] = dir;
                        queue.Enqueue(next);

                        if (next == target)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found) break;
                }

                if (found) break;
                depth++;
            }

            if (!found)
                return Loc.Get("nav_unreachable");

            // Reconstruct path as list of absolute directions
            var path = new List<int>();
            MapPiece step = target;
            while (parent.ContainsKey(step))
            {
                path.Add(directionTo[step]);
                step = parent[step];
            }
            path.Reverse();

            // Convert to relative turn-by-turn directions
            return FormatRelativeDirections(path, lookDir);
        }

        /// <summary>
        /// Converts a list of absolute movement directions into relative turn-by-turn instructions.
        /// Groups consecutive same-direction moves (e.g. "3 ahead" instead of "ahead, ahead, ahead").
        /// </summary>
        private string FormatRelativeDirections(List<int> absolutePath, int lookDir)
        {
            if (absolutePath.Count == 0)
                return Loc.Get("nav_here");

            var parts = new List<string>();
            int currentFacing = lookDir;

            int i = 0;
            while (i < absolutePath.Count)
            {
                int moveDir = absolutePath[i];
                int relDir = DirectionHelper.AbsoluteToRelative(moveDir, currentFacing);

                // If not facing the right way, add a turn instruction
                if (relDir == 1) // right
                {
                    parts.Add(Loc.Get("nav_turn_right"));
                    currentFacing = moveDir;
                    relDir = 0; // now facing ahead
                }
                else if (relDir == 3) // left
                {
                    parts.Add(Loc.Get("nav_turn_left"));
                    currentFacing = moveDir;
                    relDir = 0;
                }
                else if (relDir == 2) // behind
                {
                    parts.Add(Loc.Get("nav_turn_around"));
                    currentFacing = moveDir;
                    relDir = 0;
                }

                // Count consecutive steps in the same direction
                int count = 0;
                while (i < absolutePath.Count && absolutePath[i] == moveDir)
                {
                    count++;
                    i++;
                }

                parts.Add(Loc.Get("nav_go_ahead", count));

                // Update facing to the direction we just moved
                currentFacing = moveDir;
            }

            return string.Join(", ", parts.ToArray());
        }

        #endregion

        #region Private Methods — Formatting

        /// <summary>
        /// Formats a NavItem for announcement.
        /// </summary>
        private string FormatNavItem(NavItem item)
        {
            if (item.Distance == 0)
                return Loc.Get("nav_item_here", item.Name);

            int lookDir = _gameManager != null ? _gameManager.lookDirection : 0;
            int relDir = DirectionHelper.AbsoluteToRelative(item.AbsoluteDirection, lookDir);
            string dirName = DirectionHelper.GetRelativeName(relDir);

            return Loc.Get("nav_item", item.Name, item.Distance, dirName);
        }

        /// <summary>
        /// Gets the localized display name for a navigation category.
        /// </summary>
        private string GetCategoryName(NavCategory category)
        {
            switch (category)
            {
                case NavCategory.Exits: return Loc.Get("nav_cat_exits");
                case NavCategory.NPCs: return Loc.Get("nav_cat_npcs");
                case NavCategory.Enemies: return Loc.Get("nav_cat_enemies");
                case NavCategory.Interactables: return Loc.Get("nav_cat_interactables");
                case NavCategory.Scenery: return Loc.Get("nav_cat_scenery");
                case NavCategory.POIs: return Loc.Get("nav_cat_pois");
                default: return category.ToString();
            }
        }

        /// <summary>
        /// Wraps an index within a range (supports negative wrapping).
        /// </summary>
        private int Wrap(int index, int count)
        {
            if (count <= 0) return 0;
            return ((index % count) + count) % count;
        }

        #endregion
    }
}

using System.Collections.Generic;

namespace CryptmasterAccess
{
    /// <summary>
    /// BFS pathfinding engine for grid-based dungeon navigation.
    /// Finds paths between MapPieces and scans for reachable targets.
    /// Pure utility class — no game state, no input handling.
    /// </summary>
    public class Pathfinder
    {
        #region Types

        /// <summary>
        /// A target reachable via BFS from the player's position.
        /// </summary>
        public struct ReachableTarget
        {
            /// <summary>The MapPiece containing this target.</summary>
            public MapPiece Piece;

            /// <summary>Localization type key (e.g. "poi_npc", "poi_container").</summary>
            public string TypeKey;

            /// <summary>Display name (NPC name, enemy name, etc.).</summary>
            public string Name;

            /// <summary>BFS distance in steps from the start.</summary>
            public int Distance;

            /// <summary>Absolute direction list from start to this target.</summary>
            public List<int> Path;
        }

        #endregion

        #region Constants

        private const int DefaultMaxDepth = 100;

        #endregion

        #region Public Methods

        /// <summary>
        /// BFS from start to target. Returns list of absolute directions, or null if unreachable.
        /// </summary>
        public List<int> FindPath(MapPiece start, MapPiece target, GameManager gm, RoomScanner scanner, int maxDepth = DefaultMaxDepth)
        {
            if (start == null || target == null) return null;
            if (start == target) return new List<int>();

            var visited = new HashSet<MapPiece>();
            var parentInfo = new Dictionary<MapPiece, ParentEntry>();
            var queue = new Queue<MapPiece>();

            visited.Add(start);
            queue.Enqueue(start);

            bool found = false;
            int depth = 0;

            while (queue.Count > 0 && depth < maxDepth)
            {
                int levelSize = queue.Count;
                for (int i = 0; i < levelSize; i++)
                {
                    MapPiece current = queue.Dequeue();

                    for (int dir = 0; dir < 4; dir++)
                    {
                        MapPiece next = DirectionHelper.GetAdjacentPiece(current, dir);
                        if (next == null || visited.Contains(next)) continue;

                        if (scanner.IsDirectionBlocked(current, next, dir, gm))
                            continue;

                        // Skip rooms with alive enemies (can't walk through)
                        // Exception: the target itself (we want to path TO enemies)
                        if (next != target && next.myMapEnemy != null && !next.myMapEnemy.isDead)
                            continue;

                        visited.Add(next);
                        parentInfo[next] = new ParentEntry { Parent = current, Direction = dir };
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

            if (!found) return null;

            // Reconstruct path
            var path = new List<int>();
            MapPiece step = target;
            while (parentInfo.ContainsKey(step))
            {
                var entry = parentInfo[step];
                path.Add(entry.Direction);
                step = entry.Parent;
            }
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Scans all reachable rooms via BFS and collects targets (NPCs, containers, enemies, shops, etc.).
        /// Returns targets sorted by distance.
        /// </summary>
        public List<ReachableTarget> FindAllReachable(MapPiece start, GameManager gm, RoomScanner scanner, int maxDepth = DefaultMaxDepth)
        {
            var targets = new List<ReachableTarget>();
            if (start == null) return targets;

            var visited = new HashSet<MapPiece>();
            var parentInfo = new Dictionary<MapPiece, ParentEntry>();
            var depthMap = new Dictionary<MapPiece, int>();
            var queue = new Queue<MapPiece>();

            visited.Add(start);
            depthMap[start] = 0;
            queue.Enqueue(start);

            // Check current room too (distance 0)
            CollectTargets(start, 0, null, targets);

            while (queue.Count > 0)
            {
                MapPiece current = queue.Dequeue();
                int currentDepth = depthMap[current];

                if (currentDepth >= maxDepth) continue;

                for (int dir = 0; dir < 4; dir++)
                {
                    MapPiece next = DirectionHelper.GetAdjacentPiece(current, dir);
                    if (next == null || visited.Contains(next)) continue;

                    if (scanner.IsDirectionBlocked(current, next, dir, gm))
                        continue;

                    int nextDepth = currentDepth + 1;
                    visited.Add(next);
                    parentInfo[next] = new ParentEntry { Parent = current, Direction = dir };
                    depthMap[next] = nextDepth;

                    // Collect targets from this room
                    List<int> path = ReconstructPath(next, parentInfo);
                    CollectTargets(next, nextDepth, path, targets);

                    // Don't traverse through alive enemies
                    if (next.myMapEnemy != null && !next.myMapEnemy.isDead)
                        continue;

                    queue.Enqueue(next);
                }
            }

            // Sort by distance
            targets.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            return targets;
        }

        /// <summary>
        /// Converts an absolute direction path to a human-readable relative route description.
        /// Groups consecutive same-direction steps and inserts turn instructions.
        /// </summary>
        public string BuildRouteDescription(List<int> path, int playerLookDirection)
        {
            if (path == null || path.Count == 0)
                return Loc.Get("nav_here");

            var parts = new List<string>();
            int currentFacing = playerLookDirection;

            int i = 0;
            while (i < path.Count)
            {
                int moveDir = path[i];
                int relDir = DirectionHelper.AbsoluteToRelative(moveDir, currentFacing);

                // Insert turn instruction if needed
                if (relDir == 1) // right
                {
                    parts.Add(Loc.Get("path_turn_right"));
                    currentFacing = moveDir;
                }
                else if (relDir == 3) // left
                {
                    parts.Add(Loc.Get("path_turn_left"));
                    currentFacing = moveDir;
                }
                else if (relDir == 2) // behind
                {
                    parts.Add(Loc.Get("path_turn_around"));
                    currentFacing = moveDir;
                }

                // Count consecutive steps in the same direction
                int count = 0;
                while (i < path.Count && path[i] == moveDir)
                {
                    count++;
                    i++;
                }

                parts.Add(Loc.Get("path_go", count, Loc.Get("dir_ahead")));

                currentFacing = moveDir;
            }

            return string.Join(", ", parts.ToArray());
        }

        /// <summary>
        /// Gets the next relative instruction for GPS mode given current position in route.
        /// Returns the relative direction the player needs to go (as a localized string).
        /// </summary>
        public string GetNextGPSInstruction(int nextAbsoluteDir, int playerLookDirection)
        {
            int relDir = DirectionHelper.AbsoluteToRelative(nextAbsoluteDir, playerLookDirection);

            switch (relDir)
            {
                case 0: return Loc.Get("dir_ahead");
                case 1: return Loc.Get("path_turn_right");
                case 2: return Loc.Get("path_turn_around");
                case 3: return Loc.Get("path_turn_left");
                default: return Loc.Get("dir_ahead");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Reconstructs the path from start to a given piece using the parent map.
        /// </summary>
        private List<int> ReconstructPath(MapPiece piece, Dictionary<MapPiece, ParentEntry> parentInfo)
        {
            var path = new List<int>();
            MapPiece step = piece;
            while (parentInfo.ContainsKey(step))
            {
                var entry = parentInfo[step];
                path.Add(entry.Direction);
                step = entry.Parent;
            }
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Checks a MapPiece for targets and adds them to the list.
        /// </summary>
        private void CollectTargets(MapPiece piece, int distance, List<int> path, List<ReachableTarget> targets)
        {
            if (piece == null) return;

            // NPC
            if (piece.myMapNPC != null)
            {
                targets.Add(new ReachableTarget
                {
                    Piece = piece,
                    TypeKey = "poi_npc",
                    Name = piece.myMapNPC.npcName ?? "",
                    Distance = distance,
                    Path = path != null ? new List<int>(path) : new List<int>()
                });
            }

            // Enemy (alive)
            if (piece.myMapEnemy != null && !piece.myMapEnemy.isDead)
            {
                targets.Add(new ReachableTarget
                {
                    Piece = piece,
                    TypeKey = "poi_enemy",
                    Name = piece.myMapEnemy.enemyName ?? "",
                    Distance = distance,
                    Path = path != null ? new List<int>(path) : new List<int>()
                });
            }

            // Container (unopened, active, interactive only — skip scenery)
            if (piece.myCollectionContainer != null && !piece.myCollectionContainer.hasOpened
                && piece.myCollectionContainer.gameObject.activeInHierarchy
                && RoomScanner.IsInteractableContainer(piece.myCollectionContainer))
            {
                targets.Add(new ReachableTarget
                {
                    Piece = piece,
                    TypeKey = "poi_container",
                    Name = piece.myCollectionContainer.containerBaseName ?? "",
                    Distance = distance,
                    Path = path != null ? new List<int>(path) : new List<int>()
                });
            }

            // Shop
            if (piece.canShop)
            {
                targets.Add(new ReachableTarget
                {
                    Piece = piece,
                    TypeKey = "poi_shop",
                    Name = "",
                    Distance = distance,
                    Path = path != null ? new List<int>(path) : new List<int>()
                });
            }

            // Fishing
            if (piece.canFish)
            {
                targets.Add(new ReachableTarget
                {
                    Piece = piece,
                    TypeKey = "poi_fishing",
                    Name = "",
                    Distance = distance,
                    Path = path != null ? new List<int>(path) : new List<int>()
                });
            }

            // Cards
            if (piece.canPlayCards)
            {
                targets.Add(new ReachableTarget
                {
                    Piece = piece,
                    TypeKey = "poi_cards",
                    Name = "",
                    Distance = distance,
                    Path = path != null ? new List<int>(path) : new List<int>()
                });
            }

            // Level transition doors (walls with warpCoverDoor)
            if (piece.allWalls != null)
            {
                foreach (var wall in piece.allWalls)
                {
                    if (wall != null && wall.warpCoverDoor != null)
                    {
                        targets.Add(new ReachableTarget
                        {
                            Piece = piece,
                            TypeKey = "poi_door",
                            Name = "",
                            Distance = distance,
                            Path = path != null ? new List<int>(path) : new List<int>()
                        });
                        break; // One door target per room is enough
                    }
                }
            }
        }

        #endregion

        #region Private Types

        /// <summary>
        /// BFS parent tracking entry.
        /// </summary>
        private struct ParentEntry
        {
            public MapPiece Parent;
            public int Direction;
        }

        #endregion
    }
}

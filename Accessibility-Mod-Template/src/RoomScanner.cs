using System.Collections.Generic;

namespace CryptmasterAccess
{
    /// <summary>
    /// Scans the grid in all 4 directions from the player's current room.
    /// Detects exits, enemies, NPCs, containers, traps, shops, fishing, and cards.
    /// </summary>
    public class RoomScanner
    {
        #region Data Types

        /// <summary>
        /// An exit from the current room in a given direction.
        /// </summary>
        public struct Exit
        {
            /// <summary>Absolute grid direction (0=left, 1=up, 2=right, 3=down)</summary>
            public int AbsoluteDirection;

            /// <summary>True if this exit is blocked (door, blocker, wall)</summary>
            public bool IsBlocked;
        }

        /// <summary>
        /// A point of interest detected along a scan line.
        /// </summary>
        public struct PointOfInterest
        {
            /// <summary>Type key for localization (e.g. "poi_enemy", "poi_npc")</summary>
            public string TypeKey;

            /// <summary>Display name (enemy name, NPC name, etc.)</summary>
            public string Name;

            /// <summary>Absolute grid direction from player</summary>
            public int AbsoluteDirection;

            /// <summary>Distance in steps from player</summary>
            public int Distance;
        }

        #endregion

        #region Constants

        private const int MaxScanDepth = 3;

        #endregion

        #region Public Methods

        /// <summary>
        /// Scans the current room for contents (enemy, NPC, container, trap, etc.).
        /// Returns localized strings describing what's in the room.
        /// </summary>
        public List<string> ScanCurrentRoom(MapPiece currentPiece)
        {
            var contents = new List<string>();
            if (currentPiece == null) return contents;

            // Enemy in current room
            if (currentPiece.myMapEnemy != null && !currentPiece.myMapEnemy.isDead)
            {
                string name = currentPiece.myMapEnemy.enemyName;
                if (!string.IsNullOrEmpty(name))
                    contents.Add(Loc.Get("room_enemy", name));
            }

            // NPC in current room
            if (currentPiece.myMapNPC != null)
            {
                string name = currentPiece.myMapNPC.npcName;
                if (!string.IsNullOrEmpty(name))
                    contents.Add(Loc.Get("room_npc", name));
            }

            // Container in current room
            if (currentPiece.myCollectionContainer != null && !currentPiece.myCollectionContainer.hasOpened
                && currentPiece.myCollectionContainer.gameObject.activeInHierarchy)
            {
                if (IsInteractableContainer(currentPiece.myCollectionContainer))
                    contents.Add(Loc.Get("room_container"));
                else
                    contents.Add(Loc.Get("room_scenery", currentPiece.myCollectionContainer.containerBaseName ?? ""));
            }

            // Trap in current room
            if (currentPiece.myWorldTrap != null && currentPiece.myWorldTrap.isActive)
            {
                contents.Add(Loc.Get("room_trap"));
            }

            // Fishing spot
            if (currentPiece.canFish)
            {
                contents.Add(Loc.Get("room_fishing"));
            }

            // Card game
            if (currentPiece.canPlayCards)
            {
                contents.Add(Loc.Get("room_cards"));
            }

            // Shop
            if (currentPiece.canShop)
            {
                contents.Add(Loc.Get("room_shop"));
            }

            return contents;
        }

        /// <summary>
        /// Scans all 4 directions from the current room to find exits.
        /// An exit exists if there's an adjacent piece and the path isn't permanently blocked.
        /// </summary>
        public List<Exit> ScanExits(MapPiece currentPiece, GameManager gm)
        {
            var exits = new List<Exit>();
            if (currentPiece == null) return exits;

            for (int dir = 0; dir < 4; dir++)
            {
                MapPiece adjacent = DirectionHelper.GetAdjacentPiece(currentPiece, dir);
                if (adjacent == null) continue;

                bool blocked = IsDirectionBlocked(currentPiece, adjacent, dir, gm);
                exits.Add(new Exit
                {
                    AbsoluteDirection = dir,
                    IsBlocked = blocked
                });
            }

            return exits;
        }

        /// <summary>
        /// Scans all 4 directions up to MaxScanDepth steps for points of interest.
        /// Stops scanning a line at: null piece, blocker, alive enemy, alwaysBlockPlayer.
        /// </summary>
        public List<PointOfInterest> ScanNearbyPOIs(MapPiece currentPiece, GameManager gm)
        {
            var pois = new List<PointOfInterest>();
            if (currentPiece == null) return pois;

            for (int dir = 0; dir < 4; dir++)
            {
                ScanLine(currentPiece, dir, gm, pois);
            }

            return pois;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Scans a single direction from the current room, collecting POIs.
        /// </summary>
        private void ScanLine(MapPiece startPiece, int absoluteDir, GameManager gm, List<PointOfInterest> pois)
        {
            MapPiece current = startPiece;

            for (int step = 1; step <= MaxScanDepth; step++)
            {
                MapPiece next = DirectionHelper.GetAdjacentPiece(current, absoluteDir);
                if (next == null) break;

                // Check if movement to next is blocked
                if (IsDirectionBlocked(current, next, absoluteDir, gm))
                    break;

                // Check for alwaysBlockPlayer
                if (next.alwaysBlockPlayer) break;

                // Detect POIs in this piece
                DetectPOIs(next, absoluteDir, step, pois);

                // Stop if alive enemy (can't walk past)
                if (next.myMapEnemy != null && !next.myMapEnemy.isDead)
                    break;

                current = next;
            }
        }

        /// <summary>
        /// Detects all points of interest in a given map piece.
        /// </summary>
        private void DetectPOIs(MapPiece piece, int absoluteDir, int distance, List<PointOfInterest> pois)
        {
            // Enemy
            if (piece.myMapEnemy != null && !piece.myMapEnemy.isDead)
            {
                pois.Add(new PointOfInterest
                {
                    TypeKey = "poi_enemy",
                    Name = piece.myMapEnemy.enemyName ?? "",
                    AbsoluteDirection = absoluteDir,
                    Distance = distance
                });
            }

            // NPC
            if (piece.myMapNPC != null)
            {
                pois.Add(new PointOfInterest
                {
                    TypeKey = "poi_npc",
                    Name = piece.myMapNPC.npcName ?? "",
                    AbsoluteDirection = absoluteDir,
                    Distance = distance
                });
            }

            // Container (unopened and still active in scene) — separate interactive from scenery
            if (piece.myCollectionContainer != null && !piece.myCollectionContainer.hasOpened
                && piece.myCollectionContainer.gameObject.activeInHierarchy)
            {
                if (IsInteractableContainer(piece.myCollectionContainer))
                {
                    pois.Add(new PointOfInterest
                    {
                        TypeKey = "poi_container",
                        Name = piece.myCollectionContainer.containerBaseName ?? "",
                        AbsoluteDirection = absoluteDir,
                        Distance = distance
                    });
                }
                else
                {
                    pois.Add(new PointOfInterest
                    {
                        TypeKey = "poi_scenery",
                        Name = piece.myCollectionContainer.containerBaseName ?? "",
                        AbsoluteDirection = absoluteDir,
                        Distance = distance
                    });
                }
            }

            // Trap (active)
            if (piece.myWorldTrap != null && piece.myWorldTrap.isActive)
            {
                pois.Add(new PointOfInterest
                {
                    TypeKey = "poi_trap",
                    Name = "",
                    AbsoluteDirection = absoluteDir,
                    Distance = distance
                });
            }

            // Shop
            if (piece.canShop)
            {
                pois.Add(new PointOfInterest
                {
                    TypeKey = "poi_shop",
                    Name = "",
                    AbsoluteDirection = absoluteDir,
                    Distance = distance
                });
            }

            // Fishing
            if (piece.canFish)
            {
                pois.Add(new PointOfInterest
                {
                    TypeKey = "poi_fishing",
                    Name = "",
                    AbsoluteDirection = absoluteDir,
                    Distance = distance
                });
            }

            // Cards
            if (piece.canPlayCards)
            {
                pois.Add(new PointOfInterest
                {
                    TypeKey = "poi_cards",
                    Name = "",
                    AbsoluteDirection = absoluteDir,
                    Distance = distance
                });
            }
        }

        /// <summary>
        /// Returns true if a CollectionContainer is interactive (can be opened or toppled).
        /// Decorative objects like floormist, hanginglight, flycamera have both flags false.
        /// </summary>
        public static bool IsInteractableContainer(CollectionContainer container)
        {
            if (container == null) return false;
            return container.canBeOpened || container.canBeToppled;
        }

        /// <summary>
        /// Checks if movement from current piece to target piece in the given direction is blocked.
        /// Checks: walls on current piece, walls on target, active blockers, alwaysBlockPlayer, blockPlayerMovement.
        /// </summary>
        public bool IsDirectionBlocked(MapPiece current, MapPiece target, int absoluteDir, GameManager gm)
        {
            if (target == null) return true;

            // Target always blocks player
            if (target.alwaysBlockPlayer) return true;

            // Target node blocks player movement
            if (target.myNode != null && target.myNode.blockPlayerMovement) return true;

            // Height difference check (matches game's z-level check)
            if ((target.zPos > current.zPos + 4 || target.zPos < current.zPos - 4)
                && !target.stepUpOverride && !current.stepUpOverride)
                return true;

            // Walls on current piece blocking forward movement
            if (current.allWalls != null)
            {
                foreach (var wall in current.allWalls)
                {
                    if (wall.placeDirection == absoluteDir && wall.blockDirection)
                        return true;
                }
            }

            // Walls on target piece blocking entry from this direction
            int inverseDir = DirectionHelper.InverseDirection(absoluteDir);
            if (target.allWalls != null)
            {
                foreach (var wall in target.allWalls)
                {
                    if (wall.placeDirection == inverseDir && wall.blockDirection)
                        return true;
                }
            }

            // Active blockers
            if (gm != null && gm.allActiveBlockers != null)
            {
                foreach (var blocker in gm.allActiveBlockers)
                {
                    if (blocker == null || !blocker.isActive || blocker.myMapPiece == null || blocker.myMapPiece.myNode == null)
                        continue;

                    int blockerDir = blocker.myMapPiece.myNode.blockerDirection;

                    // Blocker on current piece: blocks if we're going in the inverse of the blocker direction
                    if (blocker.myMapPiece == current && absoluteDir == DirectionHelper.InverseDirection(blockerDir))
                        return true;

                    // Blocker on target piece: blocks if we're going in the blocker direction
                    if (blocker.myMapPiece == target && absoluteDir == blockerDir)
                        return true;
                }
            }

            // World word blocking (unsolved word puzzles)
            if (current.allWorldWords != null && current.allWorldWords.Count > 0
                && !string.IsNullOrEmpty(current.allWorldWords[0])
                && !current.hasSolvedWorldWord
                && current.myNode != null
                && absoluteDir == current.myNode.blockDirection)
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}

using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CryptmasterAccess
{
    /// <summary>
    /// Handles room entry and rotation announcements.
    /// Builds and speaks descriptions of the current room, exits, and nearby points of interest.
    /// </summary>
    public class RoomHandler
    {
        #region Fields

        private readonly RoomScanner _scanner = new RoomScanner();
        private GameManager _gameManager;
        private MapPiece _lastAnnouncedPiece;
        private float _lastAnnounceTime;

        /// <summary>Minimum seconds between announcements (handles slide rooms).</summary>
        private const float AnnounceCooldown = 0.3f;

        #endregion

        #region Public Methods

        /// <summary>
        /// Called every frame from Main.UpdateHandlers(). Re-caches GameManager if lost.
        /// </summary>
        public void Update()
        {
            if (_gameManager == null)
            {
                _gameManager = Object.FindObjectOfType<GameManager>();
            }
        }

        /// <summary>
        /// Called from Harmony postfix on GameManager.UpdateToNewPosition.
        /// Announces current room contents, exits, and nearby POIs.
        /// </summary>
        public void OnRoomEntered(GameManager gm)
        {
            if (gm == null) return;
            _gameManager = gm;

            // Throttle for slide rooms
            if (Time.time - _lastAnnounceTime < AnnounceCooldown) return;

            MapPiece currentPiece = gm.myCurrentMapPiece;
            if (currentPiece == null) return;

            _lastAnnouncedPiece = currentPiece;
            _lastAnnounceTime = Time.time;

            string announcement = BuildRoomAnnouncement(currentPiece, gm);
            DebugLogger.Log(LogCategory.Handler, "RoomHandler", $"Room entered: {announcement}");
            ScreenReader.Say(announcement);
        }

        /// <summary>
        /// Called from Harmony postfix on GameManager.RotateLeft/RotateRight.
        /// Announces facing direction and what's ahead.
        /// </summary>
        public void OnRotated(GameManager gm)
        {
            if (gm == null) return;
            _gameManager = gm;

            MapPiece currentPiece = gm.myCurrentMapPiece;
            if (currentPiece == null) return;

            _lastAnnouncedPiece = currentPiece;

            string announcement = BuildRotationAnnouncement(currentPiece, gm);
            DebugLogger.Log(LogCategory.Handler, "RoomHandler", $"Rotated: {announcement}");
            ScreenReader.Say(announcement);
        }

        /// <summary>
        /// Repeats the full room announcement for the current position. Triggered by F2.
        /// </summary>
        public void RepeatLastAnnouncement()
        {
            if (_gameManager == null) return;

            MapPiece currentPiece = _gameManager.myCurrentMapPiece;
            if (currentPiece == null) return;

            _lastAnnouncedPiece = currentPiece;

            string announcement = BuildRoomAnnouncement(currentPiece, _gameManager);
            DebugLogger.LogInput("F2", "Repeat room info");
            ScreenReader.Say(announcement);
        }

        /// <summary>
        /// Resets handler state. Called on scene change.
        /// </summary>
        public void Reset()
        {
            _gameManager = null;
            _lastAnnouncedPiece = null;
            _lastAnnounceTime = 0f;
            DebugLogger.LogState("RoomHandler reset");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Builds the full room announcement: contents, exits, nearby POIs.
        /// </summary>
        private string BuildRoomAnnouncement(MapPiece currentPiece, GameManager gm)
        {
            var sb = new StringBuilder();
            int lookDir = gm.lookDirection;

            // Visited/new tag
            if (Main.PathfindHandlerInstance != null)
            {
                if (Main.PathfindHandlerInstance.IsVisited(currentPiece))
                    sb.Append(Loc.Get("path_room_visited") + " ");
                else
                    sb.Append(Loc.Get("path_room_new") + " ");
            }

            // Current room contents
            List<string> contents = _scanner.ScanCurrentRoom(currentPiece);
            foreach (string content in contents)
            {
                sb.Append(content);
                sb.Append(". ");
            }

            // Exits
            List<RoomScanner.Exit> exits = _scanner.ScanExits(currentPiece, gm);
            if (exits.Count > 0)
            {
                sb.Append(Loc.Get("room_exits"));
                sb.Append(" ");
                bool first = true;
                foreach (var exit in exits)
                {
                    if (exit.IsBlocked) continue;
                    if (!first) sb.Append(", ");
                    int relDir = DirectionHelper.AbsoluteToRelative(exit.AbsoluteDirection, lookDir);
                    sb.Append(DirectionHelper.GetRelativeName(relDir));
                    first = false;
                }
                // If all exits are blocked, say so
                if (first)
                {
                    sb.Append(Loc.Get("room_all_blocked"));
                }
                sb.Append(". ");
            }
            else
            {
                sb.Append(Loc.Get("room_no_exits"));
                sb.Append(". ");
            }

            // Interaction hint if active word present
            string interactHint = BuildInteractionHint(gm);
            if (!string.IsNullOrEmpty(interactHint))
            {
                sb.Append(interactHint);
                sb.Append(". ");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Builds the rotation announcement: facing direction + what's immediately ahead.
        /// </summary>
        private string BuildRotationAnnouncement(MapPiece currentPiece, GameManager gm)
        {
            int lookDir = gm.lookDirection;
            string facingName = DirectionHelper.GetAbsoluteName(lookDir);

            // Check what's ahead
            MapPiece ahead = DirectionHelper.GetAdjacentPiece(currentPiece, lookDir);
            string aheadInfo;

            if (ahead == null)
            {
                aheadInfo = Loc.Get("facing_wall");
            }
            else
            {
                bool blocked = false;
                // Check walls and blockers in the forward direction
                if (ahead.alwaysBlockPlayer || (ahead.myNode != null && ahead.myNode.blockPlayerMovement))
                {
                    blocked = true;
                }
                else
                {
                    // Check walls on current piece
                    if (currentPiece.allWalls != null)
                    {
                        foreach (var wall in currentPiece.allWalls)
                        {
                            if (wall.placeDirection == lookDir && wall.blockDirection)
                            {
                                blocked = true;
                                break;
                            }
                        }
                    }

                    // Check walls on target piece
                    if (!blocked && ahead.allWalls != null)
                    {
                        int inverseDir = DirectionHelper.InverseDirection(lookDir);
                        foreach (var wall in ahead.allWalls)
                        {
                            if (wall.placeDirection == inverseDir && wall.blockDirection)
                            {
                                blocked = true;
                                break;
                            }
                        }
                    }

                    // Check active blockers
                    if (!blocked && gm.allActiveBlockers != null)
                    {
                        foreach (var blocker in gm.allActiveBlockers)
                        {
                            if (blocker == null || !blocker.isActive || blocker.myMapPiece == null || blocker.myMapPiece.myNode == null)
                                continue;

                            int blockerDir = blocker.myMapPiece.myNode.blockerDirection;

                            if (blocker.myMapPiece == currentPiece && lookDir == DirectionHelper.InverseDirection(blockerDir))
                            {
                                blocked = true;
                                break;
                            }
                            if (blocker.myMapPiece == ahead && lookDir == blockerDir)
                            {
                                blocked = true;
                                break;
                            }
                        }
                    }
                }

                if (blocked)
                {
                    aheadInfo = Loc.Get("facing_blocked");
                }
                else
                {
                    // Describe what's in the room ahead
                    if (ahead.myMapEnemy != null && !ahead.myMapEnemy.isDead)
                    {
                        aheadInfo = Loc.Get("facing_enemy", ahead.myMapEnemy.enemyName ?? "");
                    }
                    else
                    {
                        aheadInfo = Loc.Get("facing_open");
                    }
                }
            }

            string result = Loc.Get("facing_format", facingName, aheadInfo);

            // Interaction hint if active word present
            string interactHint = BuildInteractionHint(gm);
            if (!string.IsNullOrEmpty(interactHint))
            {
                result += " " + interactHint;
            }

            return result;
        }

        /// <summary>
        /// Builds an interaction hint if the current room has an active word to type.
        /// Checks myActiveWord and whether an NPC is present for a contextual message.
        /// </summary>
        private string BuildInteractionHint(GameManager gm)
        {
            if (gm == null) return null;

            MapPiece currentPiece = gm.myCurrentMapPiece;
            if (currentPiece == null) return null;

            string activeWord = currentPiece.myActiveWord;
            if (string.IsNullOrEmpty(activeWord) || currentPiece.hasActivatedActiveWord)
                return null;

            // Check if there's an NPC in current room for contextual hint
            if (currentPiece.myMapNPC != null)
            {
                string npcName = currentPiece.myMapNPC.npcName;
                if (!string.IsNullOrEmpty(npcName))
                    return Loc.Get("interact_npc", npcName, activeWord);
            }

            return Loc.Get("interact_hint", activeWord);
        }

        #endregion
    }
}

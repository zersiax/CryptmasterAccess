using System.Collections.Generic;
using System.Text;

namespace CryptmasterAccess
{
    /// <summary>
    /// Announces loot letter selection and level-up letter picking.
    /// Polls isLooting/isLevelingUp and currentHover each frame.
    /// </summary>
    public class LootHandler
    {
        #region Fields

        private GameManager _gameManager;

        private bool _wasLooting;
        private bool _wasLevelingUp;
        private int _lastHover = -1;
        private int _lastLootLetterCount;
        private string _lastAnnouncement = "";

        #endregion

        #region Public Methods

        /// <summary>
        /// Caches the GameManager reference.
        /// </summary>
        public void SetGameManager(GameManager gm)
        {
            _gameManager = gm;
        }

        /// <summary>
        /// Called every frame. Polls loot and level-up state for changes.
        /// </summary>
        public void Update()
        {
            if (_gameManager == null) return;

            PollLoot();
            PollLevelUp();
        }

        /// <summary>
        /// Clears all tracked state on scene change.
        /// </summary>
        public void Reset()
        {
            _gameManager = null;
            _wasLooting = false;
            _wasLevelingUp = false;
            _lastHover = -1;
            _lastLootLetterCount = 0;
            _lastAnnouncement = "";
        }

        /// <summary>
        /// Returns true if a loot or level-up screen is active.
        /// </summary>
        public bool IsActive()
        {
            if (_gameManager == null) return false;
            return _gameManager.isLooting || _gameManager.isLevelingUp;
        }

        /// <summary>
        /// Repeats the last loot/level-up announcement.
        /// </summary>
        public void RepeatLastAnnouncement()
        {
            if (!string.IsNullOrEmpty(_lastAnnouncement))
            {
                ScreenReader.Say(_lastAnnouncement);
            }
        }

        #endregion

        #region Polling

        /// <summary>
        /// Detects loot screen start, hover changes, and end.
        /// </summary>
        private void PollLoot()
        {
            bool isLooting = _gameManager.isLooting;

            if (isLooting && !_wasLooting)
            {
                // Loot screen just opened
                _lastHover = _gameManager.currentHover;
                _lastLootLetterCount = GetLootLetterCount();
                string announcement = BuildLootAnnouncement();
                if (!string.IsNullOrEmpty(announcement))
                {
                    _lastAnnouncement = announcement;
                    DebugLogger.Log(LogCategory.Handler, "Loot", $"Loot started: {announcement}");
                    ScreenReader.Say(announcement);
                }
            }
            else if (isLooting && _wasLooting)
            {
                // Check for hover change
                int currentHover = _gameManager.currentHover;
                if (currentHover != _lastHover)
                {
                    _lastHover = currentHover;
                    string announcement = BuildLootHoverAnnouncement();
                    if (!string.IsNullOrEmpty(announcement))
                    {
                        _lastAnnouncement = announcement;
                        DebugLogger.Log(LogCategory.Handler, "Loot", $"Hover changed: {announcement}");
                        ScreenReader.Say(announcement);
                    }
                }
            }
            else if (!isLooting && _wasLooting)
            {
                // Loot screen closed — assignment happened
                _lastHover = -1;
                _lastLootLetterCount = 0;
            }

            _wasLooting = isLooting;
        }

        /// <summary>
        /// Detects level-up screen start, hover changes, and end.
        /// </summary>
        private void PollLevelUp()
        {
            bool isLevelUp = _gameManager.isLevelingUp;

            if (isLevelUp && !_wasLevelingUp)
            {
                // Level-up screen just opened
                _lastHover = _gameManager.currentHover;
                _lastLootLetterCount = GetLootLetterCount();
                string announcement = BuildLevelUpAnnouncement();
                if (!string.IsNullOrEmpty(announcement))
                {
                    _lastAnnouncement = announcement;
                    DebugLogger.Log(LogCategory.Handler, "Loot", $"Level-up started: {announcement}");
                    ScreenReader.Say(announcement);
                }
            }
            else if (isLevelUp && _wasLevelingUp)
            {
                // Check for hover change
                int currentHover = _gameManager.currentHover;
                if (currentHover != _lastHover)
                {
                    _lastHover = currentHover;
                    string announcement = BuildLevelUpHoverAnnouncement();
                    if (!string.IsNullOrEmpty(announcement))
                    {
                        _lastAnnouncement = announcement;
                        DebugLogger.Log(LogCategory.Handler, "Loot", $"Level-up hover: {announcement}");
                        ScreenReader.Say(announcement);
                    }
                }
            }
            else if (!isLevelUp && _wasLevelingUp)
            {
                // Level-up ended
                _lastHover = -1;
                _lastLootLetterCount = 0;
            }

            _wasLevelingUp = isLevelUp;
        }

        #endregion

        #region Announcement Builders

        /// <summary>
        /// Builds the initial loot screen announcement with current finger assignments.
        /// </summary>
        private string BuildLootAnnouncement()
        {
            var sb = new StringBuilder();
            sb.Append(Loc.Get("loot_screen_start"));

            string assignments = BuildFingerAssignments();
            if (!string.IsNullOrEmpty(assignments))
            {
                sb.Append(" ");
                sb.Append(assignments);
            }

            int total = GetLootLetterCount();
            int activeFingers = CountActiveFingers();
            int position = _gameManager.currentHover + 1;
            int maxPositions = total - activeFingers + 1;
            if (maxPositions > 1)
            {
                sb.Append(" ");
                sb.Append(Loc.Get("loot_position", position, maxPositions));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds announcement when hover changes during loot.
        /// </summary>
        private string BuildLootHoverAnnouncement()
        {
            var sb = new StringBuilder();

            string assignments = BuildFingerAssignments();
            if (!string.IsNullOrEmpty(assignments))
            {
                sb.Append(assignments);
            }

            int total = GetLootLetterCount();
            int activeFingers = CountActiveFingers();
            int position = _gameManager.currentHover + 1;
            int maxPositions = total - activeFingers + 1;
            if (maxPositions > 1)
            {
                sb.Append(" ");
                sb.Append(Loc.Get("loot_position", position, maxPositions));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds the initial level-up announcement.
        /// </summary>
        private string BuildLevelUpAnnouncement()
        {
            string charName = GetLevelUpCharacterName();
            string currentName = charName ?? "Unknown";

            int total = GetLootLetterCount();
            string letter = GetLetterAt(_gameManager.currentHover);

            return Loc.Get("loot_levelup_start", currentName, letter) +
                   " " + Loc.Get("loot_position", _gameManager.currentHover + 1, total);
        }

        /// <summary>
        /// Builds announcement when hover changes during level-up.
        /// </summary>
        private string BuildLevelUpHoverAnnouncement()
        {
            string letter = GetLetterAt(_gameManager.currentHover);
            int total = GetLootLetterCount();

            return Loc.Get("loot_levelup_letter", letter) +
                   " " + Loc.Get("loot_position", _gameManager.currentHover + 1, total);
        }

        /// <summary>
        /// Builds "CharA gets X, CharB gets Y" string from active finger assignments.
        /// </summary>
        private string BuildFingerAssignments()
        {
            if (_gameManager.allFingers == null) return null;

            var parts = new List<string>();
            var aliveChars = _gameManager.allAliveCharacterUI;
            int charIndex = 0;

            foreach (var finger in _gameManager.allFingers)
            {
                if (!finger.isActive) continue;
                if (finger.myLetter == null) continue;

                string letter = finger.myLetter.storedChar;
                if (string.IsNullOrEmpty(letter)) continue;

                string charName = null;
                if (aliveChars != null && charIndex < aliveChars.Count)
                {
                    charName = aliveChars[charIndex].myName;
                }

                if (!string.IsNullOrEmpty(charName))
                {
                    parts.Add(Loc.Get("loot_assignment", charName, letter.ToUpper()));
                }
                else
                {
                    parts.Add(letter.ToUpper());
                }

                charIndex++;
            }

            return parts.Count > 0 ? string.Join(", ", parts.ToArray()) : null;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Gets the letter character at the given index in allLootLetters.
        /// </summary>
        private string GetLetterAt(int index)
        {
            var letters = _gameManager.myTextManager?.allLootLetters;
            if (letters == null || index < 0 || index >= letters.Count) return "?";

            var letter = letters[index];
            if (letter == null || string.IsNullOrEmpty(letter.storedChar)) return "?";

            return letter.storedChar.ToUpper();
        }

        /// <summary>
        /// Gets the total loot letter count.
        /// </summary>
        private int GetLootLetterCount()
        {
            var letters = _gameManager.myTextManager?.allLootLetters;
            return letters != null ? letters.Count : 0;
        }

        /// <summary>
        /// Counts the number of active fingers.
        /// </summary>
        private int CountActiveFingers()
        {
            if (_gameManager.allFingers == null) return 0;

            int count = 0;
            foreach (var finger in _gameManager.allFingers)
            {
                if (finger.isActive) count++;
            }
            return count;
        }

        /// <summary>
        /// Gets the name of the character leveling up.
        /// </summary>
        private string GetLevelUpCharacterName()
        {
            if (_gameManager.storedLevelUpChar == null) return null;
            return _gameManager.storedLevelUpChar.myName;
        }

        #endregion
    }
}

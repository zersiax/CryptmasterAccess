using System.Collections.Generic;
using System.Text;

namespace CryptmasterAccess
{
    /// <summary>
    /// Announces word puzzle state for chest item guessing and world word puzzles.
    /// Polls game state each frame to detect puzzle start, letter reveals, and puzzle end.
    /// </summary>
    public class WordPuzzleHandler
    {
        #region Fields

        private GameManager _gameManager;

        // Chest item guessing state
        private bool _wasChestGuessingActive;
        private int _lastChestRevealedCount;
        private string _lastAnnouncement = "";

        // World word puzzle state
        private bool _wasWorldWordActive;
        private int _lastWorldWordRevealedCount;

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
        /// Called every frame. Polls chest guessing and world word state for changes.
        /// </summary>
        public void Update()
        {
            if (_gameManager == null) return;

            PollChestGuessing();
            PollWorldWord();
        }

        /// <summary>
        /// Clears all tracked state on scene change.
        /// </summary>
        public void Reset()
        {
            _gameManager = null;
            _wasChestGuessingActive = false;
            _lastChestRevealedCount = 0;
            _lastAnnouncement = "";
            _wasWorldWordActive = false;
            _lastWorldWordRevealedCount = 0;
        }

        /// <summary>
        /// Ctrl+F2: repeats the current word puzzle letter state.
        /// </summary>
        public void RepeatWordPuzzle()
        {
            DebugLogger.LogInput("Ctrl+F2", "Repeat word puzzle");

            if (_gameManager == null)
            {
                ScreenReader.Say(Loc.Get("word_puzzle_no_active"));
                return;
            }

            // Check chest guessing first
            if (_gameManager.myCryptMaster != null && _gameManager.myCryptMaster.isChestItemGuessingActive)
            {
                string state = BuildLetterState(_gameManager.allItemLetters);
                string announcement = Loc.Get("word_puzzle_letters",
                    _gameManager.allItemLetters.Count, state);
                _lastAnnouncement = announcement;
                ScreenReader.Say(announcement);
                return;
            }

            // Check world word
            if (!string.IsNullOrEmpty(_gameManager.currentMoveWordName) &&
                _gameManager.allMovementLetters.Count > 0)
            {
                string state = BuildLetterState(_gameManager.allMovementLetters);
                string announcement = Loc.Get("word_puzzle_letters",
                    _gameManager.allMovementLetters.Count, state);
                _lastAnnouncement = announcement;
                ScreenReader.Say(announcement);
                return;
            }

            ScreenReader.Say(Loc.Get("word_puzzle_no_active"));
        }

        #endregion

        #region Polling

        /// <summary>
        /// Detects chest item guessing start, letter reveals, and end.
        /// </summary>
        private void PollChestGuessing()
        {
            if (_gameManager.myCryptMaster == null) return;

            bool isActive = _gameManager.myCryptMaster.isChestItemGuessingActive;

            if (isActive && !_wasChestGuessingActive)
            {
                // Puzzle just started
                int count = _gameManager.allItemLetters.Count;
                string state = BuildLetterState(_gameManager.allItemLetters);
                string announcement = Loc.Get("word_puzzle_start", count) + " " + state;
                _lastAnnouncement = announcement;
                _lastChestRevealedCount = CountRevealed(_gameManager.allItemLetters);

                DebugLogger.Log(LogCategory.Handler, "WordPuzzle",
                    $"Chest puzzle started: {count} letters");
                ScreenReader.Say(announcement);
            }
            else if (isActive && _wasChestGuessingActive)
            {
                // Check for newly revealed letters
                int currentRevealed = CountRevealed(_gameManager.allItemLetters);
                if (currentRevealed != _lastChestRevealedCount)
                {
                    string state = BuildLetterState(_gameManager.allItemLetters);
                    string announcement = Loc.Get("word_puzzle_letters",
                        _gameManager.allItemLetters.Count, state);
                    _lastAnnouncement = announcement;
                    _lastChestRevealedCount = currentRevealed;

                    DebugLogger.Log(LogCategory.Handler, "WordPuzzle",
                        $"Letters revealed: {currentRevealed}");
                    ScreenReader.Say(announcement);
                }
            }
            else if (!isActive && _wasChestGuessingActive)
            {
                // Puzzle just ended
                DebugLogger.Log(LogCategory.Handler, "WordPuzzle", "Chest puzzle solved");
                ScreenReader.Say(Loc.Get("word_puzzle_solved"));
                _lastChestRevealedCount = 0;
            }

            _wasChestGuessingActive = isActive;
        }

        /// <summary>
        /// Detects world word puzzle start, letter reveals, and end.
        /// </summary>
        private void PollWorldWord()
        {
            bool isActive = !string.IsNullOrEmpty(_gameManager.currentMoveWordName) &&
                            _gameManager.allMovementLetters.Count > 0;

            if (isActive && !_wasWorldWordActive)
            {
                // World word just appeared
                int count = _gameManager.allMovementLetters.Count;
                string state = BuildLetterState(_gameManager.allMovementLetters);
                string announcement = Loc.Get("word_world_start", count) + " " + state;
                _lastAnnouncement = announcement;
                _lastWorldWordRevealedCount = CountRevealed(_gameManager.allMovementLetters);

                DebugLogger.Log(LogCategory.Handler, "WordPuzzle",
                    $"World word started: {count} letters");
                ScreenReader.Say(announcement);
            }
            else if (isActive && _wasWorldWordActive)
            {
                // Check for newly revealed letters
                int currentRevealed = CountRevealed(_gameManager.allMovementLetters);
                if (currentRevealed != _lastWorldWordRevealedCount)
                {
                    string state = BuildLetterState(_gameManager.allMovementLetters);
                    string announcement = Loc.Get("word_puzzle_letters",
                        _gameManager.allMovementLetters.Count, state);
                    _lastAnnouncement = announcement;
                    _lastWorldWordRevealedCount = currentRevealed;

                    DebugLogger.Log(LogCategory.Handler, "WordPuzzle",
                        $"World word letters revealed: {currentRevealed}");
                    ScreenReader.Say(announcement);
                }
            }
            else if (!isActive && _wasWorldWordActive)
            {
                // World word solved or left area
                DebugLogger.Log(LogCategory.Handler, "WordPuzzle", "World word solved");
                ScreenReader.Say(Loc.Get("word_world_solved"));
                _lastWorldWordRevealedCount = 0;
            }

            _wasWorldWordActive = isActive;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Builds a comma-separated string of letter states (revealed letter or "blank").
        /// </summary>
        private string BuildLetterState(List<SpellLetter> letters)
        {
            if (letters == null || letters.Count == 0) return "";

            var parts = new StringBuilder();
            string blank = Loc.Get("word_puzzle_blank");

            for (int i = 0; i < letters.Count; i++)
            {
                if (i > 0) parts.Append(", ");

                var letter = letters[i];
                if (letter != null && letter.hasRevealed &&
                    !string.IsNullOrEmpty(letter.myBaseText))
                {
                    parts.Append(letter.myBaseText.ToUpper());
                }
                else
                {
                    parts.Append(blank);
                }
            }

            return parts.ToString();
        }

        /// <summary>
        /// Counts how many letters in the list are revealed.
        /// </summary>
        private int CountRevealed(List<SpellLetter> letters)
        {
            if (letters == null) return 0;

            int count = 0;
            for (int i = 0; i < letters.Count; i++)
            {
                if (letters[i] != null && letters[i].hasRevealed)
                    count++;
            }
            return count;
        }

        #endregion
    }
}

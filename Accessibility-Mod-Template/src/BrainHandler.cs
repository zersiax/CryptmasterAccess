using UnityEngine;

namespace CryptmasterAccess
{
    /// <summary>
    /// Handles brain screen (character abilities) accessibility: announces screen open/close,
    /// character switches, spell navigation, and spell details.
    /// Uses polling — no Harmony patches needed.
    /// </summary>
    public class BrainHandler
    {
        #region Fields

        private GameManager _gameManager;
        private BrainController _brainController;

        // Previous frame state for polling
        private bool _wasOpen;
        private int _lastCharacter;
        private int _lastX;
        private int _lastY;
        private string _lastAnnouncement;

        // Character names by index (0-3)
        private static readonly string[] CharacterNames = { "Joro", "Syn", "Maz", "Nix" };

        #endregion

        #region Public Methods

        /// <summary>
        /// Caches GameManager and BrainController references.
        /// Called from Main.UpdateHandlers() when GM is available.
        /// </summary>
        public void SetGameManager(GameManager gm)
        {
            if (gm == null) return;
            _gameManager = gm;

            if (_brainController == null && gm.myBrainController != null)
            {
                _brainController = gm.myBrainController;
            }
        }

        /// <summary>
        /// Polls brain state each frame. Called from Main.UpdateHandlers().
        /// </summary>
        public void Update()
        {
            if (_brainController == null) return;

            PollBrainState();
        }

        /// <summary>
        /// Resets all state. Called on scene change.
        /// </summary>
        public void Reset()
        {
            _gameManager = null;
            _brainController = null;
            _wasOpen = false;
            _lastCharacter = -1;
            _lastX = -1;
            _lastY = -1;
            _lastAnnouncement = null;

            DebugLogger.LogState("BrainHandler reset");
        }

        /// <summary>
        /// Repeats the last brain announcement. Triggered by F8 when brain is open.
        /// </summary>
        public void RepeatCurrentInfo()
        {
            if (!string.IsNullOrEmpty(_lastAnnouncement))
            {
                ScreenReader.Say(_lastAnnouncement);
            }
        }

        /// <summary>
        /// Returns true if the brain screen is currently open.
        /// </summary>
        public bool IsOpen()
        {
            return _brainController != null && _brainController.isActive;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Polls brain state and announces transitions.
        /// </summary>
        private void PollBrainState()
        {
            bool isOpen = _brainController.isActive;

            // Brain opened
            if (isOpen && !_wasOpen)
            {
                OnBrainOpened();
            }
            // Brain closed
            else if (!isOpen && _wasOpen)
            {
                OnBrainClosed();
            }

            // Only poll character/position changes while open
            if (isOpen)
            {
                PollCharacterChange();
                PollPositionChange();
            }

            _wasOpen = isOpen;
        }

        /// <summary>
        /// Handles brain open transition.
        /// </summary>
        private void OnBrainOpened()
        {
            string charName = GetCharacterName(_brainController.currentCharacter);
            string spellInfo = GetCurrentSpellInfo();

            string msg = Loc.Get("brain_opened", charName, spellInfo);
            DebugLogger.Log(LogCategory.Handler, "BrainHandler", $"Brain opened: {charName}");
            Announce(msg);

            // Sync tracking state
            _lastCharacter = _brainController.currentCharacter;
            _lastX = _brainController.xPos;
            _lastY = _brainController.yPos;
        }

        /// <summary>
        /// Handles brain close transition.
        /// </summary>
        private void OnBrainClosed()
        {
            string msg = Loc.Get("brain_closed");
            DebugLogger.Log(LogCategory.Handler, "BrainHandler", "Brain closed");
            Announce(msg);

            _lastCharacter = -1;
            _lastX = -1;
            _lastY = -1;
        }

        /// <summary>
        /// Polls for character changes (keys 1-4 switch characters).
        /// </summary>
        private void PollCharacterChange()
        {
            int currentChar = _brainController.currentCharacter;
            if (currentChar == _lastCharacter) return;

            _lastCharacter = currentChar;

            string charName = GetCharacterName(currentChar);
            string spellInfo = GetCurrentSpellInfo();

            string msg = Loc.Get("brain_char_changed", charName, spellInfo);
            DebugLogger.Log(LogCategory.Handler, "BrainHandler", $"Character changed: {charName}");
            Announce(msg);

            // Sync position for the new character
            _lastX = _brainController.xPos;
            _lastY = _brainController.yPos;
        }

        /// <summary>
        /// Polls for grid position changes (arrow keys move in spell grid).
        /// </summary>
        private void PollPositionChange()
        {
            int currentX = _brainController.xPos;
            int currentY = _brainController.yPos;

            if (currentX == _lastX && currentY == _lastY) return;

            _lastX = currentX;
            _lastY = currentY;

            AnnounceCurrentSpell();
        }

        /// <summary>
        /// Announces the currently selected spell with type and description.
        /// </summary>
        private void AnnounceCurrentSpell()
        {
            string spellInfo = GetCurrentSpellInfo();
            Announce(spellInfo);
        }

        /// <summary>
        /// Gets info string for the currently selected spell box.
        /// </summary>
        private string GetCurrentSpellInfo()
        {
            var spellBox = GetCurrentSpellBox();

            if (spellBox == null || !spellBox.isActive)
            {
                return Loc.Get("brain_spell_unknown");
            }

            string spellName = GetSpellName(spellBox);
            string spellType = GetSpellType(spellBox);
            string description = spellBox.description;

            // Unknown/unrevealed spell (description is "???" or empty)
            if (string.IsNullOrEmpty(spellName) || spellName == "???")
            {
                return Loc.Get("brain_spell_unknown");
            }

            // Spell with description
            if (!string.IsNullOrEmpty(description) && description != "???")
            {
                return Loc.Get("brain_spell_desc", spellName, spellType, description);
            }

            // Spell without description
            return Loc.Get("brain_spell", spellName, spellType);
        }

        /// <summary>
        /// Finds the SpellBox at the current grid position.
        /// </summary>
        private BrainController.SpellBox GetCurrentSpellBox()
        {
            if (_brainController.allSpellBoxes == null) return null;

            int targetX = _brainController.xPos;
            int targetY = _brainController.yPos;

            foreach (var box in _brainController.allSpellBoxes)
            {
                if (box != null && box.xPos == targetX && box.yPos == targetY)
                    return box;
            }

            return null;
        }

        /// <summary>
        /// Gets the display name for a spell box.
        /// </summary>
        private string GetSpellName(BrainController.SpellBox spellBox)
        {
            if (spellBox == null) return "unknown";

            if (spellBox.myText != null && !string.IsNullOrEmpty(spellBox.myText.text))
                return spellBox.myText.text;

            return "unknown";
        }

        /// <summary>
        /// Gets the type string (skill or memory) for a spell box.
        /// </summary>
        private string GetSpellType(BrainController.SpellBox spellBox)
        {
            if (spellBox == null || spellBox.myBrainNode == null)
                return "unknown";

            return spellBox.myBrainNode.isSkill
                ? Loc.Get("brain_skill")
                : Loc.Get("brain_memory");
        }

        /// <summary>
        /// Gets the character name for a character index (0-3).
        /// </summary>
        private string GetCharacterName(int index)
        {
            if (index >= 0 && index < CharacterNames.Length)
                return CharacterNames[index];

            return "Unknown";
        }

        /// <summary>
        /// Announces text and caches as last announcement.
        /// </summary>
        private void Announce(string text)
        {
            _lastAnnouncement = text;
            ScreenReader.Say(text);
        }

        #endregion
    }
}

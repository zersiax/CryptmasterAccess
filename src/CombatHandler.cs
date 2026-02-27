using System.Text;
using UnityEngine;

namespace CryptmasterAccess
{
    /// <summary>
    /// Handles combat accessibility: announces combat start/end, spell mode,
    /// enemy/party HP, timer warnings, and damage events.
    /// Uses polling for state transitions and Harmony patch callbacks for precise events.
    /// </summary>
    public class CombatHandler
    {
        #region Fields

        private GameManager _gameManager;
        private CombatManager _combatManager;

        // Previous frame state for polling
        private bool _wasInCombat;
        private bool _wasInSpellMode;
        private bool _wasGameOver;
        private bool[] _wasCharDead = new bool[4];
        private int _lastTimerThreshold;
        private string _lastEnemyName;
        private string _lastAnnouncement;

        #endregion

        #region Public Methods

        /// <summary>
        /// Caches GameManager and CombatManager references.
        /// Called each frame from Main.UpdateHandlers() when GM is available.
        /// </summary>
        public void SetGameManager(GameManager gm)
        {
            if (gm == null) return;
            _gameManager = gm;

            // CombatManager is a component on the same GameObject or child
            if (_combatManager == null)
            {
                _combatManager = Object.FindObjectOfType<CombatManager>();
            }
        }

        /// <summary>
        /// Polls combat state transitions each frame. Called from Main.UpdateHandlers().
        /// </summary>
        public void Update()
        {
            if (_gameManager == null) return;

            PollCombatState();
        }

        /// <summary>
        /// Resets all state. Called on scene change.
        /// </summary>
        public void Reset()
        {
            _gameManager = null;
            _combatManager = null;
            _wasInCombat = false;
            _wasInSpellMode = false;
            _wasGameOver = false;
            _lastTimerThreshold = 0;
            _lastEnemyName = null;
            _lastAnnouncement = null;
            for (int i = 0; i < 4; i++)
                _wasCharDead[i] = false;

            DebugLogger.LogState("CombatHandler reset");
        }

        /// <summary>
        /// Announces party HP status. Triggered by F5.
        /// </summary>
        public void AnnouncePartyStatus()
        {
            if (!IsInCombat())
            {
                Announce(Loc.Get("combat_not_in_combat"));
                return;
            }

            string status = BuildPartyStatus();
            Announce(Loc.Get("combat_party_status", status));
        }

        /// <summary>
        /// Announces current turn timer percentage. Triggered by F6.
        /// </summary>
        public void AnnounceTurnTimer()
        {
            if (!IsInCombat())
            {
                Announce(Loc.Get("combat_not_in_combat"));
                return;
            }

            if (_combatManager == null) return;

            int percent = Mathf.RoundToInt(_combatManager.battleCounter);
            Announce(Loc.Get("combat_timer", percent));
        }

        /// <summary>
        /// Announces enemy info: name, HP, attack, target. Triggered by F7.
        /// </summary>
        public void AnnounceEnemyInfo()
        {
            if (!IsInCombat())
            {
                Announce(Loc.Get("combat_not_in_combat"));
                return;
            }

            Enemy enemy = _gameManager.currentlyLoadedEnemy;
            if (enemy == null) return;

            string name = enemy.enemyName ?? "Unknown";
            int hp = GetEnemyHP(enemy);
            int attack = GetEnemyAttack(enemy);
            string targeting = GetTargetingInfo();

            Announce(Loc.Get("combat_enemy_info", name, hp, attack, targeting));
        }

        /// <summary>
        /// Repeats the last combat announcement. Triggered by F4.
        /// </summary>
        public void RepeatLastCombatInfo()
        {
            if (!IsInCombat())
            {
                Announce(Loc.Get("combat_not_in_combat"));
                return;
            }

            if (!string.IsNullOrEmpty(_lastAnnouncement))
            {
                ScreenReader.Say(_lastAnnouncement);
            }
        }

        #endregion

        #region Patch Callbacks

        /// <summary>
        /// Called from Harmony postfix on CharacterHUD.RecieveDamage.
        /// Announces damage taken by a party member.
        /// Uses SayQueued for AoE rapid hits.
        /// </summary>
        public void OnCharacterDamaged(CharacterHUD hud, int damage)
        {
            if (hud == null) return;

            string name = GetCharacterName(hud);
            int currentHP = hud.nameHP;
            int maxHP = hud.totalNameHP;

            if (currentHP <= 0)
            {
                string deadMsg = Loc.Get("combat_char_dead", name);
                DebugLogger.Log(LogCategory.Handler, "CombatHandler", $"Character died: {name}");
                _lastAnnouncement = deadMsg;
                ScreenReader.SayQueued(deadMsg);
            }
            else
            {
                string hitMsg = Loc.Get("combat_char_hit", name, currentHP, maxHP);
                DebugLogger.Log(LogCategory.Handler, "CombatHandler", $"Character damaged: {hitMsg}");
                _lastAnnouncement = hitMsg;
                ScreenReader.SayQueued(hitMsg);
            }
        }

        /// <summary>
        /// Called from Harmony postfix on CombatManager.DamageEnemyWord.
        /// Announces damage dealt to the enemy.
        /// </summary>
        public void OnEnemyDamaged(int currentHealth, int damage)
        {
            string msg = Loc.Get("combat_enemy_damaged", damage, currentHealth);
            DebugLogger.Log(LogCategory.Handler, "CombatHandler", $"Enemy damaged: {msg}");
            _lastAnnouncement = msg;
            ScreenReader.Say(msg);
        }

        /// <summary>
        /// Called from Harmony postfix on SpellManager.CastSpell.
        /// Announces spell cast by a character.
        /// </summary>
        public void OnSpellCast(string spellName, CharacterHUD caster)
        {
            string charName = caster != null ? GetCharacterName(caster) : "Unknown";
            string msg = Loc.Get("combat_spell_cast", charName, spellName ?? "unknown");
            DebugLogger.Log(LogCategory.Handler, "CombatHandler", $"Spell cast: {msg}");
            _lastAnnouncement = msg;
            ScreenReader.Say(msg);
        }

        /// <summary>
        /// Called from Harmony postfix on CombatManager.KillEnemy.
        /// Announces enemy defeat.
        /// </summary>
        public void OnEnemyKilled(Enemy enemy)
        {
            string name = enemy != null ? (enemy.enemyName ?? _lastEnemyName ?? "Enemy") : (_lastEnemyName ?? "Enemy");
            string msg = Loc.Get("combat_enemy_defeated", name);
            DebugLogger.Log(LogCategory.Handler, "CombatHandler", $"Enemy killed: {name}");
            _lastAnnouncement = msg;
            ScreenReader.Say(msg);
        }

        /// <summary>
        /// Called from Harmony postfix on CombatManager.GameOver.
        /// Announces party wipe.
        /// </summary>
        public void OnGameOver()
        {
            string msg = Loc.Get("combat_game_over");
            DebugLogger.Log(LogCategory.Handler, "CombatHandler", "Game over");
            _lastAnnouncement = msg;
            ScreenReader.Say(msg);
            _wasGameOver = true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Polls combat state each frame and announces transitions.
        /// </summary>
        private void PollCombatState()
        {
            bool inCombat = IsInCombat();

            // Combat start
            if (inCombat && !_wasInCombat)
            {
                OnCombatStart();
            }
            // Combat end
            else if (!inCombat && _wasInCombat)
            {
                OnCombatEnd();
            }

            // Only poll remaining state while in combat
            if (inCombat)
            {
                PollSpellMode();
                PollTimerThresholds();
                PollCharacterDeaths();
            }

            _wasInCombat = inCombat;
        }

        /// <summary>
        /// Handles combat start transition.
        /// </summary>
        private void OnCombatStart()
        {
            Enemy enemy = _gameManager.currentlyLoadedEnemy;
            if (enemy == null) return;

            string name = enemy.enemyName ?? "Unknown";
            _lastEnemyName = name;
            int attack = GetEnemyAttack(enemy);
            string partyStatus = BuildPartyStatus();

            string msg = Loc.Get("combat_start", name, attack, partyStatus);
            DebugLogger.Log(LogCategory.Handler, "CombatHandler", $"Combat start: {name}");
            Announce(msg);

            // Reset timer threshold tracking
            _lastTimerThreshold = 0;
            _wasInSpellMode = false;
            _wasGameOver = false;

            // Initialize character death tracking
            InitCharacterDeathTracking();
        }

        /// <summary>
        /// Handles combat end transition.
        /// </summary>
        private void OnCombatEnd()
        {
            // Don't announce "combat over" if game over already fired
            if (!_wasGameOver)
            {
                string msg = Loc.Get("combat_end");
                DebugLogger.Log(LogCategory.Handler, "CombatHandler", "Combat ended");
                Announce(msg);
            }

            _lastTimerThreshold = 0;
            _wasInSpellMode = false;
            _wasGameOver = false;
        }

        /// <summary>
        /// Polls spell mode transitions.
        /// </summary>
        private void PollSpellMode()
        {
            bool inSpellMode = _gameManager.isInSpellMode;

            if (inSpellMode && !_wasInSpellMode)
            {
                string msg = Loc.Get("combat_spell_mode_on");
                DebugLogger.Log(LogCategory.Handler, "CombatHandler", "Spell mode on");
                Announce(msg);
            }
            else if (!inSpellMode && _wasInSpellMode)
            {
                string msg = Loc.Get("combat_spell_mode_off");
                DebugLogger.Log(LogCategory.Handler, "CombatHandler", "Spell mode off");
                Announce(msg);
            }

            _wasInSpellMode = inSpellMode;
        }

        /// <summary>
        /// Polls battle counter for timer threshold warnings (50%, 75%).
        /// </summary>
        private void PollTimerThresholds()
        {
            if (_combatManager == null) return;

            float counter = _combatManager.battleCounter;
            int threshold = 0;

            if (counter >= 75f) threshold = 75;
            else if (counter >= 50f) threshold = 50;

            if (threshold > _lastTimerThreshold)
            {
                string msg = Loc.Get("combat_timer_warning", threshold);
                DebugLogger.Log(LogCategory.Handler, "CombatHandler", $"Timer warning: {threshold}%");
                _lastAnnouncement = msg;
                ScreenReader.Say(msg);
                _lastTimerThreshold = threshold;
            }
        }

        /// <summary>
        /// Polls character death state as a fallback (main path is RecieveDamage patch).
        /// </summary>
        private void PollCharacterDeaths()
        {
            if (_gameManager.allCharacterUI == null) return;

            for (int i = 0; i < _gameManager.allCharacterUI.Count && i < 4; i++)
            {
                CharacterHUD hud = _gameManager.allCharacterUI[i];
                if (hud == null) continue;

                bool isDead = hud.nameHP <= 0;
                if (isDead && !_wasCharDead[i])
                {
                    // Only announce if not already caught by the patch
                    string name = GetCharacterName(hud);
                    DebugLogger.Log(LogCategory.Handler, "CombatHandler", $"Poll detected death: {name}");
                    // Don't double-announce — the patch callback handles the primary path
                }
                _wasCharDead[i] = isDead;
            }
        }

        /// <summary>
        /// Initializes character death tracking at combat start.
        /// </summary>
        private void InitCharacterDeathTracking()
        {
            if (_gameManager.allCharacterUI == null) return;

            for (int i = 0; i < _gameManager.allCharacterUI.Count && i < 4; i++)
            {
                CharacterHUD hud = _gameManager.allCharacterUI[i];
                _wasCharDead[i] = hud != null && hud.nameHP <= 0;
            }
        }

        /// <summary>
        /// Returns true if currently in combat.
        /// </summary>
        private bool IsInCombat()
        {
            return _gameManager != null && _gameManager.currentlyLoadedEnemy != null;
        }

        /// <summary>
        /// Builds a party status string: "Joro 3 of 5, Syn dead, ..."
        /// </summary>
        private string BuildPartyStatus()
        {
            if (_gameManager.allCharacterUI == null) return "";

            var sb = new StringBuilder();
            for (int i = 0; i < _gameManager.allCharacterUI.Count && i < 4; i++)
            {
                CharacterHUD hud = _gameManager.allCharacterUI[i];
                if (hud == null) continue;

                if (sb.Length > 0) sb.Append(", ");

                string name = GetCharacterName(hud);

                if (hud.nameHP <= 0)
                {
                    sb.Append(Loc.Get("combat_char_hp_dead", name));
                }
                else
                {
                    sb.Append(Loc.Get("combat_char_hp", name, hud.nameHP, hud.totalNameHP));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets a character name from their HUD.
        /// </summary>
        private string GetCharacterName(CharacterHUD hud)
        {
            if (hud == null) return "Unknown";

            if (!string.IsNullOrEmpty(hud.myName))
                return hud.myName;

            // Fallback: use the GameObject name
            return hud.gameObject != null ? hud.gameObject.name : "Unknown";
        }

        /// <summary>
        /// Gets enemy HP from active letters count on GameManager.
        /// </summary>
        private int GetEnemyHP(Enemy enemy)
        {
            if (_gameManager == null) return 0;

            int hp = 0;
            if (_gameManager.allEnemyLettersActive != null)
            {
                foreach (var letter in _gameManager.allEnemyLettersActive)
                {
                    if (letter != null && letter.isActive) hp++;
                }
            }
            return hp;
        }

        /// <summary>
        /// Gets enemy attack power.
        /// </summary>
        private int GetEnemyAttack(Enemy enemy)
        {
            if (enemy == null) return 0;
            return enemy.attackDamage;
        }

        /// <summary>
        /// Gets targeting info string.
        /// </summary>
        private string GetTargetingInfo()
        {
            if (_combatManager == null) return "unknown";

            Enemy enemy = _gameManager.currentlyLoadedEnemy;
            if (enemy != null && enemy.doesAttackAll)
            {
                return Loc.Get("combat_targeting_all");
            }

            CharacterHUD target = _combatManager.enemyTargetPlayer;
            if (target != null)
            {
                string name = GetCharacterName(target);
                return Loc.Get("combat_targeting", name);
            }

            return "unknown";
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

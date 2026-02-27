using HarmonyLib;

namespace CryptmasterAccess.Patches
{
    /// <summary>
    /// Harmony postfix for CharacterHUD.RecieveDamage.
    /// Fires when a party member takes damage. HP is already updated in postfix.
    /// </summary>
    [HarmonyPatch(typeof(CharacterHUD), "RecieveDamage")]
    public static class CharacterRecieveDamagePatch
    {
        /// <summary>
        /// Called after RecieveDamage completes. Announces damage to the character.
        /// </summary>
        public static void Postfix(CharacterHUD __instance, int _damage)
        {
            try
            {
                if (Main.CombatHandlerInstance != null)
                {
                    Main.CombatHandlerInstance.OnCharacterDamaged(__instance, _damage);
                }
            }
            catch (System.Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"CombatPatch CharacterDamage error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Harmony postfix for CombatManager.DamageEnemyWord.
    /// Fires when the enemy takes damage from a spell.
    /// </summary>
    [HarmonyPatch(typeof(CombatManager), "DamageEnemyWord")]
    public static class DamageEnemyWordPatch
    {
        /// <summary>
        /// Called after DamageEnemyWord completes. Announces damage to the enemy.
        /// </summary>
        public static void Postfix(int _currentHealth, int _damage, string _element)
        {
            try
            {
                if (Main.CombatHandlerInstance != null)
                {
                    Main.CombatHandlerInstance.OnEnemyDamaged(_currentHealth, _damage);
                }
            }
            catch (System.Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"CombatPatch EnemyDamage error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Harmony postfix for SpellManager.CastSpell.
    /// Fires when a spell is cast.
    /// </summary>
    [HarmonyPatch(typeof(SpellManager), "CastSpell")]
    public static class CastSpellPatch
    {
        /// <summary>
        /// Called after CastSpell completes. Announces the spell cast.
        /// </summary>
        public static void Postfix(string _spellName, CharacterHUD _optionalChar, Enemy _currEnemy, bool _isDiscovered)
        {
            try
            {
                if (Main.CombatHandlerInstance != null)
                {
                    Main.CombatHandlerInstance.OnSpellCast(_spellName, _optionalChar);
                }
            }
            catch (System.Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"CombatPatch CastSpell error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Harmony postfix for CombatManager.KillEnemy.
    /// Fires when an enemy is killed. Only announces if _doesKill is true.
    /// </summary>
    [HarmonyPatch(typeof(CombatManager), "KillEnemy")]
    public static class KillEnemyPatch
    {
        /// <summary>
        /// Called after KillEnemy completes. Announces enemy defeat.
        /// </summary>
        public static void Postfix(Enemy _enem, bool _doesKill)
        {
            try
            {
                if (_doesKill && Main.CombatHandlerInstance != null)
                {
                    Main.CombatHandlerInstance.OnEnemyKilled(_enem);
                }
            }
            catch (System.Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"CombatPatch KillEnemy error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Harmony postfix for CombatManager.GameOver.
    /// Fires when all party members die.
    /// </summary>
    [HarmonyPatch(typeof(CombatManager), "GameOver")]
    public static class GameOverPatch
    {
        /// <summary>
        /// Called after GameOver completes. Announces party wipe.
        /// </summary>
        public static void Postfix()
        {
            try
            {
                if (Main.CombatHandlerInstance != null)
                {
                    Main.CombatHandlerInstance.OnGameOver();
                }
            }
            catch (System.Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"CombatPatch GameOver error: {ex.Message}");
            }
        }
    }
}

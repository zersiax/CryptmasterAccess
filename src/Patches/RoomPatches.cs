using HarmonyLib;

namespace CryptmasterAccess.Patches
{
    /// <summary>
    /// Harmony postfix for GameManager.UpdateToNewPosition.
    /// Fires when the player enters a new room.
    /// </summary>
    [HarmonyPatch(typeof(GameManager), "UpdateToNewPosition")]
    public static class UpdateToNewPositionPatch
    {
        /// <summary>
        /// Called after UpdateToNewPosition completes. Triggers room announcement.
        /// </summary>
        public static void Postfix(GameManager __instance)
        {
            if (Main.RoomHandlerInstance != null)
            {
                Main.RoomHandlerInstance.OnRoomEntered(__instance);
            }
        }
    }

    /// <summary>
    /// Harmony postfix for GameManager.RotateLeft.
    /// Fires when the player rotates left.
    /// </summary>
    [HarmonyPatch(typeof(GameManager), "RotateLeft")]
    public static class RotateLeftPatch
    {
        /// <summary>
        /// Called after RotateLeft completes. Triggers rotation announcement.
        /// </summary>
        public static void Postfix(GameManager __instance)
        {
            if (Main.RoomHandlerInstance != null)
            {
                Main.RoomHandlerInstance.OnRotated(__instance);
            }
        }
    }

    /// <summary>
    /// Harmony postfix for GameManager.RotateRight.
    /// Fires when the player rotates right.
    /// </summary>
    [HarmonyPatch(typeof(GameManager), "RotateRight")]
    public static class RotateRightPatch
    {
        /// <summary>
        /// Called after RotateRight completes. Triggers rotation announcement.
        /// </summary>
        public static void Postfix(GameManager __instance)
        {
            if (Main.RoomHandlerInstance != null)
            {
                Main.RoomHandlerInstance.OnRotated(__instance);
            }
        }
    }
}

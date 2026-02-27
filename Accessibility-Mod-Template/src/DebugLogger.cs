using MelonLoader;

namespace CryptmasterAccess
{
    /// <summary>
    /// Centralized debug logging with categories.
    /// Only active when Main.DebugMode is true (F12). Zero overhead otherwise.
    /// </summary>
    public static class DebugLogger
    {
        /// <summary>
        /// Log a debug message with category.
        /// </summary>
        public static void Log(LogCategory category, string message)
        {
            if (!Main.DebugMode) return;

            string prefix = GetPrefix(category);
            MelonLogger.Msg($"{prefix} {message}");
        }

        /// <summary>
        /// Log a debug message with category and source.
        /// </summary>
        public static void Log(LogCategory category, string source, string message)
        {
            if (!Main.DebugMode) return;

            string prefix = GetPrefix(category);
            MelonLogger.Msg($"{prefix} [{source}] {message}");
        }

        /// <summary>
        /// Log screenreader output. Called automatically by ScreenReader.Say().
        /// </summary>
        public static void LogScreenReader(string text)
        {
            if (!Main.DebugMode) return;

            MelonLogger.Msg($"[SR] {text}");
        }

        /// <summary>
        /// Log a key press event.
        /// </summary>
        public static void LogInput(string keyName, string action = null)
        {
            if (!Main.DebugMode) return;

            string msg = action != null
                ? $"{keyName} -> {action}"
                : keyName;
            MelonLogger.Msg($"[INPUT] {msg}");
        }

        /// <summary>
        /// Log a state change (screen opened/closed, mode changed).
        /// </summary>
        public static void LogState(string description)
        {
            if (!Main.DebugMode) return;

            MelonLogger.Msg($"[STATE] {description}");
        }

        /// <summary>
        /// Log a game value that was read (for debugging data extraction).
        /// </summary>
        public static void LogGameValue(string name, object value)
        {
            if (!Main.DebugMode) return;

            MelonLogger.Msg($"[GAME] {name} = {value}");
        }

        private static string GetPrefix(LogCategory category)
        {
            switch (category)
            {
                case LogCategory.ScreenReader: return "[SR]";
                case LogCategory.Input: return "[INPUT]";
                case LogCategory.State: return "[STATE]";
                case LogCategory.Handler: return "[HANDLER]";
                case LogCategory.Game: return "[GAME]";
                default: return "[DEBUG]";
            }
        }
    }

    /// <summary>
    /// Categories for debug logging.
    /// </summary>
    public enum LogCategory
    {
        /// <summary>What the screenreader announces</summary>
        ScreenReader,

        /// <summary>Key presses and input events</summary>
        Input,

        /// <summary>Screen/menu state changes</summary>
        State,

        /// <summary>Handler decisions and processing</summary>
        Handler,

        /// <summary>Values read from the game</summary>
        Game
    }
}

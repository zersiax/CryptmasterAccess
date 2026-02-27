using System;
using System.Runtime.InteropServices;
using MelonLoader;

namespace CryptmasterAccess
{
    /// <summary>
    /// Wrapper for Tolk screenreader library.
    /// Provides simple methods to announce text via NVDA, JAWS, or other screenreaders.
    /// </summary>
    public static class ScreenReader
    {
        #region Native Imports

        [DllImport("Tolk.dll")]
        private static extern void Tolk_Load();

        [DllImport("Tolk.dll")]
        private static extern void Tolk_Unload();

        [DllImport("Tolk.dll")]
        private static extern bool Tolk_IsLoaded();

        [DllImport("Tolk.dll")]
        private static extern bool Tolk_HasSpeech();

        [DllImport("Tolk.dll", CharSet = CharSet.Unicode)]
        private static extern bool Tolk_Output(string text, bool interrupt);

        [DllImport("Tolk.dll")]
        private static extern bool Tolk_Silence();

        [DllImport("Tolk.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr Tolk_DetectScreenReader();

        #endregion

        #region Fields

        private static bool _available = false;
        private static bool _initialized = false;

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes Tolk. Call once at mod startup.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                Tolk_Load();
                _available = Tolk_IsLoaded() && Tolk_HasSpeech();

                if (_available)
                {
                    IntPtr srNamePtr = Tolk_DetectScreenReader();
                    string srName = srNamePtr != IntPtr.Zero
                        ? Marshal.PtrToStringUni(srNamePtr)
                        : "Unknown";
                    MelonLogger.Msg($"Screenreader detected: {srName}");
                }
                else
                {
                    MelonLogger.Warning("No screenreader detected or Tolk not available");
                }
            }
            catch (DllNotFoundException)
            {
                MelonLogger.Error("Tolk.dll not found! Place Tolk.dll in the game folder.");
                _available = false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to initialize Tolk: {ex.Message}");
                _available = false;
            }

            _initialized = true;
        }

        /// <summary>
        /// Announces text via the screenreader.
        /// </summary>
        /// <param name="text">Text to speak</param>
        /// <param name="interrupt">If true, stops current speech before speaking</param>
        public static void Say(string text, bool interrupt = true)
        {
            if (string.IsNullOrEmpty(text)) return;

            DebugLogger.LogScreenReader(text);

            if (!_available) return;

            try
            {
                Tolk_Output(text, interrupt);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"ScreenReader.Say failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Queued announcement — waits for current speech to finish.
        /// </summary>
        public static void SayQueued(string text)
        {
            Say(text, false);
        }

        /// <summary>
        /// Stops current speech immediately.
        /// </summary>
        public static void Stop()
        {
            if (!_available) return;

            try
            {
                Tolk_Silence();
            }
            catch { }
        }

        /// <summary>
        /// Shuts down Tolk. Call when the game closes.
        /// </summary>
        public static void Shutdown()
        {
            if (!_initialized) return;

            try
            {
                Tolk_Unload();
            }
            catch { }

            _initialized = false;
            _available = false;
        }

        /// <summary>
        /// Returns true if a screenreader is available.
        /// </summary>
        public static bool IsAvailable => _available;

        #endregion
    }
}

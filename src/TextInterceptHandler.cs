using System.Collections.Generic;
using TMPro;

namespace CryptmasterAccess
{
    /// <summary>
    /// Intercepts visually-displayed text (subtitles, tooltips, loot notifications, location changes)
    /// and routes it to the screen reader. Uses per-frame polling for change detection.
    /// </summary>
    public class TextInterceptHandler
    {
        #region Fields

        private GameManager _gameManager;
        private SubtitleManager _subtitleManager;
        private Tooltip _tooltip;
        private TextManager _textManager;

        // Change detection state
        private string _lastSubtitleText = "";
        private bool _lastSubtitleRunning = false;
        private int _lastTooltipIndex = -1;
        private bool _lastTooltipActive = false;
        private string _lastLootText = "";
        private string _lastLocationText = "";

        // For F9 repeat
        private string _lastAnnouncement = "";

        #endregion

        #region Public Methods

        /// <summary>
        /// Caches GameManager and sub-manager references.
        /// </summary>
        public void SetGameManager(GameManager gm)
        {
            _gameManager = gm;
            _subtitleManager = gm.mySubtitleManager;
            _tooltip = gm.myToolTip;
            _textManager = gm.myTextManager;
        }

        /// <summary>
        /// Called every frame. Polls all text sources for changes.
        /// </summary>
        public void Update()
        {
            if (_gameManager == null) return;

            PollSubtitles();
            PollTooltips();
            PollLootText();
            PollLocationText();
        }

        /// <summary>
        /// Clears all tracked state on scene change.
        /// </summary>
        public void Reset()
        {
            _gameManager = null;
            _subtitleManager = null;
            _tooltip = null;
            _textManager = null;
            _lastSubtitleText = "";
            _lastSubtitleRunning = false;
            _lastTooltipIndex = -1;
            _lastTooltipActive = false;
            _lastLootText = "";
            _lastLocationText = "";
        }

        /// <summary>
        /// F9: repeats the last announced notification.
        /// </summary>
        public void RepeatLastNotification()
        {
            DebugLogger.LogInput("F9", "Repeat notification");
            if (!string.IsNullOrEmpty(_lastAnnouncement))
            {
                ScreenReader.Say(_lastAnnouncement);
            }
            else
            {
                ScreenReader.Say(Loc.Get("text_no_recent"));
            }
        }

        #endregion

        #region Polling

        /// <summary>
        /// Polls subtitle text for changes. Announces when new subtitle text appears.
        /// </summary>
        private void PollSubtitles()
        {
            if (_subtitleManager == null) return;

            bool isRunning = _subtitleManager.isRunning;

            if (isRunning)
            {
                // Read the actual displayed text
                string currentText = _subtitleManager.mySubtitleText != null
                    ? _subtitleManager.mySubtitleText.text
                    : null;

                if (!string.IsNullOrEmpty(currentText) && currentText != _lastSubtitleText)
                {
                    _lastSubtitleText = currentText;
                    string announcement = Loc.Get("text_subtitle", currentText);
                    DebugLogger.Log(LogCategory.Handler, "TextIntercept", $"Subtitle: {currentText}");
                    Announce(announcement);
                }
            }
            else if (_lastSubtitleRunning)
            {
                // Subtitle just stopped — clear tracking, don't announce
                _lastSubtitleText = "";
            }

            _lastSubtitleRunning = isRunning;
        }

        /// <summary>
        /// Polls tooltip state for changes. Announces when a new tooltip becomes active.
        /// </summary>
        private void PollTooltips()
        {
            if (_tooltip == null) return;

            // Suppress tooltip announcements on main menu (control scheme display is visual-only)
            if (_gameManager.MainGameState == 0)
            {
                _lastTooltipIndex = _tooltip.currentTooltip;
                return;
            }

            int currentIndex = _tooltip.currentTooltip;

            if (currentIndex >= 0 && currentIndex < _tooltip.allToolTips.Count)
            {
                var tip = _tooltip.allToolTips[currentIndex];

                // Detect tooltip change or tooltip becoming active
                bool justChanged = currentIndex != _lastTooltipIndex;
                bool justBecameActive = tip.isActive && !_lastTooltipActive;

                if (justChanged || justBecameActive)
                {
                    string tooltipText = BuildTooltipText(tip);
                    if (!string.IsNullOrEmpty(tooltipText))
                    {
                        string announcement = Loc.Get("text_tooltip", tooltipText);
                        DebugLogger.Log(LogCategory.Handler, "TextIntercept",
                            $"Tooltip [{tip.toolTipName}]: {tooltipText}");
                        Announce(announcement);
                    }
                }

                _lastTooltipActive = tip.isActive;
            }
            else
            {
                _lastTooltipActive = false;
            }

            _lastTooltipIndex = currentIndex;
        }

        /// <summary>
        /// Polls loot prompt text for changes.
        /// </summary>
        private void PollLootText()
        {
            if (_gameManager.lootPromptText == null) return;

            string currentText = _gameManager.lootPromptText.text;

            if (!string.IsNullOrEmpty(currentText) && currentText != _lastLootText)
            {
                _lastLootText = currentText;
                string announcement = Loc.Get("text_loot", currentText);
                DebugLogger.Log(LogCategory.Handler, "TextIntercept", $"Loot: {currentText}");
                Announce(announcement);
            }
            else if (string.IsNullOrEmpty(currentText) && !string.IsNullOrEmpty(_lastLootText))
            {
                // Loot text cleared
                _lastLootText = "";
            }
        }

        /// <summary>
        /// Polls location text for changes.
        /// </summary>
        private void PollLocationText()
        {
            if (_textManager == null) return;

            // Check the location overlay text (TextMeshProUGUI)
            string currentText = _textManager.locationText != null
                ? _textManager.locationText.text
                : null;

            if (!string.IsNullOrEmpty(currentText) && currentText != _lastLocationText)
            {
                _lastLocationText = currentText;
                string announcement = Loc.Get("text_location", currentText);
                DebugLogger.Log(LogCategory.Handler, "TextIntercept", $"Location: {currentText}");
                Announce(announcement);
            }
            else if (string.IsNullOrEmpty(currentText) && !string.IsNullOrEmpty(_lastLocationText))
            {
                // Location text cleared
                _lastLocationText = "";
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Builds a readable string from a tooltip's translatable text elements.
        /// </summary>
        private string BuildTooltipText(Tooltip.SubToolTip tip)
        {
            if (tip.allTranslateableText == null || tip.allTranslateableText.Count == 0)
                return tip.toolTipName;

            var parts = new List<string>();
            foreach (var textElement in tip.allTranslateableText)
            {
                if (textElement != null && !string.IsNullOrEmpty(textElement.text))
                {
                    parts.Add(textElement.text);
                }
            }

            return parts.Count > 0 ? string.Join(" ", parts.ToArray()) : tip.toolTipName;
        }

        /// <summary>
        /// Announces text via screen reader and stores it for F9 repeat.
        /// </summary>
        private void Announce(string text)
        {
            _lastAnnouncement = text;
            ScreenReader.Say(text);
        }

        #endregion
    }
}

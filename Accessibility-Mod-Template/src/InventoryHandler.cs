using UnityEngine;

namespace CryptmasterAccess
{
    /// <summary>
    /// Handles inventory screen accessibility: announces screen open/close,
    /// tab changes, item navigation, item details, and potion crafting mode.
    /// Uses polling — no Harmony patches needed.
    /// </summary>
    public class InventoryHandler
    {
        #region Fields

        private GameManager _gameManager;
        private Inventory _inventory;

        // Previous frame state for polling
        private bool _wasOpen;
        private bool _wasPotioning;
        private int _lastTabIndex;
        private int _lastItemIndex;
        private string _lastAnnouncement;

        #endregion

        #region Public Methods

        /// <summary>
        /// Caches GameManager and Inventory references.
        /// Called from Main.UpdateHandlers() when GM is available.
        /// </summary>
        public void SetGameManager(GameManager gm)
        {
            if (gm == null) return;
            _gameManager = gm;

            if (_inventory == null && gm.myInventory != null)
            {
                _inventory = gm.myInventory;
            }
        }

        /// <summary>
        /// Polls inventory state each frame. Called from Main.UpdateHandlers().
        /// </summary>
        public void Update()
        {
            if (_inventory == null) return;

            PollInventoryState();
        }

        /// <summary>
        /// Resets all state. Called on scene change.
        /// </summary>
        public void Reset()
        {
            _gameManager = null;
            _inventory = null;
            _wasOpen = false;
            _wasPotioning = false;
            _lastTabIndex = -1;
            _lastItemIndex = -1;
            _lastAnnouncement = null;

            DebugLogger.LogState("InventoryHandler reset");
        }

        /// <summary>
        /// Repeats the last inventory announcement. Triggered by F8 when inventory is open.
        /// </summary>
        public void RepeatCurrentInfo()
        {
            if (!string.IsNullOrEmpty(_lastAnnouncement))
            {
                ScreenReader.Say(_lastAnnouncement);
            }
        }

        /// <summary>
        /// Returns true if the inventory screen is currently open.
        /// </summary>
        public bool IsOpen()
        {
            return _inventory != null && _inventory.isShopping;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Polls inventory state and announces transitions.
        /// </summary>
        private void PollInventoryState()
        {
            bool isOpen = _inventory.isShopping;

            // Inventory opened
            if (isOpen && !_wasOpen)
            {
                OnInventoryOpened();
            }
            // Inventory closed
            else if (!isOpen && _wasOpen)
            {
                OnInventoryClosed();
            }

            // Only poll item/tab changes while open
            if (isOpen)
            {
                PollTabChange();
                PollItemChange();
                PollPotionMode();
            }

            _wasOpen = isOpen;
        }

        /// <summary>
        /// Handles inventory open transition.
        /// </summary>
        private void OnInventoryOpened()
        {
            string tabName = GetCurrentTabName();
            int itemCount = GetCurrentTabItemCount();

            string msg = Loc.Get("inv_opened", tabName, itemCount);
            DebugLogger.Log(LogCategory.Handler, "InventoryHandler", $"Inventory opened: {tabName}, {itemCount} items");
            Announce(msg);

            // Sync tracking state
            _lastTabIndex = _inventory.currentShopItemY;
            _lastItemIndex = _inventory.currentShopItemX;
            _wasPotioning = false;

            // Announce the first selected item (queued so tab name isn't interrupted)
            AnnounceCurrentItemQueued();
        }

        /// <summary>
        /// Handles inventory close transition.
        /// </summary>
        private void OnInventoryClosed()
        {
            string msg = Loc.Get("inv_closed");
            DebugLogger.Log(LogCategory.Handler, "InventoryHandler", "Inventory closed");
            Announce(msg);

            _lastTabIndex = -1;
            _lastItemIndex = -1;
            _wasPotioning = false;
        }

        /// <summary>
        /// Polls for tab changes (Up/Down arrow changes currentShopItemY).
        /// </summary>
        private void PollTabChange()
        {
            int currentTab = _inventory.currentShopItemY;
            if (currentTab == _lastTabIndex) return;

            _lastTabIndex = currentTab;

            string tabName = GetCurrentTabName();
            int itemCount = GetCurrentTabItemCount();

            string msg = Loc.Get("inv_tab_changed", tabName, itemCount);
            DebugLogger.Log(LogCategory.Handler, "InventoryHandler", $"Tab changed: {tabName}, {itemCount} items");
            Announce(msg);

            // Reset item index tracking for the new tab
            _lastItemIndex = _inventory.currentShopItemX;

            // Announce the first selected item on the new tab (queued so tab name isn't interrupted)
            AnnounceCurrentItemQueued();
        }

        /// <summary>
        /// Polls for item changes (Left/Right arrow changes currentShopItemX).
        /// </summary>
        private void PollItemChange()
        {
            int currentItem = _inventory.currentShopItemX;
            if (currentItem == _lastItemIndex) return;

            _lastItemIndex = currentItem;
            AnnounceCurrentItem();
        }

        /// <summary>
        /// Polls for potion crafting mode transitions.
        /// </summary>
        private void PollPotionMode()
        {
            bool isPotioning = _inventory.isPotionCreationActive;

            if (isPotioning && !_wasPotioning)
            {
                string msg = Loc.Get("inv_potion_on");
                DebugLogger.Log(LogCategory.Handler, "InventoryHandler", "Potion crafting started");
                Announce(msg);
            }
            else if (!isPotioning && _wasPotioning)
            {
                string msg = Loc.Get("inv_potion_off");
                DebugLogger.Log(LogCategory.Handler, "InventoryHandler", "Potion crafting ended");
                Announce(msg);
            }

            _wasPotioning = isPotioning;
        }

        /// <summary>
        /// Announces the currently highlighted item with context-appropriate detail.
        /// </summary>
        private void AnnounceCurrentItem()
        {
            AnnounceCurrentItemInternal(queued: false);
        }

        /// <summary>
        /// Announces the currently highlighted item queued (doesn't interrupt previous speech).
        /// Used after tab announcements so the tab name isn't cut off.
        /// </summary>
        private void AnnounceCurrentItemQueued()
        {
            AnnounceCurrentItemInternal(queued: true);
        }

        /// <summary>
        /// Internal item announcement with optional queuing.
        /// </summary>
        private void AnnounceCurrentItemInternal(bool queued)
        {
            var scroll = _inventory.currentInventoryScroll;
            if (scroll == null) return;

            var container = scroll.currentShopContainer;
            if (container == null)
            {
                AnnounceItem(Loc.Get("inv_no_items"), queued);
                return;
            }

            string itemName = GetItemName(container);
            int position = _inventory.currentShopItemX + 1;
            int total = GetCurrentTabItemCount();
            string description = container.myDescText;

            // Check for special item properties via CollectableItem
            var collectable = container.myCollectableItem;

            // New item
            if (collectable != null && collectable.isNewItem)
            {
                string msg = Loc.Get("inv_item_new", itemName, position, total);
                DebugLogger.Log(LogCategory.Handler, "InventoryHandler", $"New item: {itemName}");
                AnnounceItem(msg, queued);
                return;
            }

            // Usable item (has a cast action)
            if (collectable != null && collectable.isCastable && !string.IsNullOrEmpty(collectable.castContext))
            {
                string msg = Loc.Get("inv_item_usable", itemName, collectable.castContext, position, total);
                DebugLogger.Log(LogCategory.Handler, "InventoryHandler", $"Usable item: {itemName}, {collectable.castContext}");
                AnnounceItem(msg, queued);
                return;
            }

            // Ingredient with quantity
            if (collectable != null && collectable.ingredientCount > 0)
            {
                string msg = Loc.Get("inv_item_quantity", itemName, collectable.ingredientCount, position, total);
                DebugLogger.Log(LogCategory.Handler, "InventoryHandler", $"Ingredient: {itemName} x{collectable.ingredientCount}");
                AnnounceItem(msg, queued);
                return;
            }

            // Standard item — include description if available
            if (!string.IsNullOrEmpty(description))
            {
                string msg = Loc.Get("inv_item_desc", itemName, position, total, description);
                DebugLogger.Log(LogCategory.Handler, "InventoryHandler", $"Item: {itemName}, {position}/{total}");
                AnnounceItem(msg, queued);
            }
            else
            {
                string msg = Loc.Get("inv_item", itemName, position, total);
                DebugLogger.Log(LogCategory.Handler, "InventoryHandler", $"Item: {itemName}, {position}/{total}");
                AnnounceItem(msg, queued);
            }
        }

        /// <summary>
        /// Gets the display name for the currently active tab.
        /// </summary>
        private string GetCurrentTabName()
        {
            var scroll = _inventory.currentInventoryScroll;
            if (scroll == null) return "unknown";

            if (!string.IsNullOrEmpty(scroll.inventoryDisplayNameTranslated))
                return scroll.inventoryDisplayNameTranslated;

            return "unknown";
        }

        /// <summary>
        /// Gets the number of items in the currently active tab.
        /// </summary>
        private int GetCurrentTabItemCount()
        {
            var scroll = _inventory.currentInventoryScroll;
            if (scroll == null) return 0;

            if (scroll.allShopContainers != null)
                return scroll.allShopContainers.Count;

            return 0;
        }

        /// <summary>
        /// Gets the display name for a shop item container.
        /// Prefers translated name, falls back to raw name.
        /// </summary>
        private string GetItemName(ShopItemContainer container)
        {
            if (container == null) return "unknown";

            if (!string.IsNullOrEmpty(container.myTranslatedNameText))
                return container.myTranslatedNameText;

            if (!string.IsNullOrEmpty(container.myNameText))
                return container.myNameText;

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

        /// <summary>
        /// Announces an item with optional queuing (non-interrupting speech).
        /// Used to avoid cutting off tab announcements when items follow immediately.
        /// </summary>
        private void AnnounceItem(string text, bool queued)
        {
            _lastAnnouncement = text;
            if (queued)
                ScreenReader.SayQueued(text);
            else
                ScreenReader.Say(text);
        }

        #endregion
    }
}

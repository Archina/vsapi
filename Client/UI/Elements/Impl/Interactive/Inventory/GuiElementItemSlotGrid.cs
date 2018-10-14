﻿using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client
{
    /// <summary>
    /// Displays the slots of an inventory in the form of a slot grid
    /// </summary>
    public class GuiElementItemSlotGrid : GuiElementItemSlotGridBase
    {

        public GuiElementItemSlotGrid(ICoreClientAPI capi, IInventory inventory, API.Common.Action<object> SendPacketHandler, int cols, int[] visibleSlots, ElementBounds bounds) : base(capi, inventory, SendPacketHandler, cols, bounds)
        {
            DetermineAvailableSlots(visibleSlots);

            this.SendPacketHandler = SendPacketHandler;
        }

        /// <summary>
        /// Determines the available slots for the slot grid.
        /// </summary>
        /// <param name="visibleSlots"></param>
        public void DetermineAvailableSlots(int[] visibleSlots = null)
        {
            availableSlots.Clear();
            renderedSlots.Clear();

            if (visibleSlots != null)
            {
                for (int i = 0; i < visibleSlots.Length; i++)
                {
                    availableSlots.Add(visibleSlots[i], inventory.GetSlot(visibleSlots[i]));
                    renderedSlots.Add(visibleSlots[i], inventory.GetSlot(visibleSlots[i]));
                }
            }
            else
            {
                for (int i = 0; i < inventory.QuantitySlots; i++)
                {
                    availableSlots.Add(i, inventory.GetSlot(i));
                    renderedSlots.Add(i, inventory.GetSlot(i));
                }
            }
        }

        
    }

    public static partial class GuiComposerHelpers
    {

        /// <summary>
        /// Adds an item slot grid to the GUI.
        /// </summary>
        /// <param name="inventory">The inventory attached to the slot grid.</param>
        /// <param name="SendPacket">A handler that should send supplied network packet to the server, if the inventory modifications should be synced</param>
        /// <param name="columns">The number of columns in the slot grid.</param>
        /// <param name="bounds">the bounds of the slot grid.</param>
        /// <param name="key">The key for this particular slot grid.</param>
        public static GuiComposer AddItemSlotGrid(this GuiComposer composer, IInventory inventory, API.Common.Action<object> SendPacket, int columns, ElementBounds bounds, string key=null)
        {
            if (!composer.composed)
            {
                composer.AddInteractiveElement(new GuiElementItemSlotGrid(composer.Api, inventory, SendPacket, columns, null, bounds), key);
                GuiElementItemSlotGridBase.UpdateLastSlotGridFlag(composer);
            }
            return composer;
        }

        /// <summary>
        /// Adds an item slot grid to the GUI.
        /// </summary>
        /// <param name="inventory">The inventory attached to the slot grid.</param>
        /// <param name="SendPacket">A handler that should send supplied network packet to the server, if the inventory modifications should be synced</param>
        /// <param name="columns">The number of columns in the slot grid.</param>
        /// <param name="selectiveSlots">The slots within the inventory that are currently accessible.</param>
        /// <param name="bounds">the bounds of the slot grid.</param>
        /// <param name="key">The key for this particular slot grid.</param>
        public static GuiComposer AddItemSlotGrid(this GuiComposer composer, IInventory inventory, API.Common.Action<object> SendPacket, int columns, int[] selectiveSlots, ElementBounds bounds, string key = null)
        {
            if (!composer.composed)
            {
                composer.AddInteractiveElement(new GuiElementItemSlotGrid(composer.Api, inventory, SendPacket, columns, selectiveSlots, bounds), key);
                GuiElementItemSlotGridBase.UpdateLastSlotGridFlag(composer);
            }

            return composer;
        }

        /// <summary>
        /// Gets the slot grid by name.
        /// </summary>
        /// <param name="key">The name of the slot grid to get.</param>
        public static GuiElementItemSlotGrid GetSlotGrid(this GuiComposer composer, string key)
        {
            return (GuiElementItemSlotGrid)composer.GetElement(key);
        }
    }

}

﻿using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client
{
    /// <summary>
    /// A block entity inventory system for things like a campfire, or other things like that.
    /// </summary>
    public class GuiDialogBlockEntityInventory : GuiDialogGeneric
    {
        public override ITreeAttribute Attributes
        {
            get { return null; }
        }

        InventoryBase inventory = null;
        BlockPos blockEntityPos;
        int cols;

        /// <summary>
        /// The opening sound for the Block Entity Inventory. (Default is a chest opening)
        /// </summary>
        public AssetLocation OpenSound = new AssetLocation("sounds/block/chestopen");

        /// <summary>
        /// The closing sound for the Block Entity Inventory.  (Default is a chest closing)
        /// </summary>
        public AssetLocation CloseSound = new AssetLocation("sounds/block/chestclose");

        bool isduplicate = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="DialogTitle">The title of this dialogue. Ex: "Chest"</param>
        /// <param name="inventory">The inventory associated with this block entity.</param>
        /// <param name="blockEntityPos">The position of this block entity.</param>
        /// <param name="cols"></param>
        /// <param name="capi">The Client API</param>
        public GuiDialogBlockEntityInventory(string DialogTitle, InventoryBase inventory, BlockPos blockEntityPos, int cols, ICoreClientAPI capi) : base(DialogTitle, capi)
        {
            foreach (var val in capi.World.Player.InventoryManager.Inventories)
            {
                if (val.Value == inventory)
                {
                    isduplicate = true;
                    return;
                }
            }

            int openedchests = 0;
            foreach (var val in capi.OpenedGuis)
            {
                if (val is GuiDialogBlockEntityInventory)
                {
                    openedchests++;
                }
            }

            capi.World.Player.InventoryManager.OpenInventory(inventory);
            capi.Gui.PlaySound(OpenSound, true);

            this.inventory = inventory;
            this.blockEntityPos = blockEntityPos;
            this.cols = cols;
            
            double elemToDlgPad = ElementGeometrics.ElementToDialogPadding;
            double pad = GuiElementItemSlotGrid.unscaledSlotPadding;
            int rows = (int)Math.Ceiling(inventory.QuantitySlots / (float)cols);

            int visibleRows = Math.Min(rows, 7);

            // 1. The bounds of the slot grid itself. It is offseted by slot padding. It determines the size of the dialog, so we build the dialog from the bottom up
            ElementBounds slotGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad, pad, cols, visibleRows);

            // 1a.) Determine the full size of scrollable area, required to calculate scrollbar handle size
            ElementBounds fullGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, cols, rows);

            // 2. Around that is the 3 wide inset stroke
            ElementBounds insetBounds = slotGridBounds.ForkBoundingParent(6, 6, 6, 6);

            if (visibleRows < rows)
            {
                // 2a. The scrollable bounds is also the clipping bounds. Needs it's parent to be set.
                ElementBounds clippingBounds = slotGridBounds.CopyOffsetedSibling();
                clippingBounds.fixedHeight -= 3; // Why?

                // 3. Around all that is the dialog centered to screen middle, with some extra spacing right for the scrollbar
                ElementBounds dialogBounds =
                    insetBounds
                    .ForkBoundingParent(elemToDlgPad, elemToDlgPad + 70, elemToDlgPad + 40, elemToDlgPad)
                    .WithFixedAlignmentOffset(-ElementGeometrics.DialogToScreenPadding, 0)
                    .WithAlignment(openedchests >= 3 ? EnumDialogArea.LeftMiddle : EnumDialogArea.RightMiddle)
                ;

                if (!capi.Settings.Bool["floatyGuis"])
                {
                    if (openedchests % 3 == 1) dialogBounds.fixedOffsetY -= dialogBounds.fixedHeight + 10;
                    if (openedchests % 3 == 2) dialogBounds.fixedOffsetY += dialogBounds.fixedHeight + 10;
                }

                // 4. Right of the slot grid is the scrollbar
                ElementBounds scrollbarBounds = ElementStdBounds.VerticalScrollbar(insetBounds).WithParent(dialogBounds);

                SingleComposer =
                    capi.Gui
                    .CreateCompo("blockentityinventory" + blockEntityPos, dialogBounds, false)
                    .AddDialogBG(ElementBounds.Fill)
                    .AddDialogTitleBar(DialogTitle, CloseIconPressed)
                    .AddInset(insetBounds, 8, 0.85f)
                    .AddVerticalScrollbar(OnNewScrollbarvalue, scrollbarBounds, "scrollbar")
                    .BeginClip(clippingBounds)
                    .AddItemSlotGrid(inventory, DoSendPacket, cols, slotGridBounds, "slotgrid")
                    .EndClip()
                    .Compose()
                ;

                SingleComposer.GetScrollbar("scrollbar").SetHeights(
                    (float)(slotGridBounds.fixedHeight),
                    (float)(fullGridBounds.fixedHeight + pad)
                );

            }
            else
            {
                // 3. Around all that is the dialog centered to screen middle, with some extra spacing right for the scrollbar
                ElementBounds dialogBounds =
                    insetBounds
                    .ForkBoundingParent(elemToDlgPad, elemToDlgPad + 20, elemToDlgPad, elemToDlgPad)
                    .WithFixedAlignmentOffset(-ElementGeometrics.DialogToScreenPadding, 0)
                    .WithAlignment(openedchests >= 3 ? EnumDialogArea.LeftMiddle : EnumDialogArea.RightMiddle)
                ;

                if (!capi.Settings.Bool["floatyGuis"])
                {
                    if (openedchests % 3 == 1) dialogBounds.fixedOffsetY -= dialogBounds.fixedHeight + 10;
                    if (openedchests % 3 == 2) dialogBounds.fixedOffsetY += dialogBounds.fixedHeight + 10;
                }
                

                SingleComposer =
                    capi.Gui
                    .CreateCompo("blockentityinventory"+blockEntityPos, dialogBounds, false)
                    .AddDialogBG(ElementBounds.Fill)
                    .AddDialogTitleBar(DialogTitle, CloseIconPressed)
                    .AddInset(insetBounds, 8, 0.85f)
                    .AddItemSlotGrid(inventory, DoSendPacket, cols, slotGridBounds, "slotgrid")
                    .Compose()
                ;
            }


            SingleComposer.UnfocusOwnElements();

        }

        /// <summary>
        /// This occurs right before the frame is pushed to the screen.
        /// </summary>
        /// <param name="dt">The time elapsed.</param>
        public override void OnFinalizeFrame(float dt)
        {
            base.OnFinalizeFrame(dt);
            
            if (!IsInRangeOfBlock(blockEntityPos))
            {
                // Because we cant do it in here
                capi.Event.RegisterCallback((deltatime) => TryClose(), 0);
            }
        }

        /// <summary>
        /// Render's the object in Orthographic mode.
        /// </summary>
        /// <param name="deltaTime">The time elapsed.</param>
        public override void OnRender2D(float deltaTime)
        {
            if (capi.Settings.Bool["floatyGuis"])
            {

                EntityPlayer entityPlayer = capi.World.Player.Entity;
                Vec3d aboveHeadPos = new Vec3d(blockEntityPos.X + 0.5, blockEntityPos.Y + 1, blockEntityPos.Z + 0.5);
                Vec3d pos = MatrixToolsd.Project(aboveHeadPos, capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);

                // Z negative seems to indicate that the name tag is behind us \o/
                if (pos.Z < 0)
                {
                    return;
                }

                SingleComposer.Bounds.Alignment = EnumDialogArea.None;
                SingleComposer.Bounds.fixedOffsetX = 0;
                SingleComposer.Bounds.fixedOffsetY = 0;
                SingleComposer.Bounds.absFixedX = pos.X - SingleComposer.Bounds.OuterWidth / 2;
                SingleComposer.Bounds.absFixedY = capi.Render.FrameHeight - pos.Y - SingleComposer.Bounds.OuterHeight;
                SingleComposer.Bounds.absMarginX = 0;
                SingleComposer.Bounds.absMarginY = 0;
                
            }

            base.OnRender2D(deltaTime);
        }



        /// <summary>
        /// We tunnel our packet through a block entity packet so the block entity can 
        /// handle all the network stuff
        /// </summary>
        /// <param name="packet"></param>
        private void DoSendPacket(object p)
        {
            capi.Network.SendBlockEntityPacket(blockEntityPos.X, blockEntityPos.Y, blockEntityPos.Z, p);
        }

        /// <summary>
        /// Called whenever the scrollbar or mouse wheel is used.
        /// </summary>
        /// <param name="value">The new value of the scrollbar.</param>
        private void OnNewScrollbarvalue(float value)
        {
            ElementBounds bounds = SingleComposer.GetSlotGrid("slotgrid").Bounds;
            bounds.fixedY = 10 - GuiElementItemSlotGrid.unscaledSlotPadding - value;

            bounds.CalcWorldBounds();
        }

        /// <summary>
        /// Occurs whenever the X icon in the top right corner of the GUI (not the window) is pressed.
        /// </summary>
        private void CloseIconPressed()
        {
            TryClose();
        }

        /// <summary>
        /// Called whenver the GUI is opened.
        /// </summary>
        public override void OnGuiOpened()
        {
            inventory.Open(capi.World.Player);
        }

        /// <summary>
        /// Attempts to open this gui.
        /// </summary>
        /// <returns>Whether the attempt was successful.</returns>
        public override bool TryOpen()
        {
            if (isduplicate) return false;
            return base.TryOpen();
        }

        /// <summary>
        /// Called when the GUI is closed.
        /// </summary>
        public override void OnGuiClosed()
        {
            inventory.Close(capi.World.Player);
            capi.World.Player.InventoryManager.CloseInventory(inventory);

            SingleComposer.GetSlotGrid("slotgrid").OnGuiClosed(capi);

            capi.Network.SendBlockEntityPacket(blockEntityPos.X, blockEntityPos.Y, blockEntityPos.Z, (int)EnumBlockContainerPacketId.CloseInventory);
            
            capi.Gui.PlaySound(CloseSound, true);
        }

        /// <summary>
        /// Does this interface disable world interaction?
        /// </summary>
        /// <returns>Whether it disables world interaction or not.</returns>
        public override bool DisableWorldInteract()
        {
            return false;
        }

        /// <summary>
        /// Reloads the values of the GUI.
        /// </summary>
        public void ReloadValues()
        {
            
        }
    }

    /// <summary>
    /// Packet IDs for Block Containers.
    /// </summary>
    public enum EnumBlockContainerPacketId
    {
        OpenInventory = 1000,
        CloseInventory = 1001
    }
}

﻿using System;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client
{
    public abstract class GuiDialog
    {
        /// <summary>
        /// Dialogue Composer for the GUIDialogue.
        /// </summary>
        public class DlgComposers : IEnumerable<KeyValuePair<string, GuiComposer>>
        {
            protected Dictionary<string, GuiComposer> dialogComposers = new Dictionary<string, GuiComposer>();
            protected GuiDialog dialog;
            
            /// <summary>
            /// The values located inside the Dialogue Composers.
            /// </summary>
            public IEnumerable<GuiComposer> Values { get { return dialogComposers.Values; } }

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="dialog">The dialogue this composer belongs to.</param>
            public DlgComposers(GuiDialog dialog)
            {
                this.dialog = dialog;
            }

            /// <summary>
            /// Cleans up and clears the composers.
            /// </summary>
            public void ClearComposers()
            {
                foreach (var val in dialogComposers)
                {
                    if (!val.Value.IsCached)
                    {
                        val.Value?.Dispose();
                    }
                }
                dialogComposers.Clear();
            }

            /// <summary>
            /// Clean disposal method.
            /// </summary>
            public void Dispose()
            {
                foreach (var val in dialogComposers)
                {
                    val.Value?.Dispose();
                }
            }

            public GuiComposer this[string key]
            {
                get {
                    dialogComposers.TryGetValue(key, out GuiComposer val);
                    return val;
                }
                set {
                    dialogComposers[key] = value;
                    value.OnFocusChanged = dialog.OnFocusChanged;
                }
            }
            

            IEnumerator IEnumerable.GetEnumerator()
            {
                return dialogComposers.GetEnumerator();
            }

            IEnumerator<KeyValuePair<string, GuiComposer>> IEnumerable<KeyValuePair<string, GuiComposer>>.GetEnumerator()
            {
                return dialogComposers.GetEnumerator();
            }

            /// <summary>
            /// Checks to see if the key is located within the given dialogue composer.
            /// </summary>
            /// <param name="key">The key you are searching for.</param>
            /// <returns>Do we have your key?</returns>
            public bool ContainsKey(string key)
            {
                return dialogComposers.ContainsKey(key);
            }

            /// <summary>
            /// Removes the given key and the corresponding value from the Dialogue Composer.
            /// </summary>
            /// <param name="key">The Key to remove.</param>
            public void Remove(string key)
            {
                dialogComposers.Remove(key);
            }
        }



        /// <summary>
        /// The Instance of Dialogue Composer for this GUIDialogue.
        /// </summary>
        public DlgComposers DialogComposers;

        /// <summary>
        /// A single composer for this GUIDialogue.
        /// </summary>
        public GuiComposer SingleComposer
        {
            get { return DialogComposers["single"]; }
            set { DialogComposers["single"] = value; }
        }

        /// <summary>
        /// Debug name.  For debugging purposes.
        /// </summary>
        public virtual string DebugName
        {
            get { return GetType().Name; }
        }
        
        // First comes KeyDown event, opens the gui, then comes KeyPress event - this one we have to ignore
        protected bool ignoreNextKeyPress = false;


        protected bool opened;
        protected bool focused;

        /// <summary>
        /// Is the dialogue currently in focus?
        /// </summary>
        public virtual bool Focused { get { return focused; } }

        /// <summary>
        /// Is this dialogue a dialogue or a HUD object?
        /// </summary>
        public virtual EnumDialogType DialogType { get { return EnumDialogType.Dialog; } }

        /// <summary>
        /// The event fired when this dialogue is opened.
        /// </summary>
        public event Common.Action OnOpened;

        /// <summary>
        /// The event fired when this dialogue is closed.
        /// </summary>
        public event Common.Action OnClosed;


        protected ICoreClientAPI capi;

        protected virtual void OnFocusChanged(bool on)
        {
            if (on == focused) return;
            if (DialogType == EnumDialogType.Dialog && !opened) return;

            if (on)
            {
                capi.Gui.RequestFocus(this);
            } else
            {
                focused = false;
            }
        }

        /// <summary>
        /// Constructor for the GUIDialogue.
        /// </summary>
        /// <param name="capi">The Client API.</param>
        public GuiDialog(ICoreClientAPI capi)
        {
            DialogComposers = new DlgComposers(this);
            this.capi = capi;
        }

        /// <summary>
        /// Makes this gui pop up once a pre-set given key combination is set.
        /// </summary>
        public virtual void OnBlockTexturesLoaded()
        {
            string keyCombCode = ToggleKeyCombinationCode;
            if (keyCombCode != null)
            {
                capi.Input.SetHotKeyHandler(keyCombCode, OnKeyCombinationToggle);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void OnLevelFinalize()
        {

        }

        public virtual void OnOwnPlayerDataReceived() { }

        /// <summary>
        /// 0 = draw first, 1 = draw last. Used to enforce tooltips and held itemstack always drawn last to be visible.
        /// </summary>
        public virtual double DrawOrder { get { return 0.1; } }

        /// <summary>
        /// 0 = handle inputs first, 1 = handle inputs last.
        /// </summary>
        public virtual double InputOrder { get { return 0.5; } }

        /// <summary>
        /// Should this dialogue de-register itself once it's closed? (Defaults to no)
        /// </summary>
        public virtual bool UnregisterOnClose {  get { return false; } }

        /// <summary>
        /// Fires when the GUI is opened.
        /// </summary>
        public virtual void OnGuiOpened() {
            
        }

        /// <summary>
        /// Fires when the GUI is closed.
        /// </summary>
        public virtual void OnGuiClosed() {
            
        }

        /// <summary>
        /// Attempts to open this dialogue.
        /// </summary>
        /// <returns>Was this dialogue successfully opened?</returns>
        public virtual bool TryOpen()
        {
            bool wasOpened = opened;

            if (!capi.Gui.LoadedGuis.Contains(this))
            {
                capi.Gui.RegisterDialog(this);
            }

            opened = true;
            if (DialogType == EnumDialogType.Dialog)
            {
                capi.Gui.RequestFocus(this);
            }

            if (!wasOpened)
            {
                OnGuiOpened();
                OnOpened?.Invoke();
                capi.Gui.TriggerDialogOpened(this);
            }

            return true;
        }

        /// <summary>
        /// Attempts to close this dialogue- triggering the OnCloseDialogue event.
        /// </summary>
        /// <returns>Was this dialogue successfully closed?</returns>
        public virtual bool TryClose()
        {
            opened = false;
            UnFocus();
            OnGuiClosed();
            OnClosed?.Invoke();
            focused = false;
            capi.Gui.TriggerDialogClosed(this);

            return true;
        }

        /// <summary>
        /// Unfocuses the dialogue.
        /// </summary>
        public virtual void UnFocus() {
            focused = false;
        }

        /// <summary>
        /// Focuses the dialogue.
        /// </summary>
        public virtual void Focus() {
            focused = true;
        }

        /// <summary>
        /// If the dialogue is opened, this attempts to close it.  If the dialogue is closed, this attempts to open it.
        /// </summary>
        public virtual void Toggle()
        {
            if (IsOpened())
            {
                TryClose();
            } else
            {
                TryOpen();
            }
        }

        /// <summary>
        /// Is this dialogue opened?
        /// </summary>
        /// <returns>Whether this dialogue is opened or not.</returns>
        public virtual bool IsOpened()
        {
            return opened;
        }

        /// <summary>
        /// Is this dialogue opened in the given context?
        /// </summary>
        /// <param name="dialogComposerName">The composer context.</param>
        /// <returns>Whether this dialogue was opened or not within the given context.</returns>
        public virtual bool IsOpened(string dialogComposerName)
        {
            return IsOpened();
        }

        /// <summary>
        /// This runs before the render.  Local update method.
        /// </summary>
        /// <param name="deltaTime">The time that has elapsed.</param>
        public virtual void OnBeforeRenderFrame3D(float deltaTime)
        {
            
        }

        /// <summary>
        /// This runs when the dialogue is ready to render all of the components.
        /// </summary>
        /// <param name="deltaTime">The time that has elapsed.</param>
        public virtual void OnRender2D(float deltaTime)
        {
            foreach (var val in DialogComposers)
            {
                val.Value.Render(deltaTime);
            }
        }

        /// <summary>
        /// This runs when the dialogue is finalizing and cleaning up all of the components.
        /// </summary>
        /// <param name="dt">The time that has elapsed.</param>
        public virtual void OnFinalizeFrame(float dt)
        {
            foreach (var val in DialogComposers)
            {
                val.Value.PostRender(dt);
            }
        }

        internal virtual bool OnKeyCombinationToggle(KeyCombination viaKeyComb)
        {
            HotKey hotkey = capi.Input.GetHotKeyByCode(ToggleKeyCombinationCode);
            if (hotkey == null) return false;

            if (hotkey.KeyCombinationType == HotkeyType.CreativeTool && capi.World.Player.WorldData.CurrentGameMode != EnumGameMode.Creative) return false;

            Toggle();

            /*if (!viaKeyComb.Alt && !viaKeyComb.Ctrl && !viaKeyComb.Shift && viaKeyComb.KeyCode > 66)
            {
                ignoreNextKeyPress = true;
            }*/
            
            return true;
        }

        /// <summary>
        /// Fires when keys are held down.  
        /// </summary>
        /// <param name="args">The key or keys that were held down.</param>
        public virtual void OnKeyDown(KeyEvent args)
        {
            foreach (GuiComposer composer in DialogComposers.Values)
            {
                composer.OnKeyDown(capi, args, focused);
                if (args.Handled)
                {
                    return;
                }
            }

            HotKey hotkey = capi.Input.GetHotKeyByCode(ToggleKeyCombinationCode);
            if (hotkey == null) return;
            

            bool toggleKeyPressed = hotkey.DidPress(args, capi.World, capi.World.Player, true);
            if (toggleKeyPressed && TryClose())
            {
                args.Handled = true;
                return;
            }
        }

        /// <summary>
        /// Fires when the keys are pressed.
        /// </summary>
        /// <param name="args">The key or keys that were pressed.</param>
        public virtual void OnKeyPress(KeyEvent args)
        {
            if (ignoreNextKeyPress)
            {
                ignoreNextKeyPress = false;
                args.Handled = true;
                return;
            }

            if (args.Handled) return;

            foreach (GuiComposer composer in DialogComposers.Values)
            {
                composer.OnKeyPress(capi, args);
                if (args.Handled) return;
            }
            
        }

        /// <summary>
        /// Fires when the keys are released.
        /// </summary>
        /// <param name="args">the key or keys that were released.</param>
        public virtual void OnKeyUp(KeyEvent args) { }

        /// <summary>
        /// Fires explicitly when the Escape key is pressed and attempts to close the dialogue.
        /// </summary>
        /// <returns>Whether the dialogue was closed.</returns>
        public virtual bool OnEscapePressed()
        {
            if (DialogType == EnumDialogType.HUD) return false;
            return TryClose();
        }

        /// <summary>
        /// Fires when the mouse enters the given slot.
        /// </summary>
        /// <param name="slot">The slot the mouse entered.</param>
        /// <returns>Whether this event was handled.</returns>
        public virtual bool OnMouseEnterSlot(ItemSlot slot) { return false; }

        /// <summary>
        /// Fires when the mouse leaves the slot.
        /// </summary>
        /// <param name="itemSlot">The slot the mouse entered.</param>
        /// <returns>Whether this event was handled.</returns>
        public virtual bool OnMouseLeaveSlot(ItemSlot itemSlot) { return false; }

        /// <summary>
        /// Fires when the mouse clicks within the slot.
        /// </summary>
        /// <param name="itemSlot">The slot that the mouse clicked in.</param>
        /// <returns>Whether this event was handled.</returns>
        public virtual bool OnMouseClickSlot(ItemSlot itemSlot) { return false; }

        /// <summary>
        /// Fires when a mouse button is held down.
        /// </summary>
        /// <param name="args">The mouse button or buttons in question.</param>
        public virtual void OnMouseDown(MouseEvent args)
        {
            if (args.Handled) return;

            foreach (GuiComposer composer in DialogComposers.Values)
            {
                composer.OnMouseDown(capi, args);
                if (args.Handled)
                {
                    return;
                }
            }

            if (!args.Handled)
            {
                foreach (GuiComposer composer in DialogComposers.Values)
                {
                    if (composer.Bounds.PointInside(args.X, args.Y))
                    {
                        args.Handled = true;
                    }
                }
            }
            
        }

        /// <summary>
        /// Fires when a mouse button is released.
        /// </summary>
        /// <param name="args">The mouse button or buttons in question.</param>
        public virtual void OnMouseUp(MouseEvent args)
        {
            if (args.Handled) return;

            foreach (GuiComposer composer in DialogComposers.Values)
            {
                composer.OnMouseUp(capi, args);
                if (args.Handled) return;
            }

            foreach (GuiComposer composer in DialogComposers.Values)
            {
                if (composer.Bounds.PointInside(args.X, args.Y))
                {
                    args.Handled = true;
                }
            }
        }

        /// <summary>
        /// Fires when the mouse is moved.
        /// </summary>
        /// <param name="args">The mouse movements in question.</param>
        public virtual void OnMouseMove(MouseEvent args)
        {
            if (args.Handled) return;

            foreach (GuiComposer composer in DialogComposers.Values)
            {
                composer.OnMouseMove(capi, args);
                if (args.Handled) return;
            }
            
            foreach (GuiComposer composer in DialogComposers.Values)
            {
                if (composer.Bounds.PointInside(args.X, args.Y))
                {
                    args.Handled = true;
                    break;
                }
            }
        }

        /// <summary>
        /// Fires when the mouse wheel is scrolled.
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnMouseWheel(MouseWheelEventArgs args)
        {
            foreach (GuiComposer composer in DialogComposers.Values)
            {
                composer.OnMouseWheel(capi, args);
                if (args.IsHandled) return;
            }

            if (focused)
            {
                foreach (GuiComposer composer in DialogComposers.Values)
                {
                    if (composer.Bounds.PointInside(capi.Input.MouseX, capi.Input.MouseY))
                    {
                        args.SetHandled(true);
                    }
                }
            }
        }

        /// <summary>
        /// A check for whether the dialogue should recieve Render events.
        /// </summary>
        /// <returns>Whether the dialogue is opened or not.</returns>
        public virtual bool ShouldReceiveRenderEvents()
        {
            return opened;
        }

        /// <summary>
        /// A check for whether the dialogue should recieve keyboard events.
        /// </summary>
        /// <returns>Whether the dialogue is focused or not.</returns>
        public virtual bool ShouldReceiveKeyboardEvents()
        {
            return focused;
        }

        /// <summary>
        /// A check if the dialogue should recieve mouse events.
        /// </summary>
        /// <returns>Whether the mouse events should fire.</returns>
        public virtual bool ShouldReceiveMouseEvents()
        {
            return IsOpened();
        }

        /// <summary>
        /// Should this dialogue disable world interaction?
        /// </summary>
        /// <returns></returns>
        public virtual bool DisableWorldInteract()
        {
            return true;
        }

        // If true and gui element is opened then all keystrokes (except escape) are only received by this gui element
        /// <summary>
        /// Should this dialogue capture all the keyboard events (IE: textbox) except for escape.
        /// </summary>
        /// <returns></returns>
        public virtual bool CaptureAllInputs()
        {
            return false;
        }

        /// <summary>
        /// Disposes the Dialogue.
        /// </summary>
        public virtual void Dispose() {
            DialogComposers?.Dispose();
        }

        /// <summary>
        /// Clears the composers.
        /// </summary>
        public void ClearComposers()
        {
            DialogComposers?.ClearComposers();
        }

        /// <summary>
        /// The key combination string that toggles this GUI object.
        /// </summary>
        public abstract string ToggleKeyCombinationCode { get; }
    }
}

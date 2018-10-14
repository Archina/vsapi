﻿using Cairo;
using Vintagestory.API.Client;

namespace Vintagestory.API.Client
{
    public class GuiElementSwitch : GuiElementControl
    {
        API.Common.Action<bool> handler;

        LoadedTexture onTexture;

        public bool On;

        internal double unscaledPadding;
        internal double unscaledSize;

        public override bool Focusable { get { return true; } }

        /// <summary>
        /// Creates a switch which can be toggled.
        /// </summary>
        /// <param name="capi">The Client API</param>
        /// <param name="OnToggled">The event that happens when the switch is flipped.</param>
        /// <param name="bounds">The bounds of the element.</param>
        /// <param name="size">The size of the switch. (Default: 30)</param>
        /// <param name="padding">The padding on the outside of the switch (Default: 5)</param>
        public GuiElementSwitch(ICoreClientAPI capi, API.Common.Action<bool> OnToggled, ElementBounds bounds, double size = 30, double padding = 5) : base(capi, bounds)
        {
            onTexture = new LoadedTexture(capi);

            bounds.fixedWidth = size;
            bounds.fixedHeight = size;

            this.unscaledPadding = padding;
            this.unscaledSize = size;

            this.handler = OnToggled;
        }

        public override void ComposeElements(Context ctxStatic, ImageSurface surface)
        {
            Bounds.CalcWorldBounds();

            ctxStatic.SetSourceRGBA(0, 0, 0, 0.2);
            RoundRectangle(ctxStatic, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight, 3);
            ctxStatic.Fill();
            EmbossRoundRectangleElement(ctxStatic, Bounds, true, 1, 2);

            genOnTexture();
        }

        private void genOnTexture()
        {
            double size = scaled(unscaledSize - 2 * unscaledPadding);

            ImageSurface surface = new ImageSurface(Format.Argb32, (int)size, (int)size);
            Context ctx = genContext(surface);

            RoundRectangle(ctx, 0, 0, size, size, 3);
            fillWithPattern(api, ctx, waterTextureName);

            generateTexture(surface, ref onTexture);

            ctx.Dispose();
            surface.Dispose();
        }


        public override void RenderInteractiveElements(float deltaTime)
        {
            if (On)
            {
                double size = scaled(unscaledSize - 2 * unscaledPadding);
                double padding = scaled(unscaledPadding);

                api.Render.Render2DTexturePremultipliedAlpha(onTexture.TextureId, Bounds.renderX + padding, Bounds.renderY + padding, (int)size, (int)size);
            }
            
        }

        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            base.OnMouseDownOnElement(api, args);

            On = !On;
            handler(On);
            api.Gui.PlaySound("toggleswitch");
        }

        /// <summary>
        /// Sets the value of the switch on or off.
        /// </summary>
        /// <param name="on">on == true.</param>
        public void SetValue(bool on)
        {
            On = on;
        }

        public override void Dispose()
        {
            base.Dispose();

            onTexture.Dispose();
        }

    }

    public static partial class GuiComposerHelpers
    {

        /// <summary>
        /// Adds a switch to the GUI.
        /// </summary>
        /// <param name="onToggle">The event that happens when the switch is toggled.</param>
        /// <param name="bounds">The bounds of the switch.</param>
        /// <param name="key">the name of the switch. (Default: null)</param>
        /// <param name="size">The size of the switch (Default: 30)</param>
        /// <param name="padding">The padding around the switch (Default: 5)</param>
        public static GuiComposer AddSwitch(this GuiComposer composer, API.Common.Action<bool> onToggle, ElementBounds bounds, string key = null, double size = 30, double padding = 5)
        {
            if (!composer.composed)
            {
                composer.AddInteractiveElement(new GuiElementSwitch(composer.Api, onToggle, bounds, size, padding), key);
            }
            return composer;
        }

        /// <summary>
        /// Gets the switch by name.
        /// </summary>
        /// <param name="key">The internal name of the switch.</param>
        /// <returns>Returns the named switch.</returns>
        public static GuiElementSwitch GetSwitch(this GuiComposer composer, string key)
        {
            return (GuiElementSwitch)composer.GetElement(key);
        }
    }
}

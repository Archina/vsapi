﻿using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client
{
    public class GuiElementDynamicText : GuiElementTextBase
    {
        EnumTextOrientation orientation;
        public float lineHeightMultiplier;

        LoadedTexture textTexture;

        float[] strokeRGB = new float[] { 0, 0, 0, 1 };
        double strokeWidth = 0.5;

        public Action OnClick;
        public bool autoHeight;

        
        /// <summary>
        /// Adds a new element that renders text dynamically.
        /// </summary>
        /// <param name="capi">The client API.</param>
        /// <param name="text">The starting text on the component.</param>
        /// <param name="font">The font of the text.</param>
        /// <param name="orientation">The orientation of the text.</param>
        /// <param name="bounds">the bounds of the text.</param>
        public GuiElementDynamicText(ICoreClientAPI capi, string text, CairoFont font, EnumTextOrientation orientation, ElementBounds bounds) : base(capi, text, font, bounds)
        {
            this.orientation = orientation;
            lineHeightMultiplier = 1f;
            textTexture = new LoadedTexture(capi);
        }

        public override void ComposeTextElements(Context ctx, ImageSurface surface)
        {
            RecomposeMultiLine();
        }

     
        /// <summary>
        /// Automatically adjusts the height of the dynamic text.
        /// </summary>
        public void AutoHeight()
        {
            Bounds.fixedHeight = GetMultilineTextHeight(text, Bounds.InnerWidth, lineHeightMultiplier) / RuntimeEnv.GUIScale;
            Bounds.CalcWorldBounds();
            autoHeight = true;
        }

        /// <summary>
        /// Recomposes the element for lines.
        /// </summary>
        public void RecomposeMultiLine()
        {
            if (autoHeight) AutoHeight();
            
            ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.InnerWidth, (int)Bounds.InnerHeight);
            Context ctx = genContext(surface);
            ShowMultilineText(ctx, text, 0, 0, Bounds.InnerWidth, orientation, lineHeightMultiplier);
            
            generateTexture(surface, ref textTexture);
            ctx.Dispose();
            surface.Dispose();
        }

        public override void RenderInteractiveElements(float deltaTime)
        {
            api.Render.Render2DTexturePremultipliedAlpha(textTexture.TextureId, (int)Bounds.renderX, (int)Bounds.renderY, (int)Bounds.InnerWidth, (int)Bounds.InnerHeight);
        }

        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            base.OnMouseDownOnElement(api, args);

            OnClick?.Invoke();
        }
        


        internal void enableStroke()
        {
            textPathMode = true;
        }

        /// <summary>
        /// Sets the text value of the element.
        /// </summary>
        /// <param name="text">The text of the component.</param>
        /// <param name="autoHeight">Whether the height of the component should be modified.</param>
        /// <param name="forceRedraw">Whether the element should be redrawn.</param>
        public void SetNewText(string text, bool autoHeight = false, bool forceRedraw = false)
        {
            if (this.text != text || forceRedraw)
            {
                this.text = text;
                Bounds.CalcWorldBounds();
                if (autoHeight) AutoHeight();
                
                RecomposeMultiLine();
            }
        }

        /// <summary>
        /// Sets the thickness and color of the text.
        /// </summary>
        /// <param name="rgba">the color, expected length 4 in rgba order.</param>
        /// <param name="thickness">the thickness of the text.</param>
        public void setStroke(float[] rgba, double thickness)
        {
            textPathMode = true;
            this.strokeRGB = rgba;
            this.strokeWidth = thickness;
        }


        public override void Dispose()
        {
            textTexture?.Dispose();
        }

    }


    public static class GuiElementDynamicTextHelper
    {
        /// <summary>
        /// Adds dynamic text to the GUI.
        /// </summary>
        /// <param name="text">The text of the dynamic text.</param>
        /// <param name="font">The font of the text.</param>
        /// <param name="orientation">the text orientation.</param>
        /// <param name="bounds">the bounds of the </param>
        /// <param name="lineheightmultiplier">The multiplier for the height of the lines.</param>
        /// <param name="key">The name of the element.</param>
        public static GuiComposer AddDynamicText(this GuiComposer composer, string text, CairoFont font, EnumTextOrientation orientation, ElementBounds bounds, float lineheightmultiplier = 1f, string key = null)
        {
            if (!composer.composed)
            {
                GuiElementDynamicText elem = new GuiElementDynamicText(composer.Api, text, font, orientation, bounds);
                elem.lineHeightMultiplier = lineheightmultiplier;
                composer.AddInteractiveElement(elem, key);
            }
            return composer;
        }


        /// <summary>
        /// Gets the Dynamic Text by name from the GUI.
        /// </summary>
        /// <param name="key">The name of the element.</param>
        public static GuiElementDynamicText GetDynamicText(this GuiComposer composer, string key)
        {
            return (GuiElementDynamicText)composer.GetElement(key);
        }



    }

}

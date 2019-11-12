﻿using System;
using Cairo;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client
{
    public delegate string InfoTextDelegate(ItemSlot slot);

    public class GuiElementItemstackInfo : GuiElementTextBase
    {
        public static double ItemStackSize = GuiElementPassiveItemSlot.unscaledItemSize * 2.5;
        public static int MarginTop = 24;
        public static int BoxWidth = 400;
        public static int MinBoxHeight = 80;

        static double[] backTint = GuiStyle.DialogStrongBgColor;
        static double[] textTint = GuiStyle.DialogDefaultTextColor;

        ItemSlot curSlot;
        ItemStack curStack;


        GuiElementStaticText titleElement;
        GuiElementRichtext descriptionElement;

        LoadedTexture texture;
        double maxWidth;

        InfoTextDelegate OnRequireInfoText;

        /// <summary>
        /// Creates an ItemStackInfo element.
        /// </summary>
        /// <param name="capi">The client API</param>
        /// <param name="bounds">The bounds of the object.</param>
        /// <param name="OnRequireInfoText">The function that is called when an item information is called.</param>
        public GuiElementItemstackInfo(ICoreClientAPI capi, ElementBounds bounds, InfoTextDelegate OnRequireInfoText) : base(capi, "", CairoFont.WhiteSmallText(), bounds)
        {
            this.OnRequireInfoText = OnRequireInfoText;

            texture = new LoadedTexture(capi);

            ElementBounds textBounds = bounds.CopyOnlySize();
            ElementBounds descBounds = textBounds.CopyOffsetedSibling(ItemStackSize + 50, MarginTop, -ItemStackSize - 50, 0);
            descBounds.WithParent(bounds);

            descriptionElement = new GuiElementRichtext(capi, new RichTextComponentBase[0], descBounds);
            //new GuiElementStaticText(capi, "", EnumTextOrientation.Left, textBounds.CopyOffsetedSibling(ItemStackSize + 50, MarginTop, -ItemStackSize - 50, 0), Font);
            descriptionElement.zPos = 1001;

            CairoFont titleFont = Font.Clone();
            titleFont.FontWeight = FontWeight.Bold;
            titleElement = new GuiElementStaticText(capi, "", EnumTextOrientation.Left, textBounds, titleFont);

            maxWidth = bounds.fixedWidth;
        }


        public override void ComposeElements(Context ctx, ImageSurface surface)
        {
            Recompose();
        }

        void RecalcBounds(string title, string desc)
        {
            descriptionElement.BeforeCalcBounds();

            double currentWidth = Math.Max(descriptionElement.MaxLineWidth, descriptionElement.Bounds.InnerWidth) / RuntimeEnv.GUIScale + 10;
            double currentHeight = 0;

            currentWidth += 40 + scaled(GuiElementPassiveItemSlot.unscaledItemSize) * 3;
            currentWidth = Math.Max(currentWidth, titleElement.Font.GetTextExtents(title).Width / RuntimeEnv.GUIScale + 10);
            currentWidth = Math.Min(currentWidth, maxWidth);

            double descWidth = currentWidth - scaled(ItemStackSize) - 50;

            Bounds.fixedWidth = currentWidth;
            descriptionElement.Bounds.fixedWidth = descWidth;
            titleElement.Bounds.fixedWidth = currentWidth;
            descriptionElement.Bounds.CalcWorldBounds();

            // Height depends on the width
            double descTextHeight = descriptionElement.Bounds.fixedHeight;
            currentHeight = Math.Max(descTextHeight, scaled(25) + scaled(GuiElementPassiveItemSlot.unscaledItemSize) * 3);
            titleElement.Bounds.fixedHeight = currentHeight;
            descriptionElement.Bounds.fixedHeight = currentHeight;
            Bounds.fixedHeight = scaled(25) + currentHeight / RuntimeEnv.GUIScale;
        }


        void Recompose()
        {
            if (curSlot?.Itemstack == null) return;

            string title = curSlot.GetStackName();
            string desc = OnRequireInfoText(curSlot);
            desc.TrimEnd();


            titleElement.SetValue(title);
            descriptionElement.SetNewText(desc, Font);

            RecalcBounds(title, desc);


            Bounds.CalcWorldBounds();

            ElementBounds textBounds = Bounds.CopyOnlySize();
            textBounds.CalcWorldBounds();


            ImageSurface surface = new ImageSurface(Format.Argb32, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
            Context ctx = genContext(surface);

            ctx.SetSourceRGBA(0, 0, 0, 0);
            ctx.Paint();

            ctx.SetSourceRGBA(backTint[0], backTint[1], backTint[2], backTint[3]);
            RoundRectangle(ctx, textBounds.bgDrawX, textBounds.bgDrawY, textBounds.OuterWidthInt, textBounds.OuterHeightInt, GuiStyle.DialogBGRadius);
            ctx.FillPreserve();

            ctx.SetSourceRGBA(GuiStyle.DialogLightBgColor[0] * 1.4, GuiStyle.DialogStrongBgColor[1] * 1.4, GuiStyle.DialogStrongBgColor[2] * 1.4, 1);
            ctx.LineWidth = 3 * 1.75;
            ctx.StrokePreserve();
            surface.Blur(8.2);

            ctx.SetSourceRGBA(backTint[0] / 2, backTint[1] / 2, backTint[2] / 2, backTint[3]);
            ctx.Stroke();


            int w = (int)(scaled(ItemStackSize) + scaled(40));
            int h = (int)(scaled(ItemStackSize) + scaled(40));

            ImageSurface shSurface = new ImageSurface(Format.Argb32, w, h);
            Context shCtx = genContext(shSurface);

            shCtx.SetSourceRGBA(GuiStyle.DialogSlotBackColor);
            RoundRectangle(shCtx, 0, 0, w, h, 0);
            shCtx.FillPreserve();

            shCtx.SetSourceRGBA(GuiStyle.DialogSlotFrontColor);
            shCtx.LineWidth = 5;
            shCtx.Stroke();
            shSurface.Blur(7);
            shSurface.Blur(7);
            shSurface.Blur(7);
            EmbossRoundRectangleElement(shCtx, 0, 0, w, h, true);


            ctx.SetSourceSurface(shSurface, (int)(textBounds.drawX), (int)(textBounds.drawY + scaled(MarginTop)));
            ctx.Rectangle(textBounds.drawX, textBounds.drawY + scaled(MarginTop), w, h);
            ctx.Fill();

            shCtx.Dispose();
            shSurface.Dispose();


            titleElement.ComposeElements(ctx, surface);

            descriptionElement.ComposeElements(ctx, surface);

            generateTexture(surface, ref texture);

            ctx.Dispose();
            surface.Dispose();
        }


        public override void RenderInteractiveElements(float deltaTime)
        {
            if (curSlot?.Itemstack == null) return;

            api.Render.Render2DTexturePremultipliedAlpha(texture.TextureId, Bounds, 1000);

            descriptionElement.RenderInteractiveElements(deltaTime);

            double offset = (int)scaled(30 + ItemStackSize/2);

            api.Render.RenderItemstackToGui(
                curSlot,
                (int)Bounds.renderX + offset,
                (int)Bounds.renderY + offset + (int)scaled(MarginTop), 
                1000 + scaled(GuiElementPassiveItemSlot.unscaledItemSize) * 2, 
                (float)scaled(ItemStackSize), 
                ColorUtil.WhiteArgb,
                true,
                true,
                false
            );
        }



        
        

        /// <summary>
        /// Gets the item slot for this stack info.
        /// </summary>
        /// <returns></returns>
        public ItemSlot GetSlot()
        {
            return curSlot;
        }

        /// <summary>
        /// Sets the source slot for stacks.
        /// </summary>
        /// <param name="nowSlot"></param>
        public void SetSourceSlot(ItemSlot nowSlot)
        {
            //bool recompose = this.curStack == null || (nowSlot?.Itemstack != null && !nowSlot.Itemstack.Equals(curStack));

            bool recompose =
                ((this.curStack == null) != (nowSlot?.Itemstack == null))
                || (nowSlot?.Itemstack != null && !nowSlot.Itemstack.Equals(api.World, curStack, GlobalConstants.IgnoredStackAttributes));


            if (nowSlot?.Itemstack == null)
            {
                this.curSlot = null;
            }

            if (recompose)
            {
                this.curSlot = nowSlot;
                this.curStack = nowSlot?.Itemstack?.Clone();

                if (nowSlot?.Itemstack == null)
                {
                    Bounds.fixedHeight = 0;
                }
                
                Recompose();
            }
        }


        public override void Dispose()
        {
            base.Dispose();

            texture.Dispose();
            descriptionElement?.Dispose();
            titleElement?.Dispose();
        }
    }
}

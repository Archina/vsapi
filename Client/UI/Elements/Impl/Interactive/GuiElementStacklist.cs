﻿using System.Collections.Generic;
using Cairo;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using System;
using System.Linq;
using Vintagestory.API.Util;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Client
{
    public struct WeightedHandbookPage
    {
        public float Weight;
        public GuiHandbookPage Page;
    }

    public abstract class GuiHandbookPage
    {
        public abstract string PageCode { get; }

        public abstract string CategoryCode { get; }

        public abstract void RenderTo(ICoreClientAPI capi, double x, double y);
        public abstract void Dispose();
        public bool Visible = true;

        public abstract RichTextComponentBase[] GetPageText(ICoreClientAPI capi, ItemStack[] allStacks, Common.ActionConsumable<string> openDetailPageFor);
        public abstract float TextMatchWeight(string text);
    }

    public class GuiHandbookTextPage : GuiHandbookPage
    {
        public string pageCode;
        public string Title;
        public string Text;
        public string categoryCode = "guide";

        public LoadedTexture Texture;
        public override string PageCode => pageCode;

        public override string CategoryCode => categoryCode;

        public override void Dispose() { Texture?.Dispose(); Texture = null; }

        RichTextComponentBase[] comps;
        public int PageNumber;

        string titleCached;

        public GuiHandbookTextPage()
        {
            
        }

        public void Init(ICoreClientAPI capi)
        {
            if (Text.Length < 255)
            {
                Text = Lang.Get(Text);
            }
            
            comps = VtmlUtil.Richtextify(capi, Text, CairoFont.WhiteSmallText().WithLineHeightMultiplier(1.2));

            titleCached = Lang.Get(Title);
        }

        public override RichTextComponentBase[] GetPageText(ICoreClientAPI capi, ItemStack[] allStacks, Common.ActionConsumable<string> openDetailPageFor)
        {
            return comps;
        }

        public void Recompose(ICoreClientAPI capi)
        {
            Texture?.Dispose();
            Texture = new TextTextureUtil(capi).GenTextTexture(Lang.Get(Title), CairoFont.WhiteSmallText());

            
        }

        public override float TextMatchWeight(string searchText)
        {
            if (titleCached.Equals(searchText, StringComparison.InvariantCultureIgnoreCase)) return 3;
            if (titleCached.StartsWith(searchText, StringComparison.InvariantCultureIgnoreCase)) return 2.5f;
            if (titleCached.CaseInsensitiveContains(searchText)) return 2;
            if (Text.CaseInsensitiveContains(searchText)) return 1;
            return 0;
        }

        public override void RenderTo(ICoreClientAPI capi, double x, double y)
        {
            float size = (float)GuiElement.scaled(25);
            float pad = (float)GuiElement.scaled(10);
            //capi.Render.RenderItemstackToGui(Stack, x + pad + size / 2, y + size / 2, 100, size, ColorUtil.WhiteArgb, true, false, false);

            if (Texture == null)
            {
                Recompose(capi);
            }

            capi.Render.Render2DTexturePremultipliedAlpha(
                Texture.TextureId,
                (x + pad),
                y + size / 4 - 3,
                Texture.Width,
                Texture.Height,
                50
            );
        }
    }

    public class GuiHandbookGroupedItemstackPage : GuiHandbookItemStackPage
    {
        public List<ItemStack> Stacks = new List<ItemStack>();
        public string Name;

        public GuiHandbookGroupedItemstackPage(ICoreClientAPI capi, ItemStack stack) : base(capi, null)
        {
        }

        public override string PageCode => Name;

        public override void RenderTo(ICoreClientAPI capi, double x, double y)
        {
            float size = (float)GuiElement.scaled(25);
            float pad = (float)GuiElement.scaled(10);

            int index = (int)((capi.ElapsedMilliseconds / 1000) % Stacks.Count);

            dummySlot.Itemstack = Stacks[index];
            capi.Render.RenderItemstackToGui(dummySlot, x + pad + size / 2, y + size / 2, 100, size, ColorUtil.WhiteArgb, true, false, false);

            if (Texture == null)
            {
                Texture = new TextTextureUtil(capi).GenTextTexture(Name, CairoFont.WhiteSmallText());
            }

            capi.Render.Render2DTexturePremultipliedAlpha(
                Texture.TextureId,
                (x + size + GuiElement.scaled(25)),
                y + size / 4 - 3,
                Texture.Width,
                Texture.Height,
                50
            );
        }

        public override RichTextComponentBase[] GetPageText(ICoreClientAPI capi, ItemStack[] allStacks, Common.ActionConsumable<string> openDetailPageFor)
        {
            dummySlot.Itemstack = Stacks[0];

            return Stacks[0].Collectible.GetHandbookInfo(dummySlot, capi, allStacks, openDetailPageFor);
        }
    }

    public class GuiHandbookItemStackPage : GuiHandbookPage
    {
        public ItemStack Stack;
        public LoadedTexture Texture;
        public string TextCache;

        public int PageNumber;

        public override string PageCode => PageCodeForStack(Stack);

        public InventoryBase unspoilableInventory;
        public DummySlot dummySlot;

        ElementBounds scissorBounds;

        public override string CategoryCode => "stack";

        public GuiHandbookItemStackPage(ICoreClientAPI capi, ItemStack stack)
        {
            this.Stack = stack;
            unspoilableInventory = new CreativeInventoryTab(1, "not-used", null);
            dummySlot = new DummySlot(stack, unspoilableInventory);

            TextCache = stack.GetName() + " " + stack.GetDescription(capi.World, dummySlot, false);
        }

        public static string PageCodeForStack(ItemStack stack)
        {
            if (stack.Attributes != null && stack.Attributes.Count > 0)
            {
                ITreeAttribute tree = stack.Attributes.Clone();
                foreach (var val in GlobalConstants.IgnoredStackAttributes) tree.RemoveAttribute(val);
                tree.RemoveAttribute("durability");

                if (tree.Count != 0)
                {
                    string treeStr = tree.ToJsonToken();
                    return (stack.Class == EnumItemClass.Block ? "block" : "item") + "-" + stack.Collectible.Code.ToShortString() + "-" + treeStr;
                }
            }

            return (stack.Class == EnumItemClass.Block ? "block" : "item") + "-" + stack.Collectible.Code.ToShortString();
        }

        public void Recompose(ICoreClientAPI capi)
        {
            Texture?.Dispose();
            Texture = new TextTextureUtil(capi).GenTextTexture(Stack.GetName(), CairoFont.WhiteSmallText());

            scissorBounds = ElementBounds.FixedSize(50, 50);
            scissorBounds.ParentBounds = capi.Gui.WindowBounds;
        }

        public override void RenderTo(ICoreClientAPI capi, double x, double y)
        {
            float size = (float)GuiElement.scaled(25);
            float pad = (float)GuiElement.scaled(10);

            if (Texture == null)
            {
                Recompose(capi);
            }

            scissorBounds.fixedX = pad + x / RuntimeEnv.GUIScale - size/2;
            scissorBounds.fixedY = y / RuntimeEnv.GUIScale - size / 2;
            scissorBounds.CalcWorldBounds();

            if (scissorBounds.InnerWidth <= 0 || scissorBounds.InnerHeight <= 0) return;

            capi.Render.PushScissor(scissorBounds, true);
            capi.Render.RenderItemstackToGui(dummySlot, x + pad + size/2 , y + size / 2, 100, size, ColorUtil.WhiteArgb, true, false, false);
            capi.Render.PopScissor();

            capi.Render.Render2DTexturePremultipliedAlpha(
                Texture.TextureId,
                (x + size + GuiElement.scaled(25)), 
                y + size / 4 - 3,
                Texture.Width,
                Texture.Height,
                50
            );
        }

        public override void Dispose() {
            Texture?.Dispose();
            Texture = null;
        }

        public override RichTextComponentBase[] GetPageText(ICoreClientAPI capi, ItemStack[] allStacks, Common.ActionConsumable<string> openDetailPageFor)
        {
            return Stack.Collectible.GetHandbookInfo(dummySlot, capi, allStacks, openDetailPageFor);
        }

        public override float TextMatchWeight(string searchText)
        {
            string title = Stack.GetName();
            if (title.Equals(searchText, StringComparison.InvariantCultureIgnoreCase)) return 3;
            if (title.StartsWith(searchText, StringComparison.InvariantCultureIgnoreCase)) return 2.5f;
            if (title.CaseInsensitiveContains(searchText)) return 2;
            if (TextCache.CaseInsensitiveContains(searchText)) return 1;
            return 0;
        }
    }

    public class GuiElementHandbookList : GuiElement
    {
        public List<GuiHandbookPage> Elements = new List<GuiHandbookPage>();

        public int unscaledCellSpacing = 5;
        public int unscaledCellHeight = 40;

        public API.Common.Action<int> onLeftClick;

        LoadedTexture hoverOverlayTexture;
        public ElementBounds insideBounds;

        public GuiElementHandbookList(ICoreClientAPI capi, ElementBounds bounds, API.Common.Action<int> onLeftClick, List<GuiHandbookPage> elements = null) : base(capi, bounds)
        {
            hoverOverlayTexture = new LoadedTexture(capi);

            insideBounds = new ElementBounds().WithFixedPadding(unscaledCellSpacing).WithEmptyParent();
            insideBounds.CalcWorldBounds();

            this.onLeftClick = onLeftClick;
            if (elements != null)
            {
                Elements = elements;
            }

            CalcTotalHeight();
        }
        

        public void CalcTotalHeight()
        {
            double height = Elements.Where(e => e.Visible).Count() * (unscaledCellHeight + unscaledCellSpacing);
            insideBounds.fixedHeight = height + unscaledCellSpacing;
        }

        public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
        {
            insideBounds = new ElementBounds().WithFixedPadding(unscaledCellSpacing).WithEmptyParent();
            insideBounds.CalcWorldBounds();
            CalcTotalHeight();
            Bounds.CalcWorldBounds();

            ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.InnerWidth, (int)GuiElement.scaled(unscaledCellHeight));
            Context ctx = new Context(surface);

            ctx.SetSourceRGBA(1, 1, 1, 0.5);
            ctx.Paint();

            generateTexture(surface, ref hoverOverlayTexture);

            ctx.Dispose();
            surface.Dispose();
        }


        bool wasMouseDownOnElement = false;
        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            if (!Bounds.ParentBounds.PointInside(args.X, args.Y)) return;
            base.OnMouseDownOnElement(api, args);

            wasMouseDownOnElement = true;
        }


        public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
        {
            if (!Bounds.ParentBounds.PointInside(args.X, args.Y)) return;
            if (!wasMouseDownOnElement) return;

            wasMouseDownOnElement = false;

            int i = 0;

            int mx = api.Input.MouseX;
            int my = api.Input.MouseY;
            double posY = insideBounds.absY;


            foreach (GuiHandbookPage element in Elements)
            {
                if (!element.Visible)
                {
                    i++;
                    continue;
                }

                float y = (float)(5 + Bounds.absY + posY);

                if (mx > Bounds.absX && mx <= Bounds.absX + Bounds.InnerWidth && my >= y - 8 && my <= y + scaled(unscaledCellHeight) - 8)
                {
                    api.Gui.PlaySound("menubutton_press");
                    onLeftClick?.Invoke(i);
                    args.Handled = true;
                    return;
                }
                
                posY += scaled(unscaledCellHeight + unscaledCellSpacing);
                i++;
            }
        }

        public override void RenderInteractiveElements(float deltaTime)
        {
            int mx = api.Input.MouseX;
            int my = api.Input.MouseY;
            bool inbounds = Bounds.ParentBounds.PointInside(mx, my);

            double posY = insideBounds.absY;

            foreach (GuiHandbookPage element in Elements)
            {
                if (!element.Visible) continue;

                float y = (float)(5 + Bounds.absY + posY);

                if (inbounds && mx > Bounds.absX && mx <= Bounds.absX + Bounds.InnerWidth && my >= y-8 && my <= y + scaled(unscaledCellHeight)-8)
                {
                    api.Render.Render2DLoadedTexture(hoverOverlayTexture, (float)Bounds.absX, y-8);
                }

                if (posY > -50 && posY < Bounds.OuterHeight + 50)
                {
                    element.RenderTo(api, Bounds.absX, y);
                }

                posY += scaled(unscaledCellHeight + unscaledCellSpacing);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            hoverOverlayTexture.Dispose();

            foreach (var val in Elements)
            {
                val.Dispose();
            }
        }

    }

    public static partial class GuiComposerHelpers
    {

        public static GuiComposer AddHandbookStackList(this GuiComposer composer, ElementBounds bounds, API.Common.Action<int> onleftClick = null, List<GuiHandbookPage> stacks = null, string key = null)
        {
            if (!composer.composed)
            {
                composer.AddInteractiveElement(new GuiElementHandbookList(composer.Api, bounds, onleftClick, stacks), key);
            }

            return composer;
        }

        public static GuiElementHandbookList GetHandbookStackList(this GuiComposer composer, string key)
        {
            return (GuiElementHandbookList)composer.GetElement(key);
        }
    }

}

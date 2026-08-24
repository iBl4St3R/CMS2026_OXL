using CMS2026UITKFramework;
using System;
using UnityEngine;

namespace CMS2026_OXL
{
    /// <summary>
    /// Builds the OXL home page: logo + 4 category banners (Cars / Parts / Tools / Decorations)
    /// and, when Cars is selected, a search bar underneath that navigates into the listings
    /// page with the typed query pre-applied as a ModelQuery filter. Kept as its own static
    /// class so OXLPanel (already oversized) doesn't grow further.
    /// </summary>
    public static class OXLHomePage
    {
        public enum Category { Cars, Parts, Tools, Decorations }

        private const float BannerW = 320f;
        private const float BannerH = 200f;
        private const float BannerGap = 24f;
        private const float BannerY = 180f;

        private static readonly Color SelectedBorder = new Color(0.220f, 0.592f, 0.341f, 1f);
        private static readonly Color UnselectedBorder = new Color(0.15f, 0.25f, 0.38f, 0.5f);
        private static readonly Color DimOverlay = new Color(0f, 0f, 0f, 0.35f);
        private static readonly Color LabelBg = new Color(0.03f, 0.05f, 0.09f, 0.85f);
        private static readonly Color CardBg = new Color(0.06f, 0.09f, 0.14f, 1f);
        private static readonly Color CardBgHover = new Color(0.09f, 0.13f, 0.19f, 1f);
        private static readonly Color CardBgPress = new Color(0.04f, 0.06f, 0.10f, 1f);
        private static readonly Color FieldBorder = new Color(0.220f, 0.592f, 0.341f, 0.55f);
        private static readonly Color FieldBg = new Color(0.030f, 0.055f, 0.095f, 1f);
        private static readonly Color BtnBg = new Color(0.075f, 0.110f, 0.180f, 1f);
        private static readonly Color BtnBgHover = new Color(0.110f, 0.170f, 0.260f, 1f);
        private static readonly Color BtnBgPress = new Color(0.220f, 0.592f, 0.341f, 0.5f);

        public struct Textures
        {
            public Texture2D Cars, Parts, Tools, Decorations;
        }

        public static void Build(UIPanel panel, object container, float panelW, Textures tex, Category selected, Action<Category> onSelect, Action<string> onSearch, string initialSearchText)
        {
            float totalW = BannerW * 4 + BannerGap * 3;
            float startX = (panelW - totalW) / 2f;

            BuildBanner(panel, container, startX + 0 * (BannerW + BannerGap), "OXL CARS", tex.Cars, selected == Category.Cars, true, () => onSelect(Category.Cars));
            BuildBanner(panel, container, startX + 1 * (BannerW + BannerGap), "OXL CAR PARTS", tex.Parts, selected == Category.Parts, false, null);
            BuildBanner(panel, container, startX + 2 * (BannerW + BannerGap), "OXL WORKSHOP TOOLS", tex.Tools, selected == Category.Tools, false, null);
            BuildBanner(panel, container, startX + 3 * (BannerW + BannerGap), "OXL DECORATIONS", tex.Decorations, selected == Category.Decorations, false, null);

            if (selected == Category.Cars) BuildSearchRow(panel, container, panelW, BannerY + BannerH + 32f, onSearch, initialSearchText);
        }

        private static void BuildBanner(UIPanel panel, object container, float x, string label, Texture2D tex, bool isSelected, bool enabled, Action onClick)
        {
            var card = UIRuntime.NewVE();
            var cs = UIRuntime.GetStyle(card);
            S.Position(cs, "Absolute");
            S.Left(cs, x); S.Top(cs, BannerY);
            S.Width(cs, BannerW); S.Height(cs, BannerH);
            S.BgColor(cs, CardBg);
            S.BorderRadius(cs, 10f);
            S.BorderWidth(cs, isSelected ? 3f : 1f);
            S.BorderColor(cs, isSelected ? SelectedBorder : UnselectedBorder);
            S.Overflow(cs, "Hidden");
            UIRuntime.AddChild(container, card);
            var cardPtr = UIRuntime.GetPtr(card);

            if (tex != null) UIRuntime.SetBackgroundImage(card, tex);

            if (!enabled)
            {
                var dim = UIRuntime.NewVE();
                var ds = UIRuntime.GetStyle(dim);
                S.Position(ds, "Absolute");
                S.Left(ds, 0f); S.Top(ds, 0f);
                S.Width(ds, BannerW); S.Height(ds, BannerH);
                S.BgColor(ds, DimOverlay);
                UIRuntime.AddChild(card, dim);

                var soonLbl = panel.AddLabelToContainer(card, "COMING SOON", 0f, BannerH / 2f - 10f, BannerW, 20f, new Color(0.85f, 0.75f, 0.30f, 1f));
                soonLbl.SetFontSize(13);
                S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(soonLbl.GetRawPtr())), TextAnchor.MiddleCenter);
            }

            var labelStrip = UIRuntime.NewVE();
            var lss = UIRuntime.GetStyle(labelStrip);
            S.Position(lss, "Absolute");
            S.Left(lss, 0f); S.Top(lss, BannerH - 34f);
            S.Width(lss, BannerW); S.Height(lss, 34f);
            S.BgColor(lss, LabelBg);
            UIRuntime.AddChild(card, labelStrip);

            var lbl = panel.AddLabelToContainer(labelStrip, label, 0f, 0f, BannerW, 34f, isSelected ? SelectedBorder : Color.white);
            lbl.SetFontSize(15);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(lbl.GetRawPtr())), TextAnchor.MiddleCenter);

            if (enabled && onClick != null)
            {
                panel.WireClick(cardPtr, onClick);
                panel.WireHover(cardPtr, CardBg, CardBgHover, CardBgPress);
            }
        }

        private static void BuildSearchRow(UIPanel panel, object container, float panelW, float y, Action<string> onSearch, string initialText)
        {
            const float FieldW = 520f;
            const float FieldH = 44f;
            const float BtnW = 120f;
            const float Gap = 10f;

            float totalW = FieldW + Gap + BtnW;
            float x = (panelW - totalW) / 2f;

            var tf = BuildSearchField(container, x, y, FieldW, FieldH, initialText);

            Action runSearch = () => onSearch(ReadFieldValue(tf));
            WireEnter(tf, runSearch);

            var btnPtr = panel.AddButtonToContainer(container, "Search", x + FieldW + Gap, y, BtnW, FieldH, BtnBg, runSearch);
            panel.WireHover(btnPtr, BtnBg, BtnBgHover, BtnBgPress);
        }

        private static object BuildSearchField(object container, float x, float y, float w, float h, string initialText)
        {
            var tf = Activator.CreateInstance(UIRuntime.TextFieldType);
            var s = UIRuntime.GetStyle(tf);
            S.Position(s, "Absolute");
            S.Left(s, x); S.Top(s, y);
            S.Width(s, w); S.Height(s, h);
            S.BgColor(s, FieldBg);
            S.BorderRadius(s, h / 2f);
            S.BorderWidth(s, 1f);
            S.BorderColor(s, FieldBorder);
            S.Padding(s, 10f);
            S.Color(s, Color.white);
            S.Font(s);
            UIRuntime.TextFieldType.GetProperty("value")?.SetValue(tf, initialText ?? "");
            UIRuntime.AddChild(container, tf);
            return tf;
        }

        private static string ReadFieldValue(object tf) => (string)(UIRuntime.TextFieldType.GetProperty("value")?.GetValue(tf) ?? "");

        private static void WireEnter(object tf, Action onEnter)
        {
            try
            {
                var ue = UIRuntime.UEAsm;
                var trickleType = ue.GetType("UnityEngine.UIElements.TrickleDown");
                var regMethod = UIRuntime.VisualElementType.GetMethods().First(m => m.Name == "RegisterCallback" && m.IsGenericMethod && m.GetParameters().Length == 2).MakeGenericMethod(typeof(UnityEngine.UIElements.KeyDownEvent));
                Action<UnityEngine.UIElements.KeyDownEvent> handler = evt => { if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) onEnter(); };
                var il2cb = Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<UnityEngine.UIElements.EventCallback<UnityEngine.UIElements.KeyDownEvent>>(handler);
                regMethod.Invoke(tf, new object[] { il2cb, Enum.Parse(trickleType, "TrickleDown") });
            }
            catch (Exception ex) { OXLPlugin.Log.Msg($"[OXLHomePage] WireEnter failed: {ex.Message}"); }
        }
    }
}
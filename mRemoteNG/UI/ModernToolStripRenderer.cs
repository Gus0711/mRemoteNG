#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNG.UI
{
    /// <summary>
    /// Flat, Fluent-style renderer for menus, toolbars, context menus and status strips used by the
    /// Modern Dark / Modern Light themes: chrome-colored bars, no gradients, borders or grips, rounded
    /// hover/pressed highlights, accent check marks, Fluent chevrons and rounded (DWM) popup menus.
    /// All colors come from <see cref="ModernPalette.Current"/> at paint time, so a single instance
    /// follows theme switches without being recreated.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ModernToolStripRenderer : ToolStripProfessionalRenderer
    {
        private const char CheckMarkGlyph = '';
        private const char ChevronRightSmallGlyph = '';
        private const char ChevronDownSmallGlyph = '';
        private const char ChevronUpSmallGlyph = '';
        private const char ChevronLeftSmallGlyph = '';

        private static readonly bool IsWindows11 = Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000;
        private static readonly ConditionalWeakTable<ToolStripDropDown, StrongBox<int>> PopupChromeApplied = new();
        private static readonly ConditionalWeakTable<ToolStripItem, object> PrimaryItems = new();

        private static readonly Lazy<Font> UiFontLazy = new(() => ModernGraphics.CreateUiFont(9f));

        /// <summary>Shared modern UI font for menus and toolbars (do not dispose).</summary>
        public static Font UiFont => UiFontLazy.Value;

        /// <summary>Shared instance (stateless; colors are resolved at paint time).</summary>
        public static ModernToolStripRenderer Instance { get; } = new();

        public ModernToolStripRenderer() : base(new ModernColorTable())
        {
            RoundedEdges = false;
        }

        /// <summary>True when theming is enabled and one of the Modern themes is active.</summary>
        public static bool IsModernThemeActive
        {
            get
            {
                ThemeManager themeManager = ThemeManager.getInstance();
                if (!themeManager.ThemingActive)
                    return false;
                string? name = themeManager.ActiveTheme?.Name;
                return name == ThemeManager.ModernDarkThemeName || name == ThemeManager.ModernLightThemeName;
            }
        }

        /// <summary>
        /// Makes the Modern themes hand out this renderer wherever the app styles a tool strip through
        /// DockPanelSuite (<c>VisualStudioToolStripExtender.SetStyle</c> / <c>ThemeBase.ApplyTo</c>), so every
        /// menu, toolbar and context menu of the application picks it up without per-call-site changes.
        /// Safe to call repeatedly.
        /// </summary>
        public static void InstallIntoModernThemes()
        {
            try
            {
                ThemeManager themeManager = ThemeManager.getInstance();
                PropertyInfo? rendererProperty = typeof(ThemeBase).GetProperty("ToolStripRenderer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (rendererProperty == null)
                    return;

                foreach (string name in new[] { ThemeManager.ModernDarkThemeName, ThemeManager.ModernLightThemeName })
                {
                    ThemeBase? theme = themeManager.getTheme(name)?.Theme;
                    if (theme != null && rendererProperty.GetValue(theme) != Instance)
                        rendererProperty.SetValue(theme, Instance);
                }
            }
            catch (Exception)
            {
                // Theming internals changed: fall back to the theme's own renderer.
            }
        }

        /// <summary>
        /// Applies the renderer (when a Modern theme is active) or the given legacy renderer to the strip.
        /// </summary>
        public static void Apply(ToolStrip? toolStrip, ToolStripRenderer? legacyRenderer = null)
        {
            if (toolStrip == null)
                return;

            if (IsModernThemeActive)
            {
                if (toolStrip.Renderer != Instance)
                    toolStrip.Renderer = Instance;
            }
            else if (legacyRenderer != null && toolStrip.Renderer != legacyRenderer)
            {
                toolStrip.Renderer = legacyRenderer;
            }
        }

        /// <summary>
        /// Enables the roomy modern menu metrics on a strip that is not styled through <see cref="Apply"/> or
        /// DockPanelSuite (e.g. one using <see cref="ToolStripRenderMode.ManagerRenderMode"/>).
        /// </summary>
        public static void EnableModernMetrics(ToolStrip toolStrip) => ToolStripMetrics.Attach(toolStrip);

        /// <summary>Marks a toolbar button as the primary (accent-filled) action.</summary>
        public static void SetPrimary(ToolStripItem item, bool primary = true)
        {
            PrimaryItems.Remove(item);
            if (primary)
                PrimaryItems.Add(item, true);
            item.Invalidate();
        }

        public static bool IsPrimary(ToolStripItem? item) => item != null && PrimaryItems.TryGetValue(item, out _);

        private static ModernPalette Palette => ModernPalette.Current;

        private static readonly Lazy<FontFamily> IconFontFamily = new(() => new FontFamily(FluentIcons.FontFamily));

        /// <summary>
        /// Draws an icon-font glyph with its ink optically centered in <paramref name="bounds"/> (the icon fonts'
        /// asymmetric ascent/descent otherwise renders glyphs a few pixels low next to text).
        /// </summary>
        public static void DrawGlyph(Graphics g, char glyph, RectangleF bounds, Color color, float emSize)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0 || emSize <= 0)
                return;

            using GraphicsPath path = new();
            path.AddString(glyph.ToString(), IconFontFamily.Value, (int)FontStyle.Regular, emSize, PointF.Empty, StringFormat.GenericTypographic);
            RectangleF ink = path.GetBounds();
            if (ink.Width <= 0 || ink.Height <= 0)
                return;

            // Center the em box horizontally and the ink vertically, snapped to whole pixels for crispness.
            float dx = (float)Math.Round(bounds.X + (bounds.Width - ink.Width) / 2f - ink.X);
            float dy = (float)Math.Round(bounds.Y + (bounds.Height - ink.Height) / 2f - ink.Y);
            using Matrix matrix = new();
            matrix.Translate(dx, dy);
            path.Transform(matrix);

            SmoothingMode previous = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using SolidBrush brush = new(color);
            g.FillPath(brush, path);
            g.SmoothingMode = previous;
        }

        private static int Scale(ToolStrip? toolStrip, int logical) =>
            (int)Math.Round(logical * (toolStrip?.DeviceDpi ?? 96) / 96f);

        private static int Scale(ToolStripItem item, int logical) => Scale(item.GetCurrentParent() ?? item.Owner, logical);

        /// <summary>Background of popup menus.</summary>
        internal static Color PopupBackground(ModernPalette palette) => palette.Surface;

        /// <summary>Hover highlight inside popup menus (needs more contrast than on the chrome).</summary>
        internal static Color PopupHover(ModernPalette palette) =>
            palette.IsDark ? palette.SurfacePressed : palette.SurfaceHover;

        /// <summary>Background a non-popup strip blends into.</summary>
        internal static Color StripBackground(ToolStrip toolStrip)
        {
            ModernPalette palette = Palette;
            if (toolStrip is StatusStrip || toolStrip.Parent is ToolStripPanel || toolStrip.Parent is Form || toolStrip.Parent == null)
                return palette.Chrome;

            Color parentBack = toolStrip.Parent.BackColor;
            return parentBack.A == 255 ? parentBack : palette.Panel;
        }

        #region Initialization

        protected override void Initialize(ToolStrip toolStrip)
        {
            base.Initialize(toolStrip);
            ToolStripMetrics.Attach(toolStrip);
        }

        #endregion

        #region Backgrounds and borders

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            ModernPalette palette = Palette;
            if (e.ToolStrip is ToolStripDropDown dropDown)
            {
                e.Graphics.Clear(PopupBackground(palette));
                EnsurePopupChrome(dropDown, palette);
                return;
            }

            using SolidBrush brush = new(StripBackground(e.ToolStrip));
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            ModernPalette palette = Palette;
            if (e.ToolStrip is ToolStripDropDown dropDown)
            {
                // On Windows 11 DWM draws the rounded border (see EnsurePopupChrome).
                if (IsWindows11 && PopupChromeApplied.TryGetValue(dropDown, out _))
                    return;

                using Pen pen = new(palette.Border);
                Rectangle bounds = new(Point.Empty, e.ToolStrip.Size);
                bounds.Width -= 1;
                bounds.Height -= 1;
                e.Graphics.DrawRectangle(pen, bounds);
                return;
            }

            if (e.ToolStrip is StatusStrip)
            {
                using Pen pen = new(palette.BorderSubtle);
                e.Graphics.DrawLine(pen, 0, 0, e.ToolStrip.Width, 0);
            }
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            // Flat popup: no image margin band.
        }

        protected override void OnRenderToolStripPanelBackground(ToolStripPanelRenderEventArgs e)
        {
            using SolidBrush brush = new(Palette.Chrome);
            e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.ToolStripPanel.Size));
            e.Handled = true;
        }

        protected override void OnRenderGrip(ToolStripGripRenderEventArgs e)
        {
            // No grips / dotted handles.
        }

        protected override void OnRenderStatusStripSizingGrip(ToolStripRenderEventArgs e)
        {
        }

        protected override void OnRenderItemBackground(ToolStripItemRenderEventArgs e)
        {
        }

        protected override void OnRenderLabelBackground(ToolStripItemRenderEventArgs e)
        {
        }

        #endregion

        #region Item backgrounds

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            ToolStripItem item = e.Item;
            ModernPalette palette = Palette;

            if (item.IsOnDropDown)
            {
                if (!item.Selected)
                    return;

                Rectangle bounds = new(Point.Empty, item.Size);
                Color color = item.Enabled ? PopupHover(palette) : ModernGraphics.Blend(PopupBackground(palette), PopupHover(palette), 0.5f);
                ModernGraphics.FillRoundedRectangle(e.Graphics, color, bounds, Scale(item, 4));
                return;
            }

            if (!item.Enabled || (!item.Selected && !item.Pressed))
                return;

            Rectangle topBounds = new(Point.Empty, item.Size);
            Color topColor = item.Pressed ? palette.SurfacePressed : palette.SurfaceHover;
            ModernGraphics.FillRoundedRectangle(e.Graphics, topColor, topBounds, Scale(item, ModernPalette.CornerRadius));
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            ToolStripItem item = e.Item;
            bool isChecked = item is ToolStripButton { Checked: true };
            DrawButtonSurface(e.Graphics, item, new Rectangle(Point.Empty, item.Size), item.Selected, item.Pressed, isChecked);
        }

        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            ToolStripItem item = e.Item;
            DrawButtonSurface(e.Graphics, item, new Rectangle(Point.Empty, item.Size), item.Selected, item.Pressed, false);
        }

        protected override void OnRenderSplitButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item is not ToolStripSplitButton splitButton)
            {
                base.OnRenderSplitButtonBackground(e);
                return;
            }

            ModernPalette palette = Palette;
            Rectangle bounds = new(Point.Empty, splitButton.Size);
            bool primary = IsPrimary(splitButton);
            DrawButtonSurface(e.Graphics, splitButton, bounds, splitButton.Selected, splitButton.ButtonPressed, false);

            Rectangle dropDownBounds = splitButton.DropDownButtonBounds;
            if (!primary && splitButton.DropDownButtonPressed)
            {
                ModernGraphics.FillRoundedRectangle(e.Graphics, palette.SurfacePressed, dropDownBounds, Scale(splitButton, ModernPalette.CornerRadius));
            }

            if (splitButton.Selected || primary)
            {
                Color lineColor = primary ? ModernGraphics.Blend(palette.Accent, palette.AccentText, 0.35f) : palette.Border;
                using Pen pen = new(lineColor);
                int inset = Scale(splitButton, 6);
                e.Graphics.DrawLine(pen, dropDownBounds.Left, bounds.Top + inset, dropDownBounds.Left, bounds.Bottom - inset);
            }

            Color arrowColor = primary ? palette.AccentText : splitButton.Enabled ? palette.TextSecondary : palette.TextDisabled;
            DrawChevron(e.Graphics, ChevronDownSmallGlyph, dropDownBounds, arrowColor, Scale(splitButton, 10));
        }

        protected override void OnRenderOverflowButtonBackground(ToolStripItemRenderEventArgs e)
        {
            ToolStripItem item = e.Item;
            ModernPalette palette = Palette;
            Rectangle bounds = new(Point.Empty, item.Size);
            DrawButtonSurface(e.Graphics, item, bounds, item.Selected, item.Pressed, false);
            DrawChevron(e.Graphics, ChevronDownSmallGlyph, bounds, palette.TextSecondary, Scale(item, 10));
        }

        private static void DrawButtonSurface(Graphics g, ToolStripItem item, Rectangle bounds, bool selected, bool pressed, bool isChecked)
        {
            ModernPalette palette = Palette;
            int radius = Scale(item, ModernPalette.CornerRadius);
            Color? fill = null;

            if (IsPrimary(item))
            {
                if (!item.Enabled)
                    fill = ModernGraphics.Blend(palette.Accent, palette.Chrome, 0.55f);
                else if (pressed)
                    fill = palette.AccentPressed;
                else if (selected)
                    fill = palette.AccentHover;
                else
                    fill = palette.Accent;
            }
            else if (isChecked)
            {
                fill = selected ? ModernGraphics.Blend(palette.AccentSubtle, palette.Accent, 0.2f) : palette.AccentSubtle;
            }
            else if (item.Enabled && pressed)
            {
                fill = palette.SurfacePressed;
            }
            else if (item.Enabled && selected)
            {
                fill = palette.SurfaceHover;
            }

            if (fill.HasValue)
                ModernGraphics.FillRoundedRectangle(g, fill.Value, bounds, radius);
        }

        #endregion

        #region Separators, arrows, checks

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            ModernPalette palette = Palette;
            ToolStripItem item = e.Item;
            using Pen pen = new(palette.Border);

            if (e.Vertical)
            {
                int x = item.Width / 2;
                int inset = Scale(item, 8);
                e.Graphics.DrawLine(pen, x, inset, x, item.Height - inset);
            }
            else
            {
                int y = item.Height / 2;
                int inset = item.IsOnDropDown ? Scale(item, 8) : Scale(item, 4);
                e.Graphics.DrawLine(pen, inset, y, item.Width - inset, y);
            }
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            ModernPalette palette = Palette;
            ToolStripItem? item = e.Item;
            Color color = item == null || item.Enabled ? palette.TextSecondary : palette.TextDisabled;
            if (IsPrimary(item))
                color = palette.AccentText;

            char glyph = e.Direction switch
            {
                ArrowDirection.Right => ChevronRightSmallGlyph,
                ArrowDirection.Left => ChevronLeftSmallGlyph,
                ArrowDirection.Up => ChevronUpSmallGlyph,
                _ => ChevronDownSmallGlyph
            };

            int size = item != null ? Scale(item, 10) : 10;
            DrawChevron(e.Graphics, glyph, e.ArrowRectangle, color, size);
        }

        private static void DrawChevron(Graphics g, char glyph, Rectangle area, Color color, int emSize)
        {
            if (area.Width <= 0 || area.Height <= 0)
                return;

            int box = Math.Max(emSize + 4, Math.Min(area.Width, area.Height));
            Rectangle target = new(area.X + (area.Width - box) / 2, area.Y + (area.Height - box) / 2, box, box);
            DrawGlyph(g, glyph, target, color, emSize);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            ModernPalette palette = Palette;
            ToolStripItem item = e.Item;
            Rectangle area = e.ImageRectangle;
            if (area.Width <= 0 || area.Height <= 0)
                return;

            bool hasImage = item.Image != null && item.DisplayStyle is ToolStripItemDisplayStyle.Image or ToolStripItemDisplayStyle.ImageAndText
                            || ToolStripGlyphs.TryGet(item, out _);
            if (hasImage)
            {
                // Checked item with an icon: tinted rounded plate behind the icon.
                Rectangle plate = Rectangle.Inflate(area, Scale(item, 3), Scale(item, 3));
                ModernGraphics.FillRoundedRectangle(e.Graphics, palette.AccentSubtle, plate, Scale(item, 4));
                return;
            }

            Color color = !item.Enabled ? palette.TextDisabled : palette.IsDark ? palette.AccentHover : palette.Accent;
            int size = Math.Min(area.Width, area.Height);
            area.Offset(0, -Scale(item, 1));
            DrawGlyph(e.Graphics, CheckMarkGlyph, area, color, size * 0.85f);
        }

        #endregion

        #region Images and text

        protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
        {
            ToolStripItem item = e.Item;
            if (ToolStripGlyphs.TryGet(item, out ToolStripGlyphs.GlyphInfo glyph))
            {
                Rectangle area = e.ImageRectangle;
                if (area.Width <= 0 || area.Height <= 0)
                    return;

                Color color = GlyphColor(item, glyph.Role);
                int size = Math.Min(area.Width, area.Height);
                if (item.IsOnDropDown)
                    area.Offset(0, -Scale(item, 1));
                DrawGlyph(e.Graphics, glyph.Glyph, area, color, size);
                return;
            }

            base.OnRenderItemImage(e);
        }

        internal static Color GlyphColor(ToolStripItem item, ToolStripGlyphs.GlyphRole role)
        {
            ModernPalette palette = Palette;
            if (!item.Enabled)
                return IsPrimary(item) ? ModernGraphics.Blend(palette.AccentText, palette.Accent, 0.4f) : palette.TextDisabled;
            if (IsPrimary(item))
                return palette.AccentText;

            return role switch
            {
                ToolStripGlyphs.GlyphRole.Accent => palette.IsDark ? palette.AccentHover : palette.Accent,
                ToolStripGlyphs.GlyphRole.Success => palette.Success,
                ToolStripGlyphs.GlyphRole.Danger => palette.Danger,
                ToolStripGlyphs.GlyphRole.Secondary => palette.TextSecondary,
                _ => palette.Text
            };
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            ModernPalette palette = Palette;
            ToolStripItem item = e.Item;

            Color color;
            if (IsPrimary(item))
                color = item.Enabled ? palette.AccentText : ModernGraphics.Blend(palette.AccentText, palette.Accent, 0.4f);
            else if (!item.Enabled)
                color = palette.TextDisabled;
            else if (item is ToolStripMenuItem menuItem && menuItem.IsOnDropDown && !string.Equals(e.Text, menuItem.Text, StringComparison.Ordinal))
                color = palette.TextSecondary; // shortcut keys
            else if (item is ToolStripLabel and not { IsLink: true } && !item.IsOnDropDown)
                color = palette.TextSecondary;
            else
                color = palette.Text;

            e.TextColor = color;
            if (item.IsOnDropDown)
            {
                // Optical alignment with the icons: the line box centers the descenders, nudge the text down.
                Rectangle textRectangle = e.TextRectangle;
                textRectangle.Offset(0, Scale(item, 1));
                e.TextRectangle = textRectangle;
            }

            base.OnRenderItemText(e);
        }

        #endregion

        #region Popup chrome (Windows 11 rounded corners)

        private static void EnsurePopupChrome(ToolStripDropDown dropDown, ModernPalette palette)
        {
            if (!IsWindows11 || !dropDown.IsHandleCreated)
                return;

            int key = palette.Border.ToArgb() ^ dropDown.Handle.GetHashCode();
            if (PopupChromeApplied.TryGetValue(dropDown, out StrongBox<int>? applied) && applied.Value == key)
                return;

            try
            {
                int corner = DwmCornerRoundSmall;
                DwmSetWindowAttribute(dropDown.Handle, DwmWindowCornerPreference, ref corner, sizeof(int));
                int border = ColorTranslator.ToWin32(palette.Border);
                DwmSetWindowAttribute(dropDown.Handle, DwmBorderColor, ref border, sizeof(int));
                PopupChromeApplied.AddOrUpdate(dropDown, new StrongBox<int>(key));
            }
            catch (Exception)
            {
                // DWM not available: the flat border drawn in OnRenderToolStripBorder is used instead.
            }
        }

        private const int DwmWindowCornerPreference = 33;
        private const int DwmBorderColor = 34;
        private const int DwmCornerRoundSmall = 3;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        #endregion
    }

    /// <summary>Additional Segoe Fluent Icons / MDL2 code points used by the window chrome.</summary>
    [SupportedOSPlatform("windows")]
    public static class ChromeGlyphs
    {
        public const char List = '';
        public const char CheckMark = '';
        public const char ArrowUp = '';
        public const char ArrowDown = '';
        public const char ChevronUp = '';
        public const char ChevronDown = '';
        public const char NewFolder = '';
        public const char Stop = '';
        public const char Heart = '';
        public const char Bug = '';
        public const char Chat = '';
        public const char People = '';
        public const char Tiles = '';
        public const char Library = '';
        public const char Next = '';
        public const char Previous = '';
        public const char Admin = '';
        public const char Key = '';
        public const char Close = '';
        public const char Reconnect = '';
    }

    /// <summary>
    /// Professional color table backed by the modern palette, used for the few elements the base
    /// renderer still draws (tool strip text boxes, combo boxes...).
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal sealed class ModernColorTable : ProfessionalColorTable
    {
        public ModernColorTable()
        {
            UseSystemColors = false;
        }

        private static ModernPalette P => ModernPalette.Current;

        public override Color ButtonSelectedHighlight => P.SurfaceHover;
        public override Color ButtonSelectedHighlightBorder => P.SurfaceHover;
        public override Color ButtonPressedHighlight => P.SurfacePressed;
        public override Color ButtonPressedHighlightBorder => P.SurfacePressed;
        public override Color ButtonCheckedHighlight => P.AccentSubtle;
        public override Color ButtonCheckedHighlightBorder => P.AccentSubtle;
        public override Color ButtonPressedBorder => P.SurfacePressed;
        public override Color ButtonSelectedBorder => P.SurfaceHover;
        public override Color ButtonCheckedGradientBegin => P.AccentSubtle;
        public override Color ButtonCheckedGradientMiddle => P.AccentSubtle;
        public override Color ButtonCheckedGradientEnd => P.AccentSubtle;
        public override Color ButtonSelectedGradientBegin => P.SurfaceHover;
        public override Color ButtonSelectedGradientMiddle => P.SurfaceHover;
        public override Color ButtonSelectedGradientEnd => P.SurfaceHover;
        public override Color ButtonPressedGradientBegin => P.SurfacePressed;
        public override Color ButtonPressedGradientMiddle => P.SurfacePressed;
        public override Color ButtonPressedGradientEnd => P.SurfacePressed;
        public override Color CheckBackground => P.AccentSubtle;
        public override Color CheckSelectedBackground => P.AccentSubtle;
        public override Color CheckPressedBackground => P.AccentSubtle;
        public override Color GripDark => P.Chrome;
        public override Color GripLight => P.Chrome;
        public override Color ImageMarginGradientBegin => ModernToolStripRenderer.PopupBackground(P);
        public override Color ImageMarginGradientMiddle => ModernToolStripRenderer.PopupBackground(P);
        public override Color ImageMarginGradientEnd => ModernToolStripRenderer.PopupBackground(P);
        public override Color ImageMarginRevealedGradientBegin => ModernToolStripRenderer.PopupBackground(P);
        public override Color ImageMarginRevealedGradientMiddle => ModernToolStripRenderer.PopupBackground(P);
        public override Color ImageMarginRevealedGradientEnd => ModernToolStripRenderer.PopupBackground(P);
        public override Color MenuStripGradientBegin => P.Chrome;
        public override Color MenuStripGradientEnd => P.Chrome;
        public override Color MenuItemSelected => ModernToolStripRenderer.PopupHover(P);
        public override Color MenuItemBorder => ModernToolStripRenderer.PopupHover(P);
        public override Color MenuBorder => P.Border;
        public override Color MenuItemSelectedGradientBegin => P.SurfaceHover;
        public override Color MenuItemSelectedGradientEnd => P.SurfaceHover;
        public override Color MenuItemPressedGradientBegin => P.SurfacePressed;
        public override Color MenuItemPressedGradientMiddle => P.SurfacePressed;
        public override Color MenuItemPressedGradientEnd => P.SurfacePressed;
        public override Color RaftingContainerGradientBegin => P.Chrome;
        public override Color RaftingContainerGradientEnd => P.Chrome;
        public override Color SeparatorDark => P.Border;
        public override Color SeparatorLight => P.Border;
        public override Color StatusStripGradientBegin => P.Chrome;
        public override Color StatusStripGradientEnd => P.Chrome;
        public override Color ToolStripBorder => P.Chrome;
        public override Color ToolStripDropDownBackground => ModernToolStripRenderer.PopupBackground(P);
        public override Color ToolStripGradientBegin => P.Chrome;
        public override Color ToolStripGradientMiddle => P.Chrome;
        public override Color ToolStripGradientEnd => P.Chrome;
        public override Color ToolStripContentPanelGradientBegin => P.Content;
        public override Color ToolStripContentPanelGradientEnd => P.Content;
        public override Color ToolStripPanelGradientBegin => P.Chrome;
        public override Color ToolStripPanelGradientEnd => P.Chrome;
        public override Color OverflowButtonGradientBegin => P.Chrome;
        public override Color OverflowButtonGradientMiddle => P.Chrome;
        public override Color OverflowButtonGradientEnd => P.Chrome;
    }

    /// <summary>
    /// Gives menus generous, DPI-scaled item metrics (taller rows, padded popups) while the modern renderer
    /// is in use, and restores the original metrics when a legacy renderer takes over. Hooks dropdowns
    /// lazily so dynamically built menus are covered too.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class ToolStripMetrics
    {
        private sealed class Backup
        {
            public Padding Padding;
            public Padding Margin;
        }

        private static readonly ConditionalWeakTable<ToolStrip, object> AttachedStrips = new();
        private static readonly ConditionalWeakTable<ToolStripItem, object> HookedItems = new();
        private static readonly ConditionalWeakTable<ToolStripItem, Backup> ItemBackups = new();
        private static readonly ConditionalWeakTable<ToolStripDropDown, Backup> DropDownBackups = new();

        /// <summary>Logical height of a popup menu row.</summary>
        public const int MenuItemHeight = 30;

        public static void Attach(ToolStrip toolStrip)
        {
            if (AttachedStrips.TryGetValue(toolStrip, out _))
            {
                Refresh(toolStrip);
                return;
            }

            AttachedStrips.Add(toolStrip, true);

            if (toolStrip is ToolStripDropDown dropDown)
            {
                dropDown.Opening += (_, _) => RefreshDropDown(dropDown);
                dropDown.ItemAdded += (_, e) => OnDropDownItemAdded(dropDown, e.Item);
            }
            else
            {
                toolStrip.ItemAdded += (_, e) => HookItem(e.Item);
                toolStrip.RendererChanged += (_, _) => Refresh(toolStrip);
                toolStrip.DpiChangedAfterParent += (_, _) => Refresh(toolStrip);
            }

            Refresh(toolStrip);
        }

        private static bool UsesModernRenderer(ToolStrip toolStrip) => toolStrip.Renderer is ModernToolStripRenderer;

        private static void Refresh(ToolStrip toolStrip)
        {
            if (toolStrip is ToolStripDropDown dropDown)
            {
                RefreshDropDown(dropDown);
                return;
            }

            foreach (ToolStripItem item in toolStrip.Items)
                HookItem(item);
        }

        private static void HookItem(ToolStripItem item)
        {
            if (item is not ToolStripDropDownItem dropDownItem || HookedItems.TryGetValue(item, out _))
                return;

            HookedItems.Add(item, true);
            dropDownItem.DropDownOpening += (_, _) =>
            {
                if (dropDownItem.HasDropDown)
                    AttachDropDown(dropDownItem.DropDown);
            };
        }

        private static void AttachDropDown(ToolStripDropDown dropDown)
        {
            if (!AttachedStrips.TryGetValue(dropDown, out _))
            {
                AttachedStrips.Add(dropDown, true);
                dropDown.Opening += (_, _) => RefreshDropDown(dropDown);
                dropDown.ItemAdded += (_, e) => OnDropDownItemAdded(dropDown, e.Item);
            }

            RefreshDropDown(dropDown);
        }

        private static void OnDropDownItemAdded(ToolStripDropDown dropDown, ToolStripItem item)
        {
            HookItem(item);
            ApplyItemMetrics(item, UsesModernRenderer(dropDown), dropDown.DeviceDpi);
        }

        private static void RefreshDropDown(ToolStripDropDown dropDown)
        {
            bool modern = UsesModernRenderer(dropDown);
            int dpi = dropDown.DeviceDpi;
            int s(int v) => (int)Math.Round(v * dpi / 96f);

            if (dropDown is ToolStripDropDownMenu)
            {
                if (modern)
                {
                    if (!DropDownBackups.TryGetValue(dropDown, out _))
                        DropDownBackups.Add(dropDown, new Backup { Padding = dropDown.Padding });
                    Padding padding = new(s(4), s(4), s(4), s(4));
                    if (dropDown.Padding != padding)
                        dropDown.Padding = padding;
                }
                else if (DropDownBackups.TryGetValue(dropDown, out Backup? backup))
                {
                    dropDown.Padding = backup.Padding;
                    DropDownBackups.Remove(dropDown);
                }
            }

            dropDown.SuspendLayout();
            try
            {
                foreach (ToolStripItem item in dropDown.Items)
                {
                    HookItem(item);
                    ApplyItemMetrics(item, modern, dpi);
                }
            }
            finally
            {
                dropDown.ResumeLayout();
            }
        }

        private static void ApplyItemMetrics(ToolStripItem item, bool modern, int dpi)
        {
            if (!item.IsOnDropDown)
                return;

            int s(int v) => (int)Math.Round(v * dpi / 96f);

            if (!modern)
            {
                if (ItemBackups.TryGetValue(item, out Backup? backup))
                {
                    item.Padding = backup.Padding;
                    item.Margin = backup.Margin;
                    ItemBackups.Remove(item);
                }

                return;
            }

            if (!ItemBackups.TryGetValue(item, out _))
                ItemBackups.Add(item, new Backup { Padding = item.Padding, Margin = item.Margin });

            Padding newPadding;
            Padding newMargin;
            switch (item)
            {
                case ToolStripSeparator:
                    newPadding = Padding.Empty;
                    newMargin = new Padding(0, s(2), 0, s(2));
                    break;
                case ToolStripMenuItem:
                    // Default rows are ~22px with 1px vertical padding; grow them to MenuItemHeight.
                    int vertical = Math.Max(1, s((MenuItemHeight - 20) / 2));
                    newPadding = new Padding(0, vertical, s(8), vertical);
                    newMargin = Padding.Empty;
                    break;
                default:
                    return;
            }

            if (item.Padding != newPadding)
                item.Padding = newPadding;
            if (item.Margin != newMargin)
                item.Margin = newMargin;
        }
    }

    /// <summary>
    /// Associates Fluent icon glyphs with tool strip items. The modern renderer draws the glyph as vector
    /// text in the current theme color (enabled/disabled/primary aware); a pre-rendered bitmap is also
    /// assigned to <see cref="ToolStripItem.Image"/> for layout and for legacy renderers, and re-rendered
    /// whenever the theme changes.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class ToolStripGlyphs
    {
        public enum GlyphRole
        {
            Default,
            Secondary,
            Accent,
            Success,
            Danger
        }

        public readonly struct GlyphInfo(char glyph, GlyphRole role)
        {
            public char Glyph { get; } = glyph;
            public GlyphRole Role { get; } = role;
        }

        private static readonly ConditionalWeakTable<ToolStripItem, StrongBox<GlyphInfo>> Glyphs = new();
        private static bool _themeHooked;

        public static void Set(ToolStripItem item, char glyph, GlyphRole role = GlyphRole.Default)
        {
            if (item == null)
                return;

            HookTheme();
            Glyphs.AddOrUpdate(item, new StrongBox<GlyphInfo>(new GlyphInfo(glyph, role)));
            item.ImageScaling = ToolStripItemImageScaling.SizeToFit;
            item.Image = Render(glyph, role);
        }

        public static void Clear(ToolStripItem item)
        {
            if (item != null)
                Glyphs.Remove(item);
        }

        public static bool TryGet(ToolStripItem item, out GlyphInfo info)
        {
            if (item != null && Glyphs.TryGetValue(item, out StrongBox<GlyphInfo>? box))
            {
                info = box.Value;
                return true;
            }

            info = default;
            return false;
        }

        private static Bitmap Render(char glyph, GlyphRole role)
        {
            ModernPalette palette = ModernPalette.Current;
            Color color = role switch
            {
                GlyphRole.Accent => palette.Accent,
                GlyphRole.Success => palette.Success,
                GlyphRole.Danger => palette.Danger,
                GlyphRole.Secondary => palette.TextSecondary,
                _ => palette.Text
            };
            return FluentIcons.Get(glyph, SystemIconSize, color);
        }

        private static int SystemIconSize
        {
            get
            {
                try
                {
                    using Graphics g = Graphics.FromHwnd(IntPtr.Zero);
                    return (int)Math.Round(16 * g.DpiX / 96f);
                }
                catch (Exception)
                {
                    return 16;
                }
            }
        }

        private static void HookTheme()
        {
            if (_themeHooked)
                return;

            _themeHooked = true;
            ThemeManager.getInstance().ThemeChanged += RefreshImages;
        }

        private static void RefreshImages()
        {
            foreach (System.Collections.Generic.KeyValuePair<ToolStripItem, StrongBox<GlyphInfo>> entry in Glyphs)
            {
                ToolStripItem item = entry.Key;
                if (item.IsDisposed)
                    continue;

                item.Image = Render(entry.Value.Value.Glyph, entry.Value.Value.Role);
            }
        }
    }
}

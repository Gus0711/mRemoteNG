using System.ComponentModel;
using System.Drawing;
using System.Runtime.Versioning;
using BrightIdeasSoftware;
using mRemoteNG.Themes;

namespace mRemoteNG.UI.Controls
{
    [SupportedOSPlatform("windows")]
    //Simple coloring of ObjectListView
    //This is subclassed to avoid repeating the code in multiple places
    internal class MrngListView : ObjectListView
    {
        private CellBorderDecoration? deco;

        //Control if the gridlines are styled, must be set before the OnCreateControl is fired
        public bool DecorateLines { get; set; } = true;

        //Use the dialog background instead of the list background (e.g. navigation lists that blend with their form)
        [DefaultValue(false)]
        public bool UseDialogBackground { get; set; }

        public MrngListView()
        {
            InitializeComponent();
            ThemeManager.getInstance().ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged()
        {
            if (IsDisposed) return;
            ApplyTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ThemeManager.getInstance().ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            ThemeManager _themeManager = ThemeManager.getInstance();
            if (!_themeManager.ActiveAndExtended) return;
            ExtendedColorPalette palette = _themeManager.ActiveTheme.ExtendedPalette;
            bool modern = ModernControlColors.IsModernTheme(_themeManager.ActiveTheme);
            ModernControlColors colors = ModernControlColors.Current;

            //List back color
            BackColor = palette.getColor(UseDialogBackground ? "Dialog_Background" : "List_Background");
            ForeColor = palette.getColor("List_Item_Foreground");
            //Selected item
            SelectedBackColor = modern ? colors.Selection : palette.getColor("List_Item_Selected_Background");
            SelectedForeColor = modern ? colors.SelectionText : palette.getColor("List_Item_Selected_Foreground");
            UnfocusedSelectedBackColor = modern ? colors.Selection : SelectedBackColor;
            UnfocusedSelectedForeColor = modern ? colors.SelectionText : SelectedForeColor;

            //Header style: flat, secondary text (modern)
            HeaderUsesThemes = false;
            HeaderFormatStyle headerStylo = new()
            {
                Normal =
                {
                    BackColor = modern ? BackColor : palette.getColor("List_Header_Background"),
                    ForeColor = modern ? colors.TextSecondary : palette.getColor("List_Header_Foreground")
                },
                Hot =
                {
                    BackColor = modern ? colors.SurfaceHover : palette.getColor("List_Header_Background"),
                    ForeColor = modern ? colors.Text : palette.getColor("List_Header_Foreground")
                },
                Pressed =
                {
                    BackColor = modern ? colors.SurfacePressed : palette.getColor("List_Header_Background"),
                    ForeColor = modern ? colors.Text : palette.getColor("List_Header_Foreground")
                }
            };
            HeaderFormatStyle = headerStylo;

            //Border style: the modern themes use borderless rows, the legacy themes keep the cell grid
            FormatCell -= NGListView_FormatCell;
            if (DecorateLines && !modern)
            {
                UseCellFormatEvents = true;
                GridLines = false;
                deco = new CellBorderDecoration
                {
                    BorderPen = new Pen(palette.getColor("List_Item_Border")),
                    FillBrush = null,
                    BoundsPadding = Size.Empty,
                    CornerRounding = 0
                };
                FormatCell += NGListView_FormatCell;
            }
            else
            {
                deco = null;
                if (DecorateLines) GridLines = false;
            }

            if (Items != null && Items.Count != 0)
                BuildList();
            Invalidate();
        }

        private void NGListView_FormatCell(object? sender, FormatCellEventArgs e)
        {
            if (deco != null && e.Column.IsVisible)
            {
                e.SubItem.Decoration = deco;
            }
        }

        private void InitializeComponent()
        {
            ((ISupportInitialize)(this)).BeginInit();
            SuspendLayout();
            //
            // NGListView
            //
            Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ((ISupportInitialize)(this)).EndInit();
            ResumeLayout(false);
        }
    }
}

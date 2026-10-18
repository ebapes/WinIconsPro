using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WinIconsPro.Core;

namespace WinIconsPro.UI
{
    public enum AppTheme
    {
        Dark,
        Light
    }

    public enum AppLanguage
    {
        English,
        Russian
    }

    public class MainWindow : Window
    {
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private List<DesktopItem> _desktopItems = new List<DesktopItem>();
        private ProcessingOptions _options = new ProcessingOptions();
        private bool _isAdmin = false;
        private AppTheme _currentTheme = AppTheme.Dark;
        private AppLanguage _currentLanguage = AppLanguage.English;

        // Visual containers to update on theme change
        private Border _headerBorder;
        private Border _footerBorder;
        private Border _cardStyle;
        private Border _cardOptions;
        private Border _cardInfo;
        private Border _previewContainer;
        private Border _segmentedContainer;
        private Border _squircleRow;
        private TextBlock _titleText;
        private TextBlock _subText;
        private TextBlock _lblModes;
        private TextBlock _lblPalette;
        private TextBlock _lblContrast;
        private TextBlock _lblBrightness;
        private TextBlock _lblOptions;
        private TextBlock _squircleTitle;
        private TextBlock _squircleDesc;
        private TextBlock _infoTitle;
        private TextBlock _infoDesc;
        private TextBlock _previewHeader;
        private Button _btnGithub;
        private Button _btnThemeToggle;
        private Button _btnLangToggle;

        // Mode Segmented Buttons
        private Button _btnModeDarkTitanium;
        private Button _btnModeTint;
        private Button _btnModePureBw;
        private Button _btnModeInverted;

        // Color Palette Swatches
        private StackPanel _colorPaletteRow;
        private Border[] _swatchBorders;
        private TextBox _txtCustomHex;
        private Border _hexPreviewChip;

        // Squircle Toggle
        private Border _toggleSquircleContainer;
        private Border _toggleSquircleThumb;
        private bool _squircleEnabled = false;

        // Sliders
        private Slider _sliderContrast;
        private Slider _sliderBrightness;
        private TextBlock _lblContrastVal;
        private TextBlock _lblBrightnessVal;

        // Preview & Actions
        private WrapPanel _previewPanel;
        private TextBlock _txtStatus;
        private ProgressBar _progressBar;
        private Button _btnApply;
        private Button _btnRestore;

        // Preset Colors (Curated Duotone Tint Palette)
        private static readonly System.Drawing.Color[] PresetColors = new System.Drawing.Color[]
        {
            System.Drawing.Color.FromArgb(10, 132, 255),  // Electric Blue
            System.Drawing.Color.FromArgb(48, 209, 88),   // Emerald Mint
            System.Drawing.Color.FromArgb(255, 159, 10),  // Sunset Orange
            System.Drawing.Color.FromArgb(255, 55, 95),   // Neon Rose
            System.Drawing.Color.FromArgb(191, 90, 242),  // Electric Purple
            System.Drawing.Color.FromArgb(255, 214, 10),  // Cyber Gold
            System.Drawing.Color.FromArgb(0, 242, 254),   // Ice Cyan
            System.Drawing.Color.FromArgb(242, 242, 247)  // Platinum White
        };

        public MainWindow()
        {
            _isAdmin = CheckIsAdmin();
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private bool CheckIsAdmin()
        {
            try
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(id);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDesktopItems();
        }

        private void InitializeComponent()
        {
            Title = "WinIcons Pro";
            Width = 940;
            Height = 780;
            MinWidth = 860;
            MinHeight = 660;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable Display, Segoe UI, sans-serif");

            // Set app icon
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "app_icon.png");
                if (File.Exists(iconPath))
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(iconPath, UriKind.Absolute);
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    bi.Freeze();
                    Icon = bi;
                }
            }
            catch { }

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Controls
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Preview Grid
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // ==========================================
            // 1. HEADER
            // ==========================================
            _headerBorder = new Border
            {
                Padding = new Thickness(24, 16, 24, 16),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // App Icon
            Border iconContainer = new Border
            {
                Width = 46,
                Height = 46,
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 0, 14, 0),
                ClipToBounds = true
            };
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "app_icon.png");
                if (File.Exists(iconPath))
                {
                    System.Windows.Controls.Image headerImg = new System.Windows.Controls.Image
                    {
                        Source = new BitmapImage(new Uri(iconPath, UriKind.Absolute)),
                        Stretch = Stretch.UniformToFill
                    };
                    iconContainer.Child = headerImg;
                }
            }
            catch { }
            Grid.SetColumn(iconContainer, 0);
            headerGrid.Children.Add(iconContainer);

            // Title panel
            StackPanel titlePanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            StackPanel titleRow = new StackPanel { Orientation = Orientation.Horizontal };
            _titleText = new TextBlock
            {
                Text = "WinIcons",
                FontSize = 20,
                FontWeight = FontWeights.Bold
            };
            Border proBadge = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 99, 235)),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(7, 2, 7, 2),
                Margin = new Thickness(8, 2, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            proBadge.Child = new TextBlock
            {
                Text = "PRO",
                FontSize = 10,
                FontWeight = FontWeights.ExtraBold,
                Foreground = System.Windows.Media.Brushes.White
            };
            titleRow.Children.Add(_titleText);
            titleRow.Children.Add(proBadge);

            _subText = new TextBlock
            {
                Text = "Modern Minimalist Desktop Icon Styling for Windows",
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 0)
            };
            titlePanel.Children.Add(titleRow);
            titlePanel.Children.Add(_subText);
            Grid.SetColumn(titlePanel, 1);
            headerGrid.Children.Add(titlePanel);

            // Right header controls: Language + Theme + GitHub Link
            StackPanel headerControls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // Language Toggle Button
            _btnLangToggle = new Button
            {
                Content = "🌐 English",
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 0),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            _btnLangToggle.Click += (s, e) => ToggleLanguage();
            headerControls.Children.Add(_btnLangToggle);

            // Theme Toggle Button
            _btnThemeToggle = new Button
            {
                Content = "🌙 Dark",
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 0),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            _btnThemeToggle.Click += (s, e) => ToggleTheme();
            headerControls.Children.Add(_btnThemeToggle);

            // GitHub Profile Link Button
            _btnGithub = new Button
            {
                Content = "GitHub",
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Padding = new Thickness(12, 6, 12, 6),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            _btnGithub.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo("https://github.com/ebapes") { UseShellExecute = true });
                }
                catch { }
            };
            headerControls.Children.Add(_btnGithub);

            Grid.SetColumn(headerControls, 2);
            headerGrid.Children.Add(headerControls);

            _headerBorder.Child = headerGrid;
            Grid.SetRow(_headerBorder, 0);
            root.Children.Add(_headerBorder);

            // ==========================================
            // 2. CONTROLS CARDS
            // ==========================================
            Grid cardsGrid = new Grid { Margin = new Thickness(24, 14, 24, 10) };
            cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.35, GridUnitType.Star) });
            cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

            // Card 1: Style & Palette
            _cardStyle = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 10, 0)
            };

            StackPanel styleContent = new StackPanel();

            _lblModes = new TextBlock
            {
                Text = "ICON COLOR & STYLE",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            styleContent.Children.Add(_lblModes);

            _segmentedContainer = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(3),
                Margin = new Thickness(0, 0, 0, 12)
            };

            UniformGrid segGrid = new UniformGrid { Columns = 4 };
            _btnModeDarkTitanium = CreateSegmentButton("Dark Titanium", true);
            _btnModeTint = CreateSegmentButton("Tinted", false);
            _btnModePureBw = CreateSegmentButton("Pure B&W", false);
            _btnModeInverted = CreateSegmentButton("Inverted", false);

            _btnModeDarkTitanium.Click += (s, e) => SelectMode(MonochromeMode.DarkTitanium);
            _btnModeTint.Click += (s, e) => SelectMode(MonochromeMode.ColorTint);
            _btnModePureBw.Click += (s, e) => SelectMode(MonochromeMode.PureBlackWhite);
            _btnModeInverted.Click += (s, e) => SelectMode(MonochromeMode.Inverted);

            segGrid.Children.Add(_btnModeDarkTitanium);
            segGrid.Children.Add(_btnModeTint);
            segGrid.Children.Add(_btnModePureBw);
            segGrid.Children.Add(_btnModeInverted);
            _segmentedContainer.Child = segGrid;
            styleContent.Children.Add(_segmentedContainer);

            // Palette Swatches
            _colorPaletteRow = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 0, 12),
                Visibility = Visibility.Collapsed
            };

            _lblPalette = new TextBlock
            {
                Text = "COLOR ACCENT (DUOTONE TINT)",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            _colorPaletteRow.Children.Add(_lblPalette);

            StackPanel swatchesWrap = new StackPanel { Orientation = Orientation.Horizontal };
            _swatchBorders = new Border[PresetColors.Length];

            for (int i = 0; i < PresetColors.Length; i++)
            {
                System.Drawing.Color c = PresetColors[i];
                int index = i;

                Border swatch = new Border
                {
                    Width = 26,
                    Height = 26,
                    CornerRadius = new CornerRadius(13),
                    Margin = new Thickness(0, 0, 7, 0),
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(c.R, c.G, c.B)),
                    BorderBrush = new SolidColorBrush(i == 0 ? System.Windows.Media.Colors.White : System.Windows.Media.Color.FromArgb(60, 0, 0, 0)),
                    BorderThickness = new Thickness(i == 0 ? 2 : 1),
                    Cursor = Cursors.Hand
                };

                swatch.MouseDown += (s, e) =>
                {
                    _options.TintColor = c;
                    UpdateSwatchSelection(index);
                    _txtCustomHex.Text = string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
                    _hexPreviewChip.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(c.R, c.G, c.B));
                    UpdateLivePreviews();
                };

                _swatchBorders[i] = swatch;
                swatchesWrap.Children.Add(swatch);
            }

            // Custom Hex input
            _hexPreviewChip = new Border
            {
                Width = 14,
                Height = 14,
                CornerRadius = new CornerRadius(7),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 132, 255)),
                Margin = new Thickness(6, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            swatchesWrap.Children.Add(_hexPreviewChip);

            _txtCustomHex = new TextBox
            {
                Text = "#0A84FF",
                Width = 70,
                Height = 26,
                BorderThickness = new Thickness(1),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            _txtCustomHex.TextChanged += (s, e) =>
            {
                string text = _txtCustomHex.Text.Trim();
                if (text.StartsWith("#")) text = text.Substring(1);
                if (text.Length == 6)
                {
                    try
                    {
                        int rgb = Convert.ToInt32(text, 16);
                        System.Drawing.Color parsed = System.Drawing.Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
                        _options.TintColor = parsed;
                        _hexPreviewChip.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(parsed.R, parsed.G, parsed.B));
                        UpdateLivePreviews();
                    }
                    catch { }
                }
            };
            swatchesWrap.Children.Add(_txtCustomHex);

            _colorPaletteRow.Children.Add(swatchesWrap);
            styleContent.Children.Add(_colorPaletteRow);

            // Sliders
            Grid slidersGrid = new Grid();
            slidersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            slidersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Contrast
            StackPanel cCol = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            StackPanel cRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 2) };
            _lblContrast = new TextBlock { Text = "Contrast: ", FontSize = 11 };
            _lblContrastVal = new TextBlock { Text = "1.35x", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(96, 165, 250)) };
            cRow.Children.Add(_lblContrast);
            cRow.Children.Add(_lblContrastVal);
            cCol.Children.Add(cRow);

            _sliderContrast = new Slider
            {
                Minimum = 0.8,
                Maximum = 2.2,
                Value = 1.35,
                SmallChange = 0.05,
                Cursor = Cursors.Hand
            };
            _sliderContrast.ValueChanged += (s, e) =>
            {
                _options.Contrast = (float)_sliderContrast.Value;
                if (_lblContrastVal != null) _lblContrastVal.Text = string.Format("{0:0.00}x", _sliderContrast.Value);
                UpdateLivePreviews();
            };
            cCol.Children.Add(_sliderContrast);
            Grid.SetColumn(cCol, 0);
            slidersGrid.Children.Add(cCol);

            // Brightness
            StackPanel bCol = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            StackPanel bRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 2) };
            _lblBrightness = new TextBlock { Text = "Brightness: ", FontSize = 11 };
            _lblBrightnessVal = new TextBlock { Text = "0.00", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(96, 165, 250)) };
            bRow.Children.Add(_lblBrightness);
            bRow.Children.Add(_lblBrightnessVal);
            bCol.Children.Add(bRow);

            _sliderBrightness = new Slider
            {
                Minimum = -0.3,
                Maximum = 0.3,
                Value = 0.0,
                SmallChange = 0.05,
                Cursor = Cursors.Hand
            };
            _sliderBrightness.ValueChanged += (s, e) =>
            {
                _options.Brightness = (float)_sliderBrightness.Value;
                if (_lblBrightnessVal != null) _lblBrightnessVal.Text = string.Format("{0:+0.00;-0.00;0.00}", _sliderBrightness.Value);
                UpdateLivePreviews();
            };
            bCol.Children.Add(_sliderBrightness);
            Grid.SetColumn(bCol, 1);
            slidersGrid.Children.Add(bCol);

            styleContent.Children.Add(slidersGrid);
            _cardStyle.Child = styleContent;
            Grid.SetColumn(_cardStyle, 0);
            cardsGrid.Children.Add(_cardStyle);

            // Card 2: Framing & Isolation
            _cardOptions = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16),
                Margin = new Thickness(10, 0, 0, 0)
            };

            StackPanel optionsContent = new StackPanel();

            _lblOptions = new TextBlock
            {
                Text = "FRAMING & ISOLATION",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            optionsContent.Children.Add(_lblOptions);

            // Squircle Badge Toggle
            _squircleRow = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10),
                Cursor = Cursors.Hand
            };

            Grid sqGrid = new Grid();
            sqGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sqGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel sqInfo = new StackPanel();
            _squircleTitle = new TextBlock
            {
                Text = "Squircle Badges",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };
            _squircleDesc = new TextBlock
            {
                Text = "Wraps desktop icons in a rounded frosted-glass squircle container.",
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            };
            sqInfo.Children.Add(_squircleTitle);
            sqInfo.Children.Add(_squircleDesc);
            Grid.SetColumn(sqInfo, 0);
            sqGrid.Children.Add(sqInfo);

            _toggleSquircleContainer = CreateToggleSwitch(out _toggleSquircleThumb);
            Grid.SetColumn(_toggleSquircleContainer, 1);
            sqGrid.Children.Add(_toggleSquircleContainer);

            _squircleRow.Child = sqGrid;
            _squircleRow.MouseDown += (s, e) =>
            {
                _squircleEnabled = !_squircleEnabled;
                _options.UseSquircle = _squircleEnabled;
                UpdateToggleVisual(_toggleSquircleContainer, _toggleSquircleThumb, _squircleEnabled);
                UpdateLivePreviews();
            };
            optionsContent.Children.Add(_squircleRow);

            // Information Box (Desktop-only guarantee)
            _cardInfo = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10, 12, 10)
            };

            StackPanel infoSp = new StackPanel();
            _infoTitle = new TextBlock
            {
                Text = "🛡️ Desktop-Only Scope",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };
            _infoDesc = new TextBlock
            {
                Text = "Changes apply exclusively to your Desktop shortcuts. Taskbar, Start Menu, and Explorer windows remain untouched.",
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 0)
            };
            infoSp.Children.Add(_infoTitle);
            infoSp.Children.Add(_infoDesc);
            _cardInfo.Child = infoSp;
            optionsContent.Children.Add(_cardInfo);

            _cardOptions.Child = optionsContent;
            Grid.SetColumn(_cardOptions, 1);
            cardsGrid.Children.Add(_cardOptions);

            Grid.SetRow(cardsGrid, 1);
            root.Children.Add(cardsGrid);

            // ==========================================
            // 3. PREVIEW GRID
            // ==========================================
            _previewContainer = new Border
            {
                Margin = new Thickness(24, 4, 24, 12),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16)
            };

            Grid previewInnerGrid = new Grid();
            previewInnerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            previewInnerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            _previewHeader = new TextBlock
            {
                Text = "LIVE DESKTOP ICON PREVIEW",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(4, 0, 0, 12)
            };
            Grid.SetRow(_previewHeader, 0);
            previewInnerGrid.Children.Add(_previewHeader);

            ScrollViewer scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            _previewPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            scrollViewer.Content = _previewPanel;
            Grid.SetRow(scrollViewer, 1);
            previewInnerGrid.Children.Add(scrollViewer);

            _previewContainer.Child = previewInnerGrid;
            Grid.SetRow(_previewContainer, 2);
            root.Children.Add(_previewContainer);

            // ==========================================
            // 4. FOOTER & ACTIONS
            // ==========================================
            _footerBorder = new Border
            {
                Padding = new Thickness(24, 14, 24, 16),
                BorderThickness = new Thickness(0, 1, 0, 0)
            };

            Grid footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Status message & progress
            StackPanel statusPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _txtStatus = new TextBlock
            {
                Text = "Scanning desktop items...",
                FontSize = 12
            };
            _progressBar = new ProgressBar
            {
                Height = 3,
                Width = 260,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 6, 0, 0),
                Visibility = Visibility.Collapsed,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 99, 235))
            };
            statusPanel.Children.Add(_txtStatus);
            statusPanel.Children.Add(_progressBar);
            Grid.SetColumn(statusPanel, 0);
            footerGrid.Children.Add(statusPanel);

            // Action Buttons
            StackPanel actionButtons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            _btnRestore = new Button
            {
                Content = "↩ Restore Defaults",
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 0, 10, 0),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            _btnRestore.Click += async (s, e) => await RestoreOriginalsAsync();

            _btnApply = new Button
            {
                Content = "✨ Apply to Desktop",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(22, 9, 22, 9),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 99, 235)),
                BorderThickness = new Thickness(0),
                Foreground = System.Windows.Media.Brushes.White,
                Cursor = Cursors.Hand
            };
            _btnApply.Click += async (s, e) => await ApplyMonochromeAsync();

            actionButtons.Children.Add(_btnRestore);
            actionButtons.Children.Add(_btnApply);
            Grid.SetColumn(actionButtons, 1);
            footerGrid.Children.Add(actionButtons);

            _footerBorder.Child = footerGrid;
            Grid.SetRow(_footerBorder, 3);
            root.Children.Add(_footerBorder);

            Content = root;
            ApplyTheme(_currentTheme);
            ApplyLanguage(_currentLanguage);
        }

        private void ToggleLanguage()
        {
            _currentLanguage = (_currentLanguage == AppLanguage.English) ? AppLanguage.Russian : AppLanguage.English;
            ApplyLanguage(_currentLanguage);
            UpdateLivePreviews();
        }

        private void ApplyLanguage(AppLanguage lang)
        {
            if (lang == AppLanguage.English)
            {
                _btnLangToggle.Content = "🌐 English";
                _subText.Text = "Modern Minimalist Desktop Icon Styling for Windows";
                _btnThemeToggle.Content = (_currentTheme == AppTheme.Dark) ? "🌙 Dark" : "☀️ Light";

                _lblModes.Text = "ICON COLOR & STYLE";
                _btnModeDarkTitanium.Content = "Dark Titanium";
                _btnModeTint.Content = "Tinted";
                _btnModePureBw.Content = "Pure B&W";
                _btnModeInverted.Content = "Inverted";

                _lblPalette.Text = "COLOR ACCENT (DUOTONE TINT)";
                _lblContrast.Text = "Contrast: ";
                _lblBrightness.Text = "Brightness: ";

                _lblOptions.Text = "FRAMING & ISOLATION";
                _squircleTitle.Text = "Squircle Badges";
                _squircleDesc.Text = "Wraps desktop icons in a rounded frosted-glass squircle container.";

                _infoTitle.Text = "🛡️ Desktop-Only Scope";
                _infoDesc.Text = "Changes apply exclusively to your Desktop shortcuts. Taskbar, Start Menu, and Explorer windows remain untouched.";

                _previewHeader.Text = "LIVE DESKTOP ICON PREVIEW";
                _btnRestore.Content = "↩ Restore Defaults";
                _btnApply.Content = "✨ Apply to Desktop";

                _txtStatus.Text = string.Format("Ready. Found {0} desktop items.", _desktopItems.Count);
            }
            else
            {
                _btnLangToggle.Content = "🌐 Русский";
                _subText.Text = "Стилизация значков рабочего стола в стиле минимализма и тёмного титана";
                _btnThemeToggle.Content = (_currentTheme == AppTheme.Dark) ? "🌙 Тёмная" : "☀️ Светлая";

                _lblModes.Text = "РЕЖИМ ОФОРМЛЕНИЯ ЗНАЧКОВ";
                _btnModeDarkTitanium.Content = "Тёмный титан";
                _btnModeTint.Content = "Палитра";
                _btnModePureBw.Content = "Чистый ч/б";
                _btnModeInverted.Content = "Инверсия";

                _lblPalette.Text = "ЦВЕТОВОЙ АКЦЕНТ (DUOTONE TINT)";
                _lblContrast.Text = "Контраст: ";
                _lblBrightness.Text = "Яркость: ";

                _lblOptions.Text = "ОФОРМЛЕНИЕ И ПАРАМЕТРЫ";
                _squircleTitle.Text = "Подложка Squircle";
                _squircleDesc.Text = "Помещает значки в скруглённую рамку из тёмного матового стекла.";

                _infoTitle.Text = "🛡️ Изоляция рабочего стола";
                _infoDesc.Text = "Изменяются только ярлыки на рабочем столе. Панель задач, Пуск и Проводник остаются нетронутыми.";

                _previewHeader.Text = "ЖИВОЙ ПРЕДПРОСМОТР ЗНАЧКОВ РАБОЧЕГО СТОЛА";
                _btnRestore.Content = "↩ Восстановить стандартные";
                _btnApply.Content = "✨ Применить к рабочему столу";

                _txtStatus.Text = string.Format("Готово. Обнаружено значков: {0}.", _desktopItems.Count);
            }
        }

        private void ToggleTheme()
        {
            _currentTheme = (_currentTheme == AppTheme.Dark) ? AppTheme.Light : AppTheme.Dark;
            ApplyTheme(_currentTheme);
            ApplyLanguage(_currentLanguage);
            UpdateLivePreviews();
        }

        private void ApplyTheme(AppTheme theme)
        {
            if (theme == AppTheme.Dark)
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 16, 20));
                _headerBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(240, 22, 23, 29));
                _headerBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255));

                _footerBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(245, 22, 23, 29));
                _footerBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255));

                _cardStyle.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 25, 31));
                _cardStyle.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(35, 255, 255, 255));

                _cardOptions.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 25, 31));
                _cardOptions.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(35, 255, 255, 255));

                _squircleRow.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(18, 19, 24));
                _squircleRow.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 255, 255, 255));

                _cardInfo.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(18, 19, 24));
                _cardInfo.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 255, 255, 255));

                _previewContainer.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 21, 26));
                _previewContainer.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 255, 255, 255));

                _segmentedContainer.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 17, 21));
                _segmentedContainer.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 255, 255, 255));

                _titleText.Foreground = System.Windows.Media.Brushes.White;
                _subText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
                _lblModes.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
                _lblPalette.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
                _lblContrast.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
                _lblBrightness.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
                _lblOptions.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
                _squircleTitle.Foreground = System.Windows.Media.Brushes.White;
                _squircleDesc.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(140, 140, 150));
                _infoTitle.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(147, 197, 253));
                _infoDesc.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
                _previewHeader.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
                _txtStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 213, 219));

                _txtCustomHex.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 32, 38));
                _txtCustomHex.Foreground = System.Windows.Media.Brushes.White;
                _txtCustomHex.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255));

                _btnLangToggle.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 31, 38));
                _btnLangToggle.Foreground = System.Windows.Media.Brushes.White;
                _btnLangToggle.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255));

                _btnThemeToggle.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 31, 38));
                _btnThemeToggle.Foreground = System.Windows.Media.Brushes.White;
                _btnThemeToggle.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255));

                _btnGithub.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 31, 38));
                _btnGithub.Foreground = System.Windows.Media.Brushes.White;
                _btnGithub.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255));

                _btnRestore.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 39, 46));
                _btnRestore.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 245));
                _btnRestore.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 255, 255));
            }
            else
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 244, 246));
                _headerBorder.Background = System.Windows.Media.Brushes.White;
                _headerBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235));

                _footerBorder.Background = System.Windows.Media.Brushes.White;
                _footerBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235));

                _cardStyle.Background = System.Windows.Media.Brushes.White;
                _cardStyle.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235));

                _cardOptions.Background = System.Windows.Media.Brushes.White;
                _cardOptions.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235));

                _squircleRow.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 250, 251));
                _squircleRow.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235));

                _cardInfo.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 249, 255));
                _cardInfo.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(186, 230, 253));

                _previewContainer.Background = System.Windows.Media.Brushes.White;
                _previewContainer.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235));

                _segmentedContainer.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 244, 246));
                _segmentedContainer.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235));

                _titleText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 24, 39));
                _subText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128));
                _lblModes.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128));
                _lblPalette.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128));
                _lblContrast.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128));
                _lblBrightness.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128));
                _lblOptions.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128));
                _squircleTitle.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 24, 39));
                _squircleDesc.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128));
                _infoTitle.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(3, 105, 161));
                _infoDesc.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(14, 116, 144));
                _previewHeader.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128));
                _txtStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(55, 65, 81));

                _txtCustomHex.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 244, 246));
                _txtCustomHex.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 24, 39));
                _txtCustomHex.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 213, 219));

                _btnLangToggle.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 244, 246));
                _btnLangToggle.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 24, 39));
                _btnLangToggle.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 213, 219));

                _btnThemeToggle.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 244, 246));
                _btnThemeToggle.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 24, 39));
                _btnThemeToggle.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 213, 219));

                _btnGithub.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 244, 246));
                _btnGithub.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 24, 39));
                _btnGithub.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 213, 219));

                _btnRestore.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 244, 246));
                _btnRestore.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55));
                _btnRestore.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 213, 219));
            }

            SelectMode(_options.Mode);
        }

        private Border CreateToggleSwitch(out Border thumb)
        {
            Border container = new Border
            {
                Width = 42,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 42, 50)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center
            };

            thumb = new Border
            {
                Width = 18,
                Height = 18,
                CornerRadius = new CornerRadius(9),
                Background = System.Windows.Media.Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(2, 0, 0, 0)
            };

            container.Child = thumb;
            return container;
        }

        private void UpdateToggleVisual(Border container, Border thumb, bool isActive)
        {
            if (isActive)
            {
                container.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 99, 235));
                thumb.HorizontalAlignment = HorizontalAlignment.Right;
                thumb.Margin = new Thickness(0, 0, 2, 0);
            }
            else
            {
                container.Background = new SolidColorBrush(_currentTheme == AppTheme.Dark
                    ? System.Windows.Media.Color.FromRgb(40, 42, 50)
                    : System.Windows.Media.Color.FromRgb(209, 213, 219));
                thumb.HorizontalAlignment = HorizontalAlignment.Left;
                thumb.Margin = new Thickness(2, 0, 0, 0);
            }
        }

        private Button CreateSegmentButton(string text, bool isActive)
        {
            Button btn = new Button
            {
                Content = text,
                FontSize = 11,
                FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal,
                Height = 28,
                Padding = new Thickness(6, 2, 6, 2),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            return btn;
        }

        private void SelectMode(MonochromeMode mode)
        {
            _options.Mode = mode;

            SetSegmentState(_btnModeDarkTitanium, mode == MonochromeMode.DarkTitanium);
            SetSegmentState(_btnModeTint, mode == MonochromeMode.ColorTint);
            SetSegmentState(_btnModePureBw, mode == MonochromeMode.PureBlackWhite);
            SetSegmentState(_btnModeInverted, mode == MonochromeMode.Inverted);

            _colorPaletteRow.Visibility = (mode == MonochromeMode.ColorTint) ? Visibility.Visible : Visibility.Collapsed;
            UpdateLivePreviews();
        }

        private void SetSegmentState(Button btn, bool active)
        {
            btn.FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal;
            if (_currentTheme == AppTheme.Dark)
            {
                btn.Background = new SolidColorBrush(active ? System.Windows.Media.Color.FromRgb(45, 47, 56) : System.Windows.Media.Colors.Transparent);
                btn.Foreground = new SolidColorBrush(active ? System.Windows.Media.Colors.White : System.Windows.Media.Color.FromRgb(156, 163, 175));
            }
            else
            {
                btn.Background = new SolidColorBrush(active ? System.Windows.Media.Colors.White : System.Windows.Media.Colors.Transparent);
                btn.Foreground = new SolidColorBrush(active ? System.Windows.Media.Color.FromRgb(17, 24, 39) : System.Windows.Media.Color.FromRgb(107, 114, 128));
            }
        }

        private void UpdateSwatchSelection(int selectedIndex)
        {
            for (int i = 0; i < _swatchBorders.Length; i++)
            {
                _swatchBorders[i].BorderBrush = (i == selectedIndex)
                    ? System.Windows.Media.Brushes.White
                    : new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 0, 0, 0));
                _swatchBorders[i].BorderThickness = new Thickness(i == selectedIndex ? 2 : 1);
            }
        }

        private void LoadDesktopItems()
        {
            _txtStatus.Text = (_currentLanguage == AppLanguage.English)
                ? "Scanning desktop shortcuts..."
                : "Сканирование рабочего стола...";
            _progressBar.Visibility = Visibility.Visible;
            _progressBar.IsIndeterminate = true;

            Task.Factory.StartNew(new Func<List<DesktopItem>>(() =>
            {
                return DesktopScanner.ScanDesktopItems();
            })).ContinueWith(new Action<Task<List<DesktopItem>>>(t =>
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    _progressBar.Visibility = Visibility.Collapsed;
                    _progressBar.IsIndeterminate = false;

                    if (t.IsFaulted)
                    {
                        string err = t.Exception != null && t.Exception.InnerException != null ? t.Exception.InnerException.Message : "Error";
                        _txtStatus.Text = (_currentLanguage == AppLanguage.English) ? "Scan error: " + err : "Ошибка сканирования: " + err;
                        return;
                    }

                    _desktopItems = t.Result ?? new List<DesktopItem>();
                    _txtStatus.Text = (_currentLanguage == AppLanguage.English)
                        ? string.Format("Found {0} desktop items", _desktopItems.Count)
                        : string.Format("Обнаружено значков на рабочем столе: {0}", _desktopItems.Count);
                    UpdateLivePreviews();
                }));
            }));
        }

        private void UpdateLivePreviews()
        {
            _previewPanel.Children.Clear();
            bool isDark = (_currentTheme == AppTheme.Dark);
            bool isEn = (_currentLanguage == AppLanguage.English);

            foreach (DesktopItem item in _desktopItems)
            {
                Border card = new Border
                {
                    Width = 126,
                    Height = 142,
                    Margin = new Thickness(6),
                    Background = new SolidColorBrush(isDark
                        ? System.Windows.Media.Color.FromRgb(24, 25, 30)
                        : System.Windows.Media.Color.FromRgb(249, 250, 251)),
                    BorderBrush = new SolidColorBrush(isDark
                        ? System.Windows.Media.Color.FromArgb(35, 255, 255, 255)
                        : System.Windows.Media.Color.FromRgb(229, 231, 235)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(11),
                    Padding = new Thickness(8)
                };

                StackPanel sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

                System.Windows.Controls.Image img = new System.Windows.Controls.Image
                {
                    Width = 60,
                    Height = 60,
                    Margin = new Thickness(0, 6, 0, 8),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                using (Bitmap processed = ImageProcessor.ProcessIcon(item.OriginalIcon, _options))
                {
                    img.Source = BitmapToSource(processed);
                }

                TextBlock name = new TextBlock
                {
                    Text = item.DisplayName,
                    FontSize = 11,
                    FontWeight = FontWeights.Medium,
                    Foreground = isDark ? System.Windows.Media.Brushes.White : new SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 24, 39)),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    TextAlignment = TextAlignment.Center,
                    MaxWidth = 110
                };

                string badgeLabel;
                if (item.IsPublicDesktop)
                {
                    badgeLabel = isEn ? "Public Desktop" : "Общий стол";
                }
                else if (item.ItemType == DesktopItemType.Folder)
                {
                    badgeLabel = isEn ? "Folder" : "Папка";
                }
                else
                {
                    badgeLabel = isEn ? "Shortcut" : "Ярлык";
                }

                TextBlock typeBadge = new TextBlock
                {
                    Text = badgeLabel,
                    FontSize = 9,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(140, 140, 150)),
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 3, 0, 0)
                };

                sp.Children.Add(img);
                sp.Children.Add(name);
                sp.Children.Add(typeBadge);
                card.Child = sp;
                _previewPanel.Children.Add(card);
            }
        }

        private async Task ApplyMonochromeAsync()
        {
            _btnApply.IsEnabled = false;
            _btnRestore.IsEnabled = false;
            _progressBar.Visibility = Visibility.Visible;
            _progressBar.IsIndeterminate = false;
            _progressBar.Value = 0;
            _progressBar.Maximum = _desktopItems.Count;

            bool isEn = (_currentLanguage == AppLanguage.English);

            try
            {
                await Task.Run(() =>
                {
                    IconPatcher.ApplyMonochrome(_desktopItems, _options, (name, current, total) =>
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            _progressBar.Value = current;
                            _txtStatus.Text = isEn
                                ? string.Format("Processing ({0}/{1}): {2}", current, total, name)
                                : string.Format("Обработка ({0}/{1}): {2}", current, total, name);
                        }));
                    });
                });

                Dispatcher.Invoke(new Action(() =>
                {
                    _txtStatus.Text = isEn
                        ? "✨ Desktop icons successfully updated!"
                        : "✨ Иконки рабочего стола успешно обновлены!";
                    MessageBox.Show(this,
                        isEn ? "Desktop styling applied successfully!" : "Новое оформление успешно применено к рабочему столу!",
                        "WinIcons Pro", MessageBoxButton.OK, MessageBoxImage.Information);
                }));
            }
            catch (Exception ex)
            {
                Exception targetEx = (ex is AggregateException && ex.InnerException != null) ? ex.InnerException : ex;

                Dispatcher.Invoke(new Action(() =>
                {
                    _txtStatus.Text = (isEn ? "Error: " : "Ошибка: ") + targetEx.Message;
                    MessageBox.Show(this,
                        (isEn ? "Failed to apply icons: " : "Ошибка применения иконок: ") + targetEx.Message,
                        isEn ? "Error" : "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }));
            }
            finally
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    _btnApply.IsEnabled = true;
                    _btnRestore.IsEnabled = true;
                    _progressBar.Visibility = Visibility.Collapsed;
                }));
            }
        }

        private async Task RestoreOriginalsAsync()
        {
            bool isEn = (_currentLanguage == AppLanguage.English);

            MessageBoxResult confirm = MessageBox.Show(this,
                isEn ? "Do you want to restore default Windows icons to the desktop?" : "Вы действительно хотите вернуть стандартные иконки Windows на рабочий стол?",
                isEn ? "Restore Defaults" : "Подтверждение отката", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            _btnApply.IsEnabled = false;
            _btnRestore.IsEnabled = false;
            _progressBar.Visibility = Visibility.Visible;
            _progressBar.IsIndeterminate = false;
            _progressBar.Value = 0;

            try
            {
                await Task.Run(() =>
                {
                    IconPatcher.RestoreOriginals((name, current, total) =>
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            _progressBar.Maximum = total;
                            _progressBar.Value = current;
                            _txtStatus.Text = isEn
                                ? string.Format("Restoring ({0}/{1}): {2}", current, total, name)
                                : string.Format("Восстановление ({0}/{1}): {2}", current, total, name);
                        }));
                    });
                });

                Dispatcher.Invoke(new Action(() =>
                {
                    _txtStatus.Text = isEn
                        ? "Default icons successfully restored!"
                        : "Стандартные иконки успешно восстановлены!";
                    MessageBox.Show(this,
                        isEn ? "All standard desktop icons have been restored!" : "Все стандартные значки Windows успешно восстановлены!",
                        "WinIcons Pro", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadDesktopItems();
                }));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    _txtStatus.Text = (isEn ? "Error: " : "Ошибка: ") + ex.Message;
                    MessageBox.Show(this,
                        (isEn ? "Failed to restore icons: " : "Ошибка при восстановлении: ") + ex.Message,
                        isEn ? "Error" : "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }));
            }
            finally
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    _btnApply.IsEnabled = true;
                    _btnRestore.IsEnabled = true;
                    _progressBar.Visibility = Visibility.Collapsed;
                }));
            }
        }

        private static BitmapSource BitmapToSource(Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                BitmapSource bs = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                bs.Freeze();
                return bs;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }
    }
}

using System;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using ScottPlot;

namespace ScientificCalculator
{
    public partial class MainWindow : Avalonia.Controls.Window
    {
        private bool _newEntry = true;

        // ── History file location ─────────────────────────────────────────────
        //
        // We store history in the OS-standard "user application data" folder.
        // SpecialFolder.ApplicationData maps to:
        //   Windows: %APPDATA%                  → C:\Users\X\AppData\Roaming
        //   macOS:   ~/Library/Application Support
        //   Linux:   ~/.config (XDG_CONFIG_HOME)
        //
        // This is always writable for the current user and is where users expect
        // app data to live. Previously we wrote history.txt to the current working
        // directory which depended on where the binary was launched from — this
        // failed silently on macOS and Linux when launched from Finder/Files
        // because the CWD might be a read-only system path.
        private static readonly string HistoryFile = ResolveHistoryFile();

        private static string ResolveHistoryFile()
        {
            string baseDir = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);
            string appDir  = Path.Combine(baseDir, "OpenCalculator");
            try
            {
                Directory.CreateDirectory(appDir);
            }
            catch (Exception ex)
            {
                // If we can't create the standard folder for any reason
                // (corp lockdown, weird sandbox, etc.), fall back to the user's
                // home folder which is always writable.
                Console.Error.WriteLine($"[OpenCalculator] Could not create {appDir}: {ex.Message}");
                appDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            return Path.Combine(appDir, "history.txt");
        }

        private double[,]? _cachedZ;
        private const int   MeshRes   = 60;
        private const float MeshRange = 5f;

        private float _azimuth   = 45f;
        private float _elevation = 30f;

        private bool           _isDragging3D = false;
        private Avalonia.Point _lastDragPoint;

        public MainWindow()
        {
            InitializeComponent();
            SetupPlots();
            LoadHistory();
            this.AddHandler(KeyDownEvent, Window_KeyDown, handledEventsToo: true);

            Plot3D.PointerPressed      += Plot3D_PointerPressed;
            Plot3D.PointerMoved        += Plot3D_PointerMoved;
            Plot3D.PointerReleased     += Plot3D_PointerReleased;
            Plot3D.PointerWheelChanged += Plot3D_WheelChanged;
        }

        private void SetupPlots()
        {
            AvaPlot2D.Plot.Axes.Color(ScottPlot.Color.FromHex("#CDD6F4"));
            AvaPlot2D.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#1E1E2E");
            AvaPlot2D.Plot.DataBackground.Color   = ScottPlot.Color.FromHex("#181825");
            AvaPlot2D.Refresh();
        }

        // ── History ───────────────────────────────────────────────────────────
        // Reads/writes are wrapped in try/catch so a file system error never
        // crashes the app — at worst we lose history persistence for that run.

        private void LoadHistory()
        {
            try
            {
                if (File.Exists(HistoryFile))
                    HistoryBox.Text = File.ReadAllText(HistoryFile);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[OpenCalculator] Could not load history: {ex.Message}");
            }
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                File.WriteAllText(HistoryFile, HistoryBox.Text ?? "");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[OpenCalculator] Could not save history: {ex.Message}");
            }
        }

        private void AddHistory(string entry)
        {
            HistoryBox.Text += entry + "\n";
            HistoryScroll.ScrollToEnd();
        }

        private void ClearHistory_Click(object? sender, RoutedEventArgs e) => HistoryBox.Text = "";

        // ── Help Overlays ─────────────────────────────────────────────────────

        private void ToggleHelp2D_Click(object? sender, RoutedEventArgs e) =>
            Help2DOverlay.IsVisible = !Help2DOverlay.IsVisible;

        private void ToggleHelp3D_Click(object? sender, RoutedEventArgs e) =>
            Help3DOverlay.IsVisible = !Help3DOverlay.IsVisible;

        private void ShowError(string message) { Display.Text = message; _newEntry = true; }

        // ── Equation normalization ────────────────────────────────────────────

        private string NormalizeEquation(string input)
        {
            string p = input.Replace("×", "*").Replace("÷", "/");
            p = Regex.Replace(p, "sin",  "Sin",   RegexOptions.IgnoreCase);
            p = Regex.Replace(p, "cos",  "Cos",   RegexOptions.IgnoreCase);
            p = Regex.Replace(p, "tan",  "Tan",   RegexOptions.IgnoreCase);
            p = Regex.Replace(p, "sqrt", "Sqrt",  RegexOptions.IgnoreCase);
            p = Regex.Replace(p, "log10","Log10", RegexOptions.IgnoreCase);
            p = Regex.Replace(p, @"(?<!Log)log(?!10)", "Log10", RegexOptions.IgnoreCase);
            p = Regex.Replace(p, "ln",   "Log",   RegexOptions.IgnoreCase);
            p = Regex.Replace(p, "exp",  "Exp",   RegexOptions.IgnoreCase);
            p = Regex.Replace(p, @"(\d+(\.\d+)?|\b\w+\b|[)]|Pi)\s*\^\s*(\d+(\.\d+)?|\b\w+\b|[(].*[)])", "Pow($1, $3)");
            return p;
        }

        // ── Text insertion ────────────────────────────────────────────────────

        private void InsertText(string text)
        {
            if (_newEntry) { Display.Text = text; _newEntry = false; }
            else
            {
                int caretPos = Display.SelectionStart;
                int selLen   = Display.SelectionEnd - Display.SelectionStart;
                string cur   = Display.Text ?? "";
                Display.Text = cur.Remove(caretPos, selLen).Insert(caretPos, text);
                Display.CaretIndex = caretPos + text.Length;
            }
            Display.Focus();
        }

        // ── Calculator handlers ───────────────────────────────────────────────

        private void Num_Click(object? sender, RoutedEventArgs e) =>
            InsertText((sender as Button)?.Content?.ToString() ?? "");

        private void Display_GotFocus(object? sender, RoutedEventArgs e)
        {
            if (_newEntry && Display.Text == "0") Display.SelectAll();
        }

        private void Decimal_Click(object? sender, RoutedEventArgs e) => InsertText(".");

        private void Op_Click(object? sender, RoutedEventArgs e) =>
            InsertText((sender as Button)?.Tag?.ToString() ?? "");

        private void Sci_Click(object? sender, RoutedEventArgs e)
        {
            string tag = (sender as Button)?.Tag?.ToString() ?? "";
            if (tag == "Pi" || tag == "^2") InsertText(tag); else InsertText(tag + "(");
        }

        private void Equal_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                string raw = Display.Text ?? "0";
                var expr   = new NCalc.Expression(NormalizeEquation(raw));
                expr.Parameters["Pi"] = Math.PI; expr.Parameters["pi"] = Math.PI;
                expr.Parameters["x"] = 0.0; expr.Parameters["y"] = 0.0; expr.Parameters["z"] = 0.0;
                var result = expr.Evaluate();
                AddHistory($"{raw} = {result}");
                Display.Text = result?.ToString() ?? "0";
                _newEntry = true; Display.Focus(); Display.SelectAll();
            }
            catch (Exception ex) { ShowError("Err: " + ex.Message); }
        }

        private void Clear_Click(object? sender, RoutedEventArgs? e) { Display.Text = "0"; _newEntry = true; }

        private void Back_Click(object? sender, RoutedEventArgs? e)
        {
            string text = Display.Text ?? "";
            int selLen  = Display.SelectionEnd - Display.SelectionStart;
            if (selLen > 0) Display.Text = text.Remove(Display.SelectionStart, selLen);
            else if (Display.CaretIndex > 0)
            {
                int pos = Display.CaretIndex;
                Display.Text = text.Remove(pos - 1, 1);
                Display.CaretIndex = pos - 1;
            }
        }

        private void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                Equal_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        // ── 2D Plot ───────────────────────────────────────────────────────────

        private void Plot2D_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                AvaPlot2D.Plot.Clear();
                AvaPlot2D.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#1E1E2E");
                AvaPlot2D.Plot.DataBackground.Color   = ScottPlot.Color.FromHex("#181825");
                string formula = NormalizeEquation(EquationInput2D.Text ?? "x");
                double[] xs = ScottPlot.Generate.Consecutive(200, 0.1, -10);
                double[] ys = new double[xs.Length];
                for (int i = 0; i < xs.Length; i++)
                {
                    var expr = new NCalc.Expression(formula);
                    expr.Parameters["x"] = xs[i]; expr.Parameters["Pi"] = Math.PI;
                    ys[i] = Convert.ToDouble(expr.Evaluate());
                }
                AvaPlot2D.Plot.Add.Scatter(xs, ys, ScottPlot.Color.FromHex("#89B4FA"));
                AvaPlot2D.Plot.Axes.AutoScale(); AvaPlot2D.Refresh();
                AddHistory($"2D: y = {EquationInput2D.Text}");
            }
            catch (Exception ex) { ShowError("2D Err: " + ex.Message); }
        }

        private void Reset2D_Click(object? sender, RoutedEventArgs e)
        {
            EquationInput2D.Text = "sin(x) * x";
            AvaPlot2D.Plot.Clear();
            AvaPlot2D.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#1E1E2E");
            AvaPlot2D.Plot.DataBackground.Color   = ScottPlot.Color.FromHex("#181825");
            AvaPlot2D.Refresh();
        }

        // ── 3D Plot ───────────────────────────────────────────────────────────

        private void Plot3D_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                string formula = NormalizeEquation(EquationInput3D.Text ?? "sin(x)*cos(y)");
                int    res     = MeshRes;
                float  range   = MeshRange;
                float  step    = (range * 2f) / (res - 1);

                _cachedZ = new double[res, res];
                for (int i = 0; i < res; i++)
                {
                    double xVal = -range + i * step;
                    for (int j = 0; j < res; j++)
                    {
                        double yVal = -range + j * step;
                        try
                        {
                            var expr = new NCalc.Expression(formula);
                            expr.Parameters["x"] = xVal; expr.Parameters["y"] = yVal;
                            expr.Parameters["Pi"] = Math.PI;
                            _cachedZ[i, j] = Convert.ToDouble(expr.Evaluate());
                        }
                        catch { _cachedZ[i, j] = 0; }
                    }
                }

                var (verts, indices, zMin, zMax) = BuildMesh(_cachedZ, res, range);
                Plot3D.UpdateMesh(verts, indices, zMin, zMax, range);
                Plot3D.SetViewAngles(_azimuth, _elevation);
                AddHistory($"3D: z = {EquationInput3D.Text}");
            }
            catch (Exception ex) { ShowError("3D Err: " + ex.Message); }
        }

        private void Reset3D_Click(object? sender, RoutedEventArgs e)
        {
            EquationInput3D.Text = "sin(x) * cos(y)";
            _cachedZ = null; _azimuth = 45f; _elevation = 30f;

            AzimuthSlider.ValueChanged   -= OnAzimuthChanged;
            ElevationSlider.ValueChanged -= OnElevationChanged;
            AzimuthSlider.Value = _azimuth; ElevationSlider.Value = _elevation;
            AzimuthLabel.Text   = "45°";    ElevationLabel.Text   = "30°";
            AzimuthSlider.ValueChanged   += OnAzimuthChanged;
            ElevationSlider.ValueChanged += OnElevationChanged;

            Plot3D.ClearMesh();
        }

        private static (float[] verts, uint[] indices, float zMin, float zMax)
            BuildMesh(double[,] z, int res, float range)
        {
            float step  = (range * 2f) / (res - 1);
            var verts   = new float[res * res * 6];
            var indices = new uint[(res-1) * (res-1) * 6];
            float zMin = float.MaxValue, zMax = float.MinValue;

            for (int i = 0; i < res; i++)
                for (int j = 0; j < res; j++)
                {
                    float zv = (float)z[i, j];
                    if (zv < zMin) zMin = zv;
                    if (zv > zMax) zMax = zv;
                }

            for (int i = 0; i < res; i++)
            {
                float x = -range + i * step;
                for (int j = 0; j < res; j++)
                {
                    float y  = -range + j * step;
                    float zv = (float)z[i, j];

                    float dzx = i > 0 && i < res-1
                        ? (float)(z[i+1,j]-z[i-1,j])/(2f*step)
                        : i == 0 ? (float)(z[i+1,j]-z[i,j])/step
                                 : (float)(z[i,j]-z[i-1,j])/step;
                    float dzy = j > 0 && j < res-1
                        ? (float)(z[i,j+1]-z[i,j-1])/(2f*step)
                        : j == 0 ? (float)(z[i,j+1]-z[i,j])/step
                                 : (float)(z[i,j]-z[i,j-1])/step;

                    float nx=-dzx, ny=-dzy, nz=1f;
                    float nl=MathF.Sqrt(nx*nx+ny*ny+nz*nz);
                    nx/=nl; ny/=nl; nz/=nl;

                    int vi = (i*res+j)*6;
                    verts[vi]=x;  verts[vi+1]=y;  verts[vi+2]=zv;
                    verts[vi+3]=nx; verts[vi+4]=ny; verts[vi+5]=nz;
                }
            }

            int idx = 0;
            for (int i = 0; i < res-1; i++)
                for (int j = 0; j < res-1; j++)
                {
                    uint v00=(uint)(i*res+j),     v10=(uint)((i+1)*res+j);
                    uint v01=(uint)(i*res+j+1),   v11=(uint)((i+1)*res+j+1);
                    indices[idx++]=v00; indices[idx++]=v10; indices[idx++]=v11;
                    indices[idx++]=v00; indices[idx++]=v11; indices[idx++]=v01;
                }

            return (verts, indices, zMin, zMax);
        }

        // ── Slider handlers ───────────────────────────────────────────────────

        private void OnAzimuthChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            _azimuth = (float)e.NewValue;
            AzimuthLabel.Text = $"{(int)_azimuth}°";
            Plot3D.SetViewAngles(_azimuth, _elevation);
        }

        private void OnElevationChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            _elevation = (float)e.NewValue;
            ElevationLabel.Text = $"{(int)_elevation}°";
            Plot3D.SetViewAngles(_azimuth, _elevation);
        }

        // ── Mouse handlers ────────────────────────────────────────────────────

        private void Plot3D_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(Plot3D).Properties.IsRightButtonPressed)
            {
                _isDragging3D  = true;
                _lastDragPoint = e.GetPosition(Plot3D);
                e.Handled = true;
            }
        }

        private void Plot3D_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isDragging3D) return;
            var current = e.GetPosition(Plot3D);
            double dx = current.X - _lastDragPoint.X;
            double dy = current.Y - _lastDragPoint.Y;
            _lastDragPoint = current;

            _azimuth   = (float)((_azimuth + dx * 0.4) % 360.0);
            if (_azimuth < 0) _azimuth += 360f;
            _elevation = Math.Clamp(_elevation - (float)(dy * 0.4), 5f, 85f);

            AzimuthSlider.ValueChanged   -= OnAzimuthChanged;
            ElevationSlider.ValueChanged -= OnElevationChanged;
            AzimuthSlider.Value   = _azimuth;
            ElevationSlider.Value = _elevation;
            AzimuthLabel.Text     = $"{(int)_azimuth}°";
            ElevationLabel.Text   = $"{(int)_elevation}°";
            AzimuthSlider.ValueChanged   += OnAzimuthChanged;
            ElevationSlider.ValueChanged += OnElevationChanged;

            Plot3D.SetViewAngles(_azimuth, _elevation);
            e.Handled = true;
        }

        private void Plot3D_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                _isDragging3D = false;
                e.Handled = true;
            }
        }

        private void Plot3D_WheelChanged(object? sender, PointerWheelEventArgs e)
        {
            Plot3D.AdjustZoom((float)e.Delta.Y);
            e.Handled = true;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (e.ClickCount == 2 &&
                e.GetCurrentPoint(Plot3D).Properties.IsLeftButtonPressed)
            {
                Plot3D.ResetZoom();
                e.Handled = true;
            }
        }

        // ── Unit Converter stub ───────────────────────────────────────────────

        private void Convert_Click(object? sender, RoutedEventArgs e) { }
    }
}

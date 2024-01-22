using Microsoft.UI.Xaml;

//using Microsoft.UI.Xaml.Media;
using Reactive.Bindings;
using System;
using PathEdit.Parser;
using PathEdit.Graphics;
using Windows.UI;
using System.Reactive.Linq;
using PathEdit.common;
using Microsoft.Graphics.Canvas;
using Windows.UI.ViewManagement;
using System.Text.RegularExpressions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PathEdit {
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window {
        class MainWindowViewModel {
            public ReactiveProperty<string> SourcePath { get; } = new ("M22,11L12,21L2,11H8V3H16V11H22M12,18L17,13H14V5H10V13H7L12,18Z");
            public ReadOnlyReactiveProperty<string> ComposedPath { get; }

            public ReactiveCommand ComposeCommand { get; } = new ();
            public ReactiveProperty<int> PathWidth { get; } = new (24);
            public ReactiveProperty<int> PathHeight { get; } = new (24);

            public ReactiveProperty<bool> KeepAspect { get; } = new (true);
            public ReactiveProperty<double> Scale { get; } = new (0);

            public ReactiveProperty<double> ScaleX { get; } = new(100);
            public ReactiveProperty<double> ScaleY { get; } = new(100);
            public ReactiveProperty<double> ScalePivotX { get; } = new (0);
            public ReactiveProperty<double> ScalePivotY { get; } = new(0);
            public ReadOnlyReactiveProperty<Visibility> SingleScaleVisibility { get; }
            public ReadOnlyReactiveProperty<Visibility> DoubleScaleVisibility { get; }
            public ReactiveCommand ScaleCommand { get; } = new ();

            public ReactiveProperty<double> TranslateX { get; } = new (0);
            public ReactiveProperty<double> TranslateY { get; } = new (0);
            public ReactiveCommand TranslateCommand { get; } = new ();


            public ReactiveCommand<string> MirrorCommand { get; } = new ();

            public ReactiveProperty<double> RotatePivotX { get; } = new (12);
            public ReactiveProperty<double> RotatePivotY { get; } = new (12);
            public ReactiveProperty<double> RotateAngle { get; } = new (0);
            public ReactiveCommand RotateCommand { get; } = new ();

            public ReactiveProperty<int> RoundDigit { get; } = new(5);
            public ReactiveCommand RoundCommand { get; } = new();

            //public ReadOnlyReactiveProperty<Geometry> PathData { get; }

            public MainWindowViewModel() {
                ComposedPath = SourcePath.Select(path => {
                    try {
                        return PathDrawable.Parse(path).Compose();
                    } catch (Exception) {
                        var pathData = ExtractPath(path);
                        if(!string.IsNullOrEmpty(pathData)) {
                            try { 
                                return PathDrawable.Parse(pathData).Compose();
                            } catch (Exception) { }
                        }
                        return "M10,19H13V22H10V19M12,2C17.35,2.22 19.68,7.62 16.5,11.67C15.67,12.67 14.33,13.33 13.67,14.17C13,15 13,16 13,17H10C10,15.33 10,13.92 10.67,12.92C11.33,11.92 12.67,11.33 13.5,10.67C15.92,8.43 15.32,5.26 12,5A3,3 0 0,0 9,8H6A6,6 0 0,1 12,2Z";
                    }
                }).ToReadOnlyReactiveProperty<string>();

                SingleScaleVisibility = KeepAspect.Select(keepAspect => keepAspect ? Visibility.Visible : Visibility.Collapsed).ToReadOnlyReactiveProperty();
                DoubleScaleVisibility = KeepAspect.Select(keepAspect => keepAspect ? Visibility.Collapsed : Visibility.Visible).ToReadOnlyReactiveProperty();
                //ComposeCommand.Subscribe(_ => {
                //    try {
                //        ComposedPath.Value = PathList.Parse(RawSourcePath.Value).Compose();
                //    } catch (Exception) {
                //        ComposedPath.Value = "";
                //    }
                //});

                //PathData = Path.Select(path => {
                //    try {
                //        var pathFigures = (PathFigureCollection)XamlBindingHelper.ConvertValue(typeof(PathFigureCollection), "M 0,0 L 0,1 L 1,1 L 1,0 Z");
                //        var pathGeometry = new PathGeometry();
                //        pathGeometry.Figures = pathFigures;
                //        return pathGeometry;
                //    } catch (Exception) {
                //        return Geometry.Empty;
                //    }
                //}).ToReadOnlyReactiveProperty<Geometry>();
            }

            static Regex pathPattern = new Regex("""\s*(?:d|android:pathData|Data)="(?<path>[^"]+)["]""");
            static Regex pathPattern2 = new Regex("""["](?<path>[^"]+)["]""");
            string ExtractPath(string src) {
                var m1 = pathPattern.Match(src);
                var path = m1.Groups["path"].Value;
                if(!string.IsNullOrEmpty(path)) {
                    return path;
                }
                var m2 = pathPattern2.Match(src);
                return m2.Groups["path"].Value;
            }
        }
        private MainWindowViewModel ViewModel { get; } = new MainWindowViewModel();

        public MainWindow() {
            this.InitializeComponent();
            ViewModel.ComposedPath.Subscribe(_ => {
                PathCanvas.Invalidate();
            });
            ViewModel.ScaleCommand.Subscribe(_ => {
                try {
                    var matrix = new System.Windows.Media.Matrix();
                    if (ViewModel.KeepAspect.Value) {
                        var scale = ViewModel.Scale.Value;
                        checkValues(scale);

                        matrix.ScaleAt(scale/100, scale/100, ViewModel.ScalePivotX.Value, ViewModel.ScalePivotY.Value);
                    } else {
                        var scaleX = ViewModel.ScaleX.Value;
                        var scaleY = ViewModel.ScaleY.Value;
                        checkValues(scaleX, scaleY);
                        matrix.ScaleAt(scaleX/100, scaleY/100, ViewModel.ScalePivotX.Value, ViewModel.ScalePivotY.Value);
                    }
                    ViewModel.SourcePath.Value = PathDrawable.Parse(ViewModel.SourcePath.Value).Transform(matrix).Compose();
                } catch (Exception e) {
                    LoggerEx.error(e);
                }
            });
            ViewModel.TranslateCommand.Subscribe(_ => {
                try {
                    var matrix = new System.Windows.Media.Matrix();
                    double tx = ViewModel.TranslateX.Value;
                    double ty = ViewModel.TranslateY.Value;
                    checkValues(tx, ty);
                    matrix.Translate(tx, ty);
                    ViewModel.SourcePath.Value = PathDrawable.Parse(ViewModel.SourcePath.Value).Transform(matrix).Compose();
                }
                catch (Exception e) {
                    LoggerEx.error(e);
                }
            });
            ViewModel.MirrorCommand.Subscribe(c => {
                try {
                    var matrix = new System.Windows.Media.Matrix();
                    checkValues(ViewModel.PathHeight.Value, ViewModel.PathWidth.Value);
                    if(c=="V") {
                        matrix.ScaleAt(1, -1, ViewModel.PathWidth.Value/2, ViewModel.PathHeight.Value/2);
                    } else {
                        matrix.ScaleAt(-1, 1, ViewModel.PathWidth.Value / 2, ViewModel.PathHeight.Value / 2);
                    }
                    ViewModel.SourcePath.Value = PathDrawable.Parse(ViewModel.SourcePath.Value).Transform(matrix).Compose();
                }
                catch (Exception e) {
                    LoggerEx.error(e);
                }
            });
            ViewModel.RotateCommand.Subscribe(_ => {
                try {
                    var matrix = new System.Windows.Media.Matrix();
                    double angle = ViewModel.RotateAngle.Value;
                    double cx = ViewModel.RotatePivotX.Value;
                    double cy = ViewModel.RotatePivotY.Value;

                    checkValues(angle, cx, cy);
                    matrix.RotateAt(angle, cx, cy);
                    ViewModel.SourcePath.Value = PathDrawable.Parse(ViewModel.SourcePath.Value).PreProcessForRotation().Transform(matrix).Compose();
                }
                catch (Exception e) {
                    LoggerEx.error(e);
                }
            });
            ViewModel.RoundCommand.Subscribe(_ => {
                try {
                    ViewModel.SourcePath.Value = PathDrawable.Parse(ViewModel.SourcePath.Value).RoundCoordinateValue(ViewModel.RoundDigit.Value).Compose();
                }
                catch (Exception e) {
                    LoggerEx.error(e);
                }
            });
        }

        private void checkValues(params double[] values) {
            foreach (var value in values) {
                if (double.IsNaN(value) || double.IsInfinity(value)) {
                    throw new ArgumentException();
                }
            }
        }

        private void CanvasControl_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args) {
            //var drawingSession = args.DrawingSession;
            //using (var pathBuilder = new CanvasPathBuilder(drawingSession)) {
            //    pathBuilder.BeginFigure(0, 0);
            //    pathBuilder.AddLine(0, 10);
            //    pathBuilder.AddLine(10, 10);
            //    pathBuilder.AddLine(10, 0);
            //    pathBuilder.EndFigure(CanvasFigureLoop.Closed);
            //    var geo = CanvasGeometry.CreatePath(pathBuilder);
            //    var mx = Matrix3x2.CreateScale(10f, 10f, Vector2.Zero);
            //    geo.Transform(mx);
            //    drawingSession.FillGeometry(geo.Transform(mx), Color.FromArgb(0xff, 0, 0x80, 0xff));
            //}

            using (var graphics = new Win2DGraphics(args.DrawingSession, sender.Width, sender.Height, Color.FromArgb(0xff, 0, 0xff, 0x80))) {
                try {
                    graphics.SetPathSize(ViewModel.PathWidth.Value, ViewModel.PathHeight.Value);
                    PathDrawable.Parse(ViewModel.ComposedPath.Value).DrawTo(graphics);
                    drawGrid(args.DrawingSession, sender.Width, sender.Height);
                } catch (Exception e) {
                    LoggerEx.error(e);
                }
            }
        }
        private void drawGrid(CanvasDrawingSession ds, double width, double height) {
            int pw = ViewModel.PathWidth.Value;
            int ph = ViewModel.PathHeight.Value;
            float cx = (float)width / pw;
            float cy = (float)height / ph;
            var color = Color.FromArgb(0xff, 0x50, 0x50, 0x50);
            for (int nx = 0; nx < pw+1 ; nx++) {
                ds.DrawLine(cx * nx, 0, cx * nx, (float)height, color);
            }
            for (int ny = 0; ny < ph+1 ; ny++) {
                ds.DrawLine(0, cy * ny, (float)width, cy * ny, color);
            }
        }
    }
}

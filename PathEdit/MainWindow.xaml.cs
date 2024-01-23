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
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PathEdit {
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window {
        class MainWindowViewModel {
            public ReactiveProperty<string> SourcePath { get; } = new ("M22,11L12,21L2,11H8V3H16V11H22M12,18L17,13H14V5H10V13H7L12,18Z");

            public ReactiveProperty<string> WorkingPath { get; } = new();
            public ReactiveProperty<string> EditingPath { get; } = new ();

            public ReactiveCommand ComposeCommand { get; } = new ();
            public ReactiveProperty<int> PathWidth { get; } = new (24);
            public ReactiveProperty<int> PathHeight { get; } = new (24);

            #region Editing Mode
            public enum EditingMode {
                None,
                Scale,
                Translate,
                Rotate,
                Mirror,
                Round,
            }
            public ReactiveProperty<EditingMode> Mode { get; } = new (EditingMode.None);
            
            #endregion

            #region Scale

            public ReactiveProperty<bool> EditingScale { get; } = new (false);
            public ReactiveProperty<bool> KeepAspect { get; } = new (true);
            public ReactiveProperty<double> Scale { get; } = new (100);

            public ReactiveProperty<double> ScaleX { get; } = new(100);
            public ReactiveProperty<double> ScaleY { get; } = new(100);
            public ReactiveProperty<double> ScalePivotX { get; } = new (12);
            public ReactiveProperty<double> ScalePivotY { get; } = new(12);
            public ReadOnlyReactiveProperty<Visibility> SingleScaleVisibility { get; }
            public ReadOnlyReactiveProperty<Visibility> DoubleScaleVisibility { get; }
            public ReactiveCommand ScalePivotStepMinusX { get; } = new();
            public ReactiveCommand ScalePivotStepPlusX { get; } = new();
            public ReactiveCommand ScalePivotStepMinusY { get; } = new();
            public ReactiveCommand ScalePivotStepPlusY { get; } = new ();
            public ReactiveCommand ScaleStepMinus { get; } = new();
            public ReactiveCommand ScaleStepPlus { get; } = new();
            public ReactiveCommand ScaleStepMinusX { get; } = new();
            public ReactiveCommand ScaleStepPlusX { get; } = new();
            public ReactiveCommand ScaleStepMinusY { get; } = new();
            public ReactiveCommand ScaleStepPlusY { get; } = new();

            #endregion

            #region Translation

            public ReactiveProperty<bool> EditingTranslate { get; } = new (false);

            public ReactiveProperty<double> TranslateX { get; } = new (0);
            public ReactiveProperty<double> TranslateY { get; } = new (0);

            public ReactiveCommand<string> TranslateStepMinusX { get; } = new ();
            public ReactiveCommand<string> TranslateStepPlusX { get; } = new();
            public ReactiveCommand<string> TranslateStepMinusY { get; } = new();
            public ReactiveCommand<string> TranslateStepPlusY { get; } = new();

            #endregion

            #region Mirror            

            public ReactiveProperty<bool> EditingMirror { get; } = new (false);

            public ReactiveCommand<string> MirrorCommand { get; } = new ();

            #endregion

            #region Rotation

            public ReactiveProperty<bool> EditingRotation { get; } = new(false);


            public ReactiveProperty<double> RotatePivotX { get; } = new (12);
            public ReactiveProperty<double> RotatePivotY { get; } = new (12);
            public ReactiveProperty<double> RotateAngle { get; } = new (0);

            public ReactiveCommand RotateAngleStepMinus { get; } = new();
            public ReactiveCommand RotateAngleStepPlus { get; } = new();
            public ReactiveCommand RotatePivotStepPlusX { get; } = new();
            public ReactiveCommand RotatePivotStepMinusX { get; } = new();
            public ReactiveCommand RotatePivotStepPlusY { get; } = new();
            public ReactiveCommand RotatePivotStepMinusY { get; } = new();



            #endregion

            #region Rounding

            public ReactiveProperty<bool> EditingRound { get; } = new(false);


            public ReactiveProperty<int> RoundDigit { get; } = new(5);
            public ReactiveCommand RoundCommand { get; } = new();

            #endregion

            //public ReadOnlyReactiveProperty<Geometry> PathData { get; }

            public ReactiveCommand CopyCommand { get; } = new();

            public MainWindowViewModel() {
                SourcePath.Subscribe(path => {
                    try {
                        EditingPath.Value = PathDrawable.Parse(path).Compose();
                    } catch (Exception) {
                        EditingPath.Value = "M 3 3V 21H 21V 3";
                    }
                });

                Mode.Subscribe(m => {
                    EditingScale.Value = m == EditingMode.Scale;
                    EditingTranslate.Value = m == EditingMode.Translate;
                    EditingRotation.Value = m == EditingMode.Rotate;
                    EditingMirror.Value = m == EditingMode.Mirror;
                    EditingRound.Value = m == EditingMode.Round;

                    WorkingPath.Value = EditingPath.Value;
                });
                EditingScale.Subscribe(b => {
                    if (b && Mode.Value!=EditingMode.Scale) {
                        Scale.Value = 100;
                        ScaleX.Value = 100;
                        ScaleY.Value = 100;
                        Mode.Value = EditingMode.Scale;
                    }
                });
                EditingTranslate.Subscribe(b => {
                    if (b && Mode.Value != EditingMode.Translate) {
                        TranslateX.Value = 0;
                        TranslateY.Value = 0;
                        Mode.Value = EditingMode.Translate;
                    }
                });
                EditingRotation.Subscribe(b => {
                    if (b && Mode.Value != EditingMode.Rotate) {
                        Mode.Value = EditingMode.Rotate;
                    }
                });
                EditingMirror.Subscribe(b => {
                    if (b && Mode.Value != EditingMode.Mirror) {
                        Mode.Value = EditingMode.Mirror;
                    }
                });
                EditingRound.Subscribe(b => {
                    if (b && Mode.Value != EditingMode.Round) {
                        Mode.Value = EditingMode.Round;
                    }
                });



                SingleScaleVisibility = KeepAspect.Select(keepAspect => keepAspect ? Visibility.Visible : Visibility.Collapsed).ToReadOnlyReactiveProperty();
                DoubleScaleVisibility = KeepAspect.Select(keepAspect => keepAspect ? Visibility.Collapsed : Visibility.Visible).ToReadOnlyReactiveProperty();

                TranslateStepPlusX.Subscribe(_ => TranslateX.Value += 1);
                TranslateStepMinusX.Subscribe(_ => TranslateX.Value -= 1);
                TranslateStepPlusY.Subscribe(_ => TranslateY.Value += 1);
                TranslateStepMinusY.Subscribe(_ => TranslateY.Value -= 1);

                ScalePivotStepMinusX.Subscribe(_ => ScalePivotX.Value -= 1);
                ScalePivotStepPlusX.Subscribe(_ => ScalePivotX.Value += 1);
                ScalePivotStepMinusY.Subscribe(_ => ScalePivotY.Value -= 1);
                ScalePivotStepPlusY.Subscribe(_ => ScalePivotY.Value += 1);

                ScaleStepMinus.Subscribe(_ => Scale.Value -= 1);
                ScaleStepPlus.Subscribe(_ => Scale.Value += 1);

                ScaleStepMinusX.Subscribe(_ => ScaleX.Value -= 1);
                ScaleStepPlusX.Subscribe(_ => ScaleX.Value += 1);
                ScaleStepMinusY.Subscribe(_ => ScaleY.Value -= 1);
                ScaleStepPlusY.Subscribe(_ => ScaleY.Value += 1);


                RotatePivotStepMinusX.Subscribe(_ => RotatePivotX.Value -= 1);
                RotatePivotStepPlusX.Subscribe(_ => RotatePivotX.Value += 1);
                RotatePivotStepMinusY.Subscribe(_ => RotatePivotY.Value -= 1);
                RotatePivotStepPlusY.Subscribe(_ => RotatePivotY.Value += 1);
                RotateAngleStepMinus.Subscribe(_ => SetRotateAngle(RotateAngle.Value-1));
                RotateAngleStepPlus.Subscribe(_ => SetRotateAngle(RotateAngle.Value+1));
            }

            private void SetRotateAngle(double angle) {
                angle = angle % 360;
                if(angle < 0) {
                    angle += 360;
                }
                RotateAngle.Value = angle;
            }

            static Regex pathPattern = new Regex("""\s*(?:d|android:pathData|Data)="(?<path>[^"]+)["]""");
            static Regex pathPattern2 = new Regex("""["](?<path>[^"]+)["]""");
            public string? CheckAndExtractPath(string? src) {
                if (string.IsNullOrWhiteSpace(src)) {
                    return null;
                }
                try {
                    var r = PathDrawable.Parse(src).Compose();
                    if(!string.IsNullOrWhiteSpace(r)) {
                        return r;
                    }
                }
                catch (Exception) {
                }

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
            

            ViewModel.EditingPath.Subscribe(_ => {
                PathCanvas.Invalidate();
            });
            //ViewModel.Scale.Subscribe(_ => {
            //    try {
            //        var matrix = new System.Windows.Media.Matrix();
            //        if (ViewModel.KeepAspect.Value) {
            //            var scale = ViewModel.Scale.Value;
            //            checkValues(scale);

            //            matrix.ScaleAt(scale/100, scale/100, ViewModel.ScalePivotX.Value, ViewModel.ScalePivotY.Value);
            //        } else {
            //            var scaleX = ViewModel.ScaleX.Value;
            //            var scaleY = ViewModel.ScaleY.Value;
            //            checkValues(scaleX, scaleY);
            //            matrix.ScaleAt(scaleX/100, scaleY/100, ViewModel.ScalePivotX.Value, ViewModel.ScalePivotY.Value);
            //        }
            //        ViewModel.SourcePath.Value = PathDrawable.Parse(ViewModel.SourcePath.Value).Transform(matrix).Compose();
            //    } catch (Exception e) {
            //        LoggerEx.error(e);
            //    }
            //});
            //ViewModel.TranslateCommand.Subscribe(_ => {
            //    try {
            //        var matrix = new System.Windows.Media.Matrix();
            //        double tx = ViewModel.TranslateX.Value;
            //        double ty = ViewModel.TranslateY.Value;
            //        checkValues(tx, ty);
            //        matrix.Translate(tx, ty);
            //        ViewModel.SourcePath.Value = PathDrawable.Parse(ViewModel.SourcePath.Value).Transform(matrix).Compose();
            //    }
            //    catch (Exception e) {
            //        LoggerEx.error(e);
            //    }
            //});
            ViewModel.TranslateX.Subscribe(_ => Translate());
            ViewModel.TranslateY.Subscribe(_ => Translate());
            ViewModel.Scale.Subscribe(_ => Scale());
            ViewModel.ScaleX.Subscribe(_ => Scale());
            ViewModel.ScaleY.Subscribe(_ => Scale());
            ViewModel.ScalePivotX.Subscribe(_ => Scale());
            ViewModel.ScalePivotY.Subscribe(_ => Scale());

            ViewModel.RotateAngle.Subscribe(_ => Rotate());
            ViewModel.RotatePivotX.Subscribe(_ => Rotate());
            ViewModel.RotatePivotY.Subscribe(_ => Rotate());

            ViewModel.MirrorCommand.Subscribe(c => {
                try {
                    var matrix = new System.Windows.Media.Matrix();
                    checkValues(ViewModel.PathHeight.Value, ViewModel.PathWidth.Value);
                    if(c=="V") {
                        matrix.ScaleAt(1, -1, ViewModel.PathWidth.Value/2, ViewModel.PathHeight.Value/2);
                    } else {
                        matrix.ScaleAt(-1, 1, ViewModel.PathWidth.Value / 2, ViewModel.PathHeight.Value / 2);
                    }
                    ViewModel.EditingPath.Value = PathDrawable.Parse(ViewModel.WorkingPath.Value).Transform(matrix).Compose();
                }
                catch (Exception e) {
                    LoggerEx.error(e);
                }
            });
            //ViewModel.RotateCommand.Subscribe(_ => {
            //    try {
            //        var matrix = new System.Windows.Media.Matrix();
            //        double angle = ViewModel.RotateAngle.Value;
            //        double cx = ViewModel.RotatePivotX.Value;
            //        double cy = ViewModel.RotatePivotY.Value;

            //        checkValues(angle, cx, cy);
            //        matrix.RotateAt(angle, cx, cy);
            //        ViewModel.SourcePath.Value = PathDrawable.Parse(ViewModel.SourcePath.Value).PreProcessForRotation().Transform(matrix).Compose();
            //    }
            //    catch (Exception e) {
            //        LoggerEx.error(e);
            //    }
            //});
            ViewModel.RoundCommand.Subscribe(_ => {
                try {
                    ViewModel.EditingPath.Value = PathDrawable.Parse(ViewModel.WorkingPath.Value).RoundCoordinateValue(ViewModel.RoundDigit.Value).Compose();
                }
                catch (Exception e) {
                    LoggerEx.error(e);
                }
            });
        }

        private void Translate() {
            if (ViewModel.Mode.Value != MainWindowViewModel.EditingMode.Translate) {
                return;
            }
            try {
                var matrix = new System.Windows.Media.Matrix();
                double tx = ViewModel.TranslateX.Value;
                double ty = ViewModel.TranslateY.Value;
                checkValues(tx, ty);
                matrix.Translate(tx, ty);
                ViewModel.EditingPath.Value = PathDrawable.Parse(ViewModel.WorkingPath.Value).Transform(matrix).Compose();
            }
            catch (Exception e) {
                LoggerEx.error(e);
            }
        }

        private void Scale() {
            if (ViewModel.Mode.Value != MainWindowViewModel.EditingMode.Scale) {
                return;
            }
            try {
                var matrix = new System.Windows.Media.Matrix();
                if (ViewModel.KeepAspect.Value) {
                    var scale = ViewModel.Scale.Value;
                    checkValues(scale);
                    matrix.ScaleAt(scale / 100, scale / 100, ViewModel.ScalePivotX.Value, ViewModel.ScalePivotY.Value);
                } else {
                    var scaleX = ViewModel.ScaleX.Value;
                    var scaleY = ViewModel.ScaleY.Value;
                    checkValues(scaleX, scaleY);
                    matrix.ScaleAt(scaleX / 100, scaleY / 100, ViewModel.ScalePivotX.Value, ViewModel.ScalePivotY.Value);
                }
                ViewModel.EditingPath.Value = PathDrawable.Parse(ViewModel.WorkingPath.Value).Transform(matrix).Compose();
            }
            catch (Exception e) {
                LoggerEx.error(e);
            }
        }

        void Rotate() {
            try {
                var matrix = new System.Windows.Media.Matrix();
                double angle = ViewModel.RotateAngle.Value;
                double cx = ViewModel.RotatePivotX.Value;
                double cy = ViewModel.RotatePivotY.Value;
                checkValues(angle, cx, cy);
                matrix.RotateAt(angle, cx, cy);
                ViewModel.EditingPath.Value = PathDrawable.Parse(ViewModel.WorkingPath.Value).PreProcessForRotation().Transform(matrix).Compose();
            }
            catch (Exception e) {
                LoggerEx.error(e);
            }
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

            using (var graphics = new Win2DGraphics(args.DrawingSession, sender.Width, sender.Height, Windows.UI.Color.FromArgb(0xff, 0, 0xff, 0x80))) {
                try {
                    graphics.SetPathSize(ViewModel.PathWidth.Value, ViewModel.PathHeight.Value);
                    PathDrawable.Parse(ViewModel.EditingPath.Value).DrawTo(graphics);
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

        private async void OnPaste(object sender, Microsoft.UI.Xaml.Controls.TextControlPasteEventArgs e) {
            LoggerEx.debug("OnPaste");

            e.Handled = true;
            var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text)) {
                try {
                    var text = await dataPackageView.GetTextAsync();
                    var path = ViewModel.CheckAndExtractPath(text);
                    if(path!=null) {
                        ViewModel.SourcePath.Value = path;
                    }
                }
                catch (Exception) {
                    // Ignore or handle exception as needed.
                }
            }

        }

        private void OnDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e) {
            LoggerEx.debug("OnDragOver");
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Drop to load SVG Path";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
        }

        private void OnDragEnter(object sender, DragEventArgs e) {
            LoggerEx.debug("OnDragEnter");
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        }

        private void OnDrop(object sender, DragEventArgs e) {
            LoggerEx.debug("OnDrop");
            if(e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text)) {
                try {
                    var text = e.DataView.GetTextAsync().AsTask().Result;
                    var path = ViewModel.CheckAndExtractPath(text);
                    if (path != null) {
                        ViewModel.SourcePath.Value = path;
                        e.Handled = true;
                    }
                }
                catch (Exception) {
                    // Ignore or handle exception as needed.
                }
            } else if(e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems)) {
                var items = e.DataView.GetStorageItemsAsync().AsTask().Result;
                if (items != null && items[0] is Windows.Storage.StorageFile file) {
                    if(file.FileType != ".svg" && file.FileType != ".txt" && file.FileType != ".xml") {
                        return;
                    }
                    e.Handled = true;
                    DispatcherQueue.TryEnqueue(async () => {
                        var s = await FileIO.ReadTextAsync(file);
                        var path = ViewModel.CheckAndExtractPath(s);
                        if (path != null) {
                            ViewModel.SourcePath.Value = path;
                        }
                    });
                }
            }

        }
    }
}

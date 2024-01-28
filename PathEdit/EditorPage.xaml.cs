using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PathEdit.common;
using PathEdit.Graphics;
using PathEdit.Parser;
using System;
using Windows.Storage;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PathEdit;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class EditorPage : Page {
    private EditorViewModel ViewModel { get; } = new EditorViewModel();
    public EditorPage() {
        this.InitializeComponent();
        ViewModel.EditingPath.Subscribe(_ => {
            PathCanvas.Invalidate();
        });

        ViewModel.SelectedElement.Subscribe(_ => {
            PathCanvas.Invalidate();
        });
    }

    #region Drawing
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
        ViewModel.CanvasWidth.Value = sender.Width;
        ViewModel.CanvasHeight.Value = sender.Height;
        using (var graphics = new Win2DGraphics(args.DrawingSession, sender.Width, sender.Height, Windows.UI.Color.FromArgb(0xff, 0, 0xff, 0x80))) {
            try {
                graphics.SetPathSize(ViewModel.PathWidth.Value, ViewModel.PathHeight.Value);
                PathDrawable.Parse(ViewModel.EditingPath.Value).DrawTo(graphics);

                if (ViewModel.SelectedElement.Value != null) {
                    graphics.Color = Windows.UI.Color.FromArgb(0xff, 0, 0x00, 0xFF);
                    ViewModel.SelectedElement.Value.Element.Value.DrawTo(graphics);
                }

                drawGrid(args.DrawingSession, sender.Width, sender.Height);
            }
            catch (Exception e) {
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
        for (int nx = 0; nx < pw + 1; nx++) {
            ds.DrawLine(cx * nx, 0, cx * nx, (float)height, color);
        }
        for (int ny = 0; ny < ph + 1; ny++) {
            ds.DrawLine(0, cy * ny, (float)width, cy * ny, color);
        }
    }

    #endregion

    #region Clipboard / Drag & Drop

    private async void OnPaste(object sender, Microsoft.UI.Xaml.Controls.TextControlPasteEventArgs e) {
        LoggerEx.debug("OnPaste");

        e.Handled = true;
        if(ViewModel.EditablePathElement.IsEditing.Value) {
            return; // パス要素編集中はソース変更不可とする
        }
        var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
        if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text)) {
            try {
                var text = await dataPackageView.GetTextAsync();
                var path = ViewModel.CheckAndExtractPath(text);
                if (path != null) {
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
        if (ViewModel.EditablePathElement.IsEditing.Value) {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            return; // パス要素編集中はソース変更不可とする
        }
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Drop to load SVG Path";
        e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.IsContentVisible = true;
        e.DragUIOverride.IsGlyphVisible = true;
    }

    private void OnDragEnter(object sender, DragEventArgs e) {
        LoggerEx.debug("OnDragEnter");
        if (ViewModel.EditablePathElement.IsEditing.Value) {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            return; // パス要素編集中はソース変更不可とする
        }

        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
    }

    private void OnDrop(object sender, DragEventArgs e) {
        LoggerEx.debug("OnDrop");
        if (ViewModel.EditablePathElement.IsEditing.Value) {
            return; // パス要素編集中はソース変更不可とする
        }

        if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text)) {
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
        }
        else if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems)) {
            var items = e.DataView.GetStorageItemsAsync().AsTask().Result;
            if (items != null && items[0] is Windows.Storage.StorageFile file) {
                if (file.FileType != ".svg" && file.FileType != ".txt" && file.FileType != ".xml") {
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

    #endregion
}

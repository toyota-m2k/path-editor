using Microsoft.Graphics.Canvas;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PathEdit.common;
using PathEdit.Graphics;
using PathEdit.Parser;
using System;
using System.Linq;
using Windows.Storage;
using Windows.System;
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
        ViewModel.SourcePath.Subscribe(_ => {
            PathElementListView.SelectedIndex = -1;
        });
        ViewModel.ResultPath.Subscribe(_ => {
            PathCanvas.Invalidate();
        });
        ViewModel.OverlapSources.Subscribe(_ => {
            PathCanvas.Invalidate();
        });

        ViewModel.SelectedElement.Subscribe(item => {
            if (ViewModel.EditablePathElement.IsEditing.Value) {
                ViewModel.EditablePathElement.EndEdit();
                var drawable = ViewModel.EditingPathDrawable.Value;
                var element = item?.Element?.Value?.Current;
                if (drawable != null && element != null) { 
                    ViewModel.EditablePathElement.BeginEdit(drawable, element);
                }
            }
            PathCanvas.Invalidate();
        });

        ViewModel.PathElementAppendedEvent.Subscribe(OnPathElementAppended);
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
        using (var graphics = new Win2DGraphics(args.DrawingSession, sender.Width, sender.Height)) {
            try {
                graphics.SetPathSize(ViewModel.PathWidth.Value, ViewModel.PathHeight.Value);

                if (!ViewModel.MergeSources.Value) {
                    graphics.Color = Windows.UI.Color.FromArgb(0xff, 0, 0xff, 0x80);
                    PathDrawable.Parse(ViewModel.ResultPath.Value).DrawTo(graphics);

                    if (ViewModel.SelectedElement.Value != null) {
                        graphics.Color = Windows.UI.Color.FromArgb(0xff, 0, 0x00, 0xFF);
                        ViewModel.SelectedElement.Value.Element.Value.DrawTo(graphics);
                    }
                    if (ViewModel.OverlapSources.Value) {
                        var others = string.Join(" ", ViewModel.Sources.OtherPaths);
                        if (!string.IsNullOrWhiteSpace(others)) {
                            graphics.Color = Windows.UI.Color.FromArgb(0x20, 0x00, 0x00, 0x00);
                            try {
                                PathDrawable.Parse(others).DrawTo(graphics);
                            }
                            catch (Exception e) {
                                LoggerEx.error(e);
                            }
                        }
                    }
                }
                else {
                    graphics.Color = Windows.UI.Color.FromArgb(0xff, 0xff, 0x80, 0x00);
                    PathDrawable.Parse(ViewModel.ResultPath.Value).DrawTo(graphics);
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

    private bool Paste() {
        if (!ViewModel.IsSourcePathEditable.Value) {
            return false; // パス要素編集中はソース変更不可とする
        }
        var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
        if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text)) {
            try {
                var text = dataPackageView.GetTextAsync().GetAwaiter().GetResult();
                //var text = await dataPackageView.GetTextAsync();
                var path = ViewModel.CheckAndExtractPath(text);
                if (!string.IsNullOrWhiteSpace(path)) {
                    if(string.IsNullOrWhiteSpace  (ViewModel.SourcePath.Value) || ViewModel.SourcePath.Value == "M 0 0") {
                        ViewModel.SourcePath.Value = path;
                    } else {
                        ViewModel.AddSourceCommand.Execute(path);
                    }
                    return true;
                }
            }
            catch (Exception) {
                // Ignore or handle exception as needed.
            }
        }
        return false;
    }

    //private async void OnPasteToSourceTextBox(object sender, Microsoft.UI.Xaml.Controls.TextControlPasteEventArgs e) {
    //    LoggerEx.debug("OnPaste");

    //    e.Handled = true;
    //}

    private void OnDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e) {
        LoggerEx.debug("OnDragOver");
        if (!ViewModel.IsSourcePathEditable.Value) {
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
        if (!ViewModel.IsSourcePathEditable.Value) {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            return; // パス要素編集中はソース変更不可とする
        }

        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
    }

    private void OnDrop(object sender, DragEventArgs e) {
        LoggerEx.debug("OnDrop");
        if (!ViewModel.IsSourcePathEditable.Value) {
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

    #region Path Element Item Selection

    private void OnPathElementListItemClicked(object sender, ItemClickEventArgs e) {
        if (e.ClickedItem == ViewModel.SelectedElement.Value) {
            ViewModel.SelectedElement.Value = null;
        }
        else {
            ViewModel.SelectedElement.Value = (PathElementViewModel)e.ClickedItem;
        }
    }

    private void OnPathElementListSelectionChanged(object sender, SelectionChangedEventArgs e) {
        var sel = (PathElementViewModel?)e.AddedItems.FirstOrDefault();
        if (sel != ViewModel.SelectedElement.Value) {
            var prev = (PathElementViewModel?)e.RemovedItems.FirstOrDefault();
            if (prev == ViewModel.SelectedElement.Value) {
                ViewModel.SelectedElement.Value = sel;
            }
            else {
                ViewModel.SelectedElement.Value = null;
            }
        }
    }

    private void OnPathElementAppended(int index) {
        PathElementListView.SelectedIndex = index;
        ViewModel.EditCommand.Execute(ViewModel.PathElementList[index]);
    }

    #endregion

    #region Keyboard Shortcuts

    private bool IsCtrlKeyDown => (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
    private bool IsShiftKeyDown => (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

    private void OnPreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e) {
        switch (e.Key) {
            case Windows.System.VirtualKey.Escape:
                if (ViewModel.MergeSources.Value) {
                    ViewModel.MergeSources.Value = false;
                    e.Handled = true;
                }
                else if (ViewModel.EditingTransformType.Value != EditorViewModel.TransformType.None) {
                    ViewModel.EditingTransformType.Value = EditorViewModel.TransformType.None;
                    e.Handled = true;
                }
                else if (ViewModel.PathCommandDialogViewModel.IsActive.Value) {
                    ViewModel.PathCommandDialogViewModel.IsActive.Value = false;
                    e.Handled = true;
                }
                else if (ViewModel.EditablePathElement.IsEditing.Value) {
                    ViewModel.EditablePathElement.EndEdit();
                    e.Handled = true;
                }
                else if (ViewModel.SelectedElement.Value != null) {
                    ViewModel.SelectedElement.Value = null;
                    e.Handled = true;
                }
                break;
            default:
                break;
        }
    }

    private void OnKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e) {
        //if( e.OriginalSource is TextBox ) {
        //    return;
        //}
        switch (e.Key) {
            case Windows.System.VirtualKey.C:
                if (IsCtrlKeyDown) {
                    ViewModel.CopyCommand.Execute(null);
                    e.Handled = true;
                }
                break;
            case Windows.System.VirtualKey.V:
                if (IsCtrlKeyDown) {
                    e.Handled = Paste();
                }
                break;
            case Windows.System.VirtualKey.Z:
                if (IsCtrlKeyDown) {
                    if(IsShiftKeyDown) {
                        ViewModel.RedoCommand.Execute();
                        e.Handled = true;
                    }
                    else {
                        ViewModel.UndoCommand.Execute();
                        e.Handled = true;
                    }
                }
                break;
        }
    }

    #endregion

    #region Mouse Dragging

    class MouseDragger {
        EditorPage Page;
        string Target;
        Windows.Foundation.Point DragStart;
        double TargetOrgX = 0;
        double TargetOrgY = 0;

        private EditorViewModel ViewModel => Page.ViewModel;

        public static bool IsTransform(FrameworkElement view) {
            return (string)view.Tag == "ps" || (string)view.Tag == "pr";
        }

        public MouseDragger(EditorPage page, FrameworkElement view, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
            Target = (string)view.Tag;
            Page = page;
            DragStart = e.GetCurrentPoint(page).Position;
            page.CapturePointer(e.Pointer);

            switch (Target) {
                case "end":
                    TargetOrgX = ViewModel.EditablePathElement.EndPointAbsX.Value;
                    TargetOrgY = ViewModel.EditablePathElement.EndPointAbsY.Value;
                    break;
                case "c1":
                    TargetOrgX = ViewModel.EditablePathElement.Control1PointAbsX.Value;
                    TargetOrgY = ViewModel.EditablePathElement.Control1PointAbsY.Value;
                    break;
                case "c2":
                    TargetOrgX = ViewModel.EditablePathElement.Control2PointAbsX.Value;
                    TargetOrgY = ViewModel.EditablePathElement.Control2PointAbsY.Value;
                    break;
                case "ps":
                    TargetOrgX = ViewModel.ScalePivotX.Value;
                    TargetOrgY = ViewModel.ScalePivotY.Value;
                    break;
                case "pr":
                    TargetOrgX = ViewModel.RotatePivotX.Value;
                    TargetOrgY = ViewModel.RotatePivotY.Value;
                    break;
            }
        }

        public void OnPointerMoved(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
            var p = e.GetCurrentPoint(Page).Position;
            var dx = p.X - DragStart.X;
            var dy = p.Y - DragStart.Y;
            var x = TargetOrgX + dx*ViewModel.PathWidth.Value/ViewModel.CanvasWidth.Value;
            var y = TargetOrgY + dy*ViewModel.PathHeight.Value/ViewModel.CanvasHeight.Value;
            switch (Target) {
                case "end":
                    ViewModel.EditablePathElement.EndPointAbsX.Value = x;
                    ViewModel.EditablePathElement.EndPointAbsY.Value = y;
                    break;
                case "c1":
                    ViewModel.EditablePathElement.Control1PointAbsX.Value = x;
                    ViewModel.EditablePathElement.Control1PointAbsY.Value = y;
                    break;
                case "c2":
                    ViewModel.EditablePathElement.Control2PointAbsX.Value = x;
                    ViewModel.EditablePathElement.Control2PointAbsY.Value = y;
                    break;
                case "ps":
                    ViewModel.ScalePivotX.Value = x;
                    ViewModel.ScalePivotY.Value = y;
                    break;
                case "pr":
                    ViewModel.RotatePivotX.Value = x;
                    ViewModel.RotatePivotY.Value = y;
                    break;
            }
        }
        public void OnEndDrag(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
            Page.ReleasePointerCapture(e.Pointer);
        }
    }

    private MouseDragger? DragInfo = null;
    private void OnMouseClick(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        if (DragInfo != null) {
            DragInfo = null;
            return;
        }
        if (!MouseDragger.IsTransform((FrameworkElement)sender) && !ViewModel.EditablePathElement.IsEditing.Value) {
            var drawable = ViewModel.EditingPathDrawable.Value;
            var command = ViewModel.SelectedElement.Value?.Element?.Value?.Current;
            if (drawable == null || command==null) {
                return;
            }
            ViewModel.EditablePathElement.BeginEdit(drawable, command);
        }
        DragInfo = new MouseDragger(this, (FrameworkElement)sender, e);
    }

    private void OnPageMouseClicked(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        LoggerEx.debug($"{sender}");
        var p = e.GetCurrentPoint(this).Position;
        LoggerEx.debug($"{p}");

        //CapturePointer(e.Pointer);
    }

    private void OnPageMouseMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        //LoggerEx.debug($"{sender}");
        DragInfo?.OnPointerMoved(e);
    }

    private void OnPageMouseReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        LoggerEx.debug($"{sender}");
        DragInfo?.OnEndDrag(e);
        DragInfo = null;
    }
    #endregion

    private void OnSourceListItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args) {
        ViewModel.SetCurrentSourceCommand.Execute(args.InvokedItem);
    }

}

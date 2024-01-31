using Microsoft.UI.Xaml;
using Reactive.Bindings;
using System;
using PathEdit.Parser;
using System.Reactive.Linq;
using PathEdit.common;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using PathEdit.Parser.Command;
using System.Security.Cryptography.X509Certificates;
using Windows.Globalization.NumberFormatting;

namespace PathEdit;

public class EditorViewModel {
    #region Primitive Properties

    /**
     * ソースパス文字列
     */
    //public ReactiveProperty<string> SourcePath { get; } = new("M22,11L12,21L2,11H8V3H16V11H22M12,18L17,13H14V5H10V13H7L12,18Z");
    public ReactiveProperty<string> SourcePath { get; } = new("M6,3A1,1 0 0,1 7,4V4.88C8.06,4.44 9.5,4 11,4C14,4 14,6 16,6C19,6 20,4 20,4V12C20,12 19,14 16,14C13,14 13,12 11,12C8,12 7,14 7,14V21H5V4A1,1 0 0,1 6,3Z");
    /**
     * 編集中のパス文字列（作業用）
     */
    public ReactiveProperty<string> WorkingPath { get; } = new();
    /**
     * 編集中の解析済みパス情報
     * mode に、RaiseLatestValueOnSubscribeをセットする（DefaultのDistinctUntilChangedを無効化する）ことにより、同じValueをセットしたときにも変更イベントが発生する。
     * PathDrawableの中味を変更して、それを表示に反映する場合(UpdateEditingPathDrawable)に利用する。
     */
    public ReactiveProperty<PathDrawable?> EditingPathDrawable { get; } = new(PathDrawable.Parse("M 0 0"), ReactivePropertyMode.RaiseLatestValueOnSubscribe);
    /**
     * 編集中のパス文字列（表示用 ... EditingPathDrawableからComposeして生成する）
     */
    public ReadOnlyReactiveProperty<string> EditingPath { get; }

    /**
     * パスの幅と高さ： 通常よく使っているのは 24x24 だが変更できるようにしておく。
     */
    public ReactiveProperty<int> PathWidth { get; } = new(24);
    public ReactiveProperty<int> PathHeight { get; } = new(24);

    /**
     * CanvasControlの幅と高さ
     */
    public ReactiveProperty<double> CanvasWidth { get; } = new(400);
    public ReactiveProperty<double> CanvasHeight { get; } = new(400);

    #endregion

    #region Commands

    public ReactiveCommand<PathElementViewModel> EditCommand { get; } = new();
    public ReactiveCommand<PathElementViewModel> InsertCommand { get; } = new();
    public ReactiveCommand<PathElementViewModel> DeleteCommand { get; } = new();
    public ReactiveCommand<string> EndEditElementCommand { get; } = new();
    public ReactiveCommand CopyCommand { get; } = new();

    #endregion

    #region Transform Type
    public enum TransformType {
        None,
        Scale,
        Translate,
        Rotate,
        Mirror,
        Round,
    }
    public ReactiveProperty<TransformType> EditingTransformType { get; } = new(TransformType.None);

    #endregion

    #region Scale

    public ReactiveProperty<bool> EditingScale { get; } = new(false);
    public ReactiveProperty<bool> KeepAspect { get; } = new(true);
    public ReactiveProperty<double> Scale { get; } = new(100);
    public ReactiveProperty<double> ScaleX { get; } = new(100);
    public ReactiveProperty<double> ScaleY { get; } = new(100);
    public ReactiveProperty<double> ScalePivotX { get; } = new(12);
    public ReactiveProperty<double> ScalePivotY { get; } = new(12);
    public ReadOnlyReactiveProperty<double> ScalePivotOnScreenX { get; }
    public ReadOnlyReactiveProperty<double> ScalePivotOnScreenY { get; }
    public ReactiveCommand ScalePivotStepMinusX { get; } = new();
    public ReactiveCommand ScalePivotStepPlusX { get; } = new();
    public ReactiveCommand ScalePivotStepMinusY { get; } = new();
    public ReactiveCommand ScalePivotStepPlusY { get; } = new();
    public ReactiveCommand ScaleStepMinus { get; } = new();
    public ReactiveCommand ScaleStepPlus { get; } = new();
    public ReactiveCommand ScaleStepMinusX { get; } = new();
    public ReactiveCommand ScaleStepPlusX { get; } = new();
    public ReactiveCommand ScaleStepMinusY { get; } = new();
    public ReactiveCommand ScaleStepPlusY { get; } = new();

    #endregion

    #region Translation

    public ReactiveProperty<bool> EditingTranslate { get; } = new(false);

    public ReactiveProperty<double> TranslateX { get; } = new(0);
    public ReactiveProperty<double> TranslateY { get; } = new(0);

    public ReactiveCommand<string> TranslateStepMinusX { get; } = new();
    public ReactiveCommand<string> TranslateStepPlusX { get; } = new();
    public ReactiveCommand<string> TranslateStepMinusY { get; } = new();
    public ReactiveCommand<string> TranslateStepPlusY { get; } = new();

    #endregion

    #region Mirror            

    public ReactiveProperty<bool> EditingMirror { get; } = new(false);

    public ReactiveCommand<string> MirrorCommand { get; } = new();

    #endregion

    #region Rotation

    public ReactiveProperty<bool> EditingRotation { get; } = new(false);

    public ReactiveProperty<double> RotatePivotX { get; } = new(12);
    public ReactiveProperty<double> RotatePivotY { get; } = new(12);
    public ReactiveProperty<double> RotateAngle { get; } = new(0);
    public ReadOnlyReactiveProperty<double> RotatePivotOnScreenX { get; }
    public ReadOnlyReactiveProperty<double> RotatePivotOnScreenY { get; }

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

    #region Path Element List

    public ReactiveProperty<PathElementViewModel?> SelectedElement { get; } = new();
    public ObservableCollection<PathElementViewModel> PathElementList { get; } = new();
    public ReactiveProperty<bool> ShowAbsolute { get; } = new(true);

    #endregion

    #region Edit Path Element

    public void UpdateEditingPathDrawable() {
        var drawable = EditingPathDrawable.Value;
        if (drawable != null) {
            EditingPathDrawable.Value = drawable;
        }
    }

    public ReadOnlyReactiveProperty<Visibility> EndPointMarkVisibility { get; }
    public ReadOnlyReactiveProperty<double> EndPointOnScreenX { get; }
    public ReadOnlyReactiveProperty<double> EndPointOnScreenY { get; }

    public ReadOnlyReactiveProperty<Visibility> StartPointMarkVisibility { get; }
    public ReadOnlyReactiveProperty<double> StartPointOnScreenX { get; }
    public ReadOnlyReactiveProperty<double> StartPointOnScreenY { get; }

    public ReadOnlyReactiveProperty<Visibility> Control1PointMarkVisibility { get; }
    public ReadOnlyReactiveProperty<double> Control1PointOnScreenX { get; }
    public ReadOnlyReactiveProperty<double> Control1PointOnScreenY { get; }

    public ReadOnlyReactiveProperty<Visibility> Control2PointMarkVisibility { get; }
    public ReadOnlyReactiveProperty<double> Control2PointOnScreenX { get; }
    public ReadOnlyReactiveProperty<double> Control2PointOnScreenY { get; }


    public EditablePathElement EditablePathElement { get; } = new();
    public PathCommandDialogViewModel PathCommandDialogViewModel { get; } = new();
    public ReactiveCommand<int> PathElementAppendedEvent { get; } = new();

    public DecimalFormatter CoordinateFormatter { get; } = new() {
        FractionDigits = 2,
        IsZeroSigned = false,
        NumberRounder = new IncrementNumberRounder() {
            Increment = 0.0001,
            RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp,
        }
    };

    #endregion

    public EditorViewModel() {
        EditingPath = EditingPathDrawable.Select(d => {
            return d?.Compose() ?? "";
        }).ToReadOnlyReactiveProperty<string>();

        EditingPathDrawable.Subscribe(d => {
            if (d == null) {
                PathElementList.Clear();
                return;
            }
            d.ResolveEndPoint();
            var prev = (PathCommand?)null;
            int count = 0;
            foreach (var c in d.Commands) {
                var element = new PathElement(c, prev);
                //element.Dump();
                prev = c;
                if (count < PathElementList.Count) {
                    PathElementList[count].Element.Value = element;
                }
                else {
                    PathElementList.Add(new PathElementViewModel(element, ShowAbsolute, SelectedElement, EditCommand, InsertCommand, DeleteCommand));
                }
                count++;
            }
            for (int i = PathElementList.Count - 1; count <= i; i--) {
                PathElementList.RemoveAt(i);
            }
        });

        #region Command Type

        SourcePath.Subscribe(path => {
            if (string.IsNullOrWhiteSpace(path)) {
                SourcePath.Value = "M 0 0";
                return;
            }
            UpdatePathDrawable(path);
            WorkingPath.Value = path;
        });

        EditingTransformType.Subscribe(m => {
            EditingScale.Value = m == TransformType.Scale;
            EditingTranslate.Value = m == TransformType.Translate;
            EditingRotation.Value = m == TransformType.Rotate;
            EditingMirror.Value = m == TransformType.Mirror;
            EditingRound.Value = m == TransformType.Round;

            if (m != TransformType.None) {
                WorkingPath.Value = EditingPath.Value;
            }
        });

        var changeEditingType = (TransformType type, bool on) => {
            if(on) {
                EditingTransformType.Value = type;
                return true;
            } else if(EditingTransformType.Value == type) {
                EditingTransformType.Value = TransformType.None;
            }
            return false;
        };

        EditingScale.Subscribe(b => {
            if(changeEditingType(TransformType.Scale, b)) {
                Scale.Value = 100;
                ScaleX.Value = 100;
                ScaleY.Value = 100;
            }
        });
        EditingTranslate.Subscribe(b => {
            if (changeEditingType(TransformType.Translate, b)) {
                TranslateX.Value = 0;
                TranslateY.Value = 0;
            }
        });
        EditingRotation.Subscribe(b => {
            if (changeEditingType(TransformType.Rotate, b)) {
                RotateAngle.Value = 0;
            }
        });
        EditingMirror.Subscribe(b => {
            changeEditingType(TransformType.Mirror, b);
        });
        EditingRound.Subscribe(b => {
            changeEditingType(TransformType.Round, b);
        });

        #endregion

        #region Property Mapping

        var ScreenX = (double x, double canvasWidth) => x * canvasWidth / PathWidth.Value;
        var ScreenY = (double y, double canvasHeight) => y * canvasHeight / PathHeight.Value;

        ScalePivotOnScreenX = ScalePivotX.CombineLatest(CanvasWidth, ScreenX).ToReadOnlyReactiveProperty();
        ScalePivotOnScreenY = ScalePivotY.CombineLatest(CanvasHeight, ScreenY).ToReadOnlyReactiveProperty();
        RotatePivotOnScreenX = RotatePivotX.CombineLatest(CanvasWidth, ScreenX).ToReadOnlyReactiveProperty();
        RotatePivotOnScreenY = RotatePivotY.CombineLatest(CanvasHeight, ScreenY).ToReadOnlyReactiveProperty();

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
        RotateAngleStepMinus.Subscribe(_ => SetRotateAngle(RotateAngle.Value - 1));
        RotateAngleStepPlus.Subscribe(_ => SetRotateAngle(RotateAngle.Value + 1));

        #endregion

        #region Transform Actions

        TranslateX.Subscribe(OnTranslate);
        TranslateY.Subscribe(OnTranslate);
        Scale.Subscribe(OnScale);
        ScaleX.Subscribe(OnScale);
        ScaleY.Subscribe(OnScale);
        ScalePivotX.Subscribe(OnScale);
        ScalePivotY.Subscribe(OnScale);

        RotateAngle.Subscribe(OnRotate);
        RotatePivotX.Subscribe(OnRotate);
        RotatePivotY.Subscribe(OnRotate);

        MirrorCommand.Subscribe(OnMirror);
        RoundCommand.Subscribe(OnRound);

        #endregion

        #region Commands

        CopyCommand.Subscribe(OnCopy);
        EditCommand.Subscribe(OnEditElement);
        InsertCommand.Subscribe(OnInsertElement);
        DeleteCommand.Subscribe(OnDeleteElement);
        EndEditElementCommand.Subscribe(OnEndEditElement);

        #endregion

        #region Path Element Params

        EndPointMarkVisibility = Observable.CombineLatest(SelectedElement, EditingPath, (selected,_) => (selected?.Element.Value.HasEnd == true) ? Visibility.Visible : Visibility.Collapsed).ToReadOnlyReactiveProperty();
        //EndPointOnScreenX = Observable.CombineLatest(SelectedElement, EditablePathElement.TargetElement, EditingPath, (selected,target, _)=>
        //{
        //    return (target ?? selected?.Element?.Value)?.EndPointAbs.X ?? 0;
        //}).CombineLatest(CanvasWidth, (x,w)=> {
        //    var xx = ScreenX(x, w);
        //    Logger.debug($"EndPointOnScreenX: combined {xx}");
        //    return xx;
        //}).ToReadOnlyReactiveProperty();
        //EndPointOnScreenX.Subscribe(x =>
        //{
        //    LoggerEx.debug($"EndPointOnScreenX subscribed ={x}");
        //});
        EndPointOnScreenX = Observable.CombineLatest(SelectedElement, EditablePathElement.TargetElement, (selected, target) => (target ?? selected?.Element.Value)?.EndPointAbs.X ?? 0).CombineLatest(CanvasWidth, ScreenX).ToReadOnlyReactiveProperty();
        EndPointOnScreenY = Observable.CombineLatest(SelectedElement, EditablePathElement.TargetElement, (selected, target) => (target ?? selected?.Element.Value)?.EndPointAbs.Y ?? 0).CombineLatest(CanvasHeight, ScreenY).ToReadOnlyReactiveProperty();

        StartPointMarkVisibility = SelectedElement.Select(selected => (selected?.Element.Value.HasStart == true) ? Visibility.Visible : Visibility.Collapsed).ToReadOnlyReactiveProperty();
        StartPointOnScreenX = Observable.CombineLatest(SelectedElement, EditablePathElement.TargetElement, (selected, target) => (target ?? selected?.Element.Value)?.StartPoint.X ?? 0).CombineLatest(CanvasWidth, ScreenX).ToReadOnlyReactiveProperty();
        StartPointOnScreenY = Observable.CombineLatest(SelectedElement, EditablePathElement.TargetElement, (selected, target) => (target ?? selected?.Element.Value)?.StartPoint.Y ?? 0).CombineLatest(CanvasHeight, ScreenY).ToReadOnlyReactiveProperty();

        Control1PointMarkVisibility = SelectedElement.Select(selected => (selected?.Element.Value.HasControl1 == true) ? Visibility.Visible : Visibility.Collapsed).ToReadOnlyReactiveProperty();
        Control1PointOnScreenX = Observable.CombineLatest(SelectedElement, EditablePathElement.TargetElement, (selected, target) => (target ?? selected?.Element.Value)?.Control1Abs.X ?? 0).CombineLatest(CanvasWidth, ScreenX).ToReadOnlyReactiveProperty();
        Control1PointOnScreenY = Observable.CombineLatest(SelectedElement, EditablePathElement.TargetElement, (selected, target) => (target ?? selected?.Element.Value)?.Control1Abs.Y ?? 0).CombineLatest(CanvasHeight, ScreenY).ToReadOnlyReactiveProperty();

        Control2PointMarkVisibility = SelectedElement.Select(selected => (selected?.Element.Value.HasControl2 == true) ? Visibility.Visible : Visibility.Collapsed).ToReadOnlyReactiveProperty();
        Control2PointOnScreenX = Observable.CombineLatest(SelectedElement, EditablePathElement.TargetElement, (selected, target) => (target ?? selected?.Element.Value)?.Control2Abs.X ?? 0).CombineLatest(CanvasWidth, ScreenX).ToReadOnlyReactiveProperty();
        Control2PointOnScreenY = Observable.CombineLatest(SelectedElement, EditablePathElement.TargetElement, (selected, target) => (target ?? selected?.Element.Value)?.Control2Abs.Y ?? 0).CombineLatest(CanvasHeight, ScreenY).ToReadOnlyReactiveProperty();

        #endregion

        #region Editing Path Element

        EditablePathElement.GeneratedPathCommand.Subscribe(cmd => {
            if (cmd!=null) {
                var drawable = EditingPathDrawable.Value;
                if (drawable != null) {
                    var newDrawable = drawable.ReplaceCommand(EditablePathElement.ElementIndex, cmd);
                    EditingPathDrawable.Value = newDrawable;
                }
            }
        });


        #endregion
    }

    /**
     * パス文字列をセットする。
     * 無効な文字列の場合は、デフォルトのパスをセットする。(暫定）
     * ToDo: nullをセットしておいて、描画時にエラー画面を描画するようにしたい。
     */
    private void UpdatePathDrawable(string path) {
        try {
            var drawable = PathDrawable.Parse(path);
            EditingPathDrawable.Value = drawable;
        }
        catch (Exception) {
            // EditingPathDrawable.Value = PathDrawable.Parse("M 0 0");
        }
    }

    /**
     * 角度をセットする。
     * 0<=angle<360の範囲に収める。
     */
    private void SetRotateAngle(double angle) {
        angle = angle % 360;
        if (angle < 0) {
            angle += 360;
        }
        RotateAngle.Value = angle;
    }

    /**
     * 平行移動
     */
    private void OnTranslate(double _) {
        if (EditingTransformType.Value != TransformType.Translate) {
            return;
        }
        try {
            var matrix = new System.Windows.Media.Matrix();
            double tx = TranslateX.Value;
            double ty = TranslateY.Value;
            checkValues(tx, ty);
            matrix.Translate(tx, ty);
            EditingPathDrawable.Value = PathDrawable.Parse(WorkingPath.Value).Transform(matrix);
        }
        catch (Exception e) {
            LoggerEx.error(e);
        }
    }

    /**
     * 拡大縮小
     */
    private void OnScale(double _) {
        if (EditingTransformType.Value != TransformType.Scale) {
            return;
        }
        try {
            var matrix = new System.Windows.Media.Matrix();
            if (KeepAspect.Value) {
                var scale = Scale.Value;
                checkValues(scale);
                matrix.ScaleAt(scale / 100, scale / 100, ScalePivotX.Value, ScalePivotY.Value);
            }
            else {
                var scaleX = ScaleX.Value;
                var scaleY = ScaleY.Value;
                checkValues(scaleX, scaleY);
                matrix.ScaleAt(scaleX / 100, scaleY / 100, ScalePivotX.Value, ScalePivotY.Value);
            }
            EditingPathDrawable.Value = PathDrawable.Parse(WorkingPath.Value).Transform(matrix);
        }
        catch (Exception e) {
            LoggerEx.error(e);
        }
    }

    /**
     * 回転
     */
    private void OnRotate(double _) {
        try {
            var matrix = new System.Windows.Media.Matrix();
            double angle = RotateAngle.Value;
            double cx = RotatePivotX.Value;
            double cy = RotatePivotY.Value;
            checkValues(angle, cx, cy);
            matrix.RotateAt(angle, cx, cy);
            EditingPathDrawable.Value = PathDrawable.Parse(WorkingPath.Value).PreProcessForRotation().Transform(matrix);
        }
        catch (Exception e) {
            LoggerEx.error(e);
        }
    }

    /**
     * 線対称変換
     */
    private void OnMirror(string cmd) {
        try {
            var matrix = new System.Windows.Media.Matrix();
            checkValues(PathHeight.Value, PathWidth.Value);
            if (cmd == "V") {
                matrix.ScaleAt(1, -1, PathWidth.Value / 2, PathHeight.Value / 2);
            }
            else {
                matrix.ScaleAt(-1, 1, PathWidth.Value / 2, PathHeight.Value / 2);
            }
            EditingPathDrawable.Value = PathDrawable.Parse(EditingPath.Value).Transform(matrix);
        }
        catch (Exception e) {
            LoggerEx.error(e);
        }
    }

    /**
     * 座標値を丸める
     */
    private void OnRound() {
        try {
            EditingPathDrawable.Value = PathDrawable.Parse(WorkingPath.Value).RoundCoordinateValue(RoundDigit.Value);
        }
        catch (Exception e) {
            LoggerEx.error(e);
        }
    }

    /**
     * 編集結果のパス文字列をクリップボードにコピーする。
     */
    private void OnCopy() {
        try {
            var path = EditingPath.Value;
            if (string.IsNullOrWhiteSpace(path)) {
                return;
            }
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(path);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
        catch (Exception e) {
            LoggerEx.error(e);
        }
    }

    private void OnDeleteElement(PathElementViewModel model) {
        LoggerEx.debug($"OnDeleteElement: {model.CommandName.Value}");
        var node = model.Element.Value;
        if (node.Prev == null) {
            return;
        }
        var drawable = EditingPathDrawable.Value;
        if (drawable == null) {
            return;
        }
        EditingPathDrawable.Value = drawable.RemoveCommand(node.Current)/*.Clone()*/;   // DistinctUntilChangedを指定していないので同じオブジェクトをセットするだけで更新がかかるはず。
    }

    private void OnEditElement(PathElementViewModel model) {
        LoggerEx.debug($"OnDeleteElement: {model.CommandName.Value}");
        var editing = EditingPathDrawable.Value;
        if(editing == null) {
            return;
        }
        EditablePathElement.BeginEdit(editing, model.Element.Value.Current);
    }

    private void OnEndEditElement(string done) {
        if(done == "true") {
            // apply changes
        } else {
            // cancel
        }
        EditablePathElement.EndEdit();
    }

    private async void OnInsertElement(PathElementViewModel model) {
        LoggerEx.debug($"OnInsertElement: {model.CommandName.Value}");
        var drawable = EditingPathDrawable.Value;
        if(drawable == null) {
            return;
        }
        var cmd = await PathCommandDialogViewModel.ShowDialogAsync(model.Element.Value);
        if(cmd!=null) {
            var index = drawable.IndexOf(model.Element.Value.Current)+1;
            EditingPathDrawable.Value = drawable.InsertCommand(index, cmd);
            PathElementAppendedEvent.Execute(index);
        }
    }
    /**
     * 座標値のチェック
     */
    private void checkValues(params double[] values) {
        foreach (var value in values) {
            if (double.IsNaN(value) || double.IsInfinity(value)) {
                throw new ArgumentException();
            }
        }
    }

    static Regex pathPattern = new Regex("""(?:\s+d|:pathData|\s+Data)="(?<path>[^"]+)["]""");
    static Regex pathPattern2 = new Regex("""["](?<path>[^"]+)["]""");

    /**
     * 文字列からパス文字列を抽出する。
     * サポートする書式
     * - SVGファイル形式、
     * - XAML <PathIcon> 中の Data 文字列、
     * - Androidのlayout.xml中の pathData 文字列
     * - 二重引用符で囲まれたPath文字列
     */
    public string? CheckAndExtractPath(string? src) {
        if (string.IsNullOrWhiteSpace(src)) {
            return null;
        }
        try {
            var r = PathDrawable.Parse(src).Compose();
            if (!string.IsNullOrWhiteSpace(r)) {
                return r;
            }
        }
        catch (Exception) {
        }

        var m1 = pathPattern.Match(src);
        var path = m1.Groups["path"].Value;
        if (!string.IsNullOrEmpty(path)) {
            return path;
        }
        var m2 = pathPattern2.Match(src);
        return m2.Groups["path"].Value;
    }
}


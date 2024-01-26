using Microsoft.UI.Xaml;
using Reactive.Bindings;
using System;
using PathEdit.Parser;
using System.Reactive.Linq;
using PathEdit.common;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using PathEdit.Parser.Command;

namespace PathEdit;

public class MainWindowViewModel {
    #region Primitive Properties
    /**
     * ソースパス文字列
     */
    public ReactiveProperty<string> SourcePath { get; } = new("M22,11L12,21L2,11H8V3H16V11H22M12,18L17,13H14V5H10V13H7L12,18Z");
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
    public ReactiveCommand<PathElementViewModel> DeleteCommand { get; } = new();
    public ReactiveCommand CopyCommand { get; } = new();

    #endregion

    #region Command Type
    public enum CommandType {
        None,
        Scale,
        Translate,
        Rotate,
        Mirror,
        Round,
    }
    public ReactiveProperty<CommandType> EditingCommandType { get; } = new(CommandType.None);

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
    public ReadOnlyReactiveProperty<Visibility> SingleScaleVisibility { get; }
    public ReadOnlyReactiveProperty<Visibility> DoubleScaleVisibility { get; }
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

    public ReactiveProperty<double> RotatePivotX { get; } = new(12, ReactivePropertyMode.DistinctUntilChanged);
    public ReactiveProperty<double> RotatePivotY { get; } = new(12, ReactivePropertyMode.DistinctUntilChanged);
    public ReactiveProperty<double> RotateAngle { get; } = new(0, ReactivePropertyMode.DistinctUntilChanged);
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

    public ReactiveProperty<PathElementViewModel> SelectedElement { get; } = new();
    public ObservableCollection<PathElementViewModel> PathElementList { get; } = new();
    public ReactiveProperty<bool> ShowAbsolute { get; } = new(true);

    #endregion

    #region Edit Path Element

    public void UpdateEditingPathDrawable() {
        var drawable = EditingPathDrawable.Value;
        if(drawable!=null) {
            EditingPathDrawable.Value = drawable;
        }
    }

    public ReactiveProperty<bool> ElementEditing { get; } = new();
    public ReadOnlyReactiveProperty<Visibility> EndPointMarkVisibility { get; }
    public ReadOnlyReactiveProperty<double> EndPointX { get; }
    public ReadOnlyReactiveProperty<double> EndPointY { get; }

    public ReadOnlyReactiveProperty<Visibility> StartPointMarkVisibility { get; }
    public ReadOnlyReactiveProperty<double> StartPointX { get; }
    public ReadOnlyReactiveProperty<double> StartPointY { get; }

    public ReadOnlyReactiveProperty<Visibility> Control1PointMarkVisibility { get; }
    public ReadOnlyReactiveProperty<double> Control1PointX { get; }
    public ReadOnlyReactiveProperty<double> Control1PointY { get; }

    public ReadOnlyReactiveProperty<Visibility> Control2PointMarkVisibility { get; }
    public ReadOnlyReactiveProperty<double> Control2PointX { get; }
    public ReadOnlyReactiveProperty<double> Control2PointY { get; }


    #endregion

    public MainWindowViewModel() {
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
                element.Dump();
                prev = c;
                if (count < PathElementList.Count) {
                    PathElementList[count].Element.Value = element;
                }
                else {
                    PathElementList.Add(new PathElementViewModel(element, ShowAbsolute, SelectedElement, EditCommand, DeleteCommand));
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

        EditingCommandType.Subscribe(m => {
            EditingScale.Value = m == CommandType.Scale;
            EditingTranslate.Value = m == CommandType.Translate;
            EditingRotation.Value = m == CommandType.Rotate;
            EditingMirror.Value = m == CommandType.Mirror;
            EditingRound.Value = m == CommandType.Round;

            WorkingPath.Value = EditingPath.Value;
        });

        EditingScale.Subscribe(b => {
            if (b && EditingCommandType.Value != CommandType.Scale) {
                Scale.Value = 100;
                ScaleX.Value = 100;
                ScaleY.Value = 100;
                EditingCommandType.Value = CommandType.Scale;
            }
        });
        EditingTranslate.Subscribe(b => {
            if (b && EditingCommandType.Value != CommandType.Translate) {
                TranslateX.Value = 0;
                TranslateY.Value = 0;
                EditingCommandType.Value = CommandType.Translate;
            }
        });
        EditingRotation.Subscribe(b => {
            if (b && EditingCommandType.Value != CommandType.Rotate) {
                EditingCommandType.Value = CommandType.Rotate;
            }
        });
        EditingMirror.Subscribe(b => {
            if (b && EditingCommandType.Value != CommandType.Mirror) {
                EditingCommandType.Value = CommandType.Mirror;
            }
        });
        EditingRound.Subscribe(b => {
            if (b && EditingCommandType.Value != CommandType.Round) {
                EditingCommandType.Value = CommandType.Round;
            }
        });

        #endregion

        #region Property Mapping

        ScalePivotOnScreenX = ScalePivotX.CombineLatest(CanvasWidth, (x, w) => x * w / PathWidth.Value).ToReadOnlyReactiveProperty();
        ScalePivotOnScreenY = ScalePivotY.CombineLatest(CanvasHeight, (y, h) => y * h / PathHeight.Value).ToReadOnlyReactiveProperty();
        RotatePivotOnScreenX = RotatePivotX.CombineLatest(CanvasWidth, (x, w) => x * w / PathWidth.Value).ToReadOnlyReactiveProperty();
        RotatePivotOnScreenY = RotatePivotY.CombineLatest(CanvasHeight, (y, h) => y * h / PathHeight.Value).ToReadOnlyReactiveProperty();

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
        DeleteCommand.Subscribe(OnDeleteElement);

        #endregion

        #region Edit Path Element

        EndPointMarkVisibility = SelectedElement.Select( selected => (selected?.Element.Value.HasEnd == true) ? Visibility.Visible : Visibility.Collapsed).ToReadOnlyReactiveProperty();
        EndPointX = SelectedElement.Select(selected => selected?.Element.Value.EndPoint.X ?? 0).ToReadOnlyReactiveProperty();
        EndPointY = SelectedElement.Select(selected => selected?.Element.Value.EndPoint.Y ?? 0).ToReadOnlyReactiveProperty();

        StartPointMarkVisibility = SelectedElement.Select(selected => (selected?.Element.Value.HasStart==true) ? Visibility.Visible : Visibility.Collapsed).ToReadOnlyReactiveProperty();
        StartPointX = SelectedElement.Select(selected => selected?.Element.Value.StartPoint.X ?? 0).ToReadOnlyReactiveProperty();
        StartPointY = SelectedElement.Select(selected => selected?.Element.Value.StartPoint.Y ?? 0).ToReadOnlyReactiveProperty();

        Control1PointMarkVisibility = SelectedElement.Select(selected => (selected?.Element.Value.HasControl1 == true) ? Visibility.Visible : Visibility.Collapsed).ToReadOnlyReactiveProperty();
        Control1PointX = SelectedElement.Select(selected => selected?.Element.Value.Control1.X ?? 0).ToReadOnlyReactiveProperty();
        Control1PointY = SelectedElement.Select(selected => selected?.Element.Value.Control1.Y ?? 0).ToReadOnlyReactiveProperty();

        Control2PointMarkVisibility = SelectedElement.Select(selected => (selected?.Element.Value.HasControl2 == true) ? Visibility.Visible : Visibility.Collapsed).ToReadOnlyReactiveProperty();
        Control2PointX = SelectedElement.Select(selected => selected?.Element.Value.Control2.X ?? 0).ToReadOnlyReactiveProperty();
        Control2PointY = SelectedElement.Select(selected => selected?.Element.Value.Control2.Y ?? 0).ToReadOnlyReactiveProperty();

        #endregion
    }

    /**
     * パス文字列をセットする。
     * 無効な文字列の場合は、デフォルトのパスをセットする。(暫定）
     * ToDo: nullをセットしておいて、描画時にエラー画面を描画するようにしたい。
     */
    private void UpdatePathDrawable(string path) {
        try {
            EditingPathDrawable.Value = PathDrawable.Parse(path);
        }
        catch (Exception) {
            EditingPathDrawable.Value = PathDrawable.Parse("M 0 0V 24H 24V 0");
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
        if (EditingCommandType.Value != MainWindowViewModel.CommandType.Translate) {
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
        if (EditingCommandType.Value != MainWindowViewModel.CommandType.Scale) {
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
        if(node.Prev == null) {
            return;
        }
        var drawable = EditingPathDrawable.Value;
        if(drawable == null) {
            return;
        }
        EditingPathDrawable.Value = drawable.RemoveCommand(node.Current)/*.Clone()*/;   // DistinctUntilChangedを指定していないので同じオブジェクトをセットするだけで更新がかかるはず。
    }

    private void OnEditElement(PathElementViewModel model) {
        LoggerEx.debug($"OnDeleteElement: {model.CommandName.Value}");
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

    static Regex pathPattern = new Regex("""\s*(?:d|android:pathData|Data)="(?<path>[^"]+)["]""");
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


using PathEdit.Parser.Command;
using PathEdit.Parser;
using Reactive.Bindings;
using System.Reactive.Linq;
using System.Windows;
using System.Net;
using Windows.Devices.Radios;
using System;

namespace PathEdit;

public class EditablePathElement {
    private ReactiveProperty<PathElement?> TargetElement { get; } = new();
    public void BeginEdit(PathElement element) {
        _isModified = false;
        IsRelative.Value = element.IsRelative;
        EndPointAbsX.Value = element.EndPointAbs.X;
        EndPointAbsY.Value = element.EndPointAbs.Y;

        Control1PointAbsX.Value = element.Control1Abs.X;
        Control1PointAbsY.Value = element.Control1Abs.Y;

        Control2PointAbsX.Value = element.Control2Abs.X;
        Control2PointAbsY.Value = element.Control2Abs.Y;

        // Arc
        Radius.Value = element.Radius;
        RotationAngle.Value = element.RotationAngle;
        IsLargeArc.Value = element.IsLargeArc;
        SweepDirection.Value = element.SweepDirection;

        TargetElement.Value = element;
    }

    private bool _isModified = false;
    public bool IsModified {         
        get {
            var element = TargetElement.Value;
            if (element == null) {
                return false;
            }
            if (_isModified) { 
                return true;
            }
            _isModified = 
                EndPointAbs.Value != element.EndPointAbs ||
                IsRelative.Value != element.IsRelative ||
                Control1PointAbs.Value != element.Control1Abs ||
                Control2PointAbs.Value != element.Control2Abs ||
                Radius.Value != element.Radius ||
                RotationAngle.Value != element.RotationAngle ||
                IsLargeArc.Value != element.IsLargeArc ||
                SweepDirection.Value != element.SweepDirection;
            return _isModified;
        }
    }

    public void EndEdit() {
        TargetElement.Value = null;
        _isModified = false;
    }
    public ReadOnlyReactiveProperty<bool> IsEditing { get; }
    public PathElement? TargetElementRef => TargetElement.Value;


    public ReadOnlyReactiveProperty<string> CommandName { get; }
    public ReadOnlyReactiveProperty<string> DisplayName { get; }
    public ReadOnlyReactiveProperty<Point> StartPoint { get; }

    public ReactiveProperty<bool> IsRelative { get; } = new();
    public ReadOnlyReactiveProperty<bool> IsCloseCommand { get; }
    public ReadOnlyReactiveProperty<bool> IsArcCommand { get; }
    public ReadOnlyReactiveProperty<bool> HasControl1 { get; }
    public ReadOnlyReactiveProperty<bool> HasControl2 { get; }
    public ReadOnlyReactiveProperty<bool> IsControl1Editable { get; }   // SmoothBezierCommandの場合は、Control1は自動計算されるので編集不可
    public ReadOnlyReactiveProperty<bool> IsEndPointXEditable { get; }  // V commandの場合は、X座標は自動計算されるので編集不可
    public ReadOnlyReactiveProperty<bool> IsEndPointYEditable { get; }  // H commandの場合は、Y座標は自動計算されるので編集不可

    public ReactiveProperty<double> EndPointAbsX { get; } = new();
    public ReactiveProperty<double> EndPointAbsY { get; } = new();
    public ReadOnlyReactiveProperty<Point> EndPointAbs { get; }
    public ReadOnlyReactiveProperty<Point> EndPoint { get; }
    public ReactiveCommand<string> EndPointCommand { get; } = new();

    // Bezier
    public ReactiveProperty<double> Control1PointAbsX { get; } = new();
    public ReactiveProperty<double> Control1PointAbsY { get; } = new();
    public ReadOnlyReactiveProperty<Point> Control1PointAbs { get; }
    public ReadOnlyReactiveProperty<Point> Control1Point { get; }

    public ReactiveProperty<double> Control2PointAbsX { get; } = new();
    public ReactiveProperty<double> Control2PointAbsY { get; } = new();
    public ReadOnlyReactiveProperty<Point> Control2PointAbs { get; }
    public ReadOnlyReactiveProperty<Point> Control2Point { get; }

    // Arc
    public ReactiveProperty<Size> Radius { get; } = new();
    public ReactiveProperty<double> RotationAngle { get; } = new();
    public ReactiveProperty<bool> IsLargeArc { get; } = new();
    public ReactiveProperty<bool> SweepDirection { get; } = new();

    public ReadOnlyReactiveProperty<PathCommand> GeneratedPathCommand { get; }

    public Point PointDependsOnRelativeFlag(Point p, bool isRelative) {
        if (!isRelative) {
            return p;
        }
        else {
            return new Point(p.X - StartPoint.Value.X, p.Y - StartPoint.Value.Y);
        }
    }

    private PathCommand? _workingCommand = null;
    private PathCommand WorkingCommand {
        get {
            if (_workingCommand == null || _workingCommand.GetType() != TargetElement.Value?.Current?.GetType()) {
                _workingCommand = TargetElement.Value?.Current?.Clone() ?? new CloseCommand();
            }
            return _workingCommand!;
        }
    }

    private PathCommand GeneratePathCommand(bool isRelative, Point endPoint, Point control1, Point control2, Size radius, double rotationAngle, bool isLargeArc, bool sweepDirection) {
        if (WorkingCommand is CloseCommand) {
            return WorkingCommand;
        }
        WorkingCommand.IsRelative = isRelative;
        WorkingCommand.EndPoint = PointDependsOnRelativeFlag(endPoint, isRelative);
        if (WorkingCommand is LineHorzCommand lineHorz) {
            lineHorz.EndPoint = new Point(WorkingCommand.EndPoint.X, 0);
            return lineHorz;
        }
        else if (WorkingCommand is LineVertCommand lineVert) {
            lineVert.EndPoint = new Point(0, WorkingCommand.EndPoint.Y);
        }
        else if (WorkingCommand is BezierCubicCommand cubic) {
            cubic.Control1 = PointDependsOnRelativeFlag(control1, isRelative);
            cubic.Control2 = PointDependsOnRelativeFlag(control2, isRelative);
        }
        else if (WorkingCommand is BezierQuadraticCommand quad) {
            quad.Control = PointDependsOnRelativeFlag(control1, isRelative);
        }
        else if (WorkingCommand is SmoothBezierCubicCommand smoothCubic) {
            smoothCubic.Control2 = PointDependsOnRelativeFlag(control2, isRelative);
        }
        else if (WorkingCommand is ArcCommand arc) {
            arc.Radius = radius;
            arc.RotationAngle = rotationAngle;
            arc.IsLargeArc = isLargeArc;
            arc.SweepDirection = sweepDirection;
        }
        return WorkingCommand;
    }


    public EditablePathElement() {
        IsEditing = TargetElement.Select(element => element != null).ToReadOnlyReactiveProperty();
        StartPoint = TargetElement.Select(element => element?.StartPoint ?? new Point(0, 0)).ToReadOnlyReactiveProperty();
        CommandName = TargetElement.Select(element => element?.Current?.CommandName ?? "").ToReadOnlyReactiveProperty<string>();
        DisplayName = TargetElement.Select(element => element?.Current?.DispalyName ?? "").ToReadOnlyReactiveProperty<string>();

        IsCloseCommand = TargetElement.Select(element => element?.Current is CloseCommand).ToReadOnlyReactiveProperty();
        IsArcCommand = TargetElement.Select(element => element?.Current is ArcCommand).ToReadOnlyReactiveProperty();
        HasControl1 = TargetElement.Select(element => element?.HasControl1 ?? false).ToReadOnlyReactiveProperty();
        HasControl2 = TargetElement.Select(element => element?.HasControl2 ?? false).ToReadOnlyReactiveProperty();

        IsControl1Editable = TargetElement.Select(element => (element?.HasControl1 ?? false) && !(element?.Current is SmoothBezierCommand)).ToReadOnlyReactiveProperty();
        IsEndPointXEditable = TargetElement.Select(element => !(element?.Current is LineVertCommand)).ToReadOnlyReactiveProperty();
        IsEndPointYEditable = TargetElement.Select(element => !(element?.Current is LineHorzCommand)).ToReadOnlyReactiveProperty();

        EndPointAbs = Observable.CombineLatest(EndPointAbsX, EndPointAbsY, (x, y) => new Point(x, y)).ToReadOnlyReactiveProperty();
        EndPoint = Observable.CombineLatest(EndPointAbs, IsRelative, PointDependsOnRelativeFlag).ToReadOnlyReactiveProperty();

        Control1PointAbs = Observable.CombineLatest(Control1PointAbsX, Control1PointAbsY, (x, y) => new Point(x, y)).ToReadOnlyReactiveProperty();
        Control1Point = Control1PointAbs.CombineLatest(IsRelative, PointDependsOnRelativeFlag).ToReadOnlyReactiveProperty();

        Control2PointAbs = Observable.CombineLatest(Control2PointAbsX, Control2PointAbsY, (x, y) => new Point(x, y)).ToReadOnlyReactiveProperty();
        Control2Point = Control2PointAbs.CombineLatest(IsRelative, PointDependsOnRelativeFlag).ToReadOnlyReactiveProperty();

        GeneratedPathCommand = Observable.CombineLatest(
                       IsRelative,
                       EndPointAbs,
                       Control1PointAbs,
                       Control2PointAbs,
                       Radius,
                       RotationAngle,
                       IsLargeArc,
                       SweepDirection,
                       GeneratePathCommand).ToReadOnlyReactiveProperty<PathCommand>(new CloseCommand(), ReactivePropertyMode.None);
    }


}

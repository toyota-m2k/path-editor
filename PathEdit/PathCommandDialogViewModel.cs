using PathEdit.Parser;
using PathEdit.Parser.Command;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace PathEdit;
public class PathCommandDialogViewModel {

    public ReactiveProperty<bool> IsActive { get; } = new ReactiveProperty<bool>(false);

    public enum CommandKind {
        Move,
        Line,
        HorizontalLine,
        VerticalLine,
        BezierQuadratic,
        BezierCubic,
        SmoothBezierQuadratic,
        SmoothBezierCubic,
        Arc,
        Close,
    }
    public ReactiveProperty<CommandKind> Kind { get; } = new(CommandKind.Move);
    public ReactiveProperty<bool> IsRelative { get; } = new(false);
    public ReactiveCommand OkCommand { get; } = new();
    public ReactiveCommand CancelCommand { get; } = new();

    private TaskCompletionSource<PathCommand?> _tcs = new();
    private PathElement? _prevPathElement = null;

    public PathCommandDialogViewModel() {
        OkCommand.Subscribe(() => {
            IsActive.Value = false;
            _tcs.SetResult(CreateCommand());
        });
        CancelCommand.Subscribe(() => {
            IsActive.Value = false;
            _tcs.SetResult(null);
        });
    }

    public Task<PathCommand?> ShowDialogAsync(PathElement element) {
        _prevPathElement = element;
        _tcs = new();
        IsActive.Value = true;
        return _tcs.Task;
    }

    private PathCommand CreateCommand() {
        var isRelative = IsRelative.Value;
        var endPoint = (isRelative || _prevPathElement == null) ? new System.Windows.Point(0, 0) : _prevPathElement.EndPoint;
        switch (Kind.Value) {
            case CommandKind.Move:
                return new MoveCommand(isRelative, endPoint);
            case CommandKind.Line:
                return new LineCommand(isRelative, endPoint);
            case CommandKind.HorizontalLine:
                return new LineHorzCommand(isRelative, endPoint.X);
            case CommandKind.VerticalLine:
                return new LineVertCommand(isRelative, endPoint.Y);
            case CommandKind.BezierQuadratic:
                return new BezierQuadraticCommand(isRelative, endPoint, endPoint);
            case CommandKind.BezierCubic:
                return new BezierCubicCommand(isRelative, endPoint, endPoint, endPoint);
            case CommandKind.SmoothBezierQuadratic:
                return new SmoothBezierQuadraticCommand(isRelative, endPoint);
            case CommandKind.SmoothBezierCubic:
                return new SmoothBezierCubicCommand(isRelative, endPoint, endPoint);
            case CommandKind.Arc:
                return new ArcCommand(isRelative, new System.Windows.Size(0,0), 0, false, false, endPoint);
            case CommandKind.Close:
                return new CloseCommand();
            default:
                throw new Exception("Invalid command kind");
        }
    }
}

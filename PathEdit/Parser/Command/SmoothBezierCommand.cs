using System.Windows;

namespace PathEdit.Parser.Command;
internal abstract class SmoothBezierCommand : BezierCommand {

    protected SmoothBezierCommand(bool isRelative, Point endPoint, bool isCubic)
        : base(isRelative, endPoint, isCubic) {
    }

    protected Point GetFirstControlPoint(PathCommand? prevCommand) {
        if (null != prevCommand) {
            var prevBezierCommand = prevCommand as BezierCommand;
            if (prevBezierCommand != null && IsCubic == prevBezierCommand.IsCubic) {
                var lastControl = prevBezierCommand.LastResolvedControl;
                var lastPoint = prevBezierCommand.LastResolvedPoint;
                var diff = lastPoint - lastControl;
                return lastPoint + diff;
            } else {
                return prevCommand.LastResolvedPoint;
            }
        }
        return new Point(0, 0);
    }
}

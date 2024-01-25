using System.Windows;

namespace PathEdit.Parser.Command;
public abstract class BezierCommand : PathCommand {
    public Point LastResolvedControl { get; protected set; } = new Point(0, 0);
    public bool IsCubic { get; }

    protected BezierCommand(bool isRelative, Point endPoint, bool isCubic)
        : base(isRelative, endPoint) {
        IsCubic = isCubic;
    }
}

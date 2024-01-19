using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace PathEdit.Parser;
internal abstract class PathCommand {
    protected static readonly Point PointZero = new Point(0, 0);
    public bool IsRelative { get; }
    public Point EndPoint { get; set; }
    public Point LastResolvedPoint { get; protected set; } = new Point(0, 0);

    public abstract void DrawTo(IGraphics graphics, PathCommand? prevCommand);
    public abstract void ComposeTo(StringBuilder sb, PathCommand? prevCommand);

    protected Point ResolveRelativePoint(Point point, Point? basePoint) {
        var bp = basePoint ?? PointZero;
        return IsRelative ? new Point(point.X + bp.X, point.Y + bp.Y) : point;
    }

    protected PathCommand(bool isRelative, Point endPoint) {
        IsRelative = isRelative;
        EndPoint = endPoint;
    }


    protected static Point getPoint(List<double> paramList, int index) {
        return new Point(paramList[index], paramList[index + 1]);
    }
    protected static List<Point> getPoints(List<double> paramList, int index) {
        var points = new List<Point>();
        for (int i = index; i+1 < paramList.Count; i += 2) {
            points.Add(getPoint(paramList, i));
        }
        return points;
    }
}

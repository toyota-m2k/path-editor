using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace PathEdit.Parser;
internal abstract class PathCommand {
    protected static readonly Point PointZero = new Point(0, 0);
    public bool IsRelative { get; }
    public Point EndPoint { get; set; }
    public Point LastResolvedPoint { get; protected set; } = new Point(0, 0);

    public abstract void DrawTo(IGraphics graphics, PathCommand? prevCommand);
    public abstract void ComposeTo(StringBuilder sb, PathCommand? prevCommand);

    public virtual void Transform(Matrix matrix, PathCommand? prevCommand) {
        var endPoint = TransformPoint(matrix, EndPoint, prevCommand?.LastResolvedPoint);
        EndPoint = endPoint;
        LastResolvedPoint = ResolveRelativePoint(endPoint, prevCommand?.LastResolvedPoint);
    }

    protected Point TransformPoint(Matrix matrix, Point point, Point? basePoint) {
        return Absolute2Relative(matrix.Transform(ResolveRelativePoint(point, basePoint)), basePoint);
    }

    protected Point Absolute2Relative(Point point, Point? basePoint) {
        if (!IsRelative) {
            return point;
        }
        var bp = basePoint ?? PointZero;
        return new Point(point.X - bp.X, point.Y - bp.Y);
    }

    protected Point ResolveRelativePoint(Point point, Point? basePoint) {
        if (!IsRelative) {
            return point;
        }
        var bp = basePoint ?? PointZero;
        return new Point(point.X + bp.X, point.Y + bp.Y);
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

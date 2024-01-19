using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace PathEdit.Parser.Command;
internal class ArcCommand : PathCommand {
    public Size Radius { get; private set; }
    public double RotationAngle { get; private set; }
    public bool IsLargeArc { get; }
    public bool SweepDirection { get; }

    public ArcCommand(
        bool isRelative, 
        Size radius,
        double rotationAngle, 
        bool isLargeArc, 
        bool sweepDirection,
        Point endPoint
        )
        : base(isRelative, endPoint) {
        Radius = radius;
        RotationAngle = rotationAngle;
        IsLargeArc = isLargeArc;
        SweepDirection = sweepDirection;
    }

    public override void DrawTo(IGraphics graphics, PathCommand? prev) {
        var endPoint = ResolveRelativePoint(EndPoint, prev?.LastResolvedPoint);
        //graphics.ArcTo(Radius, RotationAngle, IsLargeArc, SweepDirection, endPoint);
        var startPoint = prev?.LastResolvedPoint ?? new Point(0, 0);
        GraphicUtil.DrawArc(graphics, Radius, RotationAngle, IsLargeArc, SweepDirection, startPoint, endPoint);
        LastResolvedPoint = endPoint;
    }

    public override void ComposeTo(StringBuilder sb, PathCommand? prev) {
        if (!(prev is ArcCommand)) {
            sb.Append(IsRelative ? "a" : "A");
        }
        sb.Append(" ");
        sb.Append($"{Radius.Width},{Radius.Height}");
        sb.Append($",{RotationAngle}");
        sb.Append($",{(IsLargeArc ? 1 : 0)}");
        sb.Append($",{(SweepDirection ? 1 : 0)}");
        sb.Append($",{EndPoint.X},{EndPoint.Y}");
    }

    public override void Transform(Matrix matrix, PathCommand? prevCommand) {
        base.Transform(matrix, prevCommand);
        var ratio = matrix.Transform(new Point(1, 1));
        Radius = new Size(Radius.Width * ratio.X, Radius.Height * ratio.Y);
        var rotation = matrix.Transform(new Point(1, 0));
        RotationAngle = (Math.Atan2(rotation.Y, rotation.X) * 180 / Math.PI)%360;
    }

    public static IEnumerable<ArcCommand> Parse(string command, List<double> paramList) {
        var lc = command.ToUpper();
        if(lc!="A") {
            throw new Exception($"Unexpected command {command}");
        }
        if(paramList.Count==0 || paramList.Count % 7 != 0) {
            throw new Exception($"Unexpected number ({paramList.Count}) of parameters for {command}");
        }
        for (int i = 0; i + 6 < paramList.Count; i += 7) {
            yield return new ArcCommand(
                               command == "a",
                               new Size(paramList[i], paramList[i + 1]),
                               paramList[i + 2],
                               paramList[i + 3] != 0,
                               paramList[i + 4] != 0,
                               new Point(paramList[i + 5], paramList[i + 6]));
        }
    }
}

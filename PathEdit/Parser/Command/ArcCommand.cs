using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace PathEdit.Parser.Command;
public class ArcCommand : PathCommand {
    public Size Radius { get; set; }
    public double RotationAngle { get; set; }
    public bool IsLargeArc { get; set; }
    public bool SweepDirection { get; set; }

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

    public override string CommandName => IsRelative ? "a" : "A";
    public override string DispalyName => "Arc";

    public override void ComposeTo(StringBuilder sb, PathCommand? prev) {
        if (prev?.CommandName != CommandName) {
            sb.Append(CommandName);
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
        var angle = (Math.Atan2(matrix.M12, matrix.M11) * 180 / Math.PI) % 360;
        var scaleX = Math.Sqrt(matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12);
        var scaleY = Math.Sqrt(matrix.M21 * matrix.M21 + matrix.M22 * matrix.M22);
        Radius = new Size(Radius.Width * scaleX, Radius.Height * scaleY);
        if(Radius.Width != Radius.Height) {
            RotationAngle = (RotationAngle + angle) % 360;
        }
        if(matrix.M11 * matrix.M22 < 0) { 
            // ミラー変形の場合は、SweepDirectionを反転する
            SweepDirection = !SweepDirection;
        }
    }

    public override PathCommand Clone() {
        return new ArcCommand(IsRelative, Radius, RotationAngle, IsLargeArc, SweepDirection, EndPoint);
    }

    public override void RoundCoordinateValue(int digit) {
        base.RoundCoordinateValue(digit);
        Radius = new Size(Math.Round(Radius.Width, digit), Math.Round(Radius.Height, digit));
        RotationAngle = Math.Round(RotationAngle, digit);
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

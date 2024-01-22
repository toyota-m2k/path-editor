using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace PathEdit.Parser.Command;
internal class BezierCubicCommand : BezierCommand {
    public Point Control1 { get; private set; }
    public Point Control2 { get; private set; }

    public BezierCubicCommand(bool isRelative, Point control1, Point control2, Point endPoint)
        : base(isRelative, endPoint, true) {
        Control1 = control1;
        Control2 = control2;
    }

    public override PathCommand Clone() {
        return new BezierCubicCommand(IsRelative, Control1, Control2, EndPoint);
    }

    public override void MakeAbsolute(PathCommand? prevCommand) {
        if (!IsRelative) {
            return;
        }
        Control1 = ResolveRelativePoint(Control1, prevCommand?.LastResolvedPoint);
        Control2 = ResolveRelativePoint(Control2, prevCommand?.LastResolvedPoint);
        base.MakeAbsolute(prevCommand);
    }

    public override void RoundCoordinateValue(int digit) {
        base.RoundCoordinateValue(digit);
        Control1 = RoundPoint(Control1, digit);
        Control2 = RoundPoint(Control2, digit);
    }

    public override void DrawTo(IGraphics graphics, PathCommand? prev) {
        var endPoint = ResolveRelativePoint(EndPoint, prev?.LastResolvedPoint);
        var control1 = ResolveRelativePoint(Control1, prev?.LastResolvedPoint);
        var control2 = ResolveRelativePoint(Control2, prev?.LastResolvedPoint);
        graphics.CurveTo(control1, control2, endPoint);
        LastResolvedPoint = endPoint;
        LastResolvedControl = control2;
    }

    public override void Transform(Matrix matrix, PathCommand? prevCommand) {
        base.Transform(matrix, prevCommand);
        Control1 = TransformPoint(matrix, Control1, prevCommand?.LastResolvedPoint);
        Control2 = TransformPoint(matrix, Control2, prevCommand?.LastResolvedPoint);
    }

    public override void ComposeTo(StringBuilder sb, PathCommand? prevCommand) {
        if (!(prevCommand is BezierQuadraticCommand)) {
            sb.Append(IsRelative ? "c" : "C");
        }
        sb.Append(" ");
        sb.Append(Control1.X);
        sb.Append(" ");
        sb.Append(Control1.Y);
        sb.Append(" ");
        sb.Append(Control2.X);
        sb.Append(" ");
        sb.Append(Control2.Y);
        sb.Append(" ");
        sb.Append(EndPoint.X);
        sb.Append(" ");
        sb.Append(EndPoint.Y);
    }

    public static IEnumerable<BezierCubicCommand> Parse(string command, List<double> paramList) {
        var lc = command.ToUpper();
        if(lc!="C") {
            throw new Exception($"Unexpected command {command}");
        }
        if(paramList.Count==0 || paramList.Count % 6 != 0) {
            throw new Exception($"Unexpected number ({paramList.Count}) of parameters for {command}");
        }
        var points = getPoints(paramList, 0);
        for (int i = 0; i+2 < points.Count; i += 3) {
            yield return new BezierCubicCommand(command == "c", points[i], points[i+1], points[i+2]);
        }
    }
}

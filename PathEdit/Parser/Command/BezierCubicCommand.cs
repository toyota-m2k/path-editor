using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace PathEdit.Parser.Command;
internal class BezierCubicCommand : BezierCommand {
    public Point Control1 { get; }
    public Point Control2 { get; }

    public BezierCubicCommand(bool isRelative, Point control1, Point control2, Point endPoint)
        : base(isRelative, endPoint, true) {
        Control1 = control1;
        Control2 = control2;
    }

    public override void DrawTo(IGraphics graphics, PathCommand? prev) {
        var endPoint = ResolveRelativePoint(EndPoint, prev?.LastResolvedPoint);
        var control1 = ResolveRelativePoint(Control1, prev?.LastResolvedPoint);
        var control2 = ResolveRelativePoint(Control2, prev?.LastResolvedPoint);
        graphics.CurveTo(control1, control2, endPoint);
        LastResolvedPoint = endPoint;
        LastResolvedControl = control2;
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace PathEdit.Parser.Command;
internal class SmoothBezierQuadraticCommand : SmoothBezierCommand{
    public SmoothBezierQuadraticCommand(bool isRelative, Point endPoint)
        : base(isRelative, endPoint, isCubic:false) {
    }

    public override void DrawTo(IGraphics graphics, PathCommand? prevCommand) {
        var control = GetFirstControlPoint(prevCommand);
        var endPoint = ResolveRelativePoint(EndPoint, prevCommand?.LastResolvedPoint);
        graphics.QuadTo(control, endPoint);
        LastResolvedPoint = endPoint;
        LastResolvedControl = control;
    }

    public override void ComposeTo(StringBuilder sb, PathCommand? prevCommand) {
        if (!(prevCommand is SmoothBezierQuadraticCommand)) {
            sb.Append(IsRelative ? "t" : "T");
        }
        sb.Append(" ");
        sb.Append(EndPoint.X);
        sb.Append(" ");
        sb.Append(EndPoint.Y);
    }

    public static IEnumerable<SmoothBezierQuadraticCommand> Parse(string command, List<double> paramList) {
        var lc = command.ToUpper();
        if(lc!="T") {
            throw new Exception($"Unexpected command {command}");
        }
        if(paramList.Count==0 || paramList.Count % 2 != 0) {
            throw new Exception($"Unexpected number ({paramList.Count}) of parameters for {command}");
        }
        var points = getPoints(paramList, 0);
        foreach(var point in points) {
            yield return new SmoothBezierQuadraticCommand(command == "t", point);
        }
    }
}

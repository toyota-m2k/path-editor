using PathEdit.common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace PathEdit.Parser.Command;
internal class LineCommand : PathCommand {
    public LineCommand(bool isRelative, Point endPoint) : base(isRelative, endPoint) {
    }

    public override void DrawTo(IGraphics graphics, PathCommand? prev) {
        var endPoint = ResolveRelativePoint(EndPoint, prev?.LastResolvedPoint ?? new Point(0, 0));
        graphics.LineTo(endPoint);
        LastResolvedPoint = endPoint;
        LoggerEx.info($"LineCommand.DrawTo: {endPoint}");
    }

    public override void ComposeTo(StringBuilder sb, PathCommand? prevCommand) {
        if (!(prevCommand is LineCommand)) {
            sb.Append(IsRelative ? "l" : "L");
        }
        sb.Append(" ");
        sb.Append(EndPoint.X);
        sb.Append(" ");
        sb.Append(EndPoint.Y);
    }

    public static IEnumerable<LineCommand> Parse(string command, List<double> paramList) {
        var lc = command.ToUpper();
        if(lc!="L") {
            throw new Exception($"Unexpected command {command}");
        }
        if(paramList.Count==0 || paramList.Count % 2 != 0) {
            throw new Exception($"Unexpected number ({paramList.Count}) of parameters for {command}");
        }
        var points = getPoints(paramList, 0);
        foreach(var point in points) {
            yield return new LineCommand(command == "l", point);
        }
    }
}

using PathEdit.common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace PathEdit.Parser.Command;
internal class LineHorzCommand : PathCommand {
    public LineHorzCommand(bool isRelative, double x)
        : base(isRelative, new Point(x, 0)) {
    }

    public override void DrawTo(IGraphics graphics, PathCommand? prevCommand) {
        Point endPoint;
        if (IsRelative) {
            endPoint = ResolveRelativePoint(EndPoint, prevCommand?.LastResolvedPoint);
        } else {
            endPoint = new Point(EndPoint.X, prevCommand?.LastResolvedPoint.Y??0);
        }
        graphics.LineTo(endPoint);
        LastResolvedPoint = endPoint;
        LoggerEx.info($"LineHorzCommand.DrawTo: {endPoint}");
    }

    public override void ComposeTo(StringBuilder sb, PathCommand? prevCommand) {
        if (!(prevCommand is LineHorzCommand)) {
            sb.Append(IsRelative ? "h" : "H");
        } 
        sb.Append(" ");
        sb.Append(EndPoint.X);
    }

    public static IEnumerable<LineHorzCommand> Parse(string command, List<double> paramList) {
        var lc = command.ToUpper();
        if(lc!="H") {
            throw new Exception($"Unexpected command {command}");
        }
        if(paramList.Count==0) {
            throw new Exception($"Unexpected number ({paramList.Count}) of parameters for {command}");
        }
        foreach(var x in paramList) {
            yield return new LineHorzCommand(command == "h", x);
        }
    }
}

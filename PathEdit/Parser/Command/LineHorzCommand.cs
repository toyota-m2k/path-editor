using PathEdit.common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace PathEdit.Parser.Command;
public class LineHorzCommand : PathCommand {
    public LineHorzCommand(bool isRelative, double x)
        : base(isRelative, new Point(x, 0)) {
    }

    public override PathCommand Clone() {
        return new LineHorzCommand(IsRelative, EndPoint.X);
    }

    public override Point CorrectedEndPoint(PathCommand? prevCommand) {
        if (IsRelative) {
            return EndPoint;
        }
        else {
            return new Point(EndPoint.X, prevCommand?.LastResolvedPoint.Y ?? 0);
        }
    }

    public LineCommand ToLineCommand(PathCommand? prevCommand) {
        return new LineCommand(IsRelative, CorrectedEndPoint(prevCommand));
    }

    public override void DrawTo(IGraphics graphics, PathCommand? prevCommand) {
        Point endPoint = ResolveRelativePoint(CorrectedEndPoint(prevCommand), prevCommand?.LastResolvedPoint);
        graphics.LineTo(endPoint);
        LastResolvedPoint = endPoint;
        //LoggerEx.info($"LineHorzCommand.DrawTo: {endPoint}");
    }

    public override string CommandName => IsRelative ? "h" : "H";

    public override void ComposeTo(StringBuilder sb, PathCommand? prevCommand) {
        if (!(prevCommand is LineHorzCommand)) {
            sb.Append(CommandName);
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

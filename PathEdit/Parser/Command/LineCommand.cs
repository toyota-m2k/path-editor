using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace PathEdit.Parser.Command;
public class LineCommand : PathCommand {
    public LineCommand(bool isRelative, Point endPoint) : base(isRelative, endPoint) {
    }

    public override PathCommand Clone() {
        return new LineCommand(IsRelative, EndPoint);
    }

    public override void DrawTo(IGraphics graphics, PathCommand? prev) {
        var endPoint = ResolveRelativePoint(EndPoint, prev?.LastResolvedPoint ?? new Point(0, 0));
        graphics.LineTo(endPoint);
        LastResolvedPoint = endPoint;
        //LoggerEx.info($"LineCommand.DrawTo: {endPoint}");
    }

    public override string CommandName => IsRelative ? "l" : "L";
    public override string DispalyName => "Line";

    public override void ComposeTo(StringBuilder sb, PathCommand? prevCommand) {
        if (prevCommand?.CommandName != CommandName) {
            sb.Append(CommandName);
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

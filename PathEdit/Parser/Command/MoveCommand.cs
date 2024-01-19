using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace PathEdit.Parser.Command;
internal class MoveCommand : PathCommand {
    public MoveCommand(bool isRelative, Point endPoint)
        : base(isRelative, endPoint) {
    }

    public override void DrawTo(IGraphics graphics, PathCommand? prevCommand) {
        var endPoint = ResolveRelativePoint(EndPoint, prevCommand?.LastResolvedPoint);
        if(prevCommand is MoveCommand) {
            graphics.LineTo(endPoint);
        } else {
            graphics.MoveTo(endPoint);
        }
        LastResolvedPoint = endPoint;
    }

    public override void ComposeTo(StringBuilder sb, PathCommand? prevCommand) {
        if (!(prevCommand is MoveCommand)) {
            sb.Append(IsRelative ? "m" : "M");
        }
        sb.Append(" ");
        sb.Append(EndPoint.X);
        sb.Append(" ");
        sb.Append(EndPoint.Y);
    }


    public static IEnumerable<MoveCommand> Parse(string command, List<double> paramList) {
        var lc = command.ToUpper();
        if (lc != "M") {
            throw new Exception($"Unexpected command {command}");
        }
        if (paramList.Count == 0 || paramList.Count % 2 != 0) {
            throw new Exception($"Unexpected number ({paramList.Count}) of parameters for {command}");
        }
        var points = getPoints(paramList, 0);
        foreach (var point in points) {
            yield return new MoveCommand(command == "m", point);
        }
    }
}

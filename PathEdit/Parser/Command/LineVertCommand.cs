using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace PathEdit.Parser.Command;
public class LineVertCommand : PathCommand {
    public LineVertCommand(bool isRelative, double y)
        : base(isRelative, new Point(0, y)) {
        }

    public override PathCommand Clone() { 
        return new LineVertCommand(IsRelative, EndPoint.Y);
    }

    public override Point CorrectedEndPoint(PathCommand? prevCommand) {
        if (IsRelative) {
            return EndPoint;
        }
        else {
            return new Point(prevCommand?.LastResolvedPoint.X ?? 0, EndPoint.Y);
        }
    }

    public LineCommand ToLineCommand(PathCommand? prevCommand) {
        return new LineCommand(IsRelative, CorrectedEndPoint(prevCommand));
    }

    public override void DrawTo(IGraphics graphics, PathCommand? prevCommand) {
        Point endPoint = ResolveRelativePoint(CorrectedEndPoint(prevCommand), prevCommand?.LastResolvedPoint);
        graphics.LineTo(endPoint);
        LastResolvedPoint = endPoint;
        //LoggerEx.info($"LineVertCommand.DrawTo: {endPoint}");
    }

    public override string CommandName => IsRelative ? "v" : "V";
    public override string DispalyName => "Line Vertical";

    public override void ComposeTo(StringBuilder sb, PathCommand? prevCommand) {
        if (!(prevCommand is LineVertCommand)) {
            sb.Append(CommandName);
        } 
        sb.Append(" ");
        sb.Append(EndPoint.Y);
    }

    public static IEnumerable<LineVertCommand> Parse(string command, List<double> paramList) {
        var lc = command.ToUpper();
        if(lc!="V") {
            throw new Exception($"Unexpected command {command}");
        }
        if(paramList.Count==0) {
            throw new Exception($"Unexpected number ({paramList.Count}) of parameters for {command}");
        }
        foreach(var y in paramList) {
            yield return new LineVertCommand(command == "v", y);
        }
    }
}

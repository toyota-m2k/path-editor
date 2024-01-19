﻿using PathEdit.common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace PathEdit.Parser.Command;
internal class LineVertCommand : PathCommand {
    public LineVertCommand(bool isRelative, double y)
        : base(isRelative, new Point(0, y)) {
        }

    public override void DrawTo(IGraphics graphics, PathCommand? prevCommand) {
        Point endPoint;
        if (IsRelative) {
            endPoint = ResolveRelativePoint(EndPoint, prevCommand?.LastResolvedPoint);
        } else {
            endPoint = new Point(prevCommand?.LastResolvedPoint.X ?? 0, EndPoint.Y);
        }
        graphics.LineTo(endPoint);
        LastResolvedPoint = endPoint;
        LoggerEx.info($"LineVertCommand.DrawTo: {endPoint}");
    }

    public override void ComposeTo(StringBuilder sb, PathCommand? prevCommand) {
        if (!(prevCommand is LineVertCommand)) {
            sb.Append(IsRelative ? "v" : "V");
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
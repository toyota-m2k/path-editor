﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace PathEdit.Parser.Command;
public class CloseCommand : PathCommand {
    public CloseCommand() : base(isRelative:true, new Point(0, 0)) {
    }

    public override PathCommand Clone() {
        return new CloseCommand();
    }

    public override void DrawTo(IGraphics graphics, PathCommand? prevCommand) {
        graphics.ClosePath();
        // ZコマンドはCurrent Pointを移動しない。
        LastResolvedPoint = prevCommand?.LastResolvedPoint ?? new Point(0, 0);
    }

    public override string CommandName => "Z";
    public override string DispalyName => "Close";

    public override void ComposeTo(StringBuilder sb, PathCommand? prevCommand) {
        sb.Append(CommandName);
    }
    public override void Transform(Matrix matrix, PathCommand? prevCommand) {
    }

    public static IEnumerable<CloseCommand> Parse(string command, List<double> paramList) {
        var lc = command.ToUpper();
        if(lc!="Z") {
            throw new Exception($"Unexpected command {command}");
        }
        if(paramList.Count!=0) {
            throw new Exception($"Unexpected number ({paramList.Count}) of parameters for {command}");
        }
        yield return new CloseCommand();
    }
}

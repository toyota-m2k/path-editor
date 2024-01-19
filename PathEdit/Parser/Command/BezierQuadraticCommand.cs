using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;
using System.Windows;
using System.Windows.Media;

namespace PathEdit.Parser.Command;
internal class BezierQuadraticCommand : BezierCommand {
    public Point Control { get; private set; }

    public BezierQuadraticCommand(bool isRelative, Point control, Point endPoint) 
        : base(isRelative, endPoint, isCubic:false) {
        Control = control;
    }

    public override void DrawTo(IGraphics graphics, PathCommand? prev) {
        var endPoint = ResolveRelativePoint(EndPoint, prev?.LastResolvedPoint);
        var control = ResolveRelativePoint(Control, prev?.LastResolvedPoint);
        graphics.QuadTo(control, endPoint);
        LastResolvedPoint = endPoint;
        LastResolvedControl = control;
    }

    public override void Transform(Matrix matrix, PathCommand? prevCommand) {
        base.Transform(matrix, prevCommand);
        Control = TransformPoint(matrix, Control, prevCommand?.LastResolvedPoint);
    }

    public override void ComposeTo(StringBuilder sb, PathCommand? prevCommand) {
        if (!(prevCommand is BezierQuadraticCommand)) {
            sb.Append(IsRelative ? "q" : "Q");
        }
        sb.Append(" ");
        sb.Append(Control.X);
        sb.Append(" ");
        sb.Append(Control.Y);
        sb.Append(" ");
        sb.Append(EndPoint.X);
        sb.Append(" ");
        sb.Append(EndPoint.Y);
    }

    public static IEnumerable<BezierQuadraticCommand> Parse(string command, List<double> paramList) {
        var lc = command.ToUpper();
        if(lc!="Q") {
            throw new Exception($"Unexpected command {command}");
        }
        if(paramList.Count==0 || paramList.Count % 4 != 0) {
            throw new Exception($"Unexpected number ({paramList.Count}) of parameters for {command}");
        }
        var points = getPoints(paramList, 0);
        for(int i=0; i+1<points.Count; i+=2) { 
            yield return new BezierQuadraticCommand(command == "q", points[i], points[i+1]);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace PathEdit.Parser.Command;
public class SmoothBezierCubicCommand : SmoothBezierCommand {
    public Point Control2 { get; set; }
    public SmoothBezierCubicCommand(bool isRelative, Point control, Point endPoint)
        : base(isRelative, endPoint, isCubic:true) {
        Control2 = control;
    }

    public override PathCommand Clone() {
        return new SmoothBezierCubicCommand(IsRelative, Control2, EndPoint);
    }

    public override void MakeAbsolute(PathCommand? prevCommand) {
        if (!IsRelative) {
            return;
        }
        Control2 = ResolveRelativePoint(Control2, prevCommand?.LastResolvedPoint);
        base.MakeAbsolute(prevCommand);
    }

    public override void RoundCoordinateValue(int digit) {
        base.RoundCoordinateValue(digit);
        Control2 = RoundPoint(Control2, digit);
    }

    public override void DrawTo(IGraphics graphics, PathCommand? prevCommand) {
        var control1 = GetFirstControlPoint(prevCommand);
        var control2 = ResolveRelativePoint(Control2, prevCommand?.LastResolvedPoint);
        var endPoint = ResolveRelativePoint(EndPoint, prevCommand?.LastResolvedPoint);
        graphics.CurveTo(control1, control2, endPoint);
        LastResolvedPoint = endPoint;
        LastResolvedControl = control2;
    }

    public override void Transform(Matrix matrix, PathCommand? prevCommand) {
        base.Transform(matrix, prevCommand);
        Control2 = TransformPoint(matrix, Control2, prevCommand?.LastResolvedPoint);
    }

    public override string CommandName => IsRelative ? "s" : "S";
    public override string DispalyName => "Smooth Bezier Cubic";

    public override void ComposeTo(StringBuilder sb, PathCommand? prevCommand) {
        if (!(prevCommand is SmoothBezierCubicCommand)) {
            sb.Append(CommandName);
        }
        sb.Append(IsRelative?"s":"S");
        sb.Append(" ");
        sb.Append(Control2.X);
        sb.Append(" ");
        sb.Append(Control2.Y);
        sb.Append(" ");
        sb.Append(EndPoint.X);
        sb.Append(" ");
        sb.Append(EndPoint.Y);
    }


    public static IEnumerable<SmoothBezierCubicCommand> Parse(string command, List<double> paramList) {
        var lc = command.ToUpper();
        if(lc!="S") {
            throw new Exception($"Unexpected command {command}");
        }
        if(paramList.Count==0 || paramList.Count % 4 != 0) {
            throw new Exception($"Unexpected number ({paramList.Count}) of parameters for {command}");
        }
        var points = getPoints(paramList, 0);
        for(int i=0; i+1<points.Count; i+=2) { 
            yield return new SmoothBezierCubicCommand(command == "s", points[i], points[i+1]);
        }
    }
}

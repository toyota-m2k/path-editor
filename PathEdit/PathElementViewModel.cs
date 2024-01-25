using PathEdit.Parser.Command;
using PathEdit.Parser;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Reactive.Linq;
using System.Diagnostics;
using Microsoft.UI.Xaml;

namespace PathEdit;
public class PathElement {
    public PathCommand Current { get; }
    public PathCommand? Prev { get; }
    public PathElement(PathCommand current, PathCommand? prev) {
        Current = current;
        Prev = prev;
    }

    public string CommandName => Current.CommandName;
    public Point StartPoint => Prev?.LastResolvedPoint ?? new Point(0, 0);
    public Point EndPoint => Current.EndPoint;
    public Point EndPointAbs => Current.ResolveRelativePoint(Current.EndPoint, Prev?.LastResolvedPoint);
    public bool IsRelative => Current.IsRelative;

    public bool HasControl1 => Current is BezierCommand || Current is SmoothBezierCommand;
    public bool HasControl2 => Current is BezierCubicCommand || Current is SmoothBezierCubicCommand;
    public bool IsArc => Current is ArcCommand;

    public Point Control1 {
        get {
            if (Current is BezierQuadraticCommand q) {
                return q.Control;
            }
            if (Current is BezierCubicCommand c) {
                return c.Control1;
            }
            if (Current is SmoothBezierCommand s) {
                return s.GetFirstControlPoint(Prev);
            }
            return new Point(0, 0);
        }
    }
    public Point Control2 {
        get {
            if (Current is BezierCubicCommand c) {
                return c.Control2;
            }
            if (Current is SmoothBezierCubicCommand s) {
                return s.Control2;
            }
            return new Point(0, 0);
        }
    }

    public Point Control1Abs {
        get {
            if (Current is BezierQuadraticCommand q) {
                return q.ResolveRelativePoint(q.Control, Prev?.LastResolvedPoint);
            }
            if (Current is BezierCubicCommand c) {
                return c.ResolveRelativePoint(c.Control1, Prev?.LastResolvedPoint);
            }
            if (Current is SmoothBezierCommand s) {
                return s.GetFirstControlPoint(Prev);
            }
            return new Point(0, 0);
        }
    }

    public Point Control2Abs => Current.ResolveRelativePoint(Control2, Prev?.LastResolvedPoint);

    public Size Radius => Current is ArcCommand a ? a.Radius : new Size(0, 0);
    public double RotationAngle => Current is ArcCommand a ? a.RotationAngle : 0;
    public bool IsLargeArc => Current is ArcCommand a ? a.IsLargeArc : false;
    public bool SweepDirection => Current is ArcCommand a ? a.SweepDirection : false;
}

public class PathElementViewModel {
    public ReactiveProperty<PathElement> Element { get; }
    public IReadOnlyReactiveProperty<PathElementViewModel> Selected { get; }
    public IReadOnlyReactiveProperty<bool> ShowAbsolute { get; }

    public ReadOnlyReactiveProperty<string> CommandName { get; }
    public ReadOnlyReactiveProperty<string> StartPoint { get; }

    public ReadOnlyReactiveProperty<string> EndPoint { get; }

    public ReadOnlyReactiveProperty<Microsoft.UI.Xaml.Visibility> Control1Visibility { get; }
    public ReadOnlyReactiveProperty<Microsoft.UI.Xaml.Visibility> Control2Visibility { get; }
    public ReadOnlyReactiveProperty<Microsoft.UI.Xaml.Visibility> ArcVisibility { get; }

    public ReadOnlyReactiveProperty<string> Control1 { get; }

    public ReadOnlyReactiveProperty<string> Control2 { get; }

    public ReadOnlyReactiveProperty<string> Radius { get; }

    public ReadOnlyReactiveProperty<double> RotationAngle { get; }
    public ReadOnlyReactiveProperty<bool> IsLargeArc { get; }
    public ReadOnlyReactiveProperty<bool> SweepDirection { get; }

    public ReadOnlyReactiveProperty<Microsoft.UI.Xaml.Visibility> CanEdit { get; }
    public ReadOnlyReactiveProperty<bool> CanDelete { get; }

    public ReactiveCommand<PathElementViewModel> EditCommand { get; }

    public ReactiveCommand<PathElementViewModel> DeleteCommand { get; }

    private static double R(double v) => Math.Round(v, 4);

    private static string PointToString(Point p) {
        return $"( {R(p.X)}, {R(p.Y)} )";
    }
    private static string SizeToString(Size s) {
        return s.Width==s.Height ? $"R={R(s.Width)}" : $"rx={R(s.Width)}, ry={R(s.Height)}";
    }

    public PathElementViewModel(PathElement element, IReadOnlyReactiveProperty<bool> showAbsolute, 
        IReadOnlyReactiveProperty<PathElementViewModel> selected,
        ReactiveCommand<PathElementViewModel> editCommand, ReactiveCommand<PathElementViewModel> deleteCommand) {
        Element = new ReactiveProperty<PathElement>(element);
        ShowAbsolute = showAbsolute;
        Selected = selected;

        StartPoint = Element.Select(element => PointToString(element.StartPoint)).ToReadOnlyReactiveProperty<string>();
        EndPoint = Element.CombineLatest(ShowAbsolute, (element, abs) => PointToString(abs ? element.EndPointAbs : element.EndPoint)).ToReadOnlyReactiveProperty<string>();
        CommandName = Element.Select(element => element.CommandName).ToReadOnlyReactiveProperty<string>();

        Control1Visibility = Element.Select(element => element.HasControl1 ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed).ToReadOnlyReactiveProperty();
        Control2Visibility = Element.Select(element => element.HasControl2 ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed).ToReadOnlyReactiveProperty();
        ArcVisibility = Element.Select(element => element.IsArc ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed).ToReadOnlyReactiveProperty();

        Control1 = Element.CombineLatest(ShowAbsolute, (element, abs) => PointToString(abs ? element.Control1Abs : element.Control1)).ToReadOnlyReactiveProperty<string>();
        Control2 = Element.CombineLatest(ShowAbsolute, (element, abs) => PointToString(abs ? element.Control2Abs : element.Control2)).ToReadOnlyReactiveProperty<string>();

        Radius = Element.Select(element => SizeToString(element.Radius)).ToReadOnlyReactiveProperty<string>();
        RotationAngle = Element.Select(element => element.RotationAngle).ToReadOnlyReactiveProperty();
        IsLargeArc = Element.Select(element => element.IsLargeArc).ToReadOnlyReactiveProperty();
        SweepDirection = Element.Select(element => element.SweepDirection).ToReadOnlyReactiveProperty();

        CanEdit = Selected.Select(selected => (this == selected) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed).ToReadOnlyReactiveProperty();
        CanDelete = Selected.Select(selected => selected?.Element.Value.Prev != null).ToReadOnlyReactiveProperty();
        EditCommand = editCommand;
        DeleteCommand = deleteCommand;
    }
}

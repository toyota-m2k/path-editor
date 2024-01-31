using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using PathEdit.Parser;
using System.Numerics;
using System.Windows;
using Windows.UI;

namespace PathEdit.Graphics;
internal class Win2DGraphics : IGraphics {
    private readonly CanvasDrawingSession DrawingSession;
    private bool isOpened = false;
    private readonly double Width;
    private readonly double Height;
    public Color Color { get; set; }
    private double PathWidth = 0;
    private double PathHeight = 0;

    private CanvasPathBuilder? _pathBuilder = null;
    private CanvasPathBuilder PathBuilder {
        get {
            if (_pathBuilder == null) {
                _pathBuilder = new CanvasPathBuilder(DrawingSession);
            }
            return _pathBuilder;
        }
    }

    private bool isAutoSize => PathWidth == 0 || PathHeight == 0;
    public Win2DGraphics SetPathSize(double width, double height) {
        PathWidth = width;
        PathHeight = height;
        return this;
    }

    public Win2DGraphics(CanvasDrawingSession drawingSession, double width, double height, Color color) {
        DrawingSession = drawingSession;
        Width = width;
        Height = height;
        Color = color;
        
        //pb.BeginFigure(0, 0);
        //pb.EndFigure(CanvasFigureLoop.Open);

        //var x = CanvasGeometry.CreatePath(pb);
        //DrawingSession.DrawGeometry(x, new System.Numerics.Vector2(0, 0), Windows.UI.Color.FromArgb(1,0,0,0));
    }


    private void Open(Point? point=null) {
        if(!isOpened) {
            PathBuilder.BeginFigure((float)(point?.X ?? 0), (float)(point?.Y ?? 0));
            isOpened = true;
        }
    }

    public void MoveTo(Point point) {
        if(isOpened) {
            PathBuilder.EndFigure(CanvasFigureLoop.Open);
            isOpened = false;
        }
        Open(point);
    }


    public void LineTo(Point point) {
        Open();
        PathBuilder.AddLine((float)point.X, (float)point.Y);
    }

    public void QuadTo(Point control, Point point) {
        Open();
        PathBuilder.AddQuadraticBezier(
            new System.Numerics.Vector2((float)control.X, (float)control.Y), 
            new System.Numerics.Vector2((float)point.X, (float)point.Y));
    }

    public void CurveTo(Point control1, Point control2, Point point) {
        Open();
        PathBuilder.AddCubicBezier(
            new System.Numerics.Vector2((float)control1.X, (float)control1.Y),
            new System.Numerics.Vector2((float)control2.X, (float)control2.Y),
            new System.Numerics.Vector2((float)point.X, (float)point.Y));
    }

    public void ArcTo(Size size, double rotationAngle, bool isLargeArc, bool sweepDirection, Point point) {
        Open();
        PathBuilder.AddArc(
            new System.Numerics.Vector2((float)point.X, (float)point.Y),
            (float)size.Width, (float)size.Height,
            (float)rotationAngle,
            isLargeArc ? CanvasSweepDirection.Clockwise : CanvasSweepDirection.CounterClockwise,
            sweepDirection ? CanvasArcSize.Large : CanvasArcSize.Small);
    }

    public void ClosePath() {
        if(isOpened) {
            PathBuilder.EndFigure(CanvasFigureLoop.Closed);
            isOpened = false;
        }
    }

    public void Fill() {
        if(isOpened) {
            PathBuilder.EndFigure(CanvasFigureLoop.Open);
            isOpened = false;
        }
        using (var geo = CanvasGeometry.CreatePath(PathBuilder)) {
            double rw = 1f, rh = 1f;
            if (isAutoSize) {
                var rc = geo.ComputeBounds();
                var w = rc.Width + rc.X;
                var h = rc.Height + rc.Y;
                if (w > 0) {
                    rw = Width / w;
                }
                if (h > 0) {
                    rh = Height / h;
                }
            }
            else {
                rw = Width / PathWidth;
                rh = Height / PathHeight;
            }
            var mx = Matrix3x2.CreateScale((float)rw, (float)rh, Vector2.Zero);
            DrawingSession.FillGeometry(geo.Transform(mx), Color);
            //DrawingSession.DrawGeometry(geo.Transform(mx), Color, 10);

            // CanvasGeometry.CreatePath を呼ぶと、PathBuilderがDisposeされるらしい。
            // 再利用に備えて （念のためDisposeしてから）nullを代入しておく。
            _pathBuilder?.Dispose();
            _pathBuilder = null;
        }
    }

    public void Stroke(double strokeWidth) {
        if (isOpened) {
            PathBuilder.EndFigure(CanvasFigureLoop.Open);
            isOpened = false;
        }
        using (var geo = CanvasGeometry.CreatePath(PathBuilder)) {
            double rw = 1f, rh = 1f;
            if (isAutoSize) {
                var rc = geo.ComputeBounds();
                var w = rc.Width + rc.X;
                var h = rc.Height + rc.Y;
                if (w > 0) {
                    rw = Width / w;
                }
                if (h > 0) {
                    rh = Height / h;
                }
            }
            else {
                rw = Width / PathWidth;
                rh = Height / PathHeight;
            }
            var mx = Matrix3x2.CreateScale((float)rw, (float)rh, Vector2.Zero);
            DrawingSession.DrawGeometry(geo.Transform(mx), Color, (float)strokeWidth);
        }
    }

    public void Dispose() {
        _pathBuilder?.Dispose();
        _pathBuilder = null;
    }
}

using System;
using System.Windows;
using Windows.UI;

namespace PathEdit.Parser;
public interface IGraphics : IDisposable {
    void MoveTo(Point point);
    void LineTo(Point point);
    void CurveTo(Point control1, Point control2, Point point);
    void QuadTo(Point control, Point point);
    void ArcTo(Size radius, double rotationAngle, bool isLargeArc, bool sweepDirection, Point point);
    void ClosePath();

    void Fill();
    void Stroke(double StrokeWidth);
}

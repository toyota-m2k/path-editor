using PathEdit.common;
using System;
using System.Windows;

namespace PathEdit.Parser;

static internal class GraphicUtil {
    public static void DrawArc(IGraphics graphics, Size radius, double rotationAngle, bool isLargeArc, bool sweepDirection, Point start, Point end) {
        drawArc(graphics, start.X, start.Y, end.X, end.Y, radius.Width, radius.Height, rotationAngle, isLargeArc, sweepDirection);
    }

    private static void drawArc(
        IGraphics p,
        double x0,
        double y0,
        double x1,
        double y1,
        double a,
        double b,
        double theta,
        bool isMoreThanHalf,
        bool isPositiveArc
    ) {

        /* Convert rotation angle from degrees to radians */
        var thetaD = Math.PI * theta / 180;
        /* Pre-compute rotation matrix entries */
        var cosTheta = Math.Cos(thetaD);
        var sinTheta = Math.Sin(thetaD);

        /* Transform (x0, y0) and (x1, y1) into unit space */
        /* using (inverse) rotation, followed by (inverse) scale */
        var x0p = (x0 * cosTheta + y0 * sinTheta) / a;
        var y0p = (-x0 * sinTheta + y0 * cosTheta) / b;
        var x1p = (x1 * cosTheta + y1 * sinTheta) / a;
        var y1p = (-x1 * sinTheta + y1 * cosTheta) / b;

        /* Compute differences and averages */
        var dx = x0p - x1p;
        var dy = y0p - y1p;
        var xm = (x0p + x1p) / 2;
        var ym = (y0p + y1p) / 2;
        /* Solve for intersecting unit circles */
        var dsq = dx * dx + dy * dy;
        if (dsq == 0.0) {
            LoggerEx.warn("Points are coincident");
            return;  /* Points are coincident */
        }
        var disc = 1.0 / dsq - 1.0 / 4.0;
        if (disc < 0.0) {
            LoggerEx.warn($"Points are too far apart {dsq}");
            var adjust = (Math.Sqrt(dsq) / 1.99999);
            drawArc(p, x0, y0, x1, y1, a * adjust, b * adjust, theta, isMoreThanHalf, isPositiveArc);
            return;  /* Points are too far apart */
        }
        var s = Math.Sqrt(disc);
        var sdx = s * dx;
        var sdy = s * dy;
        double cx;
        double cy;
        if (isMoreThanHalf == isPositiveArc) {
            cx = xm - sdy;
            cy = ym + sdx;
        }
        else {
            cx = xm + sdy;
            cy = ym - sdx;
        }
        var eta0 = Math.Atan2((y0p - cy), (x0p - cx));
        var eta1 = Math.Atan2((y1p - cy), (x1p - cx));
        var sweep = (eta1 - eta0);
        if (isPositiveArc != (sweep >= 0)) {
            if (sweep > 0) {
                sweep -= 2 * Math.PI;
            }
            else {
                sweep += 2 * Math.PI;
            }
        }
        cx *= a;
        cy *= b;
        var tcx = cx;
        cx = cx * cosTheta - cy * sinTheta;
        cy = tcx * sinTheta + cy * cosTheta;
        arcToBezier(
            p,
            cx,
            cy,
            a,
            b,
            x0,
            y0,
            thetaD,
            eta0,
            sweep
        );
    }

    /**
     * Converts an arc to cubic Bezier segments and records them in p.
     *
     * @param p     The target for the cubic Bezier segments
     * @param cx    The x coordinate center of the ellipse
     * @param cy    The y coordinate center of the ellipse
     * @param a     The radius of the ellipse in the horizontal direction
     * @param b     The radius of the ellipse in the vertical direction
     * @param e1x_   E(eta1) x coordinate of the starting point of the arc
     * @param e1y_   E(eta2) y coordinate of the starting point of the arc
     * @param theta The angle that the ellipse bounding rectangle makes with horizontal plane
     * @param start The start angle of the arc on the ellipse
     * @param sweep The angle (positive or negative) of the sweep of the arc on the ellipse
     */
    private static void arcToBezier(
        IGraphics p,
        double cx,
        double cy,
        double a,
        double b,
        double e1x,
        double e1y,
        double theta,
        double start,
        double sweep

    ) {
        // Taken from equations at: http://spaceroots.org/documents/ellipse/node8.html
        // and http://www.spaceroots.org/documents/ellipse/node22.html

        // Maximum of 45 degrees per cubic Bezier segment
        var numSegments = (int)Math.Ceiling(Math.Abs(sweep * 4 / Math.PI));
        var eta1 = start;
        var cosTheta = Math.Cos(theta);
        var sinTheta = Math.Sin(theta);
        var cosEta1 = Math.Cos(eta1);
        var sinEta1 = Math.Sin(eta1);
        var ep1x = (-a * cosTheta * sinEta1) - (b * sinTheta * cosEta1);
        var ep1y = (-a * sinTheta * sinEta1) + (b * cosTheta * cosEta1);
        var anglePerSegment = sweep / numSegments;
        for (int i = 0; i < numSegments; i++) {
            var eta2 = eta1 + anglePerSegment;
            var sinEta2 = Math.Sin(eta2);
            var cosEta2 = Math.Cos(eta2);
            var e2x = cx + (a * cosTheta * cosEta2) - (b * sinTheta * sinEta2);
            var e2y = cy + (a * sinTheta * cosEta2) + (b * cosTheta * sinEta2);
            var ep2x = -a * cosTheta * sinEta2 - b * sinTheta * cosEta2;
            var ep2y = -a * sinTheta * sinEta2 + b * cosTheta * cosEta2;
            var tanDiff2 = Math.Tan((eta2 - eta1) / 2);
            var alpha = Math.Sin(eta2 - eta1) * (Math.Sqrt(4 + (3 * tanDiff2 * tanDiff2)) - 1) / 3;
            var q1x = e1x + alpha * ep1x;
            var q1y = e1y + alpha * ep1y;
            var q2x = e2x - alpha * ep2x;
            var q2y = e2y - alpha * ep2y;

            // Adding this no-op call to workaround a proguard related issue.
            //p.rLineTo(0f, 0f)
            p.CurveTo(
                new Point(q1x, q1y),
                new Point(q2x, q2y),
                new Point(e2x, e2y)
            );
            eta1 = eta2;
            e1x = e2x;
            e1y = e2y;
            ep1x = ep2x;
            ep1y = ep2y;
        }
    }

}

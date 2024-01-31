namespace PathEdit.Parser;

//public interface IVector {
//    public double X { get; }
//    public double Y { get; }
//}

//public struct Vector : IVector {
//    public double X { get; }
//    public double Y { get; }

//    public Vector()
//    {
//        X = 0;
//        Y = 0;
//    }
//    public Vector(double x, double y)
//    {
//        X = x;
//        Y = y;
//    }
//    public Vector(IVector vector)
//    {
//        X = vector.X;
//        Y = vector.Y;
//    }
//}

//public struct Size : IVector {
//    public double X { get; }
//    public double Y { get; }

//    public Size() {
//        X = 0;
//        Y = 0;
//    }
//    public Size(double x, double y) {
//        X = x;
//        Y = y;
//    }
//    public Size(IVector vector) {
//        X = vector.X;
//        Y = vector.Y;
//    }
//}

//public struct Point : IVector {
//    public double X { get; }
//    public double Y { get; }

//    public Point() {
//        X = 0;
//        Y = 0;
//    }
//    public Point(IVector vector) {
//        X = vector.X;
//        Y = vector.Y;
//    }
//    public Point(double x, double y) {
//        X = x;
//        Y = y;
//    }

//    public Point Plus(IVector other) {
//        return new Point(X + other.X, Y + other.Y);
//    }
//    public Vector Minus(Point other) {
//        return new Vector(X - other.X, Y - other.Y);
//    }
//}

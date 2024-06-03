using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using TSMapEditor.Models;

namespace TSMapEditor.GameMath
{
    public struct Point2D
    {
        public int X;
        public int Y;

        public Point2D(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Point2D operator +(Point2D p1, Point2D p2)
        {
            return new Point2D(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static Point2D operator +(Point2D p)
        {
            return new Point2D(p.X, p.Y);
        }

        public static Point2D operator -(Point2D p1, Point2D p2)
        {
            return new Point2D(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static Point2D operator -(Point2D p)
        {
            return new Point2D(-p.X, -p.Y);
        }

        public static Point2D FromXNAPoint(Point point)
            => new Point2D(point.X, point.Y);

        public static Point2D Zero => new Point2D(0, 0);

        public static Point2D NegativeOne => new Point2D(-1, -1);

        public Vector2 ToXNAVector() => new Vector2(X, Y);

        public Point ToXNAPoint() => new Point(X, Y);

        public override int GetHashCode()
        {
            return Y * 1000 + X;
        }

        public override string ToString()
        {
            return X + ", " + Y;
        }

        public static Point2D FromString(string str)
        {
            string[] pointData = str.Split(',');
            if (pointData.Length != 2)
                throw new ArgumentException("Point2D.FromString: Invalid source string " + str);

            int x = Conversions.IntFromString(pointData[0].Trim(), -1);
            int y = Conversions.IntFromString(pointData[1].Trim(), -1);
            return new Point2D(x, y);
        }

        public static bool operator !=(Point2D p1, Point2D p2)
        {
            return !(p1 == p2);
        }

        public static bool operator ==(Point2D p1, Point2D p2)
        {
            return p1.X == p2.X && p1.Y == p2.Y;
        }

        public override bool Equals(object obj)
        {
            if (obj is Point2D objAsPoint)
            {
                return objAsPoint == this;
            }

            return false;
        }

        public Point2D NextPointFromTubeDirection(TubeDirection direction)
        {
            switch (direction)
            {
                case TubeDirection.NorthEast:
                    return this + new Point2D(0, -1);
                case TubeDirection.East:
                    return this + new Point2D(1, -1);
                case TubeDirection.SouthEast:
                    return this + new Point2D(1, 0);
                case TubeDirection.South:
                    return this + new Point2D(1, 1);
                case TubeDirection.SouthWest:
                    return this + new Point2D(0, 1);
                case TubeDirection.West:
                    return this + new Point2D(-1, 1);
                case TubeDirection.NorthWest:
                    return this + new Point2D(-1, 0);
                case TubeDirection.North:
                    return this + new Point2D(-1, -1);
                default:
                case TubeDirection.None:
                    return this;
            }
        }

        public float Angle()
        {
            return (float)Math.Atan2(Y, X);
        }

        /// <summary>
        /// Calculates and returns this point's tile-based distance to another point.
        /// </summary>
        /// <param name="other">The other point.</param>
        public int DistanceTo(Point2D other)
        {
            return Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));
        }

        public Point2D ScaleBy(double scale) => new Point2D((int)(X * scale), (int)(Y * scale));

        public Point2D ScaleBy(float scale) => new Point2D((int)(X * scale), (int)(Y * scale));

        public byte[] GetData()
        {
            byte[] buffer = new byte[sizeof(int) * 2];

            byte[] xb = BitConverter.GetBytes(X);
            byte[] yb = BitConverter.GetBytes(Y);
            Array.Copy(xb, buffer, xb.Length);
            Array.Copy(yb, 0, buffer, xb.Length, yb.Length);
            return buffer;
        }
    }
}

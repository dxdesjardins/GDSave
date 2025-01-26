using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public static class ExtensionsMath
{
    public static Vector2 RandomInsideUnitCircle(this Vector2 position) {
        float angle = GD.Randf() * 2 * Mathf.Pi;
        return position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    public static Vector2 GetUnitCenter(this Vector2 position) {
        float unitSize = 16;
        return new Vector2((Mathf.Floor(position.X / unitSize) + 0.5f) * unitSize, (Mathf.Floor(position.Y / unitSize) + 0.5f) * unitSize);
    }

    public static Color Lerp(this Color color1, Color color2, float t) {
        return color1 * (1 - t) + color2 * t;
    }

    public static float Remap(this float value, float from1, float to1, float from2, float to2) {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    public static void LerpRotationToTarget(this Sprite2D sprite, Vector2 target, float t = 0.1f) {
        sprite.Rotation = Mathf.LerpAngle(sprite.Rotation, (target - sprite.GlobalPosition).Angle(), t);
    }

    public static float ToRadians(this float degrees) {
        return degrees * (Mathf.Pi / 180);
    }

    public static float ToAngles(this float radians) {
        return radians * (180 / Mathf.Pi);
    }

    public static int Clamp(this int v, int min, int max) {
        return Mathf.Clamp(v, min, max);
    }

    public static float Clamp(this float v, float min, float max) {
        return Mathf.Clamp(v, min, max);
    }

    public static float Lerp(this float a, float b, float t) {
        return Mathf.Lerp(a, b, t);
    }

    public static float SinPulse(this float time, float frequency) {
        return 0.5f * (1 + Mathf.Sin(2 * Mathf.Pi * frequency * time));
    }

    public static Vector2 GetMinPoints(this Vector2[] vectors) {
        Vector2 min = vectors[0];
        foreach (Vector2 vector in vectors) {
            min.X = Math.Min(min.X, vector.X);
            min.Y = Math.Min(min.Y, vector.Y);
        }
        return min;
    }

    public static Vector2 GetMaxPoints(this Vector2[] vectors) {
        Vector2 max = vectors[0];
        foreach (Vector2 vector in vectors) {
            max.X = Math.Max(max.X, vector.X);
            max.Y = Math.Max(max.Y, vector.Y);
        }
        return max;
    }

    public static Vector2 GetMidPoints(this Vector2[] vectors) {
        Vector2 midValues = Vector2.Zero;
        for (int i = 0; i < vectors.Length; i++) {
            midValues.X += vectors[i].X;
            midValues.Y += vectors[i].Y;
        }
        return midValues /= vectors.Length;
    }

    public static void GetPolygonBounds(this Vector2[] vertices, out Vector2 min, out Vector2 max) {
        min = vertices[0];
        max = vertices[0];
        foreach (Vector2 vertex in vertices) {
            min.X = Mathf.Min(min.X, vertex.X);
            min.Y = Mathf.Min(min.Y, vertex.Y);
            max.X = Mathf.Max(max.X, vertex.X);
            max.Y = Mathf.Max(max.Y, vertex.Y);
        }
    }

    public static void GetPolygonBounds(this Polygon2D polygon, out Vector2 min, out Vector2 max) => GetPolygonBounds(polygon.Polygon, out min, out max);

    public static bool ContainsPoint(this Vector2[] vertices, Vector2 point) {
        int counter = 0;
        int i;
        double xinters;
        Vector2 p1, p2;
        p1 = vertices[0];
        int N = vertices.Length;
        for (i = 1; i <= N; i++) {
            p2 = vertices[i % N];
            if (point.Y > Mathf.Min(p1.Y, p2.Y)) {
                if (point.Y <= Mathf.Max(p1.Y, p2.Y)) {
                    if (point.X <= Mathf.Max(p1.X, p2.X)) {
                        if (p1.Y != p2.Y) {
                            xinters = (point.Y - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y) + p1.X;
                            if (p1.X == p2.X || point.X <= xinters)
                                counter++;
                        }
                    }
                }
            }
            p1 = p2;
        }
        if (counter % 2 == 0)
            return false;
        else
            return true;
    }

    public static bool ContainsPoint(this Polygon2D polygon, Vector2 point) => ContainsPoint(polygon.Polygon, point);

    public static bool IsWithinSquareDistanceFrom(this Vector2 point1, Vector2 point2, float distance) {
            return Mathf.Abs(point1.X - point2.X) < distance && Mathf.Abs(point1.Y - point2.Y) < distance;
    }

    public static Vector2 NormalizeToDominantAxis(this Vector2 vector) {
        if (Math.Abs(vector.X) > Math.Abs(vector.Y))
            vector = new Vector2(Math.Sign(vector.X), 0);
        else
            vector = new Vector2(0, Math.Sign(vector.Y));
        return vector;
    }
}

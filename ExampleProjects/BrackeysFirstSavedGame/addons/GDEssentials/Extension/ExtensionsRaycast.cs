using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public static class ExtensionsRaycast
{
    public static T GetRaycastCollision<T>(this Node2D node, Vector2 from, Vector2 to, uint collisionMask = 0b11111111111111111111) {
        return GetRaycastCollision<T>(node.GetWorld2D().DirectSpaceState, from, to, collisionMask);
    }

    public static T GetRaycastCollision<T>(this Control node, Vector2 from, Vector2 to, uint collisionMask = 0b11111111111111111111) {
        return GetRaycastCollision<T>(node.GetWorld2D().DirectSpaceState, from, to, collisionMask);
    }

    private static T GetRaycastCollision<T>(PhysicsDirectSpaceState2D spaceState, Vector2 from, Vector2 to, uint collisionMask = 0b11111111111111111111) {
        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(from, to, collisionMask);
        query.CollideWithAreas = true;
        query.HitFromInside = true;
        query.CollisionMask = collisionMask; // In the example '0b11111111111111111111' all 20 collision layers are set to be detected
        Godot.Collections.Dictionary result;
        do {
            result = spaceState.IntersectRay(query);
            if (result.Count > 0) {
                Node collision = (Node)result["collider"];
                T obj = collision.GetComponent<T>(false);
                if (obj != null)
                    return obj;
                Godot.Collections.Array<Rid> exclude = query.Exclude;
                exclude.Add((Rid)result["rid"]);
                query.Exclude = exclude;
            }
        } while (result.Count > 0);
        return default;
    }

    public static T[] GetRaycastCollisions<T>(this Node2D node, Vector2 from, Vector2 to, uint collisionMask = 0b11111111111111111111) {
        return GetRaycastCollisions<T>(node.GetWorld2D().DirectSpaceState, from, to, collisionMask);
    }

    public static T[] GetRaycastCollisions<T>(this Control node, Vector2 from, Vector2 to, uint collisionMask = 0b11111111111111111111) {
        return GetRaycastCollisions<T>(node.GetWorld2D().DirectSpaceState, from, to, collisionMask);
    }

    private static T[] GetRaycastCollisions<T>(PhysicsDirectSpaceState2D spaceState, Vector2 from, Vector2 to, uint collisionMask = 0b11111111111111111111) {
        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(from, to);
        query.CollideWithAreas = true;
        query.HitFromInside = true;
        query.CollisionMask = collisionMask; // In the example '0b11111111111111111111' all 20 collision layers are set to be detected
        Godot.Collections.Dictionary result;
        List<T> objects = new List<T>();
        do {
            result = spaceState.IntersectRay(query);
            if (result.Count > 0) {
                Node collision = (Node)result["collider"];
                T obj = collision.GetComponent<T>(false);
                if (obj != null)
                    objects.Add(obj);
                Godot.Collections.Array<Rid> exclude = query.Exclude;
                exclude.Add((Rid)result["rid"]);
                query.Exclude = exclude;
            }
        } while (result.Count > 0);
        return objects.ToArray();
    }

    public static T[] GetPointcastCollisions<T>(this Node2D node, Vector2 position, bool includeAreas = true, uint collisionMask = 0b11111111111111111111) {
        return GetPointcastCollisions<T>(node.GetWorld2D().DirectSpaceState, position, includeAreas, collisionMask);
    }

    public static T[] GetPointcastCollisions<T>(this Control node, Vector2 position, bool includeAreas = true, uint collisionMask = 0b11111111111111111111) {
        return GetPointcastCollisions<T>(node.GetWorld2D().DirectSpaceState, position, includeAreas, collisionMask);
    }

    private static T[] GetPointcastCollisions<T>(PhysicsDirectSpaceState2D spaceState, Vector2 position, bool includeAreas = true, uint collisionMask = 0b11111111111111111111) {
        PhysicsPointQueryParameters2D query = new() {
            CollideWithAreas = includeAreas,
            CollisionMask = collisionMask, // In the example '0b11111111111111111111' all 20 collision layers are set to be detected
            Position = position,
        };
        Godot.Collections.Array<Godot.Collections.Dictionary> results = spaceState.IntersectPoint(query);
        List<T> objects = new();
        foreach (var result in results) {
            Node collision = (Node)result["collider"];
            T obj = collision.GetComponent<T>(false);
            if (obj != null)
                objects.Add(obj);
        }
        return objects.ToArray();
    }

    public static T[] GetShapecastCollisions<T>(this Node2D node, Shape2D shape, Vector2 position, int maxResults = 32, uint collisionMask = 0b11111111111111111111) {
        return GetShapecastCollisions<T>(node.GetWorld2D().DirectSpaceState, shape, position, maxResults, collisionMask);
    }

    public static T[] GetShapecastCollisions<T>(this Control node, Shape2D shape, Vector2 position, int maxResults = 32, uint collisionMask = 0b11111111111111111111) {
        return GetShapecastCollisions<T>(node.GetWorld2D().DirectSpaceState, shape, position, maxResults, collisionMask);
    }

    private static T[] GetShapecastCollisions<T>(PhysicsDirectSpaceState2D spaceState, Shape2D shape, Vector2 position, int maxResults = 32, uint collisionMask = 0b11111111111111111111) {
        PhysicsShapeQueryParameters2D query = new() {
            CollideWithAreas = false,
            CollisionMask = collisionMask, // In the example '0b11111111111111111111' all 20 collision layers are set to be detected
            Shape = shape,
            Transform = new Transform2D(0, position),
        };
        Godot.Collections.Array<Godot.Collections.Dictionary> results = spaceState.IntersectShape(query, maxResults);
        List<T> objects = new List<T>();
        foreach (var result in results) {
            Node collision = (Node)result["collider"];
            T obj = collision.GetComponent<T>(false);
            if (obj != null)
                objects.Add(obj);
        }
        return objects.ToArray();
    }

    public static Node[] GetShapecastCollisions(this Node2D node, Shape2D shape, Vector2 position, int maxResults = 32, uint collisionMask = 0b11111111111111111111) => GetShapecastCollisions<Node>(node, shape, position, maxResults, collisionMask);
    public static Node[] GetShapecastCollisions(this Control node, Shape2D shape, Vector2 position, int maxResults = 32, uint collisionMask = 0b11111111111111111111) => GetShapecastCollisions<Node>(node, shape, position, maxResults, collisionMask);
}

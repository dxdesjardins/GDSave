using System;
using Godot;

namespace FirstGame.Scripts;

public static class Extensions
{
    public static T GetNodeOrThrow<T>(this Node self, NodePath path) where T : Node =>
        self.GetNodeOrNull<T>(path)
        ?? throw new NullReferenceException($"Node of type {typeof(T)} not found at path {path}");
}

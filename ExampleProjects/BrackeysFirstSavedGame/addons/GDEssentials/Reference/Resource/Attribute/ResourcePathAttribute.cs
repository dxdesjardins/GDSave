using System;

namespace Chomp.Essentials;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ResourcePathAttribute : Attribute
{
    public string Path { get; }

    public ResourcePathAttribute(string filePath) {
        Path = filePath;
    }
}

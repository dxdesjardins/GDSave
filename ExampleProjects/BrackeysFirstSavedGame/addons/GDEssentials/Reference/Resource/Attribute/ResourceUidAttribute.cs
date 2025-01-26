using System;

namespace Chomp.Essentials;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ResourceUidAttribute : Attribute
{
    public string Uid { get; }

    public ResourceUidAttribute(string resourceUid) {
        Uid = resourceUid;
    }
}

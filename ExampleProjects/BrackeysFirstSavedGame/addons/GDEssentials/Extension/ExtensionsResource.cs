using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public static class ExtensionsResource
{
    public static long GetUid(this Resource resource) {
        return ResourceLoader.GetResourceUid(resource.ResourcePath);
    }

    public static string GetUidString(this Resource resource) {
        return ResourceUid.IdToText(GetUid(resource));
    }

    /// <summary> Returns the resource file name without the extension. </summary>
    public static string GetFileName(this Resource resource) {
        string path = resource.ResourcePath;
        if (string.IsNullOrEmpty(path))
            GDE.LogErr($"Attempted to retrieve file name from resource({resource.GetType().Name}) with an empty ResourcePath.", 2);
        return System.IO.Path.GetFileNameWithoutExtension(path);
    }
}

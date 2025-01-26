using Godot;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Chomp.Essentials;

public abstract partial class ResourceReference<TDerived, TResource> : Resource where TResource : Resource
{
    private static TResource _Instance;

    public static TResource Instance {
        get {
            if (_Instance == null)
                CreateOrLoadInstance();
            return _Instance;
        }
    }

    public ResourceReference() {
        if (!Engine.IsEditorHint())
            return;
        _ = GDE.CallDeferred(() => {
            string uid = GetResourceUid();
            if (!string.IsNullOrEmpty(uid)) {
                long uidL = ResourceUid.TextToId(uid);
                if (!ResourceUid.HasId(uidL)) {
                    string newPath = this.ResourcePath;
                    ResourceUid.RemoveId(this.GetUid());
                    ResourceUid.AddId(uidL, newPath);
                    GDE.ChangeResourceUid(newPath, uid);
                    GDE.Log($"{typeof(TDerived).Name} UID({uid}) has been assigned to Path({newPath}).");
                }
                else if (GDE.UidToResource(uidL) is not TResource) {
                    GDE.LogErr($"{typeof(TDerived).Name} UID({uid}) is not pointing to the correct resource.");
                    string newPath = this.ResourcePath;
                    ResourceUid.RemoveId(this.GetUid());
                    ResourceUid.AddId(uidL, newPath);
                    GDE.ChangeResourceUid(newPath, uid);
                    GDE.Log($"{typeof(TDerived).Name} UID({uid}) has been assigned to Path({newPath}).");
                }
            }
            string path = GetResourcePath();
            if (string.IsNullOrEmpty(path))
                return;
            Resource resource = GD.Load(path);
            if (resource is null)
                GDE.LogErr($"{typeof(TDerived).Name} is not pointing to a resource.");
            else if (resource is not TResource)
                GDE.LogErr($"{typeof(TDerived).Name} is not pointing to the correct resource.");
        });
    }

    private static void CreateOrLoadInstance() {
        string filePath = GetResourcePath();
        if (!string.IsNullOrEmpty(filePath))
            _Instance = GD.Load<TResource>(filePath);
        else
            filePath = "res://";
        if (Engine.IsEditorHint()) {
            if (_Instance != null)
                return;
            _Instance = (TResource)Activator.CreateInstance(typeof(TResource));
            ResourceSaver.Save(_Instance, filePath);
            GDE.Log($"{typeof(TDerived).Name} instance has been created at Path({filePath}).");
        }
    }

    private static string GetResourcePath() {
        var attributes = typeof(TDerived).GetCustomAttributes(true);
        foreach (object attribute in attributes) {
            if (attribute is ResourceUidAttribute uidAttribute)
                return GDE.UidToPath(uidAttribute.Uid);
            else if (attribute is ResourcePathAttribute pathAttribute)
                return pathAttribute.Path;
        }
        GDE.LogErr($"{typeof(TDerived).Name} does not have a valid ResourceUidAttribute or ResourcePathAttribute.");
        return string.Empty;
    }

    private static string GetResourceUid() {
        var attributes = typeof(TDerived).GetCustomAttributes(true);
        foreach (object attribute in attributes) {
            if (attribute is ResourceUidAttribute uidAttribute)
                return uidAttribute.Uid;
        }
        return string.Empty;
    }
}

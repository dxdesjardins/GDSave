using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

// TODO: There is an engine limitation where resources cannot reference each other without causing recursion.
// UID string use is a temporary workaround.
// If the below proposal is ever closed, this class can be deleted.
// https://github.com/godotengine/godot-proposals/issues/7363

[Tool]
[GlobalClass]
public partial class ResourceWeakRef : Resource
{
    [Export] private string resourceName;
    private string resourceUID;
    [Export] private string ResourceUID {
        get => resourceUID;
        set {
            if (GDE.IsUidValid(value)) {
                resourceName = System.IO.Path.GetFileName(GDE.UidToPath(value));
                resourceUID = value;
            }
            else {
                resourceName = "";
                resourceUID = "";
            }
        }
    }
    public Resource Instance => IsValid ? GDE.LoadFromUid(resourceUID) : null;
    public bool IsValid => GDE.IsUidValid(resourceUID);

    public T GetInstance<T>() where T : Resource => Instance as T;

    public static implicit operator PackedScene(ResourceWeakRef weakRef) => GDE.UidToResource<PackedScene>(weakRef.resourceUID);
}

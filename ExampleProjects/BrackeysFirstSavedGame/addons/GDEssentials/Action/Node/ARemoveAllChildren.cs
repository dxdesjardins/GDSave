using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class ARemoveAllChildren : ParamAction<Node>
{
    [ExportGroup("Target")]
    [Export] NodeReference nodeReference;
    [Export] NodePath nodePath;
    [ExportGroup("Exceptions")]
    [Export] PackedScene[] packedSceneExceptions;
    [Export] bool removePersistantChildren = false;

    public override void Invoke(Node node) {
        if (nodeReference?.Instance != null)
            node = nodeReference.Instance;
        else if (!nodePath.IsEmpty)
            node = node.GetNode(nodePath);
        node.RemoveChildrenExcept(packedSceneExceptions, removePersistantChildren);
    }

    public override void Invoke(Node param, Node node) => Invoke(param);
}

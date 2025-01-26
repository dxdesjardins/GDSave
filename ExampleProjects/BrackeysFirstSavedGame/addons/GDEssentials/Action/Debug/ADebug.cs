using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class ADebug : ParamAction<float>
{
    [Export] NodePath target;

    public override void Invoke(float param, Node node) {
        if (!target.IsEmpty)
            node = node.GetNode(target);
        else
            node = node.GetParent();
        #region Debug With Node Here



        #endregion
    }

    public override void Invoke(Node node) => Invoke(default, node);
}

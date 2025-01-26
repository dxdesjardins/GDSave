using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class ASetTexture2D : ParamAction<Texture2D>
{
    [Export] private NodePath target;
    [Export] private Texture2D texture;

    public override void Invoke(Texture2D param, Node node) {
        Node tar;
        if (target.IsEmpty)
            tar = node.GetParent();
        else
            tar = node.GetNode(target);
        if (tar is Sprite2D sprite)
            sprite.Texture = param;
        else if (tar is TextureRect texRect)
            texRect.Texture = param;
    }

    public override void Invoke(Node node) => Invoke(texture, node);
}
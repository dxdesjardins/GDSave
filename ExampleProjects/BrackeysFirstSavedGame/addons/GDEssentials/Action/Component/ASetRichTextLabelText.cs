using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class ASetRichTextLabelText : ParamAction<string>
{
    [ExportGroup("Target")]
    [Export] public NodePath richTextLabel;
    [ExportGroup("Parameters")]
    [Export] public string text;

    public override void Invoke(string param, Node node) {
        if (richTextLabel.IsEmpty)
            node.GetParent<RichTextLabel>().Text = param;
        else
            node.GetNode<RichTextLabel>(richTextLabel).Text = param;
    }

    public override void Invoke(Node node) => Invoke(text, node);
}

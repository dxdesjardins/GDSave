using Godot;
using System;
using System.Collections.Generic;
using Chomp.Essentials;

namespace Chomp.Save.Internal;

[Tool]
[GlobalClass]
public partial class CachedSaveableData : Resource
{
    [Export] public StringVariable saveableId = new();
    [Export] public NodePath nodePath = new();
}

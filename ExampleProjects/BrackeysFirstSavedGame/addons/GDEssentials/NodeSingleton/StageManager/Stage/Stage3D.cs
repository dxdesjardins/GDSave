using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[Tool]
public partial class Stage3D : Node2D, IStage
{
    [Export] public Resource Data { get; private set; }
}

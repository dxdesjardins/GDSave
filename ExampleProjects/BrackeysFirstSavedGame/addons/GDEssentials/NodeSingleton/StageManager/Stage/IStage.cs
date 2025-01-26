using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public interface IStage
{
    [Export] public Resource Data { get; }
}

using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public abstract partial class GameAction : Resource
{
    public virtual void Invoke(Node node) { }
}

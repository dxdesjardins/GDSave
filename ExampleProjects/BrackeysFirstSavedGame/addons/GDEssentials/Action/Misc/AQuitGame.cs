using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class AQuitGame : GameAction
{
    public override void Invoke(Node node) => Execute();

    public static void Execute() {
        SceneTree tree = Engine.GetMainLoop() as SceneTree;
        _ = GDE.CallDeferred(() => {
            tree.Root.PropagateNotification((int)Window.NotificationWMCloseRequest);
            tree.Quit();
        });
    }
}

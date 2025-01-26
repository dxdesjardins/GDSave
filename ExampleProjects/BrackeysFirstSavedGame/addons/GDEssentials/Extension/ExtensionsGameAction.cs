using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chomp.Essentials;

public static class ExtensionsGameAction
{
    public static void Invoke<T>(this GameAction[] gameActions, T value, Node node) {
        if (gameActions != null) {
            foreach (GameAction gameAction in gameActions)
                if (gameAction is ParamAction<T> paramAction)
                    paramAction.Invoke(value, node);
                else
                    gameAction.Invoke(node);
        }
    }

    public static void Invoke(this GameAction[] gameActions, Node node) {
        if (gameActions != null) {
            foreach (GameAction gameAction in gameActions)
                gameAction.Invoke(node);
        }
    }
}

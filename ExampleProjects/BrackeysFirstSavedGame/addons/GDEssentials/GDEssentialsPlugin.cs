#if TOOLS
using Godot;

namespace Chomp.Essentials;

[Tool]
public partial class GDEssentialsPlugin : EditorPlugin
{
    private Key renameKeybind = Key.F3;

    public override void _Input(InputEvent _input) {
        if (_input is InputEventKey keyInput && keyInput.IsJustPressed(renameKeybind)) {
            var selectedNodes = EditorInterface.Singleton.GetSelection().GetSelectedNodes();
            if (selectedNodes.Count > 0) {
                Node node = selectedNodes[0];
                if (node.GetScript().AsGodotObject() is Script script)
                    node.Name = System.IO.Path.GetFileNameWithoutExtension(script.ResourcePath);
                else
                    node.Name = selectedNodes[0].GetType().Name;
            }
        }
    }
}
#endif

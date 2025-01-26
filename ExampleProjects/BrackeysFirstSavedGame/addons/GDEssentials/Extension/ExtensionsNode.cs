using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Chomp.Essentials;

public static class ExtensionsNode
{
    public static T[] GetComponentsInChildren<T>(this Node node, bool isParent = true, bool includeParent = true, bool includeInternal = false) {
        return GetComponentsInChildren(node, isParent, includeParent, includeInternal).OfType<T>().ToArray();
    }

    public static Node[] GetComponentsInChildren(this Node node, bool isParent = true, bool includeParent = true, bool includeInternal = false) {
        List<Node> components = new();
        if (!isParent)
            node = node.GetParent();
        if (includeParent)
            components.Add(node);
        if (node.GetChildCount() > 0) {
            foreach (Node i in node.GetChildren(includeInternal)) {
                foreach (Node j in i.GetComponentsInChildren(true, true, includeInternal))
                    components.Add(j);
            }
        }
        return components.ToArray();
    }

    public static T GetComponentInChildren<T>(this Node node, bool isParent = true, bool includeParent = true, bool includeInternal = false) {
        if (!isParent)
            node = node.GetParent();
        if (includeParent && node is T parent)
            return parent;
        if (node.GetChildCount() > 0) {
            foreach (Node i in node.GetChildren(includeInternal)) {
                foreach (Node j in i.GetComponentsInChildren(true, true, includeInternal))
                    if (j is T component)
                        return component;
            }
        }
        return default;
    }

    public static T GetComponentInParent<T>(this Node node, bool includeInternal = false) {
        T result;
        do {
            node = node.GetParent();
            if (node != null) {
                result = node.GetComponent<T>(true, true, includeInternal);
                if (result != null)
                    return result;
            }
        }
        while (node != null);
        return default;
    }

    public static T GetComponent<T>(this Node node, bool isParent = true, bool includeParent = true, bool includeInternal = false) {
        if (!isParent)
            node = node.GetParent();
        if (includeParent && node is T parent)
            return parent;
        if (node.GetChildCount() > 0) {
            foreach (Node i in node.GetChildren(includeInternal)) {
                if (i is T component)
                    return component;
            }
        }
        return default;
    }

    public static Node[] GetComponents(this Node node, bool isParent = true, bool includeParent = true, bool includeInternal = false) {
        List<Node> components = new();
        if (!isParent)
            node = node.GetParent();
        if (includeParent)
            components.Add(node);
        if (node.GetChildCount() > 0) {
            foreach (Node i in node.GetChildren(includeInternal))
                components.Add(i);
        }
        return components.ToArray();
    }

    public static T[] GetComponents<T>(this Node node, bool isParent = true, bool includeParent = true, bool includeInternal = false) {
        return GetComponents(node, isParent, includeParent, includeInternal).OfType<T>().ToArray();
    }

    public static bool IsParent(this Node node) {
        return node.GetChildCount() > 0;
    }

    public static void SetActive(this Node node, bool state) {
        node.SetPhysicsProcess(state);
        node.ProcessMode = (state) ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;
        if (node is CanvasItem canvasItem)
            canvasItem.Visible = state;
    }

    public static void SetTransform(this Control control, Transform2D transform) {
        control.Position = transform.Origin;
        control.Rotation = transform.Rotation;
        control.Scale = transform.Scale;
    }

    public static Node AddChild(this Node parent, Node instance, Vector2 position) {
        if (instance is Node2D node2D)
            node2D.Position = position;
        else if (instance is Control control)
            control.Position = position;
        parent.AddChild(instance);
        return instance;
    }

    public static Node AddChild(this Node parent, Node instance, Transform2D transform) {
        if (instance is Node2D node2D)
            node2D.Transform = transform;
        else if (instance is Control control)
            control.SetTransform(transform);
        parent.AddChild(instance);
        return instance;
    }

    public static Node AddChild(this Node parent, Node instance, Vector3 position) {
        if (instance is Node3D node3D)
            node3D.Position = position;
        parent.AddChild(instance);
        return instance;
    }

    public static Node InstantiateChild(this Node parent, PackedScene packedScene, Vector2 position = default, bool setUniqueName = true) {
        Node instance = packedScene.Instantiate();
        if (setUniqueName)
            instance.SetUniqueName();
        Node child = parent.AddChild(instance, position);
        return child;
    }

    public static Node InstantiateChild(this Node parent, PackedScene packedScene, Vector3 position, bool setUniqueName = true) {
        Node instance = packedScene.Instantiate();
        if (setUniqueName)
            instance.SetUniqueName();
        Node child = parent.AddChild(instance, position);
        return child;
    }

    public static T InstantiateChild<T>(this Node parent, Vector2 position = default, bool setUniqueName = true) where T : Node {
        Node instance = (Node)Activator.CreateInstance(typeof(T));
        if (setUniqueName)
            instance.SetUniqueName();
        return parent.AddChild(instance, position) as T;
    }

    public static T InstantiateChild<T>(this Node parent, Vector3 position, bool setUniqueName = true) where T : Node {
        Node instance = (Node)Activator.CreateInstance(typeof(T));
        if (setUniqueName)
            instance.SetUniqueName();
        return parent.AddChild(instance, position) as T;
    }

    public static Node InstantiateChild(this Node parent, PackedScene packedScene, Transform2D transform, bool setUniqueName = true) {
        Node instance = packedScene.Instantiate();
        if (setUniqueName)
            instance.SetUniqueName();
        Node child = parent.AddChild(instance, transform);
        return child;
    }

    public static Node InstantiateChild(this Node parent, PackedScene packedScene, bool setUniqueName) => parent.InstantiateChild(packedScene, Vector2.Zero, setUniqueName);

    public static Node SafeInstantiateChild(this Node parent, PackedScene packedScene, bool setUniqueName = true) {
        Node instance = packedScene.Instantiate();
        if (setUniqueName)
            instance.SetUniqueName();
        parent.SafeAddChild(instance);
        return instance;
    }

    /// <summary> Returns the Node that has the top rendered deep child Sprite2D and at least one shallow child inheriting T.
    /// <para> Example: If a pickaxe raycast2D hits multiple colliders and you only want it to only damage the top rendered entity. </para> </summary>
    public static Node GetTopRenderedNode2D<T>(this Node[] nodes, bool areParents = true) {
        List<Node> tNodes = new();
        foreach (Node node in nodes)
            if (node.GetComponent<T>(areParents) != null)
                tNodes.Add(node);
        Sprite2D topS = null;
        Node topN = null;
        foreach (Node tNode in tNodes) {
            Sprite2D s = tNode.GetComponentInChildren<Sprite2D>(areParents);
            if (s != null && s.Visible) {
                if (topS == null) {
                    topS = s;
                    topN = tNode;
                    continue;
                }
                int sZIndex = s.GetRelativeZIndex();
                int topSZIndex = topS.GetRelativeZIndex();
                if (sZIndex > topSZIndex) {
                    topS = s;
                    topN = tNode;
                    continue;
                }
                else if (sZIndex < topSZIndex)
                    continue;
                if (s.GetParentOrNull<Node2D>().YSortEnabled || topS.GetParentOrNull<Node2D>().YSortEnabled) {
                    if (s.Position.Y > topS.Position.Y) {
                        topS = s;
                        topN = tNode;
                        continue;
                    }
                    else if (s.Position.Y < topS.Position.Y)
                        continue;
                }
            }
        }
        return topN;
    }

    public static int GetRelativeZIndex(this CanvasItem canvasItem) {
        int z_index = canvasItem.ZIndex;
        if (!canvasItem.ZAsRelative)
            return canvasItem.ZIndex;
        while (canvasItem.GetParentOrNull<CanvasItem>() is CanvasItem parent) {
            z_index += parent.ZIndex;
            if (!parent.ZAsRelative)
                break;
            canvasItem = parent;
        }
        return z_index;
    }

    public static bool IsInGroup<[MustBeVariant] TEnum>(this Node node, Godot.Collections.Array<TEnum> groups) {
        var nodeGroups = node.GetGroups();
        for (int i = 0; i < groups.Count; i++) {
            for (int j = 0; j < nodeGroups.Count; j++) {
                if (groups[i].ToString() == nodeGroups[j].ToString())
                    return true;
            }
        }
        return false;
    }

    public static bool IsInGroup(this Node node, string[] groups) {
        for (int i = 0; i < groups.Length; i++) {
            if (node.IsInGroup(groups[i]))
                return true;
        }
        return false;
    }

    public static Node[] GetNodesInGroup(this Node[] nodes, string group) => GetNodesInGroup<Node>(nodes, group);

    public static T[] GetNodesInGroup<T>(this T[] nodes, string group) where T : Node {
        List<T> groupNodes = new();
        foreach (T node in nodes) {
            if (node.IsInGroup(group))
                groupNodes.Add(node);
        }
        return groupNodes.ToArray();
    }

    public static int GetTreeDepth(this Node node) {
        int depth = 0;
        while (node.GetParent() != null) {
            depth++;
            node = node.GetParent();
        }
        return depth;
    }

    public static int GetSiblingIndex(this Node node) {
        Node parent = node.GetParent();
        if (parent == null)
            return -1;
        int siblingCount = parent.GetChildCount();
        for (int i = 0; i < siblingCount; i++)
            if (parent.GetChild(i) == node)
                return i;
        return -1;
    }

    public static void SafeAddChild(this Node parent, Node child) {
        if (parent.IsNodeReady())
            parent.AddChild(child);
        else
            parent.CallDeferred(Node.MethodName.AddChild, child);
    }

    public static Node GetAncestor(this Node node, int generations) {
        for (int i = 0; i < generations; i++)
            node = node.GetParent();
        return node;
    }

    public static void RemoveParent(this Node node) {
        Node parent = node.GetParent();
        parent.GetParent()?.RemoveChild(parent);
    }

    public static void Remove(this Node node) {
        node.GetParent()?.RemoveChild(node);
    }

    public static void RemoveFirstParent(this Node node) {
        if (node.IsParent())
            node.Remove();
        else
            node.RemoveParent();
    }

    public static void RemoveChildren(this Node node) {
        if (node.GetChildCount() == 0)
            return;
        foreach (var child in node.GetChildren())
            node.RemoveChild(child);
    }

    public static void RemoveChildrenExcept(this Node node, PackedScene[] exceptions, bool removePersistantChildren = false) {
        if (node.GetChildCount() == 0)
            return;
        foreach (var child in node.GetChildren()) {
            bool exception = false;
            for (int i = 0; i < exceptions.Length; i++)
                if (exceptions[i].ResourcePath == child.SceneFilePath) {
                    exception = true;
                    break;
                }
            if (!exception && (removePersistantChildren || !child.IsInGroup("Persistant")))
                node.RemoveChild(child);
        }
    }

    public static async Task SafeRemoveParent(this Node node) {
        await node.GetParent().SafeRemove();
    }

    public static async Task SafeRemove(this Node node) {
        Node parent = node.GetParent();
        if (parent.IsNodeReady())
            parent.RemoveChild(node);
        else {
            await GDE.Yield();
            if (parent != null && parent.IsInsideTree())
                parent.RemoveChild(node);
        }
    }

    public static Node2D LookAt(this Node2D node, Vector2 target) {
        node.Rotation = node.GlobalPosition.AngleToPoint(target);
        return node;
    }

    public static bool IsPartOfStage(this Node node) {
        do {
            if (node is IStage)
                return true;
            node = node.GetParent();
        }
        while (node != null);
        return false;
    }

    public static bool IsPartOfScene(this Node node) {
        do {
            if (!string.IsNullOrEmpty(node.SceneFilePath))
                return true;
            node = node.GetParent();
        }
        while (node != null);
        return true;
    }

    public static bool IsPartOfSceneNotStage(this Node node) {
        do {
            if (node is IStage)
                return false;
            if (!string.IsNullOrEmpty(node.SceneFilePath))
                return true;
            node = node.GetParent();
        }
        while (node != null);
        return true;
    }

    public static Node GetStage(this Node node) {
        do {
            if (node is IStage)
                return node;
            node = node.GetParent();
        }
        while (node != null);
        return null;
    }

    public static Node GetScene(this Node node) {
        if (node.Owner != null)
            return node.Owner;
        do {
            node = node.GetParent();
            if (!string.IsNullOrEmpty(node.SceneFilePath))
                return node;
        }
        while (node != null);
        return null;
    }

    public static void SetUniqueName(this Node node) {
        if (string.IsNullOrEmpty(node.Name))
            node.Name = node.GetType().Name;
        node.Name = $"{node.Name}#{node.GetHashCode()}";
    }

    public static Vector2 GetGlobalPosition(this Node node) {
        if (node is Node2D node2D)
            return node2D.GlobalPosition;
        else if (node is Control control)
            return control.GlobalPosition;
        return Vector2.Zero;
    }

    public static bool IsMouseWithin(this Control control) {
        return control.GetGlobalRect().HasPoint(control.GetGlobalMousePosition());
    }

    /// <summary> Returns the scene file name without the extension. </summary>
    public static string GetFileName(this Node node) {
        string path = node.SceneFilePath;
        if (string.IsNullOrEmpty(path))
            GDE.LogErr($"Attempted to retrieve file name from node({node.GetType().Name}) with an empty SceneFilePath.", 2);
        return System.IO.Path.GetFileNameWithoutExtension(path);
    }

    /// <summary> Returns false if node scene is not ready, the game is quitting. </summary>
    /// <para> This is useful to determine why a node would be exiting the tree. </para></summary>
    public static bool IsRemovedExplicitly(this Node node) {
        return !StageManager.IsQuittingGame && node.GetScene().IsNodeReady();
    }

    // TODO: Engine currently does not have a better way to determine if a node is internal.
    // https://github.com/godotengine/godot-proposals/issues/4265
    public static bool IsInternal(this Node node) {
        Node parent = node.GetParent();
        if (parent == null || parent.GetChildren().Contains(node))
            return false;
        return true;
    }

    public static bool IsAnAutoload(this Node node) {
        return node.GetParent() == node.GetTree().Root && node.GetTree().CurrentScene != node;
    }

    public static string GetUidString(this Node node) {
        return GDE.PathToUidString(node.SceneFilePath);
    }

    public static long GetUid(this Node node) {
        return GDE.PathToUid(node.SceneFilePath);
    }
}

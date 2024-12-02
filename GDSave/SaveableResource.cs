using Godot;
using System;
using System.Collections.Generic;
using Chomp.Essentials;

namespace Chomp.Save;

public abstract partial class SaveableResource : Resource, ISerializationListener, ISaveable
{
    [Export] public string SaveableId { get; set; }

    public SaveableResource() {
        if (Engine.IsEditorHint()) {
            _ = GDE.CallDeferred(SetIdentification);
            return;
        }
        SaveManager.SyncSaveBegin += (_) => {
            if (OnSaveCondition())
                SaveManager.SetString(SaveableId, OnSave());
        };
        SaveManager.SyncLoadBegin += (_) => OnLoadSlot();
        SaveManager.SlotChangeDone += (newSlot, oldSlot) => OnLoadSlot();
    }

    private void SetIdentification() {
        if (string.IsNullOrEmpty(SaveableId)) {
            string uid = this.GetUidString();
            string typeName = this.GetType().Name;
            SaveableId = string.Format("{0}-{1}", typeName, uid);
        }
    }

    private void OnLoadSlot() {
        if (SaveManager.GetActiveSlot() == -1)
            return;
        string data = SaveManager.GetString(SaveableId);
        OnLoad(data);
    }

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize() {
        if (Engine.IsEditorHint())
            return;
        OnLoadSlot();
    }

    public virtual bool OnSaveCondition() {
        return true;
    }

    public abstract string OnSave();

    public abstract void OnLoad(string saveData);
}

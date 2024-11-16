using Godot;
using System;
using System.Collections.Generic;
using Chomp.Essentials;
using Chomp.Save.Internal;

namespace Chomp.Save.Components;

public partial class SaveEventListener : Node
{
    [ExportGroup("Param: Vector2I(NewSlot, OldSlot)")]
    [Export] private GameAction[] onSlotChangeBegin;
    [Export] private GameAction[] onSlotChangeDone;

    [ExportGroup("Param: Int(Slot)")]
    [Export] private GameAction[] onSyncSaveBegin;
    [Export] private GameAction[] onSyncSaveDone;
    [Export] private GameAction[] onWriteToDiskBegin;
    [Export] private GameAction[] onWriteToDiskDone;
    [Export] private GameAction[] onDeletedSave;

    [ExportGroup("Param: (PackedScene, SavedInstance)")]
    [Export] private GameAction[] onSpawnedSavedInstance;

    public override void _EnterTree() {
        SaveManager.DeletedSave += OnDeletedSave;
        SaveManager.SlotChangeBegin += OnSlotChangeBegin;
        SaveManager.SlotChangeDone += OnSlotChangeDone;
        SaveManager.SyncSaveBegin += OnSaveSyncBegin;
        SaveManager.SyncSaveDone += OnSaveSyncDone;
        SaveManager.WritingToDiskBegin += OnWritingToDiskBegin;
        SaveManager.WritingToDiskDone += OnWritingToDiskDone;
        SaveManager.SpawnedSavedScene += OnSpawnedSavedInstance;
    }

    public override void _ExitTree() {
        SaveManager.DeletedSave -= OnDeletedSave;
        SaveManager.SlotChangeBegin -= OnSlotChangeBegin;
        SaveManager.SlotChangeDone -= OnSlotChangeDone;
        SaveManager.SyncSaveBegin -= OnSaveSyncBegin;
        SaveManager.SyncSaveDone -= OnSaveSyncDone;
        SaveManager.WritingToDiskBegin -= OnWritingToDiskBegin;
        SaveManager.WritingToDiskDone -= OnWritingToDiskDone;
        SaveManager.SpawnedSavedScene -= OnSpawnedSavedInstance;
    }

    private void OnSpawnedSavedInstance(PackedScene scene, Node savedInstance) {
        onSpawnedSavedInstance.Invoke((scene, savedInstance), this);
    }

    private void OnWritingToDiskDone(int obj) {
        onWriteToDiskDone.Invoke(obj, this);
    }

    private void OnWritingToDiskBegin(int obj) {
        onWriteToDiskBegin.Invoke(obj, this);
    }

    private void OnSaveSyncDone(int obj) {
        onSyncSaveDone.Invoke(obj, this);
    }

    private void OnSaveSyncBegin(int obj) {
        onSyncSaveBegin.Invoke(obj, this);
    }

    private void OnSlotChangeDone(int slot1, int slot2) {
        onSlotChangeDone.Invoke(new Vector2I(slot1, slot2), this);
    }

    private void OnSlotChangeBegin(int slot1, int slot2) {
        onSlotChangeBegin.Invoke(new Vector2I(slot1, slot2), this);
    }

    private void OnDeletedSave(int obj) {
        onDeletedSave.Invoke(obj, this);
    }
}

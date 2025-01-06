namespace Chomp.Save;

public interface ISaveable
{
    // Must be serialized with an [Export] attribute in the inheriting class
    public string SaveableId { get; set; }

    public string OnSave();

    public void OnLoad(string data);

    public bool OnSaveCondition() {
        return true;
    }
}

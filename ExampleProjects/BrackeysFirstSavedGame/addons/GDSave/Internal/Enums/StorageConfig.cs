namespace Chomp.Save.Internal;

[System.Serializable]
public struct StorageConfig
{
    public StorageType StorageType { get; set; }
    public bool WriteIndented { get; set; }
    public bool IncludeFields { get; set; }
    public bool IncludePrivateFields { get; set; }
    public bool UnsafeRelaxedJsonEscaping { get; set; }
    public bool UseEncryption { get; set; }
}

using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public partial class PreferenceManager : NodeSingleton<PreferenceManager>
{
    [Export] private string configFilepath = "user://GodotPreferences.cfg";
    private static ConfigFile configFile = new();

    public PreferenceManager() {
        SearchTreeForInstance = false;
        AddToTree = false;
        if (System.IO.File.Exists(configFilepath)) {
            Error error = configFile.Load(configFilepath);
            if (error != Error.Ok)
                GDE.LogErr($"Error loading preferences file: {error.ToString()}");
        }
    }

    private static void SavePreferences() {
        Error error = configFile.Save(Instance.configFilepath);
        if (error != Error.Ok)
            GDE.LogErr($"Error saving preferences file: {error.ToString()}");
    }

    public static void SetPreference(string key, Variant value) {
        configFile.SetValue("preferences", key, value);
        SavePreferences();
    }

    public static void DeletePreference(string key) {
        if (configFile.HasSectionKey("preferences", key))
            configFile.EraseSectionKey("preferences", key);
        SavePreferences();
    }

    public static Variant GetPreference(string key, Variant defaultValue = default) {
        return configFile.GetValue("preferences", key, defaultValue);
    }
}

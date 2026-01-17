using Dalamud.Configuration;
using System;

namespace SamplePlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public string GameVersion { get; set; } = "";


    // Plugin settings
    public string Logpath { get; set; } = Path.Combine(System.IO.Path.GetTempPath(), "Critfarmer\\logs\\");

    public int EncounterEndDelayMs { get; set; } = 5000;

    //UI Settings
    // Header
    public bool ShowHeader = true;
    public bool ShowTimer = true;
    public bool ShowZoneName = true;
    public bool ShowTotalDamage= true;
    public bool ShowTotalHealed = true;
    public bool ShowRank = true;
    public bool ShowMaxHit = true;

    // DPS Table
    public bool ShowDpsTable = true;
    public bool ShowDpsName = true;
    public bool ShowDpsValue = true;
    public bool ShowDpsPercent = true;
    public bool ShowDamage = true;
    public bool ShowSwings = true;
    public bool ShowDirectHit = true;
    public bool ShowCritHit = true;
    public bool ShowCritDirectHit = true;
    public bool ShowMaxHitColumn = true;
    public bool ShowDeaths = true;

    // HPS Table
    public bool ShowHpsTable = true;
    public bool ShowHpsValue = true;
    public bool ShowHpsPercent = true;
    public bool ShowHealed = true;
    public bool ShowEffectiveHeal = true;
    public bool ShowShield = true;
    public bool ShowOverheal = true;

    //Debug values
    public bool IndentLogLines { get; set; } = false;
    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}

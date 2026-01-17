using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace SamplePlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("A Wonderful Configuration Window###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
        if (configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
    }

    public override void Draw()
    {

        if (ImGui.BeginTabBar("SettingsTabs"))
        {
            DrawMainSettings();
            DrawHeaderSettings();
            DrawDpsSettings();
            DrawHpsSettings();

            ImGui.EndTabBar();
        }
        
    }

    private void DrawMainSettings()
    {
        if (!ImGui.BeginTabItem("Main"))
            return;
        ImGui.Text("Main Settings");
        ImGui.Separator();
        // Can't ref a property, so use a local copy
        var configValue = configuration.SomePropertyToBeSavedAndWithADefault;
        if (ImGui.Checkbox("Indent log lines", ref configValue))
        {
            configuration.IndentLogLines = configValue;
            // Can save immediately on change if you don't want to provide a "Save and Close" button
            configuration.Save();
        }

        var endMs = configuration.EncounterEndDelayMs;
        if (ImGui.InputInt("Encounter end delay (ms)", ref endMs, 10, 100))
        {
            configuration.EncounterEndDelayMs = Math.Max(1000, endMs);
            configuration.Save();
        }
        // Add main settings here in the future
        ImGui.EndTabItem();
    }
    private void DrawHeaderSettings()
    {
        if (!ImGui.BeginTabItem("Header"))
            return;

        ImGui.Text("Header Elements");
        ImGui.Separator();

        var showHeader = configuration.ShowHeader;
        if (ImGui.Checkbox("Show Header", ref showHeader))
        {
            configuration.ShowHeader = showHeader;
            configuration.Save();
        }

        ImGui.Indent();

        var showTimer = configuration.ShowTimer;
        if (ImGui.Checkbox("Timer", ref showTimer))
        {
            configuration.ShowTimer = showTimer;
            configuration.Save();
        }

        var showZoneName = configuration.ShowZoneName;
        if (ImGui.Checkbox("Player Name", ref showZoneName))
        {
            configuration.ShowZoneName = showZoneName;
            configuration.Save();
        }

        var showShowTotalDamage = configuration.ShowTotalDamage;
        if (ImGui.Checkbox("Total Damage", ref showShowTotalDamage))
        {
            configuration.ShowTotalDamage = showShowTotalDamage;
            configuration.Save();
        }

        var showTotalHealed = configuration.ShowTotalHealed;
        if (ImGui.Checkbox("Totals (DPS / HPS)", ref showTotalHealed))
        {
            configuration.ShowTotalHealed = showTotalHealed;
            configuration.Save();
        }

        var showRank = configuration.ShowRank;
        if (ImGui.Checkbox("Rank", ref showRank))
        {
            configuration.ShowRank = showRank;
            configuration.Save();
        }

        var showMaxHit = configuration.ShowMaxHit;
        if (ImGui.Checkbox("Max Hit", ref showMaxHit))
        {
            configuration.ShowMaxHit = showMaxHit;
            configuration.Save();
        }

        ImGui.Unindent();
        ImGui.EndTabItem();
    }

    private void DrawDpsSettings()
    {
        if (!ImGui.BeginTabItem("DPS Table"))
            return;

        ImGui.Text("DPS Table Columns");
        ImGui.Separator();

        var showDpsTable = configuration.ShowDpsTable;
        if (ImGui.Checkbox("Show DPS Table", ref showDpsTable))
        {
            configuration.ShowDpsTable = showDpsTable;
            configuration.Save();
        }

        ImGui.Indent();

        var showName = configuration.ShowDpsName;
        if (ImGui.Checkbox("Name", ref showName))
        {
            configuration.ShowDpsName = showName;
            configuration.Save();
        }

        var showDpsValue = configuration.ShowDpsValue;
        if (ImGui.Checkbox("DPS Value", ref showDpsValue))
        {
            configuration.ShowDpsValue = showDpsValue;
            configuration.Save();
        }

        var showPercent = configuration.ShowDpsPercent;
        if (ImGui.Checkbox("Percent Bar", ref showPercent))
        {
            configuration.ShowDpsPercent = showPercent;
            configuration.Save();
        }

        var showDamage = configuration.ShowDamage;
        if (ImGui.Checkbox("Damage", ref showDamage))
        {
            configuration.ShowDamage = showDamage;
            configuration.Save();
        }

        var showSwings = configuration.ShowSwings;
        if (ImGui.Checkbox("Swings", ref showSwings))
        {
            configuration.ShowSwings = showSwings;
            configuration.Save();
        }

        var showDirectHit = configuration.ShowDirectHit;
        if (ImGui.Checkbox("Direct Hit %", ref showDirectHit))
        {
            configuration.ShowDirectHit = showDirectHit;
            configuration.Save();
        }

        var showCritHit = configuration.ShowCritHit;
        if (ImGui.Checkbox("Critical Hit %", ref showCritHit))
        {
            configuration.ShowCritHit = showCritHit;
            configuration.Save();
        }

        var showCritDirectHit = configuration.ShowCritDirectHit;
        if (ImGui.Checkbox("Crit + Direct %", ref showCritDirectHit))
        {
            configuration.ShowCritDirectHit = showCritDirectHit;
            configuration.Save();
        }

        var showMaxHitColumn = configuration.ShowMaxHitColumn;
        if (ImGui.Checkbox("Max Hit", ref showMaxHitColumn))
        {
            configuration.ShowMaxHitColumn = showMaxHitColumn;
            configuration.Save();
        }

        var showDeaths = configuration.ShowDeaths;
        if (ImGui.Checkbox("Deaths", ref showDeaths))
        {
            configuration.ShowDeaths = showDeaths;
            configuration.Save();
        }

        ImGui.Unindent();
        ImGui.EndTabItem();
    }

    private void DrawHpsSettings()
    {
        if (!ImGui.BeginTabItem("HPS Table"))
            return;

        ImGui.Text("HPS Table Columns");
        ImGui.Separator();

        var showHpsTable = configuration.ShowHpsTable;
        if (ImGui.Checkbox("Show HPS Table", ref showHpsTable))
        {
            configuration.ShowHpsTable = showHpsTable;
            configuration.Save();
        }

        ImGui.Indent();

        var showHpsValue = configuration.ShowHpsValue;
        if (ImGui.Checkbox("HPS Value", ref showHpsValue))
        {
            configuration.ShowHpsValue = showHpsValue;
            configuration.Save();
        }

        var showHpsPercent = configuration.ShowHpsPercent;
        if (ImGui.Checkbox("Percent Bar", ref showHpsPercent))
        {
            configuration.ShowHpsPercent = showHpsPercent;
            configuration.Save();
        }

        var showHealed = configuration.ShowHealed;
        if (ImGui.Checkbox("Total Healed", ref showHealed))
        {
            configuration.ShowHealed = showHealed;
            configuration.Save();
        }

        var showEffectiveHeal = configuration.ShowEffectiveHeal;
        if (ImGui.Checkbox("Effective Heal", ref showEffectiveHeal))
        {
            configuration.ShowEffectiveHeal = showEffectiveHeal;
            configuration.Save();
        }

        var showShield = configuration.ShowShield;
        if (ImGui.Checkbox("Shield", ref showShield))
        {
            configuration.ShowShield = showShield;
            configuration.Save();
        }

        var showOverheal = configuration.ShowOverheal;
        if (ImGui.Checkbox("Overheal %", ref showOverheal))
        {
            configuration.ShowOverheal = showOverheal;
            configuration.Save();
        }

        ImGui.Unindent();
        ImGui.EndTabItem();
    }
}

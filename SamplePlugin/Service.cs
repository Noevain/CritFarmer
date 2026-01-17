using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace SamplePlugin
{
    public class Service
    {
        [PluginService]
        public static IDalamudPluginInterface Interface { get; private set; } = null!;
        [PluginService]
        public static IClientState ClientState { get; private set; } = null!;
        [PluginService]
        public static ICommandManager CommandManager { get; private set; } = null!;

        [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;

        [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
        [PluginService] public static IDataManager DataManager { get; private set; } = null!;
        [PluginService] public static IFramework Framework { get; private set; } = null!;
        [PluginService] public static IObjectTable ObjectTable { get; private set; } = null!;
        [PluginService] public static IPluginLog Log { get; private set; } = null!;
        [PluginService] public static IFlyTextGui FlyTextGui { get; private set; } = null!;
        [PluginService] public static IChatGui ChatGui { get; private set; } = null!;
        [PluginService] public static IGameGui GameGui { get; private set; } = null!;
        [PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

        public static void Initialize(IDalamudPluginInterface pluginInterface)
            => pluginInterface.Create<Service>();
    }
}

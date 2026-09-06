using Barotrauma;
using Barotrauma.Plugins;
using HarmonyLib;
using Microsoft.Xna.Framework;
using System.Reflection;

namespace QuickInventory;

public partial class Plugin : IBarotraumaPlugin
{
    public static readonly IDebugConsole DebugConsole = PluginServiceProvider.GetService<IDebugConsole>();
    Harmony? harmony;
    public void Init()
    {
        harmony = new Harmony("drbruhman.quickinventory");

        harmony.PatchAll(Assembly.GetExecutingAssembly());

        DebugConsole.NewMessage("Plugin loaded", Color.Lime);

        InitProjectSpecific();
    }

    public partial void InitProjectSpecific();

    public void Dispose()
    {
        harmony?.UnpatchSelf();
        harmony = null;
    }

    public void OnContentLoaded()
    {
        
    }
}
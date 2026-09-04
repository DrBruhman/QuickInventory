using Barotrauma;
using Barotrauma.Networking;
using Barotrauma.Plugins;

using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;


namespace QuickInventory;

public partial class Plugin
{
    Harmony? harmony;
    ISettingsService settingsService;
    ISetting lootKeybind;
    public partial void InitProjectSpecific()
    {
        harmony = new Harmony("drbruhman.quickinventory");

        harmony.PatchAll();

        settingsService.RegisterSetting(lootKeybind);
    }
    public void Dispose()
    {
        harmony = null;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CharacterInventory), "Update")]
    public static void PatchInventoryUpdate(CharacterInventory __instance)
    {
        if (Character.Controlled?.Inventory == null ||
            CharacterHealth.OpenHealthWindow != null ||
            GameMain.Client.ChatBox.InputBox.Selected) { return; }

        if (PlayerInput.KeyDown(InputType.ShowInteractionLabels))
        {

        }
    }
}
using Barotrauma;
using Barotrauma.Networking;
using Barotrauma.Plugins;

using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Reflection;


namespace QuickInventory;

[HarmonyPatch]
public partial class Plugin
{

    public partial void InitProjectSpecific()
    {
   
    }


    private static void doQuickLoot()
    {
        if (Inventory.SelectedSlot?.Item != null)
        {
            DebugConsole.NewMessage(Inventory.SelectedSlot.Item.Name);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CharacterInventory), "Update")]
    public static void PatchInventoryUpdate(float deltaTime, CharacterInventory __instance)
    {
        if (Character.Controlled?.Inventory == null ||
            CharacterHealth.OpenHealthWindow != null ||
            __instance != Character.Controlled?.Inventory) { return; }

        if (PlayerInput.KeyDown(InputType.ShowInteractionLabels) &&
            PlayerInput.KeyDown(InputType.Select))
        {
            doQuickLoot();
        }
    }
}
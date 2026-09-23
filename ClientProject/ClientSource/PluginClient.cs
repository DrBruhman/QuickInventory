using Barotrauma;
using Barotrauma.Extensions;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using System.Data.SqlTypes;
using System.Reflection;
using static Barotrauma.Inventory;

namespace QuickInventory;

[HarmonyPatch]
public partial class Plugin
{
    //private ref SlotReference? selectedSlot;
    private static MethodInfo? tryPutItem;
    private static readonly AccessTools.FieldRef<object, SlotReference> selectedSlot =
        AccessTools.FieldRefAccess<SlotReference>( typeof(Inventory), "selectedSlot");

    public static void ClearSelectedSlot() {
        Inventory.DraggingItems.Clear();
        selectedSlot(null) = null; }

    public partial void InitProjectSpecific()
    {
        
    }

    private static void DoQuickTransfer()
    {
        if (Inventory.SelectedSlot?.Item != null)
        {
            if (tryFindContextInventory(out Inventory contextInventory))
            {
                if (Inventory.SelectedSlot.ParentInventory == contextInventory || // place in context inventory
                    Inventory.SelectedSlot.ParentInventory.Owner == contextInventory.Owner) // in case item has multiple container components, like Deconstructor
                {
                    List<Inventory> inventories = [];

                    var lefthandItem = Character.Controlled.Inventory.GetItemAt(5);
                    var righthandItem = Character.Controlled.Inventory.GetItemAt(6);
                    inventories.AddIfNotNull(lefthandItem?.OwnInventory);
                    inventories.AddIfNotNull(righthandItem == lefthandItem ? null : righthandItem?.OwnInventory);

                    inventories.Add(Character.Controlled.Inventory); // all character slots

                    inventories.AddIfNotNull(Character.Controlled.Inventory.GetItemAt(7)?.OwnInventory); // bag

                    for (int i = 8; i < 17; i++) // 1-0 inventory slots
                    { inventories.AddIfNotNull(Character.Controlled.Inventory.GetItemAt(i)?.OwnInventory); }

                    var slotDone = false;
                    foreach (Inventory inventory in inventories)
                    {
                        //DebugConsole.NewMessage(inventory.GetType().ToString());
                        if (slotDone) { ClearSelectedSlot(); return; }
                        if (inventory.CanBePut(Inventory.SelectedSlot.Item))
                        {
                            IEnumerable<Item> slotItems = Inventory.SelectedSlot.ParentInventory.GetItemsAt(Inventory.SelectedSlot.SlotIndex);
                            foreach (Item item in slotItems)
                            {
                                inventory.TryPutItem(item, Character.Controlled, CharacterInventory.AnySlot);
                                
                                if (!slotDone) { break; }
                            }
                        }
                    }
                    ClearSelectedSlot();
                }
                else // place in player character inventory slots
                {
                    foreach (Item item in Inventory.SelectedSlot.ParentInventory.GetItemsAt(Inventory.SelectedSlot.SlotIndex).ToArray())
                    {
                        contextInventory.TryPutItem(item, Character.Controlled, CharacterInventory.AnySlot);
                    }
                    ClearSelectedSlot();
                }
            }
        }

        static bool tryFindContextInventory(out Inventory contextInventory) // depending on context finds with which inventory to interact
        {
            if (Character.Controlled.SelectedItem?.OwnInventory != null) // item that is selected by player character, like Supply Cabinet
            { contextInventory = Character.Controlled.SelectedItem.OwnInventory; return true; }

            if (Character.Controlled.SelectedCharacter?.Inventory != null) // grabbed character by player character
            { contextInventory = Character.Controlled.SelectedCharacter.Inventory; return true; }

            if (Inventory.SelectedSlot.ParentInventory.Owner is Item item &&
                item.ParentInventory == Character.Controlled.Inventory) // container items that is on player character
            { contextInventory = Character.Controlled.Inventory; return true; }

            if (Inventory.SelectedSlot.ParentInventory == Character.Controlled.Inventory)
            {
                for (int i = 5; i <= 7; i++) // hands,toolbag slots
                {
                    Item possibleStorageItem = Character.Controlled.Inventory.GetItemAt(i);
                    if (possibleStorageItem?.OwnInventory != null && possibleStorageItem.OwnInventory.CanBePut(Inventory.SelectedSlot.Item))
                    { contextInventory = possibleStorageItem.OwnInventory; return true;}
                }
            }
            contextInventory = Character.Controlled.Inventory;
            return false;
        }

    }

    private static bool preventUpdateSlots = true;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CharacterInventory), "Update")]
    public static bool PatchInventoryUpdate(float deltaTime, CharacterInventory __instance)
    {
        preventUpdateSlots = true;
        if (Character.Controlled?.Inventory == null ||
            CharacterHealth.OpenHealthWindow != null ||
            __instance != Character.Controlled?.Inventory) { return true; }

        if (PlayerInput.KeyDown(InputType.ShowInteractionLabels) &&
            PlayerInput.KeyDown(InputType.Select))
        {
            DoQuickTransfer();
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Inventory), "UpdateSlot")]
    public static bool PatchInventoryUpdateSlot(VisualSlot slot, int slotIndex, Item item, bool isSubSlot)
    {
        return preventUpdateSlots;
    }
}
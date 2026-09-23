using Barotrauma;
using Barotrauma.Extensions;
using HarmonyLib;
using System.Reflection;
using static Barotrauma.Inventory;
using Item = Barotrauma.Item; // vs automaticaly put steamworks in here

namespace QuickInventory;

[HarmonyPatch]
public partial class Plugin
{
    private static MethodInfo? tryPutItem;
    private static bool updateInventory = true;
    private static readonly AccessTools.FieldRef<object, SlotReference> selectedSlot =
        AccessTools.FieldRefAccess<SlotReference>(typeof(Inventory), "selectedSlot");

    public static void ClearSelectedSlot()
    {
        selectedSlot(null) = null;
    }

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
                        if (slotDone) { ClearSelectedSlot(); return; }
                        if (inventory.CanBePut(Inventory.SelectedSlot.Item))
                        {
                            foreach (Item item in Inventory.SelectedSlot.ParentInventory.GetItemsAt(Inventory.SelectedSlot.SlotIndex).ToArray())
                            {
                                if (Character.Controlled.CanInteractWith(item))
                                {
                                    slotDone = inventory.TryPutItem(item, Character.Controlled, CharacterInventory.AnySlot);

                                    if (!slotDone) { break; }
                                }
                            }
                        }
                    }
                    if (!slotDone && Inventory.SelectedSlot.Item.ParentInventory != Character.Controlled.Inventory &&
                        Character.Controlled.CanInteractWith(Inventory.SelectedSlot.Item))
                    {
                        foreach (int slot in new int[] { 5, 6, 7, 4, 3, 2 })
                        {
                            if (Character.Controlled.Inventory.TryPutItem(Inventory.SelectedSlot.Item, slot, false, false, Character.Controlled)) { break; }
                        }
                    }
                    ClearSelectedSlot();
                }
                else // place in player character inventory slots
                {
                    foreach (Item item in Inventory.SelectedSlot.ParentInventory.GetItemsAt(Inventory.SelectedSlot.SlotIndex).ToArray())
                    {
                        if (Character.Controlled.CanInteractWith(item))
                        { contextInventory.TryPutItem(item, Character.Controlled, CharacterInventory.AnySlot); }
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
                    { contextInventory = possibleStorageItem.OwnInventory; return true; }
                }
            }
            contextInventory = Character.Controlled.Inventory;
            return false;
        }

    }


    [HarmonyPrefix]
    [HarmonyPatch(typeof(CharacterInventory), "Update")]
    public static bool PatchInventoryUpdate(float deltaTime, CharacterInventory __instance)
    {
        if (Character.Controlled?.Inventory == null ||
            __instance != Character.Controlled?.Inventory ||
            GUI.KeyboardDispatcher.Subscriber != null) { return true; }

        updateInventory = true;
        if (PlayerInput.KeyDown(InputType.ShowInteractionLabels) &&
            PlayerInput.KeyDown(InputType.Select))
        {
            DoQuickTransfer();
        }
        for (int i = 0; i < 10; i++)
        {
            if (PlayerInput.InventoryKeyHit(i))
            {
                DoQuickSwap(i + 8);
            }
        }
        if (PlayerInput.KeyDown(InputType.DropItem))
        {
            doQuickDrop();
        }
        return updateInventory;
    }

    private static void doQuickDrop()
    {
        if (Inventory.SelectedSlot?.Item != null)
        {
            var slotItems = Inventory.SelectedSlot.ParentInventory.GetItemsAt(Inventory.SelectedSlot.SlotIndex).ToArray();
            var amoutToTake = slotItems.Length;
            if (PlayerInput.KeyDown(InputType.TakeHalfFromInventorySlot)) { amoutToTake = Math.Max(amoutToTake / 2, 1); }
            else if (PlayerInput.KeyDown(InputType.TakeOneFromInventorySlot)) { amoutToTake = 1; }
            List<Item> droppedStack = new List<Item>();
            foreach (Item item in slotItems.Take(amoutToTake))
            {
                if (Character.Controlled.CanInteractWith(item))
                {
                    droppedStack.Add(item);
                    item.Drop(Character.Controlled);
                }
            }
            ClearSelectedSlot();
            slotItems[0].CreateDroppedStack(droppedStack, false);
        }
    }

    private static void DoQuickSwap(int slot, bool takeOne = false)
    {
        if (Inventory.SelectedSlot == null)
        {
            if (PlayerInput.KeyDown(InputType.ShowInteractionLabels) && Character.Controlled.FocusedItem != null && Character.Controlled.FocusedItem.PhysicsBodyActive)
            {
                Character.Controlled.Inventory.TryPutItem(Character.Controlled.FocusedItem, slot, true, true, Character.Controlled);
                updateInventory = false;
            }
        }
        else
        {
            var charSlotItems = Character.Controlled.Inventory.GetItemsAt(slot).ToArray();
            if (charSlotItems.Length > 0)
            {
                var amoutToTake = charSlotItems.Length;
                if (PlayerInput.KeyDown(InputType.TakeHalfFromInventorySlot)) { amoutToTake = Math.Max(amoutToTake / 2, 1); }
                else if (PlayerInput.KeyDown(InputType.TakeOneFromInventorySlot) || takeOne) { amoutToTake = 1; }
                foreach (Item item in charSlotItems.Take(amoutToTake))
                {
                    if (Character.Controlled.CanInteractWith(item))
                    { Inventory.SelectedSlot.ParentInventory.TryPutItem(item, Inventory.SelectedSlot.SlotIndex, true, true, Character.Controlled); }
                }
                ClearSelectedSlot();
                updateInventory = false;
            }
            else if (Inventory.SelectedSlot.Item != null)
            {
                var slotItems = Inventory.SelectedSlot.ParentInventory.GetItemsAt(Inventory.SelectedSlot.SlotIndex).ToArray();
                var amoutToTake = slotItems.Length;
                if (PlayerInput.KeyDown(InputType.TakeHalfFromInventorySlot)) { amoutToTake = Math.Max(amoutToTake / 2, 1); }
                else if (PlayerInput.KeyDown(InputType.TakeOneFromInventorySlot) || takeOne) { amoutToTake = 1; }
                foreach (Item item in slotItems.Take(amoutToTake))
                {
                    if (Character.Controlled.CanInteractWith(item))
                    { Character.Controlled.Inventory.TryPutItem(item, slot, true, true, Character.Controlled); }
                }
                ClearSelectedSlot();
                updateInventory = false;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Inventory), "UpdateSlot")]
    public static bool PatchInventoryUpdateSlot(VisualSlot slot, int slotIndex, Item item, bool isSubSlot)
    { return !(PlayerInput.KeyDown(InputType.ShowInteractionLabels) && PlayerInput.PrimaryMouseButtonDown()); }
}
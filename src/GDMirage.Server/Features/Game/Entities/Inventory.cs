using GDMirage.Server.Features.Accounts.Entities;
using GDMirage.Server.Features.Game.Messages;

namespace GDMirage.Server.Features.Game.Entities;

public sealed class Inventory
{
    private readonly List<CharacterInventorySlot?> _slots;
    private readonly ItemInfoManager _itemInfoManager;

    public int Size => _slots.Count;

    public Inventory(Character character, ItemInfoManager itemInfoManager)
    {
        _itemInfoManager = itemInfoManager;
        _slots = character.Inventory;

        while (_slots.Count < character.InventorySize)
        {
            _slots.Add(null);
        }
    }

    public List<int> TryAdd(ItemInfo itemInfo, int quantity = 1)
    {
        var changed = new List<int>();

        if (itemInfo.Stackable)
        {
            for (var i = 0; i < _slots.Count && quantity > 0; i++)
            {
                var slot = _slots[i];
                if (slot is null || slot.ItemId != itemInfo.Id || slot.Quantity >= itemInfo.MaxStackSize)
                {
                    continue;
                }

                var quantityToAdd = Math.Min(quantity, itemInfo.MaxStackSize - slot.Quantity);

                slot.Quantity += quantityToAdd;

                quantity -= quantityToAdd;

                changed.Add(i);
            }
        }

        for (var i = 0; i < _slots.Count && quantity > 0; i++)
        {
            if (_slots[i] is not null)
            {
                continue;
            }

            var stackQuantity = itemInfo.Stackable ? Math.Min(quantity, itemInfo.MaxStackSize) : 1;

            _slots[i] = new CharacterInventorySlot
            {
                ItemId = itemInfo.Id,
                Quantity = stackQuantity
            };

            quantity -= stackQuantity;

            changed.Add(i);
        }

        return changed;
    }

    public List<int> Move(int from, int to, int quantity)
    {
        if (from < 0 || from >= _slots.Count || to < 0 || to >= _slots.Count || from == to)
        {
            return [];
        }

        var slotFrom = _slots[from];
        if (slotFrom is null)
        {
            return [];
        }

        quantity = quantity <= 0 || quantity >= slotFrom.Quantity ? slotFrom.Quantity : quantity;

        var fromItemInfo = _itemInfoManager.GetItemInfo(slotFrom.ItemId);

        var slotTo = _slots[to];
        if (slotTo is null)
        {
            if (quantity == slotFrom.Quantity)
            {
                _slots[to] = slotFrom;
                _slots[from] = null;
            }
            else
            {
                _slots[to] = new CharacterInventorySlot
                {
                    ItemId = slotFrom.ItemId,
                    Quantity = quantity
                };

                slotFrom.Quantity -= quantity;
            }

            return [from, to];
        }

        if (slotTo.ItemId == slotFrom.ItemId && fromItemInfo.Stackable)
        {
            var canAdd = Math.Min(quantity, fromItemInfo.MaxStackSize - slotTo.Quantity);
            if (canAdd <= 0)
            {
                return [];
            }

            slotTo.Quantity += canAdd;
            slotFrom.Quantity -= canAdd;

            if (slotFrom.Quantity <= 0)
            {
                _slots[from] = null;
            }

            return [from, to];
        }

        if (quantity != slotFrom.Quantity)
        {
            return [];
        }

        (_slots[from], _slots[to]) = (_slots[to], _slots[from]);

        return [from, to];
    }

    public InventorySlotDto? GetSlotDto(int index)
    {
        if (index < 0 || index >= _slots.Count)
        {
            return null;
        }

        var slot = _slots[index];
        if (slot is null)
        {
            return null;
        }

        var itemInfo = _itemInfoManager.GetItemInfo(slot.ItemId);

        return MakeDto(slot, itemInfo);
    }

    public IEnumerable<(int Index, InventorySlotDto Dto)> GetAllSlotDtos()
    {
        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot is null)
            {
                continue;
            }

            var itemInfo = _itemInfoManager.GetItemInfo(slot.ItemId);

            yield return (i, MakeDto(slot, itemInfo));
        }
    }

    private static InventorySlotDto MakeDto(CharacterInventorySlot slot, ItemInfo itemInfo)
    {
        return new InventorySlotDto
        {
            ItemId = itemInfo.Id,
            Name = itemInfo.Name,
            Texture = itemInfo.Texture,
            SpriteIndex = itemInfo.SpriteIndex,
            Quantity = slot.Quantity
        };
    }
}

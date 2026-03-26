namespace GDMirage.Server.Features.Accounts.Entities;

public sealed class CharacterInventorySlot
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}

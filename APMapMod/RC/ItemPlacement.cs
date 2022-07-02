using RandomizerCore;

namespace APMapMod.RC;


public readonly record struct ItemPlacement(RandoItem Item, RandoLocation Location)
{
    /// <summary>
    /// The index of the item placement in the RandoModContext item placements. Initialized to -1 if the placement is not part of the ctx.
    /// </summary>
    public int Index { get; init; } = -1;

    public void Deconstruct(out RandoItem item, out RandoLocation location)
    {
        item = Item;
        location = Location;
    }

    public static implicit operator GeneralizedPlacement(ItemPlacement p) => new(p.Item, p.Location);
    public static explicit operator ItemPlacement(GeneralizedPlacement p) => new((RandoItem)p.Item, (RandoLocation)p.Location);
}
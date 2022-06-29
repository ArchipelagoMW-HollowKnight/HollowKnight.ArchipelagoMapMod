using Archipelago.HollowKnight.IC;
using ItemChanger;

namespace APMapMod.Data
{
    public class ItemDef
    {
        public ItemDef(AbstractItem item)
        {
            if (item.GetTag(out ArchipelagoItemTag tag))
            {
                id = 0;
            }

            itemName = item.name;
            poolGroup = this.GetPlacementGroup();
            persistent = item.IsPersistent();
            this.item = item;
        }

        public long id;
        public string itemName = "Unknown";
        public string poolGroup = "Unknown";
        public bool persistent = false;
        public AbstractItem item;
    }
}

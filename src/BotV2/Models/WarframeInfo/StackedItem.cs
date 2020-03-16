namespace BotV2.Models.WarframeInfo
{
    public readonly struct StackedItem
    {
        public string Type { get; }
        public int Count { get; }

        public StackedItem(string type, int count)
        {
            this.Type = type;
            this.Count = count;
        }
    }
}

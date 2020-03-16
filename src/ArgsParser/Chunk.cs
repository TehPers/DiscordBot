namespace ArgsParser
{
    public readonly struct Chunk
    {
        public ChunkTypes ChunkType { get; }

        public string Value { get; }

        public Chunk(ChunkTypes chunkType, string value)
        {
            this.ChunkType = chunkType;
            this.Value = value;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace ArgsParser
{
    public class ArgumentTokenizer
    {
        private readonly int _bufferSize;

        public ArgumentTokenizer(int bufferSize = 1024)
        {
            this._bufferSize = bufferSize;
        }

        public async IAsyncEnumerable<Token> ParseAsync(TextReader reader, [EnumeratorCancellation] CancellationToken token = default)
        {
            var curToken = new StringBuilder();
            await using var enumerator = this.StreamTokensAsync(reader, token).GetAsyncEnumerator(token);
            while (await enumerator.MoveNextAsync())
            {

            }
        }

        private delegate object TokenParser()

        private async IAsyncEnumerable<char> StreamTokensAsync(TextReader reader, [EnumeratorCancellation] CancellationToken token = default)
        {
            var buffer = new char[this._bufferSize];
            int charsRead;

            do
            {
                charsRead = await reader.ReadAsync(buffer, 0, this._bufferSize);
                for (var i = 0; i < this._bufferSize; i++)
                {
                    yield return buffer[i];
                }
            } while (charsRead > 0);
        }

        public readonly struct Token
        {

        }
    }

    public class ChunkParser
    {
        private readonly int _bufferSize;

        public ChunkParser(int bufferSize = 1024)
        {
            this._bufferSize = bufferSize;
        }

        public IEnumerable<Chunk> Parse(TextReader reader)
        {
            var result = this.ParseAsync(reader);
            var enumerator = result.GetAsyncEnumerator();

            var buffer = new char[this._bufferSize];
            var charsRead = 0;

            // ReSharper disable once TooWideLocalVariableScope
            var bufferIndex = 0;
            char? GetNextChar()
            {
                if (bufferIndex < charsRead)
                {
                    return buffer[bufferIndex++];
                }

                charsRead = reader.Read(buffer, 0, this._bufferSize);
                if (charsRead == 0)
                {
                    return default;
                }

                bufferIndex = 0;
                return GetNextChar();
            }


            var quoted = false;
            var escaped = false;
            var chunk = new StringBuilder();
            var shortOptions = false;
            var longOption = false;
            while (GetNextChar() is { } c)
            {
                switch (c)
                {
                    case { } when escaped && shortOptions:
                        yield return new Chunk(ChunkTypes.ShortOption, c.ToString());
                        break;
                    case { } when escaped:
                        chunk.Append(c);
                        escaped = false;
                        break;
                    case ' ' when shortOptions:
                        shortOptions = false;
                        break;
                    case ' ' when longOption:
                        yield return new Chunk(ChunkTypes.LongOption, chunk.ToString());
                        chunk.Clear();
                        longOption = false;
                        break;
                    case ' ' when !quoted:
                    case '"' when quoted:
                        yield return new Chunk(ChunkTypes.Normal, chunk.ToString());
                        quoted = false;
                        chunk.Clear();
                        break;
                    case { } when shortOptions:
                        yield return new Chunk(ChunkTypes.ShortOption, c.ToString());
                        break;
                    case '"' when chunk.Length == 0 && !longOption:
                        quoted = true;
                        break;
                    case '"':

                        yield return new { Flags = false, LongOption = false, Chunk = chunk.ToString() };
                        chunk.Clear();
                        quoted = false;
                        break;
                    case ' ' when !quoted && !escaped:
                        yield return new { Flag = shortOptions, LongOption = longOption, Chunk = chunk.ToString() };
                        shortOptions = false;
                        longOption = false;
                        chunk.Clear();
                        break;
                    case '\\' when !escaped:
                        escaped = true;
                        break;
                    case '-' when chunk.Length == 0 && !shortOptions && !longOption:
                        shortOptions = true;
                        break;
                    case '-' when shortOptions && !longOption:
                        longOption = true;
                        shortOptions = false;
                        break;
                    case char _ when shortOptions:
                        yield return new { Flags = true, LongOption = false }
                        default:
                        chunk.Append(c);
                        break;
                }
            }


        }

        public async IAsyncEnumerable<object> ParseAsync(TextReader reader)
        {

        }

        private async IAsyncEnumerable<Token> TokenizeAsync(TextReader reader)
        {

        }

        private readonly struct Token
        {
            public string Value { get; }

            public bool Quoted { get; }
        }
    }

    public interface IValueParser
    {

    }
}

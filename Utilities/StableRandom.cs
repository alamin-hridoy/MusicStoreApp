using System.Security.Cryptography;
using System.Text;

namespace MusicStoreApp.Utilities;

public sealed class StableRandom
{
    private ulong _state;

    public StableRandom(ulong seed)
    {
        _state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
    }

    public ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;
        var z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    public int Next(int minInclusive, int maxExclusive)
    {
        if (minInclusive >= maxExclusive)
        {
            return minInclusive;
        }

        var range = (ulong)(maxExclusive - minInclusive);
        return minInclusive + (int)(NextUInt64() % range);
    }

    public double NextDouble()
    {
        return (NextUInt64() >> 11) * (1.0 / (1UL << 53));
    }

    public bool NextBool(double trueChance = 0.5)
    {
        return NextDouble() < trueChance;
    }

    public T Pick<T>(IReadOnlyList<T> items)
    {
        return items[Next(0, items.Count)];
    }

    public static ulong Compose(params ulong[] values)
    {
        unchecked
        {
            var hash = 14695981039346656037UL;
            foreach (var value in values)
            {
                hash ^= value;
                hash *= 1099511628211UL;
            }

            return hash;
        }
    }

    public static ulong ComposeString(params string[] values)
    {
        var joined = string.Join('|', values);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(joined));
        return BitConverter.ToUInt64(bytes, 0);
    }
}

using System;

namespace TOHFE;

public class HashRandomWrapper : IRandom
{
    public HashRandomWrapper()
    {
        HashRandom.src = new XXHash((int)DateTime.UtcNow.Ticks);
    }

    public int Next(int minValue, int maxValue) => HashRandom.Next(minValue, maxValue);
    public int Next(int maxValue) => HashRandom.Next(maxValue);
    public uint Next() => HashRandom.Next();
    public int FastNext(int maxValue) => HashRandom.FastNext(maxValue);
}

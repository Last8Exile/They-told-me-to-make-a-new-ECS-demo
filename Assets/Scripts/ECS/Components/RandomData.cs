using System;

using Unity.Entities;

using URanom = UnityEngine.Random;
using MRandom = Unity.Mathematics.Random;

[Serializable]
public struct RandomData : IComponentData
{
    public MRandom Random;

    /// <summary>
    /// Creates RandomData with Random inited by UnityEngine.Random
    /// </summary>
    public static RandomData Create()
    {
        return Create(GetRandomUint());
    }

    /// <summary>
    /// Creates RandomData with Random inited by index
    /// </summary>
    /// <param name="index">can't be uint.MaxVBalue</param>
    public static RandomData Create(uint index)
    {
        return new RandomData
        {
            Random = MRandom.CreateFromIndex(index)
        };
    }

    /// <summary>
    /// Returns uint in range [uint.MinValue..uint.MaxValue) using unity UnityEngine.Random
    /// </summary>
    public static uint GetRandomUint()
    {
        int index = URanom.Range(int.MinValue, int.MaxValue);
        if (index == -1)
            index = int.MaxValue;
        uint uindex = unchecked((uint)index);
        return uindex;
    }
}

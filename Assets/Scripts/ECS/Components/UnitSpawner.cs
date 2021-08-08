using System;

using Unity.Entities;
using Unity.Mathematics;

using UnityEngine;

[Serializable]
[GenerateAuthoringComponent]
public struct UnitSpawner : IComponentData
{
    public Entity Prefab;

    public float2 SpawnArea;
    public float2 VelocityBase;
    public float2 VelocitySpread;

    public ushort SpawnCount;

    [Min(1)]
    public ushort BurstSize;
    public float BurstInterval;

    [Header("State")]
    public ushort State_SpawnedCount;
    public float State_Countdown;
}

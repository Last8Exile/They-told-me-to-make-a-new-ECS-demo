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

    public int SpawnCount;

    public int BurstSize;
    public float BurstInterval;

    [Header("State")]
    public int Spawned;
    public float Delay;
}

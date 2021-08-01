using System;

using Unity.Entities;
using Unity.Mathematics;

using UnityEngine;

[Serializable]
[GenerateAuthoringComponent]
public struct Weapon : IComponentData
{
    public float FireInterval;
    public float2 MuzzleOffet;
    public Entity ProjectilePrefab;

    [Header("State")]
    public float FireDelay;
}

using System;

using Unity.Entities;

using UnityEngine;

[Serializable]
[GenerateAuthoringComponent]
public struct WeaponBurst : IComponentData
{
    public bool CanInterrupt;
    public byte BurstProjectileCount;
    public float BurstInterval;

    [Header("State")]
    public byte State_BurstFiredProjectiles;
}
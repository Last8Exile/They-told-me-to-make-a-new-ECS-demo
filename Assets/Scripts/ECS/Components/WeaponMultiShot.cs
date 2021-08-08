using System;

using Unity.Entities;

using UnityEngine;

[Serializable]
[GenerateAuthoringComponent]
public struct WeaponMultiShot : IComponentData
{
    [Min(1)]
    public byte MuzzleCount;
    public float MuzzlePosSeparation;
    public float MuzzleAngleSeparation;

    public bool SequentialFire;

    [Header("State")]
    public byte State_MuzzleIndex;
}
using System;

using Unity.Entities;
using Unity.Mathematics;

using UnityEngine;

[Serializable]
[GenerateAuthoringComponent]
public struct Ship : IComponentData
{
    public ShipState ShipState;
    public float CruiseSpeedSQ;
    public float ReachDistanceSQ;

    [Header("State")]
    public float Coundown;
    public float2 TargetPosition;
    public Entity TargetEntity;
}

public enum ShipState
{
    Idle = 0,
    Stop = 1,
    MoveToPosition = 10,
}
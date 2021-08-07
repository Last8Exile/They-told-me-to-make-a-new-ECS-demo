using System;

using Unity.Entities;
using Unity.Mathematics;

using UnityEngine;

[Serializable]
[GenerateAuthoringComponent]
public struct Ship : IComponentData
{
    public ShipState ShipState;
    public float CruiseSpeed;

    [Header("State")]
    public float State_Coundown;
    public float2 State_TargetPosition;
    public Entity State_TargetEntity;
}

public enum ShipState
{
    Idle = 0,
    Stop = 1,
    MoveToPosition = 10,
}
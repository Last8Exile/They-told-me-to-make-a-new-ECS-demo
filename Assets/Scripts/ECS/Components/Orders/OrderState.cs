using System;

using Unity.Entities;

[Serializable]
public struct OrderState : IComponentData
{
    public bool Completed;
    public Order Order;
    public float Timeout;
}

public enum Order
{
    None,
    Idle,
    Stop,
    FlyTo,
    FireAt,
}
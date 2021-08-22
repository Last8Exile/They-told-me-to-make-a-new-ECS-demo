using System;

using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct FlyToOrder : IComponentData
{
    public float2 Position;
}

[Serializable]
public struct FlyToTarget : IComponentData
{
    public Entity Entity;
}
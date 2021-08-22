using System;

using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct FireAtOrder : IComponentData
{
    public float2 Position;
    public float2 Velocity;
}

[Serializable]
public struct FireAtTarget : IComponentData
{
    public Entity Entity;
}

[Serializable]
public struct FindTarget : IComponentData
{

}
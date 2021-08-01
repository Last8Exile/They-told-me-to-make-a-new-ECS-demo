using System;

using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[GenerateAuthoringComponent]
public struct Velocity : IComponentData
{
    public float2 LinearValue;
    public float AngularValue;
}

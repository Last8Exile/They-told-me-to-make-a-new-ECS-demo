using System;

using Unity.Entities;

[Serializable]
[GenerateAuthoringComponent]
public struct Lifetime : IComponentData
{
    public float Seconds;
}
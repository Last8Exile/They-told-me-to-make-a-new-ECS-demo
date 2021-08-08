using System;

using Unity.Entities;

[Serializable]
[GenerateAuthoringComponent]
public struct Team : IComponentData
{
    public byte Id;
}

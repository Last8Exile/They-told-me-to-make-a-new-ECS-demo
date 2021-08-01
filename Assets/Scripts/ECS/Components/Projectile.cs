using System;

using Unity.Entities;

[Serializable]
[GenerateAuthoringComponent]
public struct Projectile : IComponentData
{
    public float Damage;
}

using System;
using Unity.Entities;

[Serializable]
[GenerateAuthoringComponent]
public struct DamageProxy : IComponentData
{
    public Entity EntityWithPhysicsCollider;
}
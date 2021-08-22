using System;
using Unity.Entities;

[Serializable]
[GenerateAuthoringComponent]
public struct CollisionAvoidanceProxy : IComponentData
{
    public Entity EntityWithPhysicsCollider;
}
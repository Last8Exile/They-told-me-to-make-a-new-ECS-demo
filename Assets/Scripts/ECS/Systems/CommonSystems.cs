using Unity.Entities;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public abstract class FixedSystem : SystemBase
{
}

public abstract class FixedEcbSystem : FixedSystem
{
    protected EndFixedStepSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }
}

[UpdateAfter(typeof(EndFramePhysicsSystem))]
public abstract class PhysicsSystem : FixedSystem
{
    protected BuildPhysicsWorld _buildPhysicsWorld;

    protected override void OnCreate()
    {
        _buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
    }
}

public enum CollisionLayer : uint
{
    Default = 1 << 0,
    NoCollisions = 1 << 1,
    //Reserved = 1 << 2
    //Reserved = 1 << 3
    Ship = 1 << 4,
    Projectile = 1 << 5,
}
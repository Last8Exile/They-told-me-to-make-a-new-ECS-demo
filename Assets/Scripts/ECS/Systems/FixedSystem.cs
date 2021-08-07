using Unity.Entities;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public abstract class FixedSystem : SystemBase
{
}

public abstract class FixedEcbSystem : FixedSystem
{
    protected EndFixedStepSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetExistingSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }
}

using Unity.Collections;
using Unity.Entities;

[UpdateBefore(typeof(ShipSystem))]
public class FindTargetSystem : FixedEcbSystem
{
    private EntityQuery _findTargetQuerry;
    private EntityQuery _targetsQuery;

    protected override void OnCreate()
    {
        base.OnCreate();

        _findTargetQuerry = GetEntityQuery(
            ComponentType.ReadWrite<OrderState>(),
            ComponentType.ReadWrite<FindTarget>(),
            ComponentType.ReadWrite<FireAtTarget>(),
            ComponentType.ReadWrite<RandomData>(),
            ComponentType.ReadOnly<Team>());

        RequireForUpdate(_findTargetQuerry);

        _targetsQuery = GetEntityQuery(
            ComponentType.ReadOnly<Ship>(), 
            ComponentType.ReadOnly<Team>());
    }

    protected override void OnUpdate()
    {
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        var targetEntities = _targetsQuery.ToEntityArray(Allocator.TempJob);
        var teams = GetComponentDataFromEntity<Team>(true);

        Entities
            .WithName("FindTarget")
            .WithAll<FindTarget>()
            .WithAll<Team>()
            .WithReadOnly(teams)
            .WithReadOnly(targetEntities)
            .WithDisposeOnCompletion(targetEntities)
            .ForEach(
            (Entity entity, int entityInQueryIndex,
            ref OrderState orederState, ref FireAtTarget fireAtTarget, ref RandomData randomData) =>
            {
                var teamId = teams[entity].Id;
                var targets = new NativeList<Entity>(Allocator.Temp);
                for (int i = 0; i < targetEntities.Length; i++)
                {
                    var target = targetEntities[i];
                    if (teams[target].Id != teamId)
                        targets.Add(target);
                }
                if (targets.Length > 0)
                    fireAtTarget.Entity = targets[randomData.Random.NextInt(targets.Length)];
                else
                    orederState.Completed = true;
                ecb.RemoveComponent<FindTarget>(entityInQueryIndex, entity);
                targets.Dispose();

            }).ScheduleParallel();


        _ecbSystem.AddJobHandleForProducer(Dependency);
    }
}

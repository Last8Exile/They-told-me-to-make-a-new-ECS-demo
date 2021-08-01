using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class UnitSpawnerSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem ecbSystem;

    protected override void OnCreate()
    {
        ecbSystem = World.GetExistingSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        Entities.ForEach(
            (Entity entity, int entityInQueryIndex, 
            ref UnitSpawner spawner, ref RandomData randomData, 
            in Translation translation, in Rotation rotation, in Team team) =>
            {
                spawner.Delay -= dt;

                if (spawner.Delay >= 0)
                    return;

                var spawnCount = math.min(spawner.BurstSize, spawner.SpawnCount - spawner.Spawned);
                var spawnTeam = new Team { Id = team.Id };
                for (int i = 0; i < spawnCount; i++)
                {
                    var spawnedEntity = ecb.Instantiate(entityInQueryIndex, spawner.Prefab);

                    ecb.AddComponent(entityInQueryIndex, spawnedEntity, spawnTeam);

                    var randomPos = randomData.Random.NextFloat2(-spawner.SpawnArea, spawner.SpawnArea);
                    var spawnPos = translation.Value.xy + math.rotate(rotation.Value, new float3(randomPos, 0)).xy;
                    ecb.SetComponent(entityInQueryIndex, spawnedEntity, new Translation { Value = new float3(spawnPos, 0) });

                    ecb.SetComponent(entityInQueryIndex, spawnedEntity, rotation);

                    var randomVelocity = randomData.Random.NextFloat2(spawner.VelocityBase - spawner.VelocitySpread, spawner.VelocityBase + spawner.VelocitySpread);
                    var spawnVelocity = math.rotate(rotation.Value, new float3(randomVelocity, 0)).xy;
                    ecb.AddComponent(entityInQueryIndex, spawnedEntity, new Velocity { LinearValue = spawnVelocity });
                }

                spawner.Delay = spawner.BurstInterval;
                spawner.Spawned += spawnCount;

                if (spawner.Spawned >= spawner.SpawnCount)
                {
                    ecb.DestroyEntity(entityInQueryIndex, spawner.Prefab);
                    ecb.DestroyEntity(entityInQueryIndex, entity);
                }
            }).ScheduleParallel();
        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}

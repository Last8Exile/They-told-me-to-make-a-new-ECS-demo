using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class UnitSpawnerSystem : FixedEcbSystem
{
    public static readonly bool InitRandom = true;

    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        Entities.ForEach(
            (Entity entity, int entityInQueryIndex, 
            ref UnitSpawner spawner, ref RandomData randomData, 
            in Translation translation, in Rotation rotation, in Team team) =>
            {
                spawner.State_Countdown -= dt;

                if (spawner.State_Countdown >= 0)
                    return;

                var spawnCount = math.min(spawner.BurstSize, spawner.SpawnCount - spawner.State_SpawnedCount);
                var spawnTeam = new Team { Id = team.Id };
                for (int i = 0; i < spawnCount; i++)
                {
                    var spawnedEntity = ecb.Instantiate(entityInQueryIndex, spawner.Prefab);

                    ecb.SetComponent(entityInQueryIndex, spawnedEntity, spawnTeam);

                    var randomPos = randomData.Random.NextFloat2(-spawner.SpawnArea, spawner.SpawnArea);
                    var spawnPos = translation.Value.xy + randomPos.Rotate2D(rotation.Value);
                    ecb.SetComponent(entityInQueryIndex, spawnedEntity, new Translation { Value = new float3(spawnPos, 0) });

                    ecb.SetComponent(entityInQueryIndex, spawnedEntity, rotation);

                    var randomVelocity = randomData.Random.NextFloat2(spawner.VelocityBase - spawner.VelocitySpread, spawner.VelocityBase + spawner.VelocitySpread);
                    var spawnVelocity = randomVelocity.Rotate2D(rotation.Value);
                    ecb.SetComponent(entityInQueryIndex, spawnedEntity, new Velocity { LinearValue = spawnVelocity });

                    if (InitRandom)
                        ecb.SetComponent(entityInQueryIndex, spawnedEntity, new RandomData { Random = Random.CreateFromIndex(randomData.Random.NextUInt()) });
                }

                spawner.State_Countdown = spawner.BurstInterval;
                spawner.State_SpawnedCount += (ushort)spawnCount;

                if (spawner.State_SpawnedCount >= spawner.SpawnCount)
                    ecb.DestroyEntity(entityInQueryIndex, entity);
            }).ScheduleParallel();
        _ecbSystem.AddJobHandleForProducer(Dependency);
    }
}

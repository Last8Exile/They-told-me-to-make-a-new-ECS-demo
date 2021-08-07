using Unity.Entities;

public class LifetimeSystem : FixedEcbSystem
{
    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        Entities.ForEach(
            (Entity entity, int entityInQueryIndex,
            ref Lifetime lifetime) =>
            {
                lifetime.Seconds -= dt;
                if (lifetime.Seconds <= 0)
                    ecb.DestroyEntity(entityInQueryIndex, entity);
            }).ScheduleParallel();
        _ecbSystem.AddJobHandleForProducer(Dependency);
    }
}

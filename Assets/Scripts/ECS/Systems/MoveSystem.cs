using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class MoveSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        Entities.ForEach(
            (ref Translation translation, ref Rotation rotation,
            in Velocity velocity) =>
            {
                translation.Value.xy += velocity.LinearValue * dt;
                rotation.Value = rotation.Value.Rotate2D(velocity.AngularValue * dt);
            }).ScheduleParallel();
    }
}

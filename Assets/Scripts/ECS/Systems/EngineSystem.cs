using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class EngineSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        Entities.ForEach(
            (ref Velocity moveVelocity,
            in Engine engine, in Translation translation, in Rotation rotation) =>
            {
                moveVelocity.LinearValue += engine.LinerAcceleration * engine.LinearPower * rotation.Value.Direction2D();
                moveVelocity.AngularValue  = engine.RotationSpeed * engine.RotationPower;
            }).ScheduleParallel();
    }
}
using Unity.Entities;
using Unity.Transforms;

public class EngineSystem : FixedSystem
{
    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        Entities.ForEach(
            (ref Velocity moveVelocity,
            in Engine engine, in Translation translation, in Rotation rotation) =>
            {
                moveVelocity.LinearValue += dt * engine.LinerAcceleration * engine.State_LinearPower * rotation.Value.Direction2D();
                moveVelocity.AngularValue = engine.RotationSpeed * engine.State_RotationPower;
            }).ScheduleParallel();
    }
}
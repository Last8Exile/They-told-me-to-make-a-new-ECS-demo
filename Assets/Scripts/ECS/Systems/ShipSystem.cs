using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class ShipSystem : SystemBase
{
    public static readonly float2 FlyAreaExtents = new float2(75,50);
    public static readonly float StopSpeedSQ = 1;
    public static readonly float AccelerateAngle = math.PI * 0.25f;

    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;

        Entities.ForEach(
            (Entity entity, int entityInQueryIndex,
            ref Ship ship, ref RandomData randomData, ref Engine engine,
            in Translation translation, in Rotation rotation, in Velocity moveVelocity) =>
            {

                switch (ship.ShipState)
                {
                    case ShipState.Idle:
                    {
                        ship.Coundown -= dt;

                        engine.LinearPower = 0;
                        engine.RotationPower = 0;

                        if (ship.Coundown > 0)
                            return;

                        MoveToRandomPosition(ref ship, ref randomData, FlyAreaExtents);
                        break;
                    }
                    case ShipState.Stop:
                    {
                        var speedSQ = math.lengthsq(moveVelocity.LinearValue);
                        if (speedSQ < StopSpeedSQ)
                        {
                            Idle(ref ship, 1f);
                            return;
                        }
                        var direction = MathExtensions.Direction2D(rotation.Value);
                        var angleToStopDirection = MathExtensions.SignedAngle(direction, -moveVelocity.LinearValue);
                        engine.SetRotationPowerClamped(math.unlerp(angleToStopDirection, 0, engine.RotationSpeed));
                        engine.SetLineraPowerClamped(1 - math.unlerp(math.abs(angleToStopDirection), 0, AccelerateAngle));
                        break;
                    }
                    case ShipState.MoveToPosition:
                    {
                        break;
                    }
                }
            }).ScheduleParallel();
    }

    private static void MoveToRandomPosition(ref Ship ship, ref RandomData randomData, float2 flyAreaExtents)
    {
        ship.ShipState = ShipState.MoveToPosition;
        ship.TargetPosition = randomData.Random.NextFloat2(-flyAreaExtents, flyAreaExtents);
    }

    private static void Idle(ref Ship ship, float length)
    {
        ship.ShipState = ShipState.Idle;
        ship.Coundown = length;
    }
}
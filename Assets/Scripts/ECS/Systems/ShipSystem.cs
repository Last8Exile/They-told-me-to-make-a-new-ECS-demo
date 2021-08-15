using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class ShipSystem : FixedSystem
{
    public static readonly float2 FLY_AREA_EXTENTS = new float2(50f, 30f);
    public static readonly float STOP_SPEED_SQ = 1f;
    public static readonly float ENGAGE_ENGINE_ANGLE = math.PI * 0.25f;
    public static readonly float REACH_DISTANCE_SQ = 25;
    public static readonly float EQUAL_SPEED_SQ = 4;
    public static readonly float INV_PI = 1f / math.PI;

    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;

        Entities.ForEach(
            (Entity entity, int entityInQueryIndex,
            ref Ship ship, ref RandomData randomData, ref Engine engine,
            in Translation translation, in Rotation rotation, in Velocity velocity) =>
            {

                switch (ship.ShipState)
                {
                    case ShipState.Idle:
                    {
                        if (CooldownComplete(ref ship, dt))
                        {
                            MoveToRandomPosition(ref ship, ref randomData, float2.zero, FLY_AREA_EXTENTS, 5f);
                            return;
                        }
                        engine.State_LinearPower = 0;
                        engine.State_RotationPower = 0;
                        break;
                    }
                    case ShipState.Stop:
                    {
                        if (CooldownComplete(ref ship, dt))
                        {
                            Idle(ref ship, 1f);
                            return;
                        }
                        var speedSQ = math.lengthsq(velocity.LinearValue);
                        if (speedSQ < STOP_SPEED_SQ)
                        {
                            Idle(ref ship, 1f);
                            return;
                        }
                        Rotate(ref engine, in rotation, out var angleDiff, -velocity.LinearValue);
                        Thrust(ref engine, math.sqrt(speedSQ), GetAngleEngage(angleDiff));
                        break;
                    }
                    case ShipState.MoveToPosition:
                    {
                        if (CooldownComplete(ref ship, dt))
                        {
                            Stop(ref ship, 2f);
                            return;
                        }
                        var targetDirection = ship.State_TargetPosition - translation.Value.xy;
                        var distanceSQ = math.lengthsq(targetDirection);
                        if (distanceSQ < REACH_DISTANCE_SQ)
                        {
                            Stop(ref ship, 2f);
                            return;
                        }
                        var targetVelocity = math.normalize(targetDirection) * ship.CruiseSpeed;
                        var thrustVelocity = targetVelocity - velocity.LinearValue;
                        var thrustLengthSQ = math.lengthsq(thrustVelocity);
                        if (thrustLengthSQ < EQUAL_SPEED_SQ)
                        {
                            Rotate(ref engine, in rotation, targetDirection);
                            engine.State_LinearPower = 0;
                        }
                        else
                        {
                            Rotate(ref engine, in rotation, out var angleDiff, thrustVelocity);
                            Thrust(ref engine, math.sqrt(thrustLengthSQ), GetAngleEngage(angleDiff));
                        }
                        break;
                    }
                }
            }).ScheduleParallel();
    }

    private static void Rotate(ref Engine engine, in Rotation rotation, float2 targetDirection, float engage = 1f)
    {
        Rotate(ref engine, in rotation, out _, targetDirection, engage);
    }
    private static void Rotate(ref Engine engine, in Rotation rotation, out float angleDiff, float2 targetDirection, float engage = 1f)
    {
        var currentDirection = MathExtensions.Direction2D(rotation.Value);
        angleDiff = MathExtensions.SignedAngle(currentDirection, targetDirection);
        engine.SetRotationPowerClamped(engine.GetClampedRotationalEngage(angleDiff) * engage);
    }

    private static void Thrust(ref Engine engine, float targetSpeed, float engage = 1f)
    {
        var speedEngage = engine.GetClampedLinearEngage(targetSpeed);
        engine.SetLinearPowerClamped(speedEngage * engage);
    }
    private static float GetAngleEngage(float angleDiff)
    {
        return math.unlerp(ENGAGE_ENGINE_ANGLE, 0, math.abs(angleDiff));
    }

    private static bool CooldownComplete(ref Ship ship, float dt)
    {
        ship.State_Coundown -= dt;
        return ship.State_Coundown <= 0;
    }

    private static void Idle(ref Ship ship, float length = 1)
    {
        ship.ShipState = ShipState.Idle;
        ship.State_Coundown = length;
    }

    private static void Stop(ref Ship ship, float length = float.MaxValue)
    {
        ship.ShipState = ShipState.Stop;
        ship.State_Coundown = length;
    }

    private static void MoveToRandomPosition(ref Ship ship, ref RandomData randomData, float2 center, float2 flyAreaExtents, float length = float.MaxValue)
    {
        ship.ShipState = ShipState.MoveToPosition;
        ship.State_TargetPosition = randomData.Random.NextFloat2(center - flyAreaExtents, center + flyAreaExtents);
        ship.State_Coundown = length;
    }
}
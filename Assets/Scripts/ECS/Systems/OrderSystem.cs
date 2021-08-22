using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using UnityEditor;

using static ShipSystem;

public class OrderSystem : FixedEcbSystem
{
    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        var invDt = 1f / dt;
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        var translations = GetComponentDataFromEntity<Translation>(true);
        var velocities = GetComponentDataFromEntity<Velocity>(true);

        Entities
            .WithName("OrderState")
            .ForEach(
            (ref OrderState orderState) =>
            {
                CheckOrderTimeout(ref orderState, dt);
            }).ScheduleParallel();

        Entities
            .WithName("IdleOrder")
            .WithAll<IdleOrder>()
            .ForEach(
            (ref Engine engine) =>
            {
                engine.State_LinearPower = 0;
                engine.State_RotationPower = 0;
            }).ScheduleParallel();

        Entities
            .WithName("StopOrder")
            .WithAll<StopOrder>()
            .ForEach(
            (ref OrderState orderState, ref Engine engine,
            in Rotation rotation, in Velocity velocity) =>
            {
                var speedSQ = math.lengthsq(velocity.LinearValue);
                if (speedSQ <  STOP_SPEED_SQ)
                    orderState.Completed = true;
                Rotate(ref engine, in rotation, out var angleDiff, -velocity.LinearValue, invDt: invDt);
                Thrust(ref engine, math.sqrt(speedSQ), GetAngleEngage(angleDiff), invDt: invDt);
            }).ScheduleParallel();

        Entities
            .WithName("FlyToTarget")
            .WithReadOnly(translations)
            .ForEach(
            (ref OrderState orderState, ref FlyToOrder flyToOrder, in FlyToTarget flyToTarget) =>
            {
                if (!translations.HasComponent(flyToTarget.Entity))
                {
                    orderState.Completed = true;
                    return;
                }
                flyToOrder.Position = translations[flyToTarget.Entity].Value.xy;
            }).ScheduleParallel();

        Entities
            .WithName("FlyToOrder")
            .ForEach(
            (ref OrderState orderState, ref Engine engine,
            in FlyToOrder flyToOrder, in Translation translation, in Rotation rotation, in Velocity velocity) =>
            {
                var targetDirection = flyToOrder.Position - translation.Value.xy;
                var distanceSQ = math.lengthsq(targetDirection);
                if (distanceSQ < REACH_DISTANCE_SQ)
                    orderState.Completed = true;
                var targetVelocity = math.normalize(targetDirection) * engine.CruiseSpeed;
                var thrustVelocity = targetVelocity - velocity.LinearValue;
                var thrustLengthSQ = math.lengthsq(thrustVelocity);
                if (thrustLengthSQ < EQUAL_SPEED_SQ)
                {
                    Rotate(ref engine, in rotation, targetDirection, invDt: invDt);
                    engine.State_LinearPower = 0;
                }
                else
                {
                    Rotate(ref engine, in rotation, out var angleDiff, thrustVelocity, invDt: invDt);
                    Thrust(ref engine, math.sqrt(thrustLengthSQ), GetAngleEngage(angleDiff), invDt: invDt);
                }
            }).ScheduleParallel();

        Entities
            .WithName("FireAtTarget")
            .WithReadOnly(translations)
            .WithReadOnly(velocities)
            .WithNone<FindTarget>()
            .ForEach(
            (ref OrderState orderState, ref FireAtOrder fireAtOrder, in FireAtTarget fireAtTarget) =>
            {
                if (!translations.HasComponent(fireAtTarget.Entity))
                {
                    orderState.Completed = true;
                    return;
                }

                fireAtOrder.Position = translations[fireAtTarget.Entity].Value.xy;
                fireAtOrder.Velocity = velocities.HasComponent(fireAtTarget.Entity) ? velocities[fireAtTarget.Entity].LinearValue : float2.zero;
            }).ScheduleParallel();

        Entities
            .WithName("FireAtOrder")
            .WithNone<FindTarget>()
            .ForEach(
            (ref OrderState orderState, ref Weapon weapon, ref Engine engine,
            in FireAtOrder fireAtOrder, in Translation translation, in Rotation rotation, in Velocity velocity) =>
            {
                var turret = new Turret
                {
                    Position = translation.Value.xy,
                    Velocity = velocity.LinearValue,
                    MuzzleOffset = weapon.MuzzleOffet.x,
                    ProjectileSpeed = weapon.InitialVelocity,
                };
                var target = new Target
                {
                    Position = fireAtOrder.Position,
                    Velocity = fireAtOrder.Velocity,
                };
                var haveAim = Aim(turret, target, out var aimResult);
                if (haveAim)
                {
                    var dir = aimResult.AimPosition - turret.Position;
                    Rotate(ref engine, in rotation, out float angleDiff, dir, invDt: invDt);
                    var normalizedDir = math.normalize(dir);
                    var targetVelocity = normalizedDir * engine.CruiseSpeed + target.Velocity;
                    var diffVelocity = targetVelocity - velocity.LinearValue;
                    var engineControl = math.dot(diffVelocity, normalizedDir);
                    Thrust(ref engine, engineControl, GetAngleEngage(angleDiff), invDt: invDt);
                    weapon.State_AllowedToFire = math.abs(angleDiff) < FIRE_MAX_ANGLE;
                }
                else
                {
                    Rotate(ref engine, in rotation, out float angleDiff, target.Position - turret.Position, invDt: invDt);
                    Thrust(ref engine, engine.CruiseSpeed, GetAngleEngage(angleDiff), invDt: invDt);
                    weapon.State_AllowedToFire = math.abs(angleDiff) < FIRE_MAX_ANGLE;
                }
            }).ScheduleParallel();

        Entities
            .WithName("StopFire")
            .WithAll<OrderState>()
            .WithNone<FireAtOrder, FireAtTarget>()
            .ForEach(
            (ref Weapon weapon) =>
            {
                weapon.State_AllowedToFire = false;
            }).ScheduleParallel();

        _ecbSystem.AddJobHandleForProducer(Dependency);
    }

    private static void CheckOrderTimeout(ref OrderState orderState, float dt)
    {
        orderState.Timeout -= dt;
        orderState.Completed = orderState.Timeout <= 0;
    }
}

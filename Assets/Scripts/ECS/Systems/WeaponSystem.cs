using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.ECS.Systems
{
    public class WeaponSystem : FixedEcbSystem
    {
        protected override void OnUpdate()
        {
            var dt = Time.DeltaTime;
            var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

            Entities
                .WithNone<WeaponBurst, WeaponMultiShot>()
                .ForEach((int entityInQueryIndex, ref Weapon weapon,
                in Translation translation, in Rotation rotation, in Velocity velocity, in Team team) =>
                {
                    if (Cooldown(dt, ref weapon))
                        return;
                    var body = new Body(translation.Value.xy, velocity.LinearValue, rotation.Value);
                    var fireBody = GetFireBody(body, weapon);
                    CreateProjectile(entityInQueryIndex, ecb, weapon.ProjectilePrefab, team, fireBody);
                }).ScheduleParallel();

            Entities
                .WithNone<WeaponMultiShot>()
                .ForEach((int entityInQueryIndex, ref Weapon weapon, ref WeaponBurst weaponBurst,
                in Translation translation, in Rotation rotation, in Velocity velocity, in Team team) =>
                {
                    if (Cooldown(dt, ref weapon, ref weaponBurst))
                        return;
                    var body = new Body(translation.Value.xy, velocity.LinearValue, rotation.Value);
                    var fireBody = GetFireBody(body, weapon);
                    CreateProjectile(entityInQueryIndex, ecb, weapon.ProjectilePrefab, team, fireBody);
                }).ScheduleParallel();

            Entities
                .WithNone<WeaponBurst>()
                .ForEach((int entityInQueryIndex, ref Weapon weapon, ref WeaponMultiShot weaponMultiShot,
                in Translation translation, in Rotation rotation, in Velocity velocity, in Team team) =>
                {
                    if (Cooldown(dt, ref weapon))
                        return;

                    var body = new Body(translation.Value.xy, velocity.LinearValue, rotation.Value);
                    var multiStart = GetMultiStart(weaponMultiShot);
                    if (weaponMultiShot.SequentialFire | weaponMultiShot.MuzzleCount == 1)
                    {
                        var fireBody = GetFireBody(body, weapon, weaponMultiShot, multiStart, weaponMultiShot.State_MuzzleIndex);
                        CreateProjectile(entityInQueryIndex, ecb, weapon.ProjectilePrefab, team, fireBody);
                        weaponMultiShot.State_MuzzleIndex = (byte)MathExtensions.Wrap(weaponMultiShot.State_MuzzleIndex + 1, weaponMultiShot.MuzzleCount);
                    }
                    else
                        for (byte i = 0; i < weaponMultiShot.MuzzleCount; i++)
                        {
                            var fireBody = GetFireBody(body, weapon, weaponMultiShot, multiStart, i);
                            CreateProjectile(entityInQueryIndex, ecb, weapon.ProjectilePrefab, team, fireBody);
                        }
                }).ScheduleParallel();

            Entities
                .ForEach((int entityInQueryIndex, ref Weapon weapon, ref WeaponBurst weaponBurst, ref WeaponMultiShot weaponMultiShot,
                in Translation translation, in Rotation rotation, in Velocity velocity, in Team team) =>
                {
                    if (Cooldown(dt, ref weapon, ref weaponBurst))
                        return;

                    var body = new Body(translation.Value.xy, velocity.LinearValue, rotation.Value);
                    var multiStart = GetMultiStart(weaponMultiShot);
                    if (weaponMultiShot.SequentialFire | weaponMultiShot.MuzzleCount == 1)
                    {
                        var fireBody = GetFireBody(body, weapon, weaponMultiShot, multiStart, weaponMultiShot.State_MuzzleIndex);
                        CreateProjectile(entityInQueryIndex, ecb, weapon.ProjectilePrefab, team, fireBody);
                        weaponMultiShot.State_MuzzleIndex = (byte)MathExtensions.Wrap(weaponMultiShot.State_MuzzleIndex + 1, weaponMultiShot.MuzzleCount);
                    }
                    else
                        for (byte i = 0; i < weaponMultiShot.MuzzleCount; i++)
                        {
                            var fireBody = GetFireBody(body, weapon, weaponMultiShot, multiStart, i);
                            CreateProjectile(entityInQueryIndex, ecb, weapon.ProjectilePrefab, team, fireBody);
                        }
                }).ScheduleParallel();

            _ecbSystem.AddJobHandleForProducer(Dependency);
        }

        private static bool Cooldown(float dt, ref Weapon weapon)
        {
            weapon.State_Countdown -= dt;
            if (weapon.State_Countdown > 0)
                return true;
            if (!weapon.State_AllowedToFire)
                return true;
            weapon.State_Countdown = weapon.FireInterval;
            return false;
        }

        private static bool Cooldown(float dt, ref Weapon weapon, ref WeaponBurst weaponBurst)
        {
            weapon.State_Countdown -= dt;
            if (weapon.State_Countdown > 0)
                return true;

            var isBursting = weaponBurst.State_BurstFiredProjectiles > 0;

            if (!weapon.State_AllowedToFire & (!isBursting | weaponBurst.CanInterrupt))
                return true;

            weaponBurst.State_BurstFiredProjectiles++;
            if (weaponBurst.State_BurstFiredProjectiles < weaponBurst.BurstProjectileCount)
                weapon.State_Countdown = weaponBurst.BurstInterval;
            else
            {
                weaponBurst.State_BurstFiredProjectiles = 0;
                weapon.State_Countdown = weapon.FireInterval;
            }
            return false;
        }

        private static float2 InitialVelocity(float2 currentVelocity, quaternion currentRotation, float initialVelocity)
        {
            return currentVelocity + new float2(initialVelocity, 0).Rotate2D(currentRotation);
        }

        private static MultiStart GetMultiStart(WeaponMultiShot weaponMultiShot)
        {
            var startCoeff = (weaponMultiShot.MuzzleCount - 1) * -0.5f;
            var pos = weaponMultiShot.MuzzlePosSeparation * startCoeff;
            var angle = weaponMultiShot.MuzzleAngleSeparation * startCoeff;
            return new MultiStart(pos, angle);
        }

        private static Body GetFireBody(Body parameters, Weapon weapon)
        {
            var pos = parameters.Pos + weapon.MuzzleOffet.Rotate2D(parameters.Rot);
            var vel = InitialVelocity(parameters.Vel, parameters.Rot, weapon.InitialVelocity);
            return new Body(pos, vel, parameters.Rot);
        }

        private static Body GetFireBody(Body parameters, Weapon weapon, WeaponMultiShot weaponMultiShot, MultiStart multiStart, byte index)
        {
            var pos = parameters.Pos + (weapon.MuzzleOffet + new float2(0, multiStart.Pos + weaponMultiShot.MuzzlePosSeparation * index)).Rotate2D(parameters.Rot);
            var rot = parameters.Rot.Rotate2D(multiStart.Angle + weaponMultiShot.MuzzleAngleSeparation * index);
            var vel = InitialVelocity(parameters.Vel, rot, weapon.InitialVelocity);
            return new Body(pos, vel, rot);
        }

        private static void CreateProjectile(
            int entityInQueryIndex, 
            EntityCommandBuffer.ParallelWriter ecb, Entity prefab, 
            Team team, Body parameters)
        {
            var projectile = ecb.Instantiate(entityInQueryIndex, prefab);
            ecb.SetComponent(entityInQueryIndex, projectile, team);
            ecb.SetComponent(entityInQueryIndex, projectile, new Translation { Value = new float3(parameters.Pos, 0) });
            ecb.SetComponent(entityInQueryIndex, projectile, new Velocity { LinearValue = parameters.Vel });
            ecb.SetComponent(entityInQueryIndex, projectile, new Rotation { Value = parameters.Rot });
        }

        public struct Body
        {
            public float2 Pos;
            public float2 Vel;
            public quaternion Rot;

            public Body(float2 pos, float2 vel, quaternion rot)
            {
                Pos = pos;
                Vel = vel;
                Rot = rot;
            }
        }

        public struct MultiStart
        {
            public float Pos;
            public float Angle;

            public MultiStart(float pos, float angle)
            {
                Pos = pos;
                Angle = angle;
            }
        }
    }
}

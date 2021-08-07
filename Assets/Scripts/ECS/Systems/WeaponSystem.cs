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

            Entities.ForEach(
                (Entity entity, int entityInQueryIndex,
                ref Weapon weapon,
                in Translation translation, in Rotation rotation, in Velocity velocity, in Team team) =>
                {
                    weapon.State_Countdown -= dt;
                    if (weapon.State_AllowedToFire && weapon.State_Countdown <= 0)
                    {
                        weapon.State_Countdown = weapon.FireInterval;
                        var projectile = ecb.Instantiate(entityInQueryIndex, weapon.ProjectilePrefab);
                        ecb.SetComponent(entityInQueryIndex, projectile, 
                            new Translation { Value = new float3(translation.Value.xy + weapon.MuzzleOffet, 0) });
                        ecb.SetComponent(entityInQueryIndex, projectile, rotation);
                        ecb.SetComponent(entityInQueryIndex, projectile,
                            new Velocity { LinearValue = velocity.LinearValue + new float2(weapon.InitialVelocity, 0).Rotate2D(rotation.Value) });
                        ecb.SetComponent(entityInQueryIndex, projectile, team);
                    }
                }).ScheduleParallel();
            _ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}

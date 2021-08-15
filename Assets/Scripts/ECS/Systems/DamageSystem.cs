using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

public class DamageSystem : PhysicsEcbSystem
{
    protected unsafe override void OnUpdate()
    {
        var dt = Time.DeltaTime;

        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        var collisionWorld = _buildPhysicsWorld.PhysicsWorld.CollisionWorld;
        var projectiles = GetComponentDataFromEntity<Projectile>(true);
        var teams = GetComponentDataFromEntity<Team>(true);

        Entities
            .WithReadOnly(collisionWorld)
            .WithReadOnly(projectiles)
            .WithReadOnly(teams)
            .WithAll<Ship>()
            .ForEach(
            (Entity entity, int entityInQueryIndex,
            ref Health health,
            in Translation translation, in Rotation rotation, in PhysicsCollider collider, in Team team) =>
            {
                var input = new ColliderCastInput
                {
                    Collider = collider.ColliderPtr,
                    Start = translation.Value,
                    End = translation.Value,
                    Orientation = rotation.Value,
                };
                var hits = new NativeList<ColliderCastHit>(Allocator.Temp);

                if (collisionWorld.CastCollider(input, ref hits))
                {
                    for (int i = 0; i < hits.Length; i++)
                    {
                        var hit = hits[i];
                        var hitEntity = hit.Entity;
                        if (entity == hitEntity)
                            continue;
                        if (!projectiles.HasComponent(hitEntity))
                            continue;
                        if (!teams.HasComponent(hitEntity))
                            continue;
                        if (teams[hitEntity].Id == team.Id)
                            continue;

                        health.State_Health -= projectiles[hitEntity].Damage;
                        ecb.DestroyEntity(entityInQueryIndex, hitEntity);
                    }

                    if (health.State_Health <= 0)
                        ecb.DestroyEntity(entityInQueryIndex, entity);
                }
                hits.Dispose();
            }).ScheduleParallel();

        _buildPhysicsWorld.AddInputDependencyToComplete(Dependency);
        _ecbSystem.AddJobHandleForProducer(Dependency);
    }

    [BurstCompile]
    public struct DamageJob : ITriggerEventsJob
    {
        public EntityCommandBuffer Ecb;
        public ComponentDataFromEntity<Health> Healths;
        [ReadOnly] public ComponentDataFromEntity<Projectile> Projectiles;
        [ReadOnly] public ComponentDataFromEntity<Team> Teams;

        public void Execute(TriggerEvent triggerEvent)
        {
            RunCollision(triggerEvent.EntityA, triggerEvent.EntityB);
            RunCollision(triggerEvent.EntityB, triggerEvent.EntityA);
        }

        private void RunCollision(Entity damageable, Entity damager)
        {
            if (!Healths.HasComponent(damageable) || !Teams.HasComponent(damageable))
                return;

            if (!Projectiles.HasComponent(damager) || !Teams.HasComponent(damager))
                return;

            if (Teams[damageable].Id == Teams[damager].Id)
                return;

            var health = Healths[damageable];
            health.State_Health -= Projectiles[damager].Damage;
            Healths[damageable] = health;

            Ecb.DestroyEntity(damager);

            if (health.State_Health <= 0)
                Ecb.DestroyEntity(damageable);
        }
    }
}

using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public class CollisionAvoidanceSystem : PhysicsSystem
{
    private static readonly float _AVOIDANCE_POWER = 5;
    private static readonly float _AVOIDANCE_BASE = 3;
    private static readonly float _MIN_DISTANCE = 2f;

    private static readonly float _MAX_DISTANCE = 10;
    private static readonly int _MAX_HITS = 16;
    private static readonly CollisionFilter _FILTER = new CollisionFilter
    {
        BelongsTo = (uint)CollisionLayer.Projectile,
        CollidesWith = (uint)CollisionLayer.Ship,
    };

    unsafe protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        var collisionWorld = _buildPhysicsWorld.PhysicsWorld.CollisionWorld;
        //var ships = GetComponentDataFromEntity<Ship>(true);

        Entities
            //.WithReadOnly(ships)
            .WithReadOnly(collisionWorld)
            .WithAll<Ship>()
            .ForEach(
            (Entity entity, int entityInQueryIndex, 
            ref Velocity velocity,
            in Translation translation, in Rotation rotation, in PhysicsCollider collider) =>
            {
                var input = new PointDistanceInput
                {
                    Position = translation.Value,
                    MaxDistance = _MAX_DISTANCE,
                    Filter = _FILTER,
                };
                var hits = new NativeList<DistanceHit>(Allocator.Temp);

                if (collisionWorld.CalculateDistance(input, ref hits))
                {
                    var maxHits = math.min(_MAX_HITS, hits.Length);
                    //var maxHits = hits.Length;
                    for (int i = 0; i < hits.Length; i++)
                    {
                        var hit = hits[i];
                        var hitEntity = hit.Entity;
                        if (entity != hitEntity /*&& ships.HasComponent(hitEntity)*/)
                        {
                            var diff = translation.Value.xy - hit.Position.xy;
                            var len = math.lengthsq(diff);
                            if (len > _MIN_DISTANCE*_MIN_DISTANCE)
                            {
                                var direction = math.normalize(diff);
                                var power = _AVOIDANCE_POWER * math.pow(_AVOIDANCE_BASE, math.min(0, -(hit.Distance-_MIN_DISTANCE)));
                                //var power = _AVOIDANCE_POWER * math.pow(_AVOIDANCE_BASE, -hit.Distance);
                                velocity.LinearValue += direction * power;
                            }
                        }
                    }
                }
                hits.Dispose();
            }).ScheduleParallel();

        _buildPhysicsWorld.AddInputDependencyToComplete(Dependency);
    }
}

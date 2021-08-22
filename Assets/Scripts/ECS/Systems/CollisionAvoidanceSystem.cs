using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public class CollisionAvoidanceSystem : PhysicsSystem
{
    private static readonly float _AVOIDANCE_POWER = 2;
    private static readonly float _AVOIDANCE_BASE = 2;

    private static readonly float _MAX_DISTANCE = 5;
    private static readonly int _MAX_HITS = 8;

    unsafe protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        var collisionWorld = _buildPhysicsWorld.PhysicsWorld.CollisionWorld;
        var colliders = GetComponentDataFromEntity<PhysicsCollider>(true);

        Entities
            .WithName("AvoidCollisions")
            .WithReadOnly(collisionWorld)
            .WithReadOnly(colliders)
            .ForEach(
            (ref Velocity velocity,
            in Translation translation, in Rotation rotation, in CollisionAvoidanceProxy collisionAvoidance) =>
            {
                if (!colliders.HasComponent(collisionAvoidance.EntityWithPhysicsCollider))
                    return;

                var input = new ColliderDistanceInput
                {
                    Collider = colliders[collisionAvoidance.EntityWithPhysicsCollider].ColliderPtr,
                    Transform = new RigidTransform { pos = translation.Value, rot = rotation.Value },
                    MaxDistance = _MAX_DISTANCE,
                };

                var hits = new NativeList<DistanceHit>(_MAX_HITS, Allocator.Temp);
                var collector = new MaxHitsCollector<DistanceHit>(_MAX_DISTANCE, _MAX_HITS, ref hits);

                if (collisionWorld.CalculateDistance(input, ref collector))
                {
                    var maxHits = math.min(_MAX_HITS, hits.Length);
                    for (int i = 0; i < hits.Length; i++)
                    {
                        var hit = hits[i];
                        if (collisionAvoidance.EntityWithPhysicsCollider != hit.Entity)
                        {
                            var diff = translation.Value.xy - hit.Position.xy;
                            var direction = math.normalize(diff);
                            var power = _AVOIDANCE_POWER * math.pow(_AVOIDANCE_BASE, -hit.Distance);
                            var force = direction * power;
                            velocity.LinearValue += force;
                        }
                    }
                }
                hits.Dispose();
            }).ScheduleParallel();

        _buildPhysicsWorld.AddInputDependencyToComplete(Dependency);
    }

    public struct MaxHitsCollector<T> : ICollector<T> where T : struct, IQueryResult
    {
        public NativeList<T> Hits;

        public float MaxFraction { get; }
        public int MaxHits { get; }

        public int NumHits => Hits.Length;
        public bool EarlyOutOnFirstHit => false;

        public MaxHitsCollector(float maxDistance, int maxHits, ref NativeList<T> hits)
        {
            Hits = hits;
            MaxFraction = maxDistance;
            MaxHits = maxHits;
        }

        public bool AddHit(T hit)
        {
            Hits.Add(hit);
            return NumHits < MaxHits;
        }
    }
}

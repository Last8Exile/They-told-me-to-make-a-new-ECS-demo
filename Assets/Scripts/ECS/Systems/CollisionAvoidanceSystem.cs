using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public class CollisionAvoidanceSystem : PhysicsSystem
{
    private static readonly float _AVOIDANCE_POWER = 2;
    private static readonly float _MAX_DISTANCE = 5;

    unsafe protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        var physicsWorld = _physicsWorldSystem.PhysicsWorld;
        var ships = GetComponentDataFromEntity<Ship>(true);

        Entities
            .WithReadOnly(ships)
            .WithAll<Ship>()
            .ForEach(
            (Entity entity, int entityInQueryIndex, 
            ref Velocity velocity,
            in Translation translation, in Rotation rotation, in PhysicsCollider collider) =>
            {
                var input = new ColliderDistanceInput
                {
                    Transform = new RigidTransform { pos = translation.Value, rot = rotation.Value },
                    MaxDistance = _MAX_DISTANCE,
                    Collider = collider.ColliderPtr,
                };
                var hits = new NativeList<DistanceHit>(Allocator.Temp);

                if (physicsWorld.CalculateDistance(input, ref hits))
                {
                    for (int i = 0; i < hits.Length; i++)
                    {
                        var hit = hits[i];
                        var hitEntity = hit.Entity;
                        if (entity != hitEntity && ships.HasComponent(hitEntity))
                        {
                            velocity.LinearValue += math.normalize(translation.Value.xy - hit.Position.xy) * math.pow(_AVOIDANCE_POWER, -hit.Distance);
                        }
                    }
                }
                hits.Dispose();
            }).Schedule();
    }
}

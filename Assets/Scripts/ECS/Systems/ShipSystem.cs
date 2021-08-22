using Unity.Assertions;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using ECB = Unity.Entities.EntityCommandBuffer;
using ECBP = Unity.Entities.EntityCommandBuffer.ParallelWriter;

public class ShipSystem : FixedEcbSystem
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
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        Entities
            .WithName("ShipAIFix")
            .WithAll<Ship>()
            .WithNone<OrderState>()
            .ForEach(
            (Entity entity, int entityInQueryIndex) =>
            {
                ecb.AddComponent<OrderState>(entityInQueryIndex, entity);
            }).ScheduleParallel();

        Entities
            .WithName("ShipAI")
            .WithAll<Ship>()
            .ForEach(
            (Entity entity, int entityInQueryIndex,
            ref OrderState orderState, ref RandomData randomData) =>
            {
                if (!orderState.Completed)
                    return;

                var prevOrder = orderState.Order;
                var args = new FSMArgs { Ecb = ecb, Entity = entity, Index = entityInQueryIndex };

                //Stop previous order
                FromOrder(ref args, ref orderState);

                //Start new order
                switch (prevOrder)
                {
                    case Order.None:
                        ToIdle(ref args, ref orderState, 1f);
                        break;
                    case Order.Idle:
                        var center = float2.zero;
                        var position = randomData.Random.NextFloat2(center - FLY_AREA_EXTENTS, center + FLY_AREA_EXTENTS);
                        ToFlyTo(ref args, ref orderState, position, 5f);
                        break;
                    case Order.Stop:
                        ToIdle(ref args, ref orderState, 1f);
                        break;
                    case Order.FlyTo:
                        ToStop(ref args, ref orderState, 2f);
                        break;
                    case Order.FireAt:
                        ToIdle(ref args, ref orderState, 1f);
                        break;
                }
            }).ScheduleParallel();


        /*
        MoveToRandomPosition(ref ship, ref randomData, float2.zero, FLY_AREA_EXTENTS, 5f);
        Idle(ref ship, 1f);
        Stop(ref ship, 2f);
        */

        _ecbSystem.AddJobHandleForProducer(Dependency);
    }

    #region OrderFSM

    public ref struct FSMArgs
    {
        public Entity Entity;
        public int Index;
        public ECBP Ecb;

        public FSMArgs(Entity entity, int index, ECBP ecb)
        {
            Entity = entity;
            Index = index;
            Ecb = ecb;
        }
    }

    private static void CheckOrder(Order currentOrder, Order expectedOrder)
    {
        if (currentOrder != expectedOrder)
            throw new System.Exception($"Wrong order. Current {currentOrder}, expected {expectedOrder}.");
    }

    private static void SetOrder(ref OrderState orderState, Order order, float timeout)
    {
        orderState.Completed = false;
        orderState.Order = order;
        orderState.Timeout = timeout;
    }

    private static void FromOrder(ref FSMArgs args, ref OrderState orderState)
    {
        switch (orderState.Order)
        {
            case Order.None: /*Nothing to do*/ break;
            case Order.Idle: FromIdle(ref args, ref orderState); break;
            case Order.Stop: FromStop(ref args, ref orderState); break;
            case Order.FlyTo: FromFlyTo(ref args, ref orderState); break;
            case Order.FireAt: /*Not Implemented*/ break;
        }
    }

    private static void ToIdle(ref FSMArgs args, ref OrderState orderState, float timout = float.PositiveInfinity)
    {
        CheckOrder(orderState.Order, Order.None);
        SetOrder(ref orderState, Order.Idle, timout);
        args.Ecb.AddComponent<IdleOrder>(args.Index, args.Entity);
    }
    private static void FromIdle(ref FSMArgs args, ref OrderState orderState)
    {
        CheckOrder(orderState.Order, Order.Idle);
        SetOrder(ref orderState, Order.None, 0);
        args.Ecb.RemoveComponent<IdleOrder>(args.Index, args.Entity);
    }

    private static void ToStop(ref FSMArgs args, ref OrderState orderState, float timout = float.PositiveInfinity)
    {
        CheckOrder(orderState.Order, Order.None);
        SetOrder(ref orderState, Order.Stop, timout);
        args.Ecb.AddComponent<StopOrder>(args.Index, args.Entity);
    }
    private static void FromStop(ref FSMArgs args, ref OrderState orderState)
    {
        CheckOrder(orderState.Order, Order.Stop);
        SetOrder(ref orderState, Order.None, 0);
        args.Ecb.RemoveComponent<StopOrder>(args.Index, args.Entity);
    }

    private static void ToFlyTo(ref FSMArgs args, ref OrderState orderState, float2 position, float timout = float.PositiveInfinity)
    {
        CheckOrder(orderState.Order, Order.None);
        SetOrder(ref orderState, Order.FlyTo, timout);
        args.Ecb.AddComponent(args.Index, args.Entity, new FlyToOrder { Position = position });
    }
    private static void ToFlyTo(ref FSMArgs args, ref OrderState orderState, Entity target, float timout = float.PositiveInfinity)
    {
        CheckOrder(orderState.Order, Order.None);
        SetOrder(ref orderState, Order.FlyTo, timout);
        args.Ecb.AddComponent(args.Index, args.Entity, new FlyToTarget { Entity = target });
        args.Ecb.AddComponent<FlyToOrder>(args.Index, args.Entity);
    }
    private static void FromFlyTo(ref FSMArgs args, ref OrderState orderState)
    {
        CheckOrder(orderState.Order, Order.FlyTo);
        SetOrder(ref orderState, Order.None, 0);
        args.Ecb.RemoveComponent<FlyToOrder>(args.Index, args.Entity);
        args.Ecb.RemoveComponent<FlyToTarget>(args.Index, args.Entity);
    }

    #endregion

    #region ProcessOrder

    public static void Rotate(ref Engine engine, in Rotation rotation, float2 targetDirection, float engage = 1f)
    {
        Rotate(ref engine, in rotation, out _, targetDirection, engage);
    }
    public static void Rotate(ref Engine engine, in Rotation rotation, out float angleDiff, float2 targetDirection, float engage = 1f)
    {
        var currentDirection = MathExtensions.Direction2D(rotation.Value);
        angleDiff = MathExtensions.SignedAngle(currentDirection, targetDirection);
        engine.SetRotationPowerClamped(engine.GetClampedRotationalEngage(angleDiff) * engage);
    }

    public static void Thrust(ref Engine engine, float targetSpeed, float engage = 1f)
    {
        var speedEngage = engine.GetClampedLinearEngage(targetSpeed);
        engine.SetLinearPowerClamped(speedEngage * engage);
    }
    public static float GetAngleEngage(float angleDiff)
    {
        return math.unlerp(ENGAGE_ENGINE_ANGLE, 0, math.abs(angleDiff));
    }

    #endregion
}
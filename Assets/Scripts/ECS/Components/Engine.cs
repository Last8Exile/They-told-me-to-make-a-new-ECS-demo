using System;

using Unity.Entities;
using Unity.Mathematics;

using UnityEngine;

[Serializable]
[GenerateAuthoringComponent]
public struct Engine : IComponentData
{
    public float LinerAcceleration;
    public float RotationSpeed;

    public float CruiseSpeed;

    [Header("State")]
    [Range(0, 1)]
    public float State_LinearPower;
    [Range(-1, 1)]
    public float State_RotationPower;

    public float GetClampedLinearEngage(float speed, float invDt = 1f)
    {
        return math.clamp(speed * invDt / LinerAcceleration, 0, 1);
    }
    public void SetLinearPowerClamped(float power)
    {
        State_LinearPower = math.clamp(power, 0, 1);
    }


    public float GetClampedRotationalEngage(float speed, float invDt = 1f)
    {
        return math.clamp(speed * invDt / RotationSpeed, -1, 1);
    }
    public void SetRotationPowerClamped(float power)
    {
        State_RotationPower = math.clamp(power, -1, 1);
    }
}
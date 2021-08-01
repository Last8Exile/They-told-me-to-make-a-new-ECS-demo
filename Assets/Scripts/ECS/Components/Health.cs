using System;

using Unity.Entities;

using UnityEngine;

[Serializable]
[GenerateAuthoringComponent]
public struct Health : IComponentData
{
    public float MaxHealth;

    [Header("State")]
    public float CurrentHealth;
}

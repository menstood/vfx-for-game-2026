using System;
using UnityEngine;

[Serializable]
public class SkillSlot
{
    public string skillName = "Skill";

    [Tooltip("Follows the bow while the archer aims. Its forward (Z) axis points at the target.")]
    public GameObject cast;

    [Tooltip("Flies from the bow to the target. Its forward (Z) axis points along the flight direction.")]
    public GameObject projectile;

    [Tooltip("Spawned at the hit point. Its forward (Z) axis points back toward the archer.")]
    public GameObject impact;

    [Min(0.1f)] public float projectileSpeed = 15f;
    [Min(0f)] public float impactDuration = 1.5f;
}

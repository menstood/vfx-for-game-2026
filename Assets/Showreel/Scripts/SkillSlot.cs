using System;
using UnityEngine;

public enum SkillType
{
    ChargedShot,
    RainOfArrows,
    CrouchShot,
    AerialShot,
}

// Each VFX slot is a scene object: put your VFX inside it and turn off its Placeholder.
[Serializable]
public class SkillSlot
{
    [SerializeField] string skillName = "Skill";
    [SerializeField] SkillType type;

    [SerializeField] GameObject circle;
    [SerializeField] GameObject cast;
    [SerializeField] GameObject projectile;
    [SerializeField] GameObject impact;

    [SerializeField, Min(0f)] float releaseTime = 2f;
    [SerializeField, Min(0.1f)] float projectileSpeed = 20f;
    [SerializeField, Min(0f)] float impactDuration = 1.5f;

    public string SkillName => skillName;
    public SkillType Type => type;
    public GameObject Circle => circle;
    public GameObject Cast => cast;
    public GameObject Projectile => projectile;
    public GameObject Impact => impact;
    public float ReleaseTime => releaseTime;
    public float ProjectileSpeed => projectileSpeed;
    public float ImpactDuration => impactDuration;
}

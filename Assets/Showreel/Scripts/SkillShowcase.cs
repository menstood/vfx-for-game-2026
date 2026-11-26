using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Plays each skill: animation + cast VFX on the bow -> release -> projectile -> impact.
// Keys 1-9 play one skill, Space restarts the auto loop.
public class SkillShowcase : MonoBehaviour
{
    static readonly int ChargedTrigger = Animator.StringToHash("Charged");
    static readonly int RainTrigger = Animator.StringToHash("Rain");
    static readonly int AerialTrigger = Animator.StringToHash("Aerial");
    static readonly int ReleaseTrigger = Animator.StringToHash("Release");
    static readonly int CrouchingBool = Animator.StringToHash("Crouching");
    static readonly int IdleState = Animator.StringToHash("Idle");
    static readonly int EmptyState = Animator.StringToHash("Empty");
    static readonly int HitTrigger = Animator.StringToHash("Hit");
    static readonly int HitIndexInt = Animator.StringToHash("HitIndex");
    const int HitVariations = 2;

    const int BaseLayer = 0;
    const int UpperBodyLayer = 1;

    [SerializeField] Animator archer;
    [SerializeField] Animator dummy;
    [SerializeField] ShowreelCamera showreelCamera;
    [SerializeField] Transform castPoint;
    [SerializeField] Transform hitPoint;
    [SerializeField] Transform groundPoint;

    [SerializeField, Min(1f)] float rainHeight = 7f;
    [SerializeField, Range(0f, 90f)] float skyShotAngle = 58f;
    [SerializeField, Min(0f)] float crouchRecoverDelay = 0.6f;
    [SerializeField, Min(0f)] float delayBetweenSkills = 1f;
    [SerializeField] bool autoLoop = true;

    [SerializeField] SkillSlot[] skills = new SkillSlot[5];

    int hitCount;

    // The VFX slots are scene objects; they're hidden until their moment and switched on to (re)start.
    void Awake()
    {
        HideAll();
    }

    void Start()
    {
        if (autoLoop)
            StartCoroutine(LoopAll());
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            Restart(LoopAll());
            return;
        }

        int count = Mathf.Min(skills.Length, 9);
        for (int i = 0; i < count; i++)
        {
            if (keyboard[Key.Digit1 + i].wasPressedThisFrame)
            {
                Restart(Play(i));
                return;
            }
        }
    }

    IEnumerator LoopAll()
    {
        while (true)
        {
            for (int i = 0; i < skills.Length; i++)
            {
                yield return Play(i);
                yield return new WaitForSeconds(delayBetweenSkills);
            }
        }
    }

    IEnumerator Play(int index)
    {
        SkillSlot skill = skills[index];
        GameObject cast = skill.Cast;
        GameObject circle = skill.Circle;
        GameObject projectile = skill.Projectile;
        GameObject impact = skill.Impact;

        if (showreelCamera != null)
            showreelCamera.ShowSkill(index);

        if (circle != null)
        {
            Vector3 feet = archer.transform.position;
            Vector3 toTarget = Vector3.ProjectOnPlane(GetTarget(skill.Type) - feet, Vector3.up);
            circle.transform.SetPositionAndRotation(feet, Quaternion.LookRotation(toTarget, Vector3.up));
            circle.SetActive(true);
        }

        StartAnimation(skill.Type);
        SetActive(cast, true);

        // Follow the bow without parenting, so the rig's bone scale doesn't affect the VFX.
        for (float t = 0f; t < skill.ReleaseTime; t += Time.deltaTime)
        {
            if (cast != null)
                cast.transform.SetPositionAndRotation(castPoint.position, Quaternion.LookRotation(GetAimDirection(skill.Type)));
            yield return null;
        }

        SetActive(cast, false);
        SetActive(circle, false);
        Release(skill.Type);

        Vector3 end = GetTarget(skill.Type);
        Vector3 start = castPoint.position;

        if (skill.Type == SkillType.RainOfArrows)
        {
            // Shoot up out of the shot, then fall onto the target from above.
            yield return Fly(projectile, start, start + GetAimDirection(skill.Type) * rainHeight, skill.ProjectileSpeed);
            start = end + Vector3.up * rainHeight;
        }

        yield return Fly(projectile, start, end, skill.ProjectileSpeed);
        Vector3 direction = (end - start).normalized;

        if (impact != null)
        {
            impact.transform.SetPositionAndRotation(end, Quaternion.LookRotation(-direction));
            impact.SetActive(true);
        }
        PlayHitReaction();

        yield return new WaitForSeconds(skill.ImpactDuration);
        SetActive(impact, false);

        while (!IsArcherIdle())
            yield return null;

        if (showreelCamera != null)
            showreelCamera.ShowWide();
    }

    // The projectile is switched off and on per leg, so trails restart instead of streaking across the jump.
    static IEnumerator Fly(GameObject projectile, Vector3 start, Vector3 end, float speed)
    {
        Vector3 direction = (end - start).normalized;
        if (projectile != null)
        {
            projectile.transform.SetPositionAndRotation(start, Quaternion.LookRotation(direction));
            projectile.SetActive(true);
        }

        Vector3 position = start;
        float step;
        while ((end - position).magnitude > (step = speed * Time.deltaTime))
        {
            position += direction * step;
            if (projectile != null)
                projectile.transform.position = position;
            yield return null;
        }

        SetActive(projectile, false);
    }

    Vector3 GetAimDirection(SkillType type)
    {
        Vector3 toTarget = GetTarget(type) - castPoint.position;
        if (type != SkillType.RainOfArrows)
            return toTarget.normalized;

        Vector3 flat = Vector3.ProjectOnPlane(toTarget, Vector3.up).normalized;
        return Quaternion.AngleAxis(-skyShotAngle, Vector3.Cross(Vector3.up, flat)) * flat;
    }

    void StartAnimation(SkillType type)
    {
        switch (type)
        {
            case SkillType.ChargedShot: archer.SetTrigger(ChargedTrigger); break;
            case SkillType.RainOfArrows: archer.SetTrigger(RainTrigger); break;
            case SkillType.CrouchShot: archer.SetBool(CrouchingBool, true); break;
            case SkillType.AerialShot: archer.SetTrigger(AerialTrigger); break;
        }
    }

    void Release(SkillType type)
    {
        // Rain and Aerial release inside their own clips; the others wait for this trigger.
        if (type == SkillType.ChargedShot || type == SkillType.CrouchShot)
            archer.SetTrigger(ReleaseTrigger);

        if (type == SkillType.CrouchShot)
            StartCoroutine(StandUpAfter(crouchRecoverDelay));
    }

    void PlayHitReaction()
    {
        if (dummy == null)
            return;

        // Alternate the reactions so repeated hits don't look identical.
        dummy.SetInteger(HitIndexInt, hitCount % HitVariations);
        dummy.SetTrigger(HitTrigger);
        hitCount++;
    }

    IEnumerator StandUpAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        archer.SetBool(CrouchingBool, false);
    }

    Vector3 GetTarget(SkillType type)
    {
        return type == SkillType.RainOfArrows || type == SkillType.AerialShot
            ? groundPoint.position
            : hitPoint.position;
    }

    bool IsArcherIdle()
    {
        return !archer.IsInTransition(BaseLayer)
            && archer.GetCurrentAnimatorStateInfo(BaseLayer).shortNameHash == IdleState;
    }

    void Restart(IEnumerator next)
    {
        StopAllCoroutines();
        HideAll();
        archer.ResetTrigger(ChargedTrigger);
        archer.ResetTrigger(RainTrigger);
        archer.ResetTrigger(AerialTrigger);
        archer.ResetTrigger(ReleaseTrigger);
        archer.SetBool(CrouchingBool, false);
        archer.Play(IdleState, BaseLayer, 0f);
        archer.Play(EmptyState, UpperBodyLayer, 0f);
        StartCoroutine(next);
    }

    void HideAll()
    {
        foreach (SkillSlot skill in skills)
        {
            SetActive(skill.Cast, false);
            SetActive(skill.Circle, false);
            SetActive(skill.Projectile, false);
            SetActive(skill.Impact, false);
        }
    }

    static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}

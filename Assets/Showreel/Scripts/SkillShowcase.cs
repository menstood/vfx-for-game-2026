using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Plays each skill as Aim (cast VFX) -> Release (projectile VFX) -> Hit (impact VFX).
// Keys 1-9 play one skill, Space restarts the auto loop.
public class SkillShowcase : MonoBehaviour
{
    static readonly int AimTrigger = Animator.StringToHash("Aim");
    static readonly int ReleaseTrigger = Animator.StringToHash("Release");
    static readonly int IdleState = Animator.StringToHash("Idle");

    [SerializeField] Animator archer;
    [SerializeField] Transform castPoint;
    [SerializeField] Transform hitPoint;

    [Tooltip("How long the archer holds the aim (cast VFX time) before releasing.")]
    [SerializeField, Min(0f)] float aimDuration = 1.2f;
    [SerializeField, Min(0f)] float delayBetweenSkills = 1f;
    [SerializeField] bool autoLoop = true;

    [SerializeField] SkillSlot[] skills = new SkillSlot[5];

    GameObject[] castInstances;
    GameObject[] projectileInstances;
    GameObject[] impactInstances;
    Coroutine routine;

    void Awake()
    {
        castInstances = new GameObject[skills.Length];
        projectileInstances = new GameObject[skills.Length];
        impactInstances = new GameObject[skills.Length];

        // Spawn each VFX once and toggle it on/off, so re-enabling restarts it.
        for (int i = 0; i < skills.Length; i++)
        {
            castInstances[i] = Spawn(skills[i].cast, transform);
            projectileInstances[i] = Spawn(skills[i].projectile, transform);
            impactInstances[i] = Spawn(skills[i].impact, transform);
        }
    }

    void Start()
    {
        if (autoLoop)
            routine = StartCoroutine(LoopAll());
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
        GameObject cast = castInstances[index];
        GameObject projectile = projectileInstances[index];
        GameObject impact = impactInstances[index];

        archer.SetTrigger(AimTrigger);
        SetActive(cast, true);

        // Follow the bow without parenting, so the rig's bone scale doesn't shrink the VFX.
        for (float t = 0f; t < aimDuration; t += Time.deltaTime)
        {
            if (cast != null)
                cast.transform.SetPositionAndRotation(castPoint.position,
                    Quaternion.LookRotation(hitPoint.position - castPoint.position));
            yield return null;
        }

        archer.SetTrigger(ReleaseTrigger);
        SetActive(cast, false);

        Vector3 position = castPoint.position;
        Vector3 direction = (hitPoint.position - position).normalized;

        if (projectile != null)
        {
            projectile.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            projectile.SetActive(true);
        }

        while (true)
        {
            Vector3 toTarget = hitPoint.position - position;
            float step = skill.projectileSpeed * Time.deltaTime;
            if (toTarget.magnitude <= step)
                break;

            position += toTarget.normalized * step;
            if (projectile != null)
                projectile.transform.position = position;
            yield return null;
        }

        SetActive(projectile, false);

        if (impact != null)
        {
            impact.transform.SetPositionAndRotation(hitPoint.position, Quaternion.LookRotation(-direction));
            impact.SetActive(true);
        }

        yield return new WaitForSeconds(skill.impactDuration);
        SetActive(impact, false);
    }

    void Restart(IEnumerator next)
    {
        if (routine != null)
            StopCoroutine(routine);

        HideAll();
        archer.ResetTrigger(AimTrigger);
        archer.ResetTrigger(ReleaseTrigger);
        archer.Play(IdleState, 0, 0f);
        routine = StartCoroutine(next);
    }

    void HideAll()
    {
        for (int i = 0; i < skills.Length; i++)
        {
            SetActive(castInstances[i], false);
            SetActive(projectileInstances[i], false);
            SetActive(impactInstances[i], false);
        }
    }

    static GameObject Spawn(GameObject prefab, Transform parent)
    {
        if (prefab == null)
            return null;

        GameObject instance = Instantiate(prefab, parent);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.SetActive(false);
        return instance;
    }

    static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}

using UnityEngine;

// Turn-based battle camera: cuts to one still locator per skill and holds it until the hit is over.
// To change a shot, select its locator and use GameObject > Align With View.
public class ShowreelCamera : MonoBehaviour
{
    [SerializeField] Transform wideShot;
    [SerializeField] float driftSpeed = 0.35f;
    [SerializeField] Transform[] skillShots = new Transform[5];

    Transform shot;
    float drift;

    // Awake, not Start: the showcase may request a shot in its own Start.
    void Awake()
    {
        ShowWide();
    }

    public void ShowSkill(int index)
    {
        if (index >= 0 && index < skillShots.Length)
            CutTo(skillShots[index]);
    }

    public void ShowWide()
    {
        CutTo(wideShot);
    }

    void CutTo(Transform next)
    {
        if (next == null)
            return;

        shot = next;
        drift = 0f;
        transform.SetPositionAndRotation(shot.position, shot.rotation);
    }

    void LateUpdate()
    {
        if (shot != wideShot || shot == null)
            return;

        drift += driftSpeed * Time.deltaTime;
        transform.position = shot.position + shot.right * drift;
    }
}

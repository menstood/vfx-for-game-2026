# VFX Showreel – Final Project

Open `Assets/Scenes/Showreel.unity` and press Play.
The archer fires 5 skills in a loop. Each skill has 3 VFX slots:

| Slot | When it plays | Orientation |
|---|---|---|
| `SkillXX_Cast` | On the bow while the archer aims (~1.2 s) | Z points at the target |
| `SkillXX_Projectile` | Flies from the bow to the dummy | Z points along the flight |
| `SkillXX_Impact` | At the hit point on the dummy (1.5 s) | Z points back at the archer |

## Your job

1. Open a prefab in `Assets/Showreel/Skills/SkillXX/`.
2. Build your VFX as a child of the prefab root (mesh + Shader Graph material, etc.).
3. Turn off (or delete) the `Placeholder` child when you're done.

The prefab is switched **on** every time it plays, so anything that starts on enable restarts each time:
an **Animator / Animation clip** on your prefab (animate scale, material properties, ...), Particle Systems (Play On Awake), VFX Graph.

## Controls (Play mode)

- `1`–`5`: play one skill
- `Space`: restart the auto loop

## Tweaking

Select **SkillShowcase** in the scene:
- `Aim Duration`: how long the cast VFX shows before release
- per skill: `Projectile Speed`, `Impact Duration`
- rename skills with `Skill Name`

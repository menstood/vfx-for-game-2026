using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Bakes the custom archer clips by blending Mixamo clips and editing their humanoid curves.
// Change the constants and re-run the menu item to re-bake.
public static class ShowcaseClipBaker
{
    const string ArcherDir = "Assets/Model/Archer/";
    const string AnimDir = "Assets/Showreel/Animation/";
    const float FrameRate = 30f;

    // ---------- Aerial Shot: Jump -> Aim (hang, tilted down) -> Recoil -> Landing ----------
    const float AerialAimStart = 0.85f;
    const float AerialRelease = 2.0f;
    const float AerialLandingStart = 2.4f;
    const float TakeOffTime = 0.4f;
    const float ApexTime = 0.9f;
    const float ApexHeight = 3.2f; // humanoid units (x avatar human scale ~1.08 in metres)
    const float HangRise = 0.2f;
    const float TouchDownTime = 2.85f;
    const float AerialTiltDegrees = 22f;

    // ---------- Sky Shot: Draw -> Aim (upper body bent back, bow up) -> Recoil ----------
    const float SkyAimStart = 1.0f;
    const float SkyRelease = 1.9f;
    // Added to each spine muscle; together they tilt the shoulders so the bow points up.
    const float SkySpineBend = 0.6f; // ~58 degrees bow pitch
    // Humanoid root orientation averages hips and chest, so bending the spine also rolls the hips.
    // This rolls the root back the other way to keep the feet planted.
    const float SkyRootRoll = -27f; // tuned with SkySpineBend so hips stay level
    const float SkyHeadLookUp = 0.5f;

    // ---------- Crouch Spin: CrouchDown with a full turn on the way down ----------
    const float SpinDegrees = 360f;
    const float SpinEnd = 0.7f;

    class Segment
    {
        public float outStart, srcStart, srcEnd, blendIn;
        public readonly Dictionary<EditorCurveBinding, AnimationCurve> curves = new();
    }

    [MenuItem("Tools/Showreel/Bake Aerial Shot")]
    public static void BakeAerialShot()
    {
        Segment[] segments =
        {
            CreateSegment("Jump", 0f, AerialAimStart, 0f),
            CreateSegment("Aim", AerialAimStart, AerialRelease - AerialAimStart, 0.15f),
            CreateSegment("Recoil", AerialRelease, AerialLandingStart - AerialRelease, 0.06f),
            CreateSegment("Landing", AerialLandingStart, 1.07f, 0.15f),
        };

        Dictionary<string, float[]> values = Sample(segments, out EditorCurveBinding[] bindings, out int frames);
        ApplyJumpArc(values["RootT.y"]);
        float[] tilt = Envelope(frames, 0.75f, 1.0f, 2.3f, 2.6f);
        ApplyRootRotation(values, Vector3.right, AerialTiltDegrees, tilt, local: false);
        Save("AerialShot", bindings, values, frames);
    }

    [MenuItem("Tools/Showreel/Bake Sky Shot")]
    public static void BakeSkyShot()
    {
        Segment[] segments =
        {
            CreateSegment("DrawArrow", 0f, SkyAimStart, 0f),
            CreateSegment("Aim", SkyAimStart, SkyRelease - SkyAimStart, 0.1f),
            CreateSegment("Recoil", SkyRelease, 0.7f, 0.06f),
        };

        Dictionary<string, float[]> values = Sample(segments, out EditorCurveBinding[] bindings, out int frames);
        float[] weight = Envelope(frames, 0.3f, 1.0f, 2.2f, 2.6f);
        AddMuscle(values, "Spine Left-Right", SkySpineBend, weight);
        AddMuscle(values, "Chest Left-Right", SkySpineBend, weight);
        AddMuscle(values, "UpperChest Left-Right", SkySpineBend, weight);
        AddMuscle(values, "Head Nod Down-Up", SkyHeadLookUp, weight);
        ApplyRootRotation(values, Vector3.forward, SkyRootRoll, weight, local: true);
        Save("SkyShot", bindings, values, frames);
    }

    [MenuItem("Tools/Showreel/Bake Crouch Spin")]
    public static void BakeCrouchSpin()
    {
        Segment[] segments = { CreateSegment("CrouchDown", 0f, 0.93f, 0f) };

        Dictionary<string, float[]> values = Sample(segments, out EditorCurveBinding[] bindings, out int frames);
        float[] spin = Envelope(frames, 0f, SpinEnd, float.MaxValue, float.MaxValue);
        ApplyRootRotation(values, Vector3.up, SpinDegrees, spin, local: false);
        Save("CrouchSpin", bindings, values, frames);
    }

    static Segment CreateSegment(string file, float outStart, float duration, float blendIn)
    {
        var segment = new Segment { outStart = outStart, srcStart = 0f, srcEnd = duration, blendIn = blendIn };
        AnimationClip clip = LoadClip(file);
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            segment.curves[binding] = AnimationUtility.GetEditorCurve(clip, binding);
        return segment;
    }

    static AnimationClip LoadClip(string file)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(ArcherDir + file + ".fbx"))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        throw new System.IO.FileNotFoundException("No clip in " + file);
    }

    static Dictionary<string, float[]> Sample(Segment[] segments, out EditorCurveBinding[] bindings, out int frames)
    {
        Segment last = segments[^1];
        float length = last.outStart + (last.srcEnd - last.srcStart);
        frames = Mathf.CeilToInt(length * FrameRate) + 1;

        bindings = new EditorCurveBinding[segments[0].curves.Count];
        segments[0].curves.Keys.CopyTo(bindings, 0);

        var values = new Dictionary<string, float[]>();
        foreach (EditorCurveBinding binding in bindings)
        {
            var samples = new float[frames];
            for (int f = 0; f < frames; f++)
                samples[f] = SampleBlended(segments, binding, f / FrameRate);
            values[binding.propertyName] = samples;
        }
        return values;
    }

    static float SampleBlended(Segment[] segments, EditorCurveBinding binding, float time)
    {
        int index = 0;
        for (int i = 1; i < segments.Length; i++)
        {
            if (time >= segments[i].outStart)
                index = i;
        }

        Segment current = segments[index];
        float value = Evaluate(current, binding, time);
        if (index == 0 || time >= current.outStart + current.blendIn)
            return value;

        float previous = Evaluate(segments[index - 1], binding, time);
        float weight = Mathf.SmoothStep(0f, 1f, (time - current.outStart) / current.blendIn);
        return Mathf.Lerp(previous, value, weight);
    }

    static float Evaluate(Segment segment, EditorCurveBinding binding, float time)
    {
        if (!segment.curves.TryGetValue(binding, out AnimationCurve curve))
            return 0f;
        float srcTime = Mathf.Clamp(segment.srcStart + time - segment.outStart, segment.srcStart, segment.srcEnd);
        return curve.Evaluate(srcTime);
    }

    // 0 before inStart, eases to 1 by inEnd, holds, eases back to 0 by outEnd.
    static float[] Envelope(int frames, float inStart, float inEnd, float outStart, float outEnd)
    {
        var weight = new float[frames];
        for (int f = 0; f < frames; f++)
        {
            float time = f / FrameRate;
            weight[f] = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inStart, inEnd, time))
                      * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(outStart, outEnd, time)));
        }
        return weight;
    }

    static void AddMuscle(Dictionary<string, float[]> values, string muscle, float amount, float[] weight)
    {
        float[] samples = values[muscle];
        for (int f = 0; f < samples.Length; f++)
            samples[f] = Mathf.Clamp(samples[f] + amount * weight[f], -1f, 1f);
    }

    static void ApplyJumpArc(float[] height)
    {
        int takeOff = Mathf.RoundToInt(TakeOffTime * FrameRate);
        int touchDown = Mathf.RoundToInt(TouchDownTime * FrameRate);

        var arc = new AnimationCurve(
            new Keyframe(TakeOffTime, height[takeOff]),
            new Keyframe(ApexTime, ApexHeight),
            new Keyframe(AerialRelease, ApexHeight + HangRise),
            new Keyframe(TouchDownTime, height[touchDown]));
        for (int k = 0; k < arc.length; k++)
            arc.SmoothTangents(k, 0f);

        for (int f = takeOff; f <= touchDown; f++)
            height[f] = arc.Evaluate(f / FrameRate);
    }

    // local: rotate around the body's own axis; otherwise around the character's (world-facing) axis.
    static void ApplyRootRotation(Dictionary<string, float[]> values, Vector3 axis, float degrees, float[] weight, bool local)
    {
        float[] x = values["RootQ.x"], y = values["RootQ.y"], z = values["RootQ.z"], w = values["RootQ.w"];
        Quaternion previous = Quaternion.identity;
        for (int f = 0; f < x.Length; f++)
        {
            Quaternion q = Normalize(new Quaternion(x[f], y[f], z[f], w[f]));
            Quaternion r = Quaternion.AngleAxis(degrees * weight[f], axis);
            q = local ? q * r : r * q;

            // Keep quaternions in the same hemisphere so keys interpolate the short way.
            if (Quaternion.Dot(previous, q) < 0f)
                q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
            previous = q;

            x[f] = q.x; y[f] = q.y; z[f] = q.z; w[f] = q.w;
        }
    }

    static Quaternion Normalize(Quaternion q)
    {
        float m = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        return m > 0f ? new Quaternion(q.x / m, q.y / m, q.z / m, q.w / m) : Quaternion.identity;
    }

    static void Save(string name, EditorCurveBinding[] bindings, Dictionary<string, float[]> values, int frames)
    {
        string path = AnimDir + name + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.ClearCurves();
        clip.frameRate = FrameRate;

        var curves = new AnimationCurve[bindings.Length];
        for (int i = 0; i < bindings.Length; i++)
        {
            float[] samples = values[bindings[i].propertyName];
            var keys = new Keyframe[frames];
            for (int f = 0; f < frames; f++)
                keys[f] = new Keyframe(f / FrameRate, samples[f]);
            curves[i] = new AnimationCurve(keys);
        }
        AnimationUtility.SetEditorCurves(clip, bindings, curves);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.loopBlendOrientation = true;
        settings.keepOriginalOrientation = true;
        settings.loopBlendPositionY = true;
        settings.keepOriginalPositionY = true;
        settings.loopBlendPositionXZ = true;
        settings.keepOriginalPositionXZ = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        Debug.Log($"Baked {path} ({(frames - 1) / FrameRate:F2}s)");
    }
}

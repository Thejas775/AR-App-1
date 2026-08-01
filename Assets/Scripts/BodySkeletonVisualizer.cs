using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws whatever OVRSkeleton is tracking as joint spheres + bone lines.
///
/// OVRSkeletonRenderer only accepts a hand data provider (OVRSkeleton does not
/// implement IOVRSkeletonRendererDataProvider), so body skeletons need their own
/// visualiser. This is deliberately dumb: it exists to let you judge tracking
/// fidelity before committing to an art pipeline.
/// </summary>
[RequireComponent(typeof(OVRSkeleton))]
public class BodySkeletonVisualizer : MonoBehaviour
{
    [Header("Look")]
    public Material material;
    public Color color = new Color(0.20f, 1f, 0.35f);
    public float jointRadius = 0.018f;
    public float boneWidth = 0.008f;

    [Header("Debug")]
    public bool logWhenTrackingStarts = true;

    private OVRSkeleton skeleton;
    private readonly List<Transform> jointVisuals = new List<Transform>();
    private readonly List<LineRenderer> boneVisuals = new List<LineRenderer>();
    private readonly List<int> boneFrom = new List<int>();
    private readonly List<int> boneTo = new List<int>();

    private bool built;
    private bool visible = true;
    private bool loggedOnce;

    public int JointCount => jointVisuals.Count;
    public bool IsTracking => skeleton != null && skeleton.IsInitialized && skeleton.IsDataValid;

    void Awake()
    {
        skeleton = GetComponent<OVRSkeleton>();
    }

    void LateUpdate()
    {
        if (!IsTracking)
        {
            SetVisible(false);
            return;
        }

        if (!built)
            Build();

        SetVisible(true);

        for (int i = 0; i < boneVisuals.Count; i++)
        {
            var a = skeleton.Bones[boneFrom[i]].Transform;
            var b = skeleton.Bones[boneTo[i]].Transform;
            if (a == null || b == null)
                continue;

            boneVisuals[i].SetPosition(0, a.position);
            boneVisuals[i].SetPosition(1, b.position);
        }
    }

    private void Build()
    {
        var bones = skeleton.Bones;
        if (bones == null || bones.Count == 0)
            return;

        var mat = material != null ? material : MakeFallbackMaterial();

        for (int i = 0; i < bones.Count; i++)
        {
            var bone = bones[i];
            if (bone.Transform == null)
                continue;

            // Joint marker - parented to the bone so it follows for free.
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Joint_" + bone.Id;
            Destroy(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(bone.Transform, false);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * (jointRadius * 2f);
            sphere.GetComponent<Renderer>().sharedMaterial = mat;
            jointVisuals.Add(sphere.transform);

            // Bone segment back to the parent joint.
            int parent = bone.ParentBoneIndex;
            if (parent < 0 || parent >= bones.Count || bones[parent].Transform == null)
                continue;

            var lineGO = new GameObject("Bone_" + bone.Id);
            lineGO.transform.SetParent(transform, false);

            var line = lineGO.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = boneWidth;
            line.endWidth = boneWidth;
            line.sharedMaterial = mat;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            boneVisuals.Add(line);
            boneFrom.Add(parent);
            boneTo.Add(i);
        }

        built = true;

        if (logWhenTrackingStarts && !loggedOnce)
        {
            loggedOnce = true;
            Debug.Log("BodySkeletonVisualizer: tracking " + bones.Count + " joints, drew "
                      + boneVisuals.Count + " bones (skeleton type " + skeleton.GetSkeletonType() + ").");
        }
    }

    private Material MakeFallbackMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var m = new Material(shader);
        m.SetColor("_BaseColor", color);
        m.SetColor("_Color", color);
        return m;
    }

    private void SetVisible(bool value)
    {
        if (visible == value)
            return;

        visible = value;

        foreach (var j in jointVisuals)
            if (j != null) j.gameObject.SetActive(value);

        foreach (var b in boneVisuals)
            if (b != null) b.gameObject.SetActive(value);
    }
}

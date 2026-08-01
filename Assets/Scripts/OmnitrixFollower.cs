using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Keeps the Omnitrix strapped to the forearm.
///
/// Prefers the RAW tracked hand pose over the hand visual's wrist transform.
/// The visual skeleton is driven by Meta's SyntheticHand, which grab/poke
/// interactions are allowed to displace - that is why the watch used to shift
/// when you closed your fist near it. The raw pose is never touched by
/// interaction logic.
/// </summary>
public class OmnitrixFollower : MonoBehaviour
{
    [Header("Anchor")]
    [Tooltip("Fallback wrist joint transform. Used when no raw hand source is available.")]
    public Transform wrist;

    [Tooltip("Raw tracked hand (LastKnownGoodHand on OVRHandDataSourceLeft). Preferred.")]
    public MonoBehaviour rawHandSource;

    [Header("Offsets (wrist space: +Y back of hand, -Z up the forearm)")]
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    [Header("Stabilisation")]
    [Tooltip("0 = rigid. 20-30 damps tracking jitter with no lag you can feel.")]
    public float smoothing = 25f;

    private MethodInfo getRootPose;
    private readonly object[] poseArgs = new object[1];

    private bool hasLast;
    private Vector3 lastPos;
    private Quaternion lastRot;

    public bool UsingRawHand => getRootPose != null;

    void Awake()
    {
        CacheRawHand();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            CacheRawHand();
    }

    private void CacheRawHand()
    {
        getRootPose = null;

        if (rawHandSource == null)
            return;

        // Resolve through the interface so explicit implementations still bind.
        var iHand = rawHandSource.GetType()
            .GetInterfaces()
            .FirstOrDefault(i => i.FullName == "Oculus.Interaction.Input.IHand");

        if (iHand == null)
        {
            Debug.LogWarning("OmnitrixFollower: " + rawHandSource.GetType().Name +
                             " does not implement IHand - falling back to the wrist transform.", this);
            return;
        }

        getRootPose = iHand.GetMethod("GetRootPose");
    }

    private bool TryGetWristPose(out Vector3 pos, out Quaternion rot)
    {
        if (getRootPose != null)
        {
            poseArgs[0] = null;
            bool valid = (bool)getRootPose.Invoke(rawHandSource, poseArgs);

            if (valid && poseArgs[0] is Pose pose)
            {
                pos = pose.position;
                rot = pose.rotation;
                return true;
            }
            // Tracking lost this frame - hold the last good pose.
            if (hasLast)
            {
                pos = lastPos;
                rot = lastRot;
                return true;
            }
        }

        if (wrist != null)
        {
            pos = wrist.position;
            rot = wrist.rotation;
            return true;
        }

        pos = Vector3.zero;
        rot = Quaternion.identity;
        return false;
    }

    void LateUpdate()
    {
        if (!TryGetWristPose(out var wristPos, out var wristRot))
            return;

        var targetPos = wristPos + (wristRot * positionOffset);
        var targetRot = wristRot * Quaternion.Euler(rotationOffset);

        if (smoothing > 0f && hasLast)
        {
            // Framerate-independent exponential smoothing.
            float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            targetPos = Vector3.Lerp(lastPos, targetPos, t);
            targetRot = Quaternion.Slerp(lastRot, targetRot, t);
        }

        transform.SetPositionAndRotation(targetPos, targetRot);

        lastPos = targetPos;
        lastRot = targetRot;
        hasLast = true;
    }
}

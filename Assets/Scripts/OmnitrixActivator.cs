using UnityEngine;

/// <summary>
/// Drives the Omnitrix pop-up animation. The clip it plays (OmnitrixActivate)
/// is a merge of the FBX's "Empty|EmptyAction" and "head|EmptyAction" actions,
/// so the pivot and the head animate together in one pass.
/// </summary>
public class OmnitrixActivator : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;
    public string triggerName = "Activate";

    [Header("Behaviour")]
    public bool ignoreWhilePlaying = true;

    private float busyUntil;

    public bool IsPlaying => Time.time < busyUntil;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void Activate()
    {
        if (animator == null)
        {
            Debug.LogWarning("OmnitrixActivator: no Animator assigned.", this);
            return;
        }

        if (ignoreWhilePlaying && IsPlaying)
            return;

        busyUntil = Time.time + LongestClipLength();
        animator.SetTrigger(triggerName);

        Debug.Log("Omnitrix Activated!");
    }

    private float LongestClipLength()
    {
        var controller = animator.runtimeAnimatorController;
        if (controller == null)
            return 0f;

        float longest = 0f;
        foreach (var clip in controller.animationClips)
            longest = Mathf.Max(longest, clip.length);

        return longest;
    }
}

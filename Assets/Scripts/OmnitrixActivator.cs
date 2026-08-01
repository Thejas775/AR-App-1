using System.Collections;
using UnityEngine;

/// <summary>
/// Omnitrix open/close cycle.
///
/// Poke a bead  -> the dial pops up and STAYS up.
/// Slam your palm onto it -> it retracts and the transform sound fires.
///
/// The palm check is armed with hysteresis: after opening, your hand is still
/// right next to the watch (you just poked it), so the slam only becomes live
/// once the palm has first moved away past 'rearmDistance'.
/// </summary>
public class OmnitrixActivator : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;
    public string openTrigger = "Open";
    public string closeTrigger = "Close";

    [Header("Palm slam")]
    [Tooltip("Right hand palm joint.")]
    public Transform palm;
    [Tooltip("The watch face - Empty/head.")]
    public Transform watchHead;
    [Tooltip("Palm this close to the dial counts as a slam.")]
    public float slamDistance = 0.06f;
    [Tooltip("Palm must first get this far away before a slam can register.")]
    public float rearmDistance = 0.15f;

    [Header("Sound")]
    public AudioSource transformSound;

    public bool IsOpen { get; private set; }
    public bool IsClosing { get; private set; }
    public bool SlamArmed { get; private set; }

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    /// <summary>Called by BeadTouch. Pops the dial up and leaves it up.</summary>
    public void Activate()
    {
        if (animator == null)
        {
            Debug.LogWarning("OmnitrixActivator: no Animator assigned.", this);
            return;
        }

        if (IsOpen || IsClosing)
            return;

        animator.SetTrigger(openTrigger);

        IsOpen = true;
        SlamArmed = false;

        Debug.Log("Omnitrix opened - slam your palm on it to transform.");
    }

    void Update()
    {
        if (!IsOpen || IsClosing)
            return;

        if (palm == null || watchHead == null)
            return;

        float distance = Vector3.Distance(palm.position, watchHead.position);

        if (!SlamArmed)
        {
            // Wait until the hand has cleared the watch before listening for a slam.
            if (distance > rearmDistance)
                SlamArmed = true;
            return;
        }

        if (distance <= slamDistance)
            Close();
    }

    /// <summary>Retracts the dial and plays the transform sound.</summary>
    public void Close()
    {
        if (!IsOpen || IsClosing)
            return;

        IsClosing = true;
        animator.SetTrigger(closeTrigger);

        if (transformSound != null)
            transformSound.Play();

        Debug.Log("Omnitrix slammed - transforming!");

        StartCoroutine(FinishClose());
    }

    private IEnumerator FinishClose()
    {
        yield return new WaitForSeconds(CloseClipLength());

        IsOpen = false;
        IsClosing = false;
        SlamArmed = false;
    }

    private float CloseClipLength()
    {
        var controller = animator != null ? animator.runtimeAnimatorController : null;
        if (controller == null)
            return 0.3f;

        foreach (var clip in controller.animationClips)
            if (clip.name.Contains("Close"))
                return clip.length;

        return 0.3f;
    }
}

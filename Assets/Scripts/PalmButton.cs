using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A poke button that rides on a hand joint. Same interaction model as
/// <see cref="BeadTouch"/>: the fingertip collider enters the trigger,
/// the button dips inward, then springs back.
/// </summary>
public class PalmButton : MonoBehaviour
{
    [Header("Trigger")]
    public string fingerTipName = "XRHand_IndexTip";
    public float cooldown = 1f;

    [Header("Effects")]
    public AudioSource clickSound;
    public float pressDistance = 0.004f;
    public float popSpeed = 15f;
    public float resetDelay = 0.15f;

    [Header("Action")]
    public PassthroughSceneToggle sceneToggle;
    public UnityEvent onPressed;

    private Vector3 startPos;
    private Vector3 targetPos;
    private float readyAt;

    void Start()
    {
        startPos = transform.localPosition;
        targetPos = startPos;
    }

    void Update()
    {
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPos,
            Time.deltaTime * popSpeed);
    }

    void OnTriggerEnter(Collider other)
    {
        if (Time.time < readyAt)
            return;

        if (other.name != fingerTipName)
            return;

        readyAt = Time.time + cooldown;
        StartCoroutine(PressButton());
    }

    IEnumerator PressButton()
    {
        targetPos = startPos - transform.up * pressDistance;

        if (clickSound != null)
            clickSound.Play();

        if (sceneToggle != null)
            sceneToggle.Toggle();

        onPressed.Invoke();

        Debug.Log("Palm Button Pressed!");

        yield return new WaitForSeconds(resetDelay);

        targetPos = startPos;
    }
}

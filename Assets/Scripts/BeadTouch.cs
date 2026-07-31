using UnityEngine;
using System.Collections;

public class BeadTouch : MonoBehaviour
{
    [Header("Effects")]
    public AudioSource clickSound;

    [Header("Omnitrix")]
    public OmnitrixActivator omnitrix;

    [Header("Animation")]
    public float pressDistance = 0.003f;
    public float popSpeed = 15f;
    public float resetDelay = 0.15f;

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool pressed;

    void Start()
    {
        startPos = transform.localPosition;
        targetPos = startPos;

        if (omnitrix == null)
            omnitrix = GetComponentInParent<OmnitrixActivator>();
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
        if (pressed) return;

        if (other.name == "XRHand_IndexTip")
        {
            StartCoroutine(PressButton());
        }
    }

    IEnumerator PressButton()
    {
        pressed = true;

        // Press inward
        targetPos = startPos - transform.up * pressDistance;

        if (clickSound != null)
            clickSound.Play();

        if (omnitrix != null)
            omnitrix.Activate();

        Debug.Log("Bead Pressed!");

        yield return new WaitForSeconds(resetDelay);

        // Return outward
        targetPos = startPos;

        yield return new WaitForSeconds(0.2f);

        pressed = false;
    }
}
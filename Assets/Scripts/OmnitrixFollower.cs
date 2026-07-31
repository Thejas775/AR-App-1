using UnityEngine;

public class OmnitrixFollower : MonoBehaviour
{
    public Transform wrist;

    [Header("Offsets")]
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    void LateUpdate()
    {
        if (wrist == null)
            return;

        transform.position = wrist.TransformPoint(positionOffset);

        transform.rotation =
            wrist.rotation *
            Quaternion.Euler(rotationOffset);
    }
}
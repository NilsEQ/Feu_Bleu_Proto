using UnityEngine;

public class LockJaw : MonoBehaviour
{
    private Transform jaw;
    private Vector3 originalPosition;

    void Start()
    {
        jaw = GetComponent<Transform>();
        originalPosition = jaw.localPosition;
        originalPosition = new Vector3(-0.024f, originalPosition.y, originalPosition.z);
    }

    void LateUpdate()
    {
        if (jaw != null)
            jaw.localPosition = originalPosition;
    }

}

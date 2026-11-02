using UnityEngine;

public class LockJaw : MonoBehaviour
{
    private Transform jaw;
    private Vector3 originalPosition;

    void Start()
    {
        jaw = GetComponent<Transform>();
        originalPosition = jaw.localPosition;
    }

    void LateUpdate()
    {
        if (jaw != null)
            jaw.localPosition = originalPosition;
    }

}

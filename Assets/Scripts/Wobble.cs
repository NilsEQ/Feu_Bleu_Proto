using UnityEngine;

public class Wobble : MonoBehaviour
{
    public float wobbleSpeed = 2f;
    public float wobbleAmount = 0.5f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float wobble = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount;

        rb.AddTorque(
            transform.forward * wobble,
            ForceMode.Force
        );
    }
}
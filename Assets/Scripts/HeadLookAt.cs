using UnityEngine;
using UnityEngine.UIElements;

public class HeadLookAt : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject Player;

    public bool todo = true;
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (todo)
        {
            Vector3 LookAt = Player.transform.position - transform.position;
            transform.rotation = Quaternion.LookRotation(LookAt);
        }
    }
}

using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class WatchPlayer : MonoBehaviour
{

    Transform myTransform;

    public GameObject Player;

    public bool todo = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        myTransform = transform;
    }

    // Update is called once per frame
    void Update()
    {

        if (todo) 
        {
            Vector3 LookAt = Player.transform.position - myTransform.position;
            LookAt = new Vector3(LookAt.x, 0, LookAt.z);
            transform.rotation = Quaternion.LookRotation(LookAt);
        }


    }
}

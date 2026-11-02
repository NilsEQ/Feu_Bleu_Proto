using UnityEngine;

public class CheckSize : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Renderer rend = GetComponent<Renderer>();

        Vector3 size = rend.bounds.size;

        Debug.Log("World size: " + size);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

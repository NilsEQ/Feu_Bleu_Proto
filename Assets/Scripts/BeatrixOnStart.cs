using UnityEngine;

public class BeatrixOnStart : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public BeatrixBehavior BeatrixBehavior;
    void Start()
    {
        BeatrixBehavior.Moment10();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

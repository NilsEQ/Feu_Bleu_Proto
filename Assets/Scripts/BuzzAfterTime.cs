using UnityEngine;
using System.Collections;

public class BuzzAfterTime : MonoBehaviour
{

    public float timer = 10;

    public WatchInteraction myWatch;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(PlayIfWaiting());
    }

    // Update is called once per frame
    private IEnumerator PlayIfWaiting()
    {
        yield return new WaitForSeconds(timer);
        if (!myWatch.active)
        {
            myWatch.alarm();
        }
    }

}

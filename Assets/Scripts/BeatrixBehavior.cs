using UnityEngine;
using UnityEngine.Audio;
using System.Collections;




public class BeatrixBehavior : MonoBehaviour
{
    public WatchPlayer WatchHim;

    [Header("Moment 1")]


    public Vector3 FirstPosition;

    public Vector3 LookAt;

    public NextMomentOnFinish Sound1;



    [Header("Moment 2")]

    public AudioPlay Sound2;

    public bool StillWaiting;

    public AudioPlay Cmon;

    [Header("Moment 3")]

    public Vector3 Target;

    public AudioPlay Sound3;
    public NextMomentOnFinish Sound3Bis;

    public HeadLookAt HeadLocker;


    [Header("Moment 5")]
    public AudioPlay Sound4;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void Moment1()
    {
        transform.position = new Vector3(FirstPosition.x,transform.position.y,FirstPosition.z);
        transform.rotation = Quaternion.LookRotation(LookAt);


        Sound1.PlaySound();

    }


    public void Moment2()
    {

        Sound2.PlaySound();
        StartCoroutine(PlayIfWaiting());
    }

    private IEnumerator PlayIfWaiting()
    {


        yield return new WaitForSeconds(20f);

        if (StillWaiting) {
            Cmon.PlaySound();
        }

        while (StillWaiting)
        {
            yield return null;
        }

        Cmon.StopSound();


    }

    public void Moment3()
    {
        Target = new Vector3(Target.x, transform.position.y, Target.z);
        transform.position = Target;

        Sound2.StopSound();
        Sound3Bis.PlaySound();


        WatchHim.todo = false;
        HeadLocker.todo = false;
    }

    public void Moment5()
    {
        Sound4.PlaySound();
    }


}

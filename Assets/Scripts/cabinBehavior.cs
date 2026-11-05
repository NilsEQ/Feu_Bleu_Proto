using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class cabinBehavior : MonoBehaviour
{

    public enum CabinState
    {
        Free,
        Occupied,
        Playing
    }



    public CabinState State;

    private GameObject GameZone;

    private Animator myAnimator;

    public AudioSource Lock;

    public AudioSource SlamSound;


    public cabinBehavior nextCabin;


    public GraffitiLight Graf1;
    public GraffitiLight Graf2;
    public GraffitiLight Graf3;

    public GameObject warning;

    public void Start()
    {

        
        myAnimator = GetComponent<Animator>();
        GameZone = transform.Find("GameZone").gameObject;
        GameZone.SetActive(false);

        if (State == CabinState.Free)
        {
            myAnimator.Play("CabinIdle");
        }
        else if (State == CabinState.Occupied)
        {
            myAnimator.Play("CabinIdleClosed");
        }
        
    }
    public void Moment2() { 
        if (State == CabinState.Free)
        {
            GameZone.SetActive(true);
        }
    }

    public void chosen()
    {
        State = CabinState.Playing;
        myAnimator.Play("CabinClose");
        StartCoroutine(LockSound());
        StartCoroutine(Warning());


    }
    private IEnumerator Warning()
    {
        yield return new WaitForSeconds(2f);
        warning.SetActive(false);
    }

    public void Done()
    {
        warning.SetActive(false);
        State = CabinState.Free;
        Lock.Play();
        myAnimator.Play("CabinOpen");
    }


    public void nextOne()
    {
        myAnimator.Play("CabinClose");
        StartCoroutine(LockSound());
    }


    public void nextOneOpen()
    {
        Lock.Play();
        myAnimator.Play("CabinOpen");

    }

    private IEnumerator LockSound()
    {
        yield return new WaitForSeconds(0.5f);
        Lock.Play();
    }

    public void Slam()
    {
        myAnimator.Play("Secousse");
        SlamSound.Play();

        Graf1.EnableEmission();
        Graf2.EnableEmission();
        Graf3.EnableEmission();

    }
}

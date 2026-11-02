using UnityEngine;
using UnityEngine.SceneManagement;

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


    }

}

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;

public class StoryProgression : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Global Stuff
    public int n_Moments;

    public int current_Moment;

    public WatchInteraction WatchScript;

    public GameObject player;


    public GameObject BeatrixObj;

    public BeatrixBehavior Beatrix;


    // Moment 1
    [Header("Moment 1")]

    public GameObject GUI_Beginning;

    public GameObject OnBoarding;

    public GameObject MainScene;


    //Moment 2

    [Header("Moment 2")]


    public GameObject DialBeatrix2;

    public GameObject Chatter;

    public GameObject Cabins;

    private List<GameObject> ListOfCabins;


    //Moment 3

    [Header("Moment 3")]


    public GameObject myCabin;
    public cabinBehavior CabinBeh;

    public cabinBehavior NextCabin;



    // Moment 4
    [Header("Moment 4")]

    public PlayableDirector CliqueDirector;


    // Moment 5
    [Header("Moment 5")]

    public GameObject GUI_warning;

    [Header("Moment 6")]

    public GameObject paper;

    public float PaperTime = 30.0F;

    public AdvanceWhenCloseOnce PleaseClose;


[Header("Moment 7")]

    public GameObject Clique;

    public GameObject Fight;

    public PlayableDirector FightDirector;


    [Header("Moment 9")]

    public GameObject BeatrixFight;

    [Header("Moment 10")]

    public GameObject GUI_Final;


    [Header("Moment 11")]

    public PlayableDirector EndingDirector;

    public AudioPlay BeaLastWords;





    void Start()
    {
        current_Moment = 0;

        ListOfCabins = new List<GameObject>();

        foreach (Transform child in Cabins.transform)
        {
            ListOfCabins.Add(child.gameObject);
            cabinBehavior cab = child.gameObject.GetComponent<cabinBehavior>();
        }
    }

    public void Advance()
    {
        current_Moment += 1;

        if (current_Moment == 1)
        {
            OnBoarding.SetActive(false);
            MainScene.SetActive(true);


            Beatrix.FirstPosition = player.transform.position + Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f)*new Vector3(0, 0, 1f);
            Beatrix.LookAt = player.transform.position - Beatrix.FirstPosition;
            Beatrix.LookAt.y = 0f;

            Beatrix.Moment1();

            UpdateWatch(GUI_Beginning);
        }


        if (current_Moment == 2)
        {
            foreach (GameObject Cabin in ListOfCabins) 
            {
                cabinBehavior behav = Cabin.GetComponent<cabinBehavior>();
                if (behav.State == cabinBehavior.CabinState.Free) behav.Moment2();
            }

            Beatrix.Moment2();
            Beatrix.StillWaiting = true;

            Chatter.SetActive(true);


        }


        if (current_Moment == 3)
        {
            CabinBeh = myCabin.GetComponent<cabinBehavior>();
            NextCabin = CabinBeh.nextCabin;

            Debug.Log("MOMENT 3 IS HERE BABY");

            Beatrix.StillWaiting = false;
            Beatrix.Target = myCabin.transform.position + new Vector3(0.5f,0, 1.25577f/2f);

            StartCoroutine(Moment3Coroutine());

            Beatrix.Moment3();

            Chatter.SetActive(false);


            foreach (GameObject Cabin in ListOfCabins)
            {
                GameObject GameZone = Cabin.transform.Find("GameZone").gameObject;
                Collider col = GameZone.GetComponent<Collider>();


                if (col != null)
                {
                    col.enabled = false;
                }

            }


        }





        if (current_Moment == 4)
        {

            CliqueDirector.Play();
            //director.playableGraph.GetRootPlayable(0).SetSpeed(10f);

        }


        if (current_Moment == 5)
        {

            Debug.Log("MOMENT 5 IS HERE BABY");


            UpdateWatch(GUI_warning);
            WatchScript.alarm();

            StartCoroutine(Moment5Coroutine());

        }


        if (current_Moment == 6)
        {

            Debug.Log("MOMENT 6 IS HERE BABY");
            Beatrix.Moment5();

            StartCoroutine(WaitPaper());
            StartCoroutine(WaitForNext());


        }


        if (current_Moment == 7)
        {

            Debug.Log("MOMENT 7 IS HERE BABY");
            Clique.SetActive(false);
            BeatrixObj.SetActive(false);
            Fight.SetActive(true);

            Fight.transform.position = myCabin.transform.position + new Vector3(-1F, 0, -0.5f);

            FightDirector.Play();
        }


        if (current_Moment == 8)
        {
            Beatrix.transform.position = BeatrixFight.transform.position;
            BeatrixObj.SetActive(true);
            Fight.SetActive(false);

            Beatrix.Moment8();

        }


        if (current_Moment == 9)
        {
            CabinBeh.Done();

            Beatrix.Moment9();

        }



        if (current_Moment == 10)
        {

            UpdateWatch(GUI_Final);
            WatchScript.alarm();
            StartCoroutine(Moment10Coroutine());

        }

        if (current_Moment == 11)
        {

            EndingDirector.Play();
            BeaLastWords.StopSound();
        }

    }



    public void UpdateWatch(GameObject newGUI)
    {
        WatchScript.deactivate();
        WatchScript.ChangeSettings(newGUI);
    }


    private IEnumerator Moment3Coroutine()
    {
        yield return new WaitForSeconds(1.5f);

        NextCabin.nextOne();

    }

    private IEnumerator Moment5Coroutine()
    {

        Debug.Log("Coroutine Start");
        yield return new WaitForSeconds(90f);

        if (!PleaseClose.Once)
        {
            PleaseClose.Once = true;
            WatchScript.deactivate();
            Advance();
        }

    }

    private IEnumerator Moment10Coroutine()
    {

        yield return new WaitForSeconds(90f);
        WatchScript.deactivate();
        Advance();

    }

    private IEnumerator WaitPaper()
    {
        yield return new WaitForSeconds(2);
        paper.transform.position = myCabin.transform.position + new Vector3(0.4f, 0.05f, -0.4f);
        paper.SetActive(true);

    }


    private IEnumerator WaitForNext()
    {
        yield return new WaitForSeconds(PaperTime);
        Advance();

    }

    public void Crash()
    {
        CabinBeh.Slam();
    }


    public void OpenNextCabin()
    {
        NextCabin.nextOneOpen();
    }

}

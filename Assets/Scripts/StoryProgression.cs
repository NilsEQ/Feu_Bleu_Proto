using System.Collections.Generic;
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



    // Moment 4
    [Header("Moment 4")]

    public PlayableDirector director;


    // Moment 5
    [Header("Moment 5")]

    public GameObject GUI_warning;

    [Header("Moment X")]

    public GameObject paper;

    public 


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



            Debug.Log("MOMENT 3 IS HERE BABY");

            Beatrix.StillWaiting = false;
            Beatrix.Target = myCabin.transform.position + new Vector3(0.5f,0, 1.25577f/2f);
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

            director.Play();
            //director.playableGraph.GetRootPlayable(0).SetSpeed(10f);

        }


        if (current_Moment == 5)
        {

            Debug.Log("MOMENT 5 IS HERE BABY");


            UpdateWatch(GUI_warning);
            WatchScript.alarm();

        }


        if (current_Moment == 6)
        {

            Debug.Log("MOMENT 6 IS HERE BABY");

            paper.transform.position = myCabin.transform.position + new Vector3(0.4f, 0.05f, -0.4f);
            paper.SetActive(true);
            Beatrix.Moment5();

        }


        if (current_Moment == 7)
        {

            Debug.Log("MOMENT 7 IS HERE BABY");


        }


    }



    public void UpdateWatch(GameObject newGUI)
    {
        WatchScript.deactivate();
        WatchScript.ChangeSettings(newGUI);
    }

}

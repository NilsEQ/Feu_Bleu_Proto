using UnityEngine;
using System.Collections.Generic;

public class WatchInteraction : MonoBehaviour
{

    public bool active = false;

    public GameObject GUI;
    public GameObject StartPage;
    public GameObject Player;


    private Transform GUI_Transform;
    private Transform Player_Transform;

    private List<GameObject> Screens;


    public Animator watchLight;

    public GameObject Buzz;






    void Start()
    {

        active = false;

        Player_Transform = Player.GetComponent<Transform>();

        GUI_Transform = GUI.GetComponent<Transform>();




        Screens = new List<GameObject>();

        foreach (Transform child in GUI.transform)
        {
            Screens.Add(child.gameObject);
        }

    }


    private void OnTriggerEnter(Collider other)
    {


        if (other.CompareTag("FingerTip"))
        {
            Pressed();
        }
    }

    public void Pressed()
    {

        Buzz.SetActive(false);


        Debug.Log("Finger touched the trigger!");

        Debug.Log(GUI_Transform.gameObject.name);
        // Put your interaction here.

        active = !active;

        if (active)
        {
            GUI_Transform.parent.eulerAngles = new Vector3(0, Player_Transform.eulerAngles.y, 0);
            GUI_Transform.parent.position = Player_Transform.position + Quaternion.Euler(0f, Player_Transform.eulerAngles.y, 0f) * new Vector3(0, 0, 0.3f) - new Vector3(0, 0.2f, 0);
            GUI.SetActive(true);
            StartPage.SetActive(true);

            watchLight.Play("Active");


        }

        else
        {

            foreach (GameObject Screen in Screens)
            {
                if (Screen.name != "QuitAnyTime")
                {
                    Screen.SetActive(false);
                }

            }

            GUI.SetActive(false);
            watchLight.Play("Idle");


        }

    }



    public void ChangeSettings(GameObject newGUI) {
        GUI = newGUI;
        GUI_Transform = GUI.GetComponent<Transform>();
        GameObject newStartPage = GUI_Transform.Find("SplashScreen").gameObject;
        StartPage = newStartPage;

        Screens = new List<GameObject>();

        foreach (Transform child in GUI.transform)
        {
            Screens.Add(child.gameObject);
        }

    }

    public void deactivate()
    {
        active = false;

        foreach (GameObject Screen in Screens)
        {
            if (Screen.name != "QuitAnyTime")
            {
                Screen.SetActive(false);
            }
        }

        GUI.SetActive(false);
        watchLight.Play("Idle");
        Buzz.SetActive(false);


        
    }

    public void alarm()
    {
        active = false;

        foreach (GameObject Screen in Screens)
        {
            if (Screen.name != "QuitAnyTime")
            {
                Screen.SetActive(false);
            }
        }

        GUI.SetActive(false);


        Buzz.SetActive(true);
        watchLight.Play("Alarm");
    }


}

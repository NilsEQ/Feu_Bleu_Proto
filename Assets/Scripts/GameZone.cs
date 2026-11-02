using UnityEngine;
using UnityEngine.SceneManagement;


public class GameZone : MonoBehaviour
{

    public float requiredTime = 1f;

    private float timer = 0f;
    private bool isInside = false;

    public StoryProgression myStory;
    public cabinBehavior beh;


    private void Start()
    {
        Scene scene = SceneManager.GetActiveScene();


        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "StoryManager")
                myStory= root.GetComponent<StoryProgression>();
        }

        beh = GetComponentInParent<cabinBehavior>();


    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            isInside = true;
            timer = 0f;

            Debug.Log("InCabin");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("MainCamera") && isInside)
        {
            timer += Time.deltaTime;

            if (timer >= requiredTime)
            {
                Debug.Log("Player stayed inside for 1 seconds!");

                Advance();

                isInside = false; // Trigger only once
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            isInside = false;
            timer = 0f;
        }
    }


    public void Advance()
    {


        Debug.Log("You have advanced");
        beh.chosen();

        myStory.myCabin = transform.parent.gameObject;
        myStory.Advance();
    }


}

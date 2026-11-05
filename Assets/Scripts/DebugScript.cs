using UnityEngine;
using UnityEngine.InputSystem;

public class DebugScript : MonoBehaviour
{

    public WatchInteraction MyWatch;
    public GameObject toActivate;

    public ForTheEnd Ending;


    public StoryProgression Story;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        Debug.Log("HIVEBET CONFIG FOLDER: " + Application.persistentDataPath);

        // Your existing code continues here...
    }
    void Start()
    {
        toActivate.SetActive(true);

    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("Space key pressed!");
            MyWatch.Pressed();
        }

        if (Keyboard.current != null &&
            Keyboard.current.enterKey.wasPressedThisFrame)
        {
            Debug.Log("Enter key pressed!");
            Story.Advance();
        }

        if (Keyboard.current != null &&
            Keyboard.current.bKey.wasPressedThisFrame)
        {
            Debug.Log("b key pressed!");
            Ending.ChangeColor();
        }


        if (Keyboard.current != null &&
            Keyboard.current.nKey.wasPressedThisFrame)
        {
            Debug.Log("n key pressed!");
            Ending.ReturnColor();

        }

    }
}

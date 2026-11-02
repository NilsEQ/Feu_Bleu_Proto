using UnityEngine;
using UnityEngine.InputSystem;

public class DebugScript : MonoBehaviour
{

    public WatchInteraction MyWatch;
    public GameObject toActivate;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

    }
}

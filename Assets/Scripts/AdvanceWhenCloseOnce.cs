using Unity.VisualScripting;
using UnityEngine;

public class AdvanceWhenCloseOnce : MonoBehaviour
{

    public bool Once = false;

    public StoryProgression Story;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    private void OnDisable()
    {
        if (!Once)
        {
            Story.Advance();
            Once = true;
        }
    }
}

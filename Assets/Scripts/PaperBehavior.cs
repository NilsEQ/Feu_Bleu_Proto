using UnityEngine;

public class PaperBehavior : MonoBehaviour
{
    public StoryProgression Story;

    public bool todo = true;
    // Update is called once per frame
    public void OnExit()
    {
        if (todo) 
        {
            Story.Advance();
            todo = false;
        }
    }
}

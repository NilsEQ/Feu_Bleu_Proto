using UnityEngine;

public class changeAfterSplash : MonoBehaviour
{

    public UITransition Splash;

    public GameObject newFirstScreen;
    

    public void change()
    {
        Splash.UI_after = newFirstScreen;
    }
}

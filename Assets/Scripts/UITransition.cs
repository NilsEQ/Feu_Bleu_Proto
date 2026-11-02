using UnityEngine;

public class UITransition : MonoBehaviour
{
    public GameObject UI_before;
    public GameObject UI_after;
    public void OnButtonPressed()
    {
        UI_after.SetActive(true);
        UI_before.SetActive(false);
    }


}

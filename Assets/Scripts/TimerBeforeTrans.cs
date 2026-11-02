using UnityEngine;

public class TimerBeforeTrans : MonoBehaviour
{
    public UITransition myTrans;

    public float seconds = 3f;

    private void OnEnable()
    {
        Debug.Log("Object activated!");
        StartCoroutine(MyCoroutine());
    }

    private System.Collections.IEnumerator MyCoroutine()
    {
        yield return new WaitForSeconds(seconds);
        Debug.Log("3 seconds passed!");
        myTrans.OnButtonPressed();
    }
}



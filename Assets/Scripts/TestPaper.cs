using UnityEngine;

public class TestPaper : MonoBehaviour
{

    public GameObject Cube1;

    public GameObject Cube2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    public void Enter()
    {
        Cube1.SetActive(true);
        Cube2.SetActive(false);

    }


    public void Exit()
    {
        Cube1.SetActive(false);
        Cube2.SetActive(true);

    }
}

using UnityEngine;
using System.Collections;


public class NextMomentOnFinish : MonoBehaviour
{
    
    [SerializeField] private AudioSource audioSource;

    public StoryProgression StoryProg;

    public MouthTalking MouthController;

    public float delay = 0;


    public void Start()
    {

    }


    public void PlaySound()
        {
            StartCoroutine(WaitForAudio());
        }

    private IEnumerator WaitForAudio()
        {
            audioSource.Play();
            MouthController.LetsTalk();

            Debug.Log(this.gameObject.name + " is Starting");


            yield return new WaitWhile(() => audioSource.isPlaying);


            yield return new WaitForSeconds(delay);

        // Runs once after the audio finishes
            Debug.Log(this.gameObject.name + " has stopped");
            MouthController.LetsNotTalk();


        DoSomething();
        }

    private void DoSomething()
        {
            StoryProg.Advance();
        }
}

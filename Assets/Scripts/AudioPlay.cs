using UnityEngine;
using System.Collections;

public class AudioPlay : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public AudioSource audioSource;
    public MouthTalking MouthController;

    void Start()
    {
    }

    public void PlaySound()
    {
        Debug.Log(this.gameObject.name + " is Starting");
        audioSource.PlayOneShot(audioSource.clip);
        MouthController.LetsTalk();
        StartCoroutine(WaitForAudio());

    }

    public void StopSound()
    {
        audioSource.Stop();
        Debug.Log(this.gameObject.name + " has Stopped");

    }


    private IEnumerator WaitForAudio()
    {


        yield return new WaitWhile(() => audioSource.isPlaying);

        MouthController.LetsNotTalk();

        // Runs once after the audio finishes
        Debug.Log(this.gameObject.name + " has stopped");

    }

}

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class ForTheEnd : MonoBehaviour
{
    public Volume globalVolume;
    public float transitionDuration = 20f;

    private ColorAdjustments colorAdjustments;


    public GameObject Scene;

    public GameObject paper;


    public GameObject EndEnv;
    private void Start()
    {
        globalVolume.profile.TryGet(out colorAdjustments);
    }

    public void ChangeColor()
    {
        StartCoroutine(ChangeColorCoroutine());
    }

    private IEnumerator ChangeColorCoroutine()
    {
        Color startColor = colorAdjustments.colorFilter.value;
        float startContrast = colorAdjustments.contrast.value;

        Color targetColor = new Color(0f / 255f, 0 / 255f, 225f / 255f);
        float targetContrast = 100f;

        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;


            float t = elapsedTime / transitionDuration;

            colorAdjustments.colorFilter.value =
                Color.Lerp(startColor, targetColor, t);

            colorAdjustments.contrast.value =
                Mathf.Lerp(startContrast, targetContrast, t);


            yield return null;
        }

        colorAdjustments.colorFilter.value = targetColor;
        colorAdjustments.contrast.value = targetContrast;

    }


    public void ReturnColor()
    {
        colorAdjustments.colorFilter.value = new Color(255f / 255f, 225f / 255f, 225f / 255f);
    }


    public void ThisIsTheEnd()
    {
        Scene.SetActive(false);
        paper.SetActive(false);

        EndEnv.SetActive(true);
    }
}

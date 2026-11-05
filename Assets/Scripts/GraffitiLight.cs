using UnityEngine;
using System.Collections;

public class GraffitiLight : MonoBehaviour
{
    [SerializeField] private Material material;
    private Color emissionColor = Color.white;
    [SerializeField] private float targetIntensity = 2f;
    [SerializeField] private float duration = 1f;


    public void Start()
    {
        material.SetColor("_EmissionColor", Color.black);
        material.DisableKeyword("_EMISSION");

    }
    public void EnableEmission()
    {
        StartCoroutine(FadeEmission());
    }

    private IEnumerator FadeEmission()
    {
        material.EnableKeyword("_EMISSION");
        emissionColor = new Color(5.6f, 5.6f, 5.6f, 1f);


        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float intensity = Mathf.Lerp(0f, targetIntensity, t);

            material.SetColor("_EmissionColor", emissionColor * intensity);

            yield return null;
        }
        material.SetColor("_EmissionColor", emissionColor * targetIntensity);


    }


}



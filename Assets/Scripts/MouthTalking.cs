using UnityEngine;

public class MouthTalking : MonoBehaviour
{
    public SkinnedMeshRenderer skinMesh;

    public int blendShapeIndex = 67;
    public float openAmount = 70f;
    public float speed = 8f;

    private float timer;
    public bool talking;
    private bool opening;

    void Update()
    {
        if (talking)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                opening = !opening;
                timer = Random.Range(0.05f, 0.2f);
            }

            float target = opening ? openAmount : 0f;

            float current = skinMesh.GetBlendShapeWeight(blendShapeIndex);

            skinMesh.SetBlendShapeWeight(
                blendShapeIndex,
                Mathf.MoveTowards(current, target, speed * 100f * Time.deltaTime)
            );
        }
        else
        {
            float current = skinMesh.GetBlendShapeWeight(blendShapeIndex);

            skinMesh.SetBlendShapeWeight(
                blendShapeIndex,
                Mathf.MoveTowards(current, 0f, speed * 100f * Time.deltaTime)
            );

        }
    }

    public void LetsTalk()
    {
        talking = true;
    }

    public void LetsNotTalk()
    {
        talking = false;

        Debug.Log("Yep its false");
    }
}
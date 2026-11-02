using UnityEngine;

public class ReflectionMov : MonoBehaviour
{

    public Camera playerCamera;
    public Camera reflectionCamera;    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 PosY = new Vector3(transform.position.x, playerCamera.transform.position.y, transform.position.z);
        Vector3 side1 = playerCamera.transform.position - transform.position;
        Vector3 side2 = transform.forward;

        float angle = Vector3.SignedAngle(side1, side2, Vector3.up);

        reflectionCamera.transform.localEulerAngles = new Vector3(0, angle, 0);
    }
}

//using UnityEngine;

//[ExecuteAlways]
//public class PlanarMirror : MonoBehaviour
//{
//    public Camera playerCamera;
//    public Camera reflectionCamera;

//    void LateUpdate()
//    {
//        if (playerCamera == null ||
//            reflectionCamera == null)
//            return;

//        // Mirror plane
//        Vector3 normal = transform.up.normalized;
//        Vector3 point = transform.position;

//        // Reflect the player's camera position
//        Vector3 cameraPosition = playerCamera.transform.position;
//        float distance = Vector3.Dot(
//            normal, cameraPosition - point
//        );

//        Vector3 reflectedPosition =
//            cameraPosition - 2f * distance * normal;

//        // Reflect the camera's forward and up directions
//        Vector3 forward = Vector3.Reflect(
//            playerCamera.transform.forward, normal
//        );

//        Vector3 up = Vector3.Reflect(
//            playerCamera.transform.up, normal
//        );

//        reflectionCamera.transform.position =
//            reflectedPosition;

//        reflectionCamera.transform.rotation =
//            Quaternion.LookRotation(forward, up);

//        // Match projection settings
//        reflectionCamera.fieldOfView =
//            playerCamera.fieldOfView;
//        reflectionCamera.aspect =
//            playerCamera.aspect;
//        reflectionCamera.nearClipPlane =
//            playerCamera.nearClipPlane;
//        reflectionCamera.farClipPlane =
//            playerCamera.farClipPlane;
//    }
//}
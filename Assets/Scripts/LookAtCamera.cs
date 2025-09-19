using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        transform.forward = mainCamera.transform.forward;
    }
}
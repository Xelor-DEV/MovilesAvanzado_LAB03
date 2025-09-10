using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;

    [Header("Offset Settings")]
    public Vector3 offset = new Vector3(0, 2, -5);
    public bool calculateInitialOffset = true;

    [Header("Gizmo Settings")]
    public bool showGizmos = true;
    public Color gizmoColor = Color.cyan;
    public float sphereSize = 0.2f;

    private void OnValidate()
    {
        if (target != null && calculateInitialOffset)
        {
            CalculateOffset();
        }
    }

    void Start()
    {
        if (target != null && calculateInitialOffset)
        {
            CalculateOffset();
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            FollowTarget();
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (calculateInitialOffset && target != null)
        {
            CalculateOffset();
        }
    }


    private void CalculateOffset()
    {
        offset = transform.position - target.position;
        calculateInitialOffset = false; // Prevenir recálculo automático
    }

    private void FollowTarget()
    {
        Vector3 targetPosition = target.position + offset;
        transform.position = targetPosition;
    }

    private void OnDrawGizmos()
    {
        if (showGizmos && target != null)
        {
            // Dibujar esfera en la posición de la cámara
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, sphereSize);

            // Dibujar línea hacia el objetivo
            Gizmos.DrawLine(transform.position, target.position);

            // Dibujar esfera en el objetivo
            Gizmos.DrawWireSphere(target.position, sphereSize);

            // Dibujar vector de offset
            Gizmos.DrawRay(target.position, offset);
        }
    }
}
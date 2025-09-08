using UnityEngine;
using Unity.Netcode;

public class SimplePlayerController : NetworkBehaviour
{
    public float JumpForce = 5;
    public float Speed = 10 ;

    private Animator animator;
    private Rigidbody rb;
    public LayerMask groundLayer;

    public GameObject projectilePrefab;
    public Transform firePoint;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void Update()
    {
        if (!IsOwner) return;

        HandleMovement();
        HandleRotation();
        CheckGroundRpc();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            JumpTriggerRpc("Jump");
        }

        if (Input.GetMouseButtonDown(0))
        {
            ShootRpc();
        }
    }

    private void HandleMovement()
    {
        if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
        {
            float VelX = Input.GetAxisRaw("Horizontal") * Speed * Time.deltaTime;
            float VelY = Input.GetAxisRaw("Vertical") * Speed * Time.deltaTime;
            UpdatePositionRpc(VelX, VelY);
        }
    }

    private void HandleRotation()
    {
        // Crear un rayo desde la cámara hasta la posición del ratón
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector3 targetPosition = hit.point;
            targetPosition.y = transform.position.y; // Mantener la altura del personaje
            UpdateRotationRpc(targetPosition);
        }
    }

    [Rpc(SendTo.Server)]
    public void UpdatePositionRpc(float x, float y)
    {
        transform.position += new Vector3(x, 0, y);
    }

    [Rpc(SendTo.Server)]
    public void UpdateRotationRpc(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
        }
    }

    [Rpc(SendTo.Server)]
    public void JumpTriggerRpc(string animationName)
    {
        rb = GetComponent<Rigidbody>();
        rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
        animator.SetTrigger(animationName);
    }

    [Rpc(SendTo.Server)]
    public void CheckGroundRpc()
    {
        if (Physics.Raycast(transform.position, Vector3.down, 1.1f, groundLayer))
        {
            animator.SetBool("Grounded", true);
            animator.SetBool("FreeFall", false);
        }
        else
        {
            animator.SetBool("Grounded", false);
            animator.SetBool("FreeFall", true);
        }
    }

    [Rpc(SendTo.Server)]
    public void ShootRpc()
    {
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        proj.GetComponent<NetworkObject>().Spawn(true);
        proj.GetComponent<Rigidbody>().AddForce(firePoint.forward * 5, ForceMode.Impulse);
    }
}

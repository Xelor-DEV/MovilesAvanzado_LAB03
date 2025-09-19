using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.UI;
using System.Collections;

public class SimplePlayerController : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> accountID = new NetworkVariable<FixedString32Bytes>();

    public NetworkVariable<int> health = new NetworkVariable<int>();

    public NetworkVariable<int> attack = new NetworkVariable<int>();

    private NetworkVariable<float> animationSpeed = new NetworkVariable<float>();
    private bool isShooting = false;

    public float JumpForce = 5;
    public float Speed = 10 ;
    public float bulletSpeed = 35;

    private Animator animator;
    private Rigidbody rb;
    public LayerMask groundLayer;

    public GameObject projectilePrefab;
    public Transform firePoint;

    public Image lifeBar;
    public Image worldLifeBar;

    /*
    public void Start()
    {
        animator = GetComponent<Animator>();

        if (IsOwner)
        {
            GameManager.Instance.cameraFollower.SetTarget(gameObject.transform);
            lifeBar = GameManager.Instance.lifeBar;
        }
    }
    */

    public override void OnNetworkSpawn()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        // Suscribirse a cambios de salud
        health.OnValueChanged += OnHealthChanged;
        animationSpeed.OnValueChanged += OnAnimationSpeedChanged;

        if (worldLifeBar != null)
        {
            worldLifeBar.fillAmount = health.Value / 100f;

            // Ocultar barra de mundo para el jugador local
            if (IsOwner)
            {
                worldLifeBar.gameObject.SetActive(false);
            }
        }

        if (IsOwner)
        {
            GameManager.Instance.cameraFollower.SetTarget(gameObject.transform);
            lifeBar = GameManager.Instance.lifeBar;
            UpdateHealthBar();
        }
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        if (IsOwner)
        {
            UpdateHealthBar();
        }

        if (!IsOwner && worldLifeBar != null)
        {
            worldLifeBar.fillAmount = newHealth / 100f;
        }

        if (newHealth <= 0 && IsServer)
        {
            animator.SetBool("IsShooting", false);
            HandleDeath();
        }
    }

    private void UpdateHealthBar()
    {
        if (lifeBar != null)
        {
            lifeBar.fillAmount = health.Value / 100f;
        }
    }

    private void HandleDeath()
    {
        RespawnPlayerRpc(accountID.Value.ToString());
        print("dEATH");
    }

    [Rpc(SendTo.Server)]
    public void RespawnPlayerRpc(string accountId)
    {
        print("llamadno rpc");
        PlayerData player = GameManager.Instance.RespawnPlayer(accountId);
        SetData(player);
    }

    public void TakeDamage(int damage)
    {
        if (IsServer)
        {
            health.Value -= damage;
        }
        else
        {
            TakeDamageServerRpc(damage);
        }
    }

    [Rpc(SendTo.Server)]
    private void TakeDamageServerRpc(int damage)
    {
        health.Value -= damage;
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
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 movement = new Vector3(horizontal, 0, vertical);

        // Calcula la velocidad para las animaciones
        float speedValue = movement.magnitude;
        UpdateAnimationSpeedRpc(speedValue);

        if (speedValue > 0)
        {
            float VelX = horizontal * Speed * Time.deltaTime;
            float VelY = vertical * Speed * Time.deltaTime;
            UpdatePositionRpc(VelX, VelY);
        }
    }

    private void ResetShooting()
    {
        isShooting = false;
    }

    [Rpc(SendTo.Server)]
    public void UpdateAnimationSpeedRpc(float speed)
    {
        animationSpeed.Value = speed;
        animator.SetFloat("Speed", speed);
    }


    private void HandleRotation()
    {
        // Crear un rayo desde la c�mara hasta la posici�n del rat�n
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
        // Activar la animación de disparo
        animator.SetBool("IsShooting", true);

        // Crear el proyectil
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        proj.GetComponent<NetworkObject>().Spawn(true);
        proj.GetComponent<Rigidbody>().AddForce(firePoint.forward * bulletSpeed, ForceMode.Impulse);
        proj.GetComponent<Projectile>().damage = attack.Value;
        proj.GetComponent<Projectile>().SetOwner(OwnerClientId);

        // Programar el fin de la animación después de un tiempo
        StartCoroutine(EndShootingAnimation(0.15f)); // Ajusta según duración de tu animación
    }

    private IEnumerator EndShootingAnimation(float duration)
    {
        yield return new WaitForSeconds(duration);
        animator.SetBool("IsShooting", false);
    }

    private void OnAnimationSpeedChanged(float oldSpeed, float newSpeed)
    {
        animator.SetFloat("Speed", newSpeed);
    }

    public void SetData(PlayerData data)
    {
        accountID.Value = data.accountID;
        health.Value = data.health;
        attack.Value = data.attack;
        transform.position = data.position;
    }

    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= OnHealthChanged;
        animationSpeed.OnValueChanged -= OnAnimationSpeedChanged;

        GameManager.Instance.playerStatesByAccountID[accountID.Value.ToString()] = new PlayerData(accountID.Value.ToString(), transform.position, health.Value, attack.Value);

        print("Me e desconectado" + NetworkManager.Singleton.LocalClientId +  " y se a guardado la data de " + accountID.Value);
        // antes de destruirse se llama
    }
}

public class PlayerData
{
    public string accountID;
    public Vector3 position;
    public int health;
    public int attack;

    public PlayerData(string id, Vector3 position, int health, int attack)
    {
        accountID = id;
        this.position = position;
        this.health = health;
        this.attack = attack;
    }
}
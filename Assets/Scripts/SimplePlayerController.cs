using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.UI;

public class SimplePlayerController : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> accountID = new NetworkVariable<FixedString32Bytes>();

    public NetworkVariable<int> health = new NetworkVariable<int>();

    public NetworkVariable<int> attack = new NetworkVariable<int>();
    
    public float JumpForce = 5;
    public float Speed = 10 ;
    public float bulletSpeed = 15;

    private Animator animator;
    private Rigidbody rb;
    public LayerMask groundLayer;

    public GameObject projectilePrefab;
    public Transform firePoint;

    public Image lifeBar;

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

        if (newHealth <= 0 && IsServer)
        {
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
        proj.GetComponent<Rigidbody>().AddForce(firePoint.forward * bulletSpeed, ForceMode.Impulse);
        proj.GetComponent<Projectile>().damage = attack.Value;
        proj.GetComponent<Projectile>().SetOwner(OwnerClientId);
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
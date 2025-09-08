using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkEnemy : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Image healthBar;
    [SerializeField] private float maxHealth = 100f;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>();
    private Transform targetPlayer;
    private EnemySpawner spawner;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        // Subscribe to health changes
        currentHealth.OnValueChanged += OnHealthChanged;
        UpdateHealthBar();
    }
    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    public void SetSpawner(EnemySpawner enemySpawner)
    {
        spawner = enemySpawner;
    }

    private void Update()
    {
        if (!IsServer) return;

        FindClosestPlayerRpc();

        if (targetPlayer != null)
        {
            Vector3 moveDirection = (targetPlayer.position - transform.position).normalized;
            RequestMoveRpc(moveDirection);
        }
    }

    [Rpc(SendTo.Server)]
    private void FindClosestPlayerRpc()
    {
        if (GameManager.Instance.players.Count == 0) return;

        float closestDistance = Mathf.Infinity;

        foreach (GameObject player in GameManager.Instance.players)
        {
            if (player == null) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                targetPlayer = player.transform;
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestMoveRpc(Vector3 direction)
    {
        transform.position += direction * moveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void OnHealthChanged(float previousValue, float newValue)
    {
        UpdateHealthBar();

        if (newValue <= 0 && IsServer)
        {
            Die();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = currentHealth.Value / maxHealth;
        }
    }

    [Rpc(SendTo.Server)]
    public void TakeDamageRpc(float damage)
    {
        currentHealth.Value -= damage;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            EnemyCollisionPlayerRpc();
        }
        else if (other.CompareTag("Projectile") && IsServer)
        {
            float damage = other.GetComponent<Projectile>().damage;
            TakeDamageRpc(damage);
        }
    }

    [Rpc(SendTo.Server)]
    public void EnemyCollisionPlayerRpc()
    {
        Die();
    }

    private void Die()
    {
        GetComponent<NetworkObject>().Despawn();
        spawner.EnemyDestroyed();
    }

    private void OnDrawGizmos()
    {
        if (targetPlayer != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetPlayer.position);
        }
    }
}
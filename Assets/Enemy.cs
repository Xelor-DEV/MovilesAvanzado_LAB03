using Unity.Netcode;
using UnityEngine;

public class NetworkEnemy : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 3f;

    private Transform targetPlayer;
    private EnemySpawner spawner;

    public void SetSpawner(EnemySpawner enemySpawner)
    {
        spawner = enemySpawner;
    }

    private void Update()
    {
        if (!IsServer) return;

        MoveTowardsPlayer();

        FindClosestPlayerRpc();
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

    private void MoveTowardsPlayer()
    {
        if (targetPlayer == null) return;

        Vector3 moveDirection = (targetPlayer.position - transform.position).normalized;
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            EnemyCollisionPlayerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    public void EnemyCollisionPlayerRpc()
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
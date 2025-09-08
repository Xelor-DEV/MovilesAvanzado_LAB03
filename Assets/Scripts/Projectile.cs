using UnityEngine;
using Unity.Netcode;

public class Projectile : NetworkBehaviour
{
    public float damage = 25f;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Wall"))
        {
            SimpleDespawn();
        }
        else if (other.CompareTag("Enemy"))
        {
            // Damage the enemy
            NetworkEnemy enemy = other.GetComponent<NetworkEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamageRpc(damage);
            }
            SimpleDespawn();
        }
    }

    public void SimpleDespawn()
    {
        GetComponent<NetworkObject>().Despawn(true);
    }
}

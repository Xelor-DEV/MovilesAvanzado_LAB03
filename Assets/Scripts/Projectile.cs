using UnityEngine;
using Unity.Netcode;

public class Projectile : NetworkBehaviour
{
    public int damage = 25;
    private ulong ownerClientId;
    public void SetOwner(ulong clientId)
    {
        ownerClientId = clientId;
    }

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
        else if (other.CompareTag("Player"))
        {
            // Damage the player (but not the owner of the projectile)
            if (other.GetComponent<NetworkObject>().OwnerClientId != ownerClientId)
            {
                SimplePlayerController player = other.GetComponent<SimplePlayerController>();
                if (player != null)
                {
                    player.TakeDamage(damage);
                }
                SimpleDespawn();
            }
        }
    }

    public void SimpleDespawn()
    {
        GetComponent<NetworkObject>().Despawn(true);
    }
}

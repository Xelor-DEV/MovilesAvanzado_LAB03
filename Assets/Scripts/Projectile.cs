using UnityEngine;
using Unity.Netcode;

public class Projectile : NetworkBehaviour
{
    void Start()
    {
        if (IsServer)
        {
            Invoke("SimpleDespawn", 5);
        }
    }

    public void SimpleDespawn()
    {
        GetComponent<NetworkObject>().Despawn(true);
    }
}

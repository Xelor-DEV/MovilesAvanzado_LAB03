using Unity.Netcode;
using UnityEngine;

public class RandomBuff : NetworkBehaviour
{
    void Start()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
           ulong playerID =  other.GetComponent<NetworkObject>().OwnerClientId;
           AddBuffToPlayerRpc(playerID);
        }
    }

    [Rpc(SendTo.Server)]
    private void AddBuffToPlayerRpc(ulong playerID)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerID, out var client))
        {
            var playerObj = client.PlayerObject;
            var controller = playerObj.GetComponent<SimplePlayerController>();

            // Aumentar ataque entre 1 y 3
            int buffAmount = Random.Range(1, 4);
            controller.attack.Value += buffAmount;

            Debug.Log($"Aplicado buff de +{buffAmount} de ataque a jugador {playerID}");
        }

        GetComponent<NetworkObject>().Despawn(true);
    }
    /*[Rpc(SendTo.Server)]
    public void AddHPToPlayerRpc(ulong playerID, int amount)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerID, out var client))
        {
            var playerObj = client.PlayerObject;
            var controller = playerObj.GetComponent<SimplePlayerController>();
            controller.Life.Value += amount;
        }
    }*/
}

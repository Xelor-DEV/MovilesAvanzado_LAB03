using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject playerPrefab;
    public GameObject buffPrefab;

    public float BuffSpawnCount = 4;
    public float currentBuffCount = 0;

    public List<GameObject> players = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        print("CurrentPlayer" + NetworkManager.Singleton.ConnectedClients.Count);
        SpawnPlayerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [Rpc(SendTo.Server)]
    public void SpawnPlayerRpc(ulong id)
    {
        GameObject player = Instantiate(playerPrefab);
        player.GetComponent<NetworkObject>().SpawnWithOwnership(id, true);

        players.Add(player);
    }

    void Update()
    {
        if (IsServer && NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            currentBuffCount += Time.deltaTime;
            if (currentBuffCount > BuffSpawnCount)
            {
                Vector3 randomPos = new Vector3(Random.Range(-8, 8), 0.5f, Random.Range(-8, 8));
                GameObject buff = Instantiate(buffPrefab, randomPos, Quaternion.identity);
                buff.GetComponent<NetworkObject>().Spawn(true);
                currentBuffCount = 0;
            }
        }
    }
}
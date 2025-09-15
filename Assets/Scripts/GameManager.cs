using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject playerPrefab;
    public GameObject buffPrefab;
    public CameraFollower cameraFollower;
    public Image lifeBar;

    public float BuffSpawnCount = 4;
    public float currentBuffCount = 0;

    //public List<GameObject> players = new List<GameObject>();

    public Dictionary<string,PlayerData> playerStatesByAccountID = new Dictionary<string,PlayerData>();

    public Action OnConnection;

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
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
        }

        OnConnection?.Invoke();
        /*
        print("CurrentPlayer" + NetworkManager.Singleton.ConnectedClients.Count);
        SpawnPlayerRpc(NetworkManager.Singleton.LocalClientId);
        */
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;
        }
    }

    private void HandleDisconnect(ulong clientID)
    {
        // cuando todo ya se ha destruido
        print("El jugador" + clientID + "Se a desconectado");
    }

    /*
    [Rpc(SendTo.Server)]
    public void SpawnPlayerRpc(ulong id)
    {
        GameObject player = Instantiate(playerPrefab);
        player.GetComponent<NetworkObject>().SpawnWithOwnership(id, true);

        //players.Add(player);
    }
    */

    void Update()
    {
        if (IsServer && NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            currentBuffCount += Time.deltaTime;
            if (currentBuffCount > BuffSpawnCount)
            {
                Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-8, 8), 0.5f, UnityEngine.Random.Range(-8, 8));
                GameObject buff = Instantiate(buffPrefab, randomPos, Quaternion.identity);
                buff.GetComponent<NetworkObject>().Spawn(true);
                currentBuffCount = 0;
            }
        }
    }

    public PlayerData RespawnPlayer(string accountID)
    {
        if (playerStatesByAccountID.TryGetValue(accountID, out PlayerData data))
        {
            // Resetear vida y posición aleatoria
            Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-8, 8), 0, UnityEngine.Random.Range(-8, 8));
            PlayerData newData = new PlayerData(accountID, randomPos, 100, 5);
            // playerStatesByAccountID[accountID] = newData;

            return newData;
        }
        return null;

    }

    [Rpc(SendTo.Server)]
    public void RegisterPlayerServerRpc(string accountID, ulong ID)
    {
        if (!playerStatesByAccountID.TryGetValue(accountID, out PlayerData data))
        {
            PlayerData NewData = new PlayerData(accountID, Vector3.zero, 100, 5);

            playerStatesByAccountID[accountID] = NewData;

            //instanciar al player con la data
            SpawnPlayerServer(ID, NewData);

            print("Nueva id creada con el nombre de " + accountID);
        }
        else
        {
            SpawnPlayerServer(ID, data);

            print("Se encontro cuenta con el nombre de  " + accountID);
        };
    }

    public void SpawnPlayerServer(ulong ID, PlayerData data)
    {
        if (!IsServer) return;

        GameObject player = Instantiate(playerPrefab);

        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(ID, true);
        player.GetComponent<SimplePlayerController>().SetData(data);
    }
}
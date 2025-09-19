using Unity.Netcode;
using UnityEngine;

public class RandomBuff : NetworkBehaviour
{
    [Header("Visual Settings")]
    public Transform model; // Referencia al objeto modelo que se animará
    public float rotationSpeed = 45f; // Velocidad de rotación en grados/segundo
    public float bobHeight = 0.5f; // Altura del movimiento vertical
    public float bobSpeed = 2f; // Velocidad del movimiento vertical

    private Vector3 initialPosition; // Posición inicial del modelo

    void Start()
    {
        // Guardar la posición inicial relativa si el modelo existe
        if (model != null)
        {
            initialPosition = model.localPosition;
        }
    }

    void Update()
    {
        if (model == null) return;

        // Rotación continua suave
        model.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // Movimiento vertical suave (sube y baja)
        float newY = initialPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        model.localPosition = new Vector3(initialPosition.x, newY, initialPosition.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            ulong playerID = other.GetComponent<NetworkObject>().OwnerClientId;
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
}
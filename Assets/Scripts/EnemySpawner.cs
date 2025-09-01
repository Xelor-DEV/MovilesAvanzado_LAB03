using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxEnemies = 10;
    [SerializeField] private Vector2 spawnArea = new Vector2(10f, 10f);

    private float spawnTimer;
    private int currentEnemies = 0;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        spawnTimer = spawnInterval;
    }

    private void Update()
    {
        if (!IsServer) return;

        if (currentEnemies >= maxEnemies) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnEnemy();
            spawnTimer = spawnInterval;
        }
    }

    private void SpawnEnemy()
    {
        Vector3 spawnPos = new Vector3(Random.Range(-spawnArea.x / 2, spawnArea.x / 2), 0f, Random.Range(-spawnArea.y / 2, spawnArea.y / 2)) + transform.position;

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        enemy.GetComponent<NetworkEnemy>().SetSpawner(this);
        enemy.GetComponent<NetworkObject>().Spawn(true);
        currentEnemies++;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnArea.x, 0.1f, spawnArea.y));
    }

    public void EnemyDestroyed()
    {
        if (!IsServer) return;

        currentEnemies--;
    }
}
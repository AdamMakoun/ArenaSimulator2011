using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    public GameObject[] enemyPrefabs; // Prefab for the enemy
    public int enemiesPerWave = 5; // Number of enemies per wave
    public Transform[] spawnPoints; // Array of spawn points
    private int enemiesAlive = 0; // Number of enemies currently alive
    private int currentWave = 0; // Current wave number

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(SpawnWave());
    }

    // Update is called once per frame
    void Update()
    {
        if (enemiesAlive == 0)
        {
            StartCoroutine(SpawnWave());
        }
    }

    IEnumerator SpawnWave()
    {
        currentWave++;
        for (int i = 0; i < enemiesPerWave + (currentWave * 2); i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(1f); // Wait for 1 second between spawns
        }
    }

    void SpawnEnemy()
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points set.");
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Instantiate(ChooseRandomEnemy(), spawnPoint.position, spawnPoint.rotation);
        enemiesAlive++;
    }
    GameObject ChooseRandomEnemy()
    {
        return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
    }
    public void EnemyDied()
    {
        enemiesAlive--;
    }
}
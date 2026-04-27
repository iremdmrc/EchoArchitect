using UnityEngine;

public class PlatformSpawner : MonoBehaviour
{
    public GameObject platformPrefab;
    public Transform spawnPoint;

    public float interval = 1.0f;
    public int maxCount = 10;
    public float lateralJitter = 0.5f; // sağ-sol random
    public float stepForward = 1.2f;   // her platformdan sonra ne kadar ileri gitsin
    public float fixedY = 0.15f;       // platformların Y seviyesi



    [Header("Path Settings")]
    public float stepDistance = 3f;          // platform arası mesafe
    public bool useSpawnPointRotation = true;

    private bool running = false;
    private int spawned = 0;

    private Vector3 nextPos;
    private Quaternion nextRot;

    public void StartSpawning()
    {
        if (running) return;
        running = true;
        spawned = 0;

        if (platformPrefab == null || spawnPoint == null)
        {
            Debug.LogError("PlatformSpawner: Prefab veya SpawnPoint boş!");
            running = false;
            return;
        }

        nextPos = spawnPoint.position;
        nextRot = useSpawnPointRotation ? spawnPoint.rotation : Quaternion.identity;

        InvokeRepeating(nameof(SpawnOne), 0f, interval);
    }

    void SpawnOne()
    {
        if (platformPrefab == null || spawnPoint == null)
        {
            Debug.LogError("PlatformSpawner: Prefab veya SpawnPoint boş!");
            StopSpawning();
            return;
        }

        Vector3 pos = spawnPoint.position;
        pos.y = fixedY;

        Instantiate(platformPrefab, pos, Quaternion.identity);

        // ✅ spawnPoint'i ileri taşı
        spawnPoint.position += spawnPoint.forward * stepForward;

        spawned++;

        if (spawned >= maxCount)
            StopSpawning();
    }


    public void StopSpawning()
    {
        if (!running) return;
        running = false;
        CancelInvoke(nameof(SpawnOne));
    }

    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.P))
    //    {
    //        StartSpawning();
    //        Debug.Log("P basıldı -> StartSpawning çalıştı");
    //    }
    //}
}

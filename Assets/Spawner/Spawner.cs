using UnityEngine;

/// <summary>
/// Spawns a set number of objects, each a random pick from a prefab list,
/// at random positions inside a cube centered on this Spawner — but never
/// within minDistanceFromPlayer of the player. Also spawns exactly ONE of a
/// separate "special" prefab at its own random valid position.
///
/// Setup:
/// 1. Attach to an empty GameObject called "Spawner", positioned where you want
///    the spawn area centered.
/// 2. Set boxSize to the cube's side length (it's a single float, so the box
///    is always a cube: boxSize x boxSize x boxSize).
/// 3. Assign your prefabs to spawnablePrefabs.
/// 4. Assign the single special prefab to specialPrefab (e.g. a key, an exit, a boss).
/// 5. Assign the player's Transform to playerTransform.
/// 6. Call Spawn() (e.g. from Start, a button, or another script) to spawn.
/// </summary>
public class Spawner : MonoBehaviour
{
    [Header("What to spawn")]
    [SerializeField] private GameObject[] spawnablePrefabs;
    [SerializeField] private int spawnCount = 5;

    [Header("Special single spawn")]
    [Tooltip("Exactly one of this prefab is spawned at its own random valid position.")]
    [SerializeField] private GameObject specialPrefab;

    [Header("Where to spawn")]
    [Tooltip("Side length of the cubic spawn area, centered on this Spawner's position.")]
    [SerializeField] private float boxSize = 20f;

    [Header("Player exclusion")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float minDistanceFromPlayer = 5f;

    [Header("Behaviour")]
    [Tooltip("How many times to re-roll a position before giving up on one spawn.")]
    [SerializeField] private int maxAttemptsPerSpawn = 30;

    void Awake()
    {
        Spawn();
    }

    /// <summary>
    /// Spawns spawnCount objects (random prefabs) plus one specialPrefab, all at
    /// random valid positions in the box.
    /// </summary>
    public void Spawn()
    {
        SpawnRandomPrefabs();
        SpawnSpecialPrefab();
    }

    private void SpawnRandomPrefabs()
    {
        if (spawnablePrefabs == null || spawnablePrefabs.Length == 0)
        {
            Debug.LogWarning("Spawner: no prefabs assigned to spawnablePrefabs.", this);
            return;
        }

        int spawned = 0;
        for (int i = 0; i < spawnCount; i++)
        {
            if (TryGetValidPosition(out Vector3 position))
            {
                int prefabIndex = Random.Range(0, spawnablePrefabs.Length);
                GameObject prefab = spawnablePrefabs[prefabIndex];

                Instantiate(prefab, position, Quaternion.identity);
                spawned++;
            }
            else
            {
                Debug.LogWarning($"Spawner: couldn't find a valid position after {maxAttemptsPerSpawn} attempts " +
                                  $"(spawned {spawned}/{spawnCount} so far). Try a bigger box or smaller exclusion distance.", this);
            }
        }
    }

    private void SpawnSpecialPrefab()
    {
        if (specialPrefab == null) return;

        if (TryGetValidPosition(out Vector3 position))
        {
            Instantiate(specialPrefab, position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning($"Spawner: couldn't find a valid position for specialPrefab after {maxAttemptsPerSpawn} attempts. " +
                              "Try a bigger box or smaller exclusion distance.", this);
        }
    }

    /// <summary>
    /// Rolls random positions inside the box until one is far enough from the player,
    /// or gives up after maxAttemptsPerSpawn tries.
    /// </summary>
    private bool TryGetValidPosition(out Vector3 result)
    {
        float half = boxSize * 0.5f;

        for (int attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
        {
            Vector3 candidate = transform.position + new Vector3(
                Random.Range(-half, half),
                0f,
                Random.Range(-half, half)
            );

            if (playerTransform == null ||
                Vector3.Distance(candidate, playerTransform.position) >= minDistanceFromPlayer)
            {
                result = candidate;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        // Spawn area
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * boxSize);

        // Player exclusion radius
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, minDistanceFromPlayer);
        }
    }
}
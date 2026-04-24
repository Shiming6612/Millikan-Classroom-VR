using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpraySpawner : MonoBehaviour
{
    private enum SpawnMode
    {
        Random,
        TeachingFixedRadius
    }

    [Header("Refs")]
    public Transform spawnOrigin;
    public Transform aimTarget;
    public OilDrop dropPrefab;

    [Header("Limits")]
    public int maxTotalDrops = 15;
    public int minDropsPerSpray = 3;
    public int maxDropsPerSpray = 6;
    public float burstDuration = 0.12f;
    public float minTimeBetweenSprays = 0.05f;

    [Header("Spawn + Launch")]
    public float spawnRadius = 0.01f;
    public bool useAimTarget = true;
    [Range(0f, 60f)] public float coneAngle = 18f;
    public float baseLaunchSpeed = 2.0f;
    public float speedRandomPercent = 0.25f;
    public float lateralJitterSpeed = 0.35f;
    public float upwardBias = 0.15f;

    [Header("Teaching Radius Mode")]
    public float[] teachingRadiiMicrometer = new float[] { 0.5f, 1.0f, 1.5f };
    public int currentTeachingRadiusIndex = 0;
    public float currentTeachingRadiusMicrometer = 1.0f;

    [Tooltip("How many drops are sprayed for each radius teaching group.")]
    public int teachingDropsPerGroup = 15;

    [Tooltip("Charge multiple used during radius teaching. 1 means 1e.")]
    public int fixedChargeMultipleForTeaching = 1;

    [Tooltip("If enabled, old drops are removed when switching to another teaching radius.")]
    public bool resetDropsWhenChangingTeachingRadius = true;

    [Header("Runtime Debug")]
    [SerializeField] private SpawnMode currentSpawnMode = SpawnMode.Random;

    [Header("Nozzle Feedback")]
    public AudioSource nozzleSfxSource;
    public AudioClip nozzleSfx;
    public ParticleSystem nozzleVfxPrefab;
    public Transform nozzlePoint;

    private int spawnedCount = 0;
    private float lastSprayTime = -999f;
    private Coroutine burstRoutine;
    private readonly List<OilDrop> spawnedDrops = new List<OilDrop>();

    public bool IsTeachingModeActive()
    {
        return currentSpawnMode == SpawnMode.TeachingFixedRadius;
    }

    public void SprayOnce()
    {
        if (spawnOrigin == null || dropPrefab == null)
            return;

        if (spawnedCount >= maxTotalDrops)
            return;

        if (Time.time - lastSprayTime < minTimeBetweenSprays)
            return;

        lastSprayTime = Time.time;

        int wantedCount;

        if (currentSpawnMode == SpawnMode.TeachingFixedRadius)
        {
            wantedCount = Mathf.Max(1, teachingDropsPerGroup);
        }
        else
        {
            wantedCount = Random.Range(minDropsPerSpray, maxDropsPerSpray + 1);
        }

        wantedCount = Mathf.Min(wantedCount, maxTotalDrops - spawnedCount);

        if (wantedCount <= 0)
            return;

        PlayNozzleFeedback();

        if (burstRoutine != null)
            StopCoroutine(burstRoutine);

        burstRoutine = StartCoroutine(SpawnBurst(wantedCount));
    }

    public void ResetAllDrops()
    {
        if (burstRoutine != null)
            StopCoroutine(burstRoutine);

        burstRoutine = null;

        for (int i = 0; i < spawnedDrops.Count; i++)
        {
            if (spawnedDrops[i] != null)
                Destroy(spawnedDrops[i].gameObject);
        }

        spawnedDrops.Clear();
        spawnedCount = 0;
    }

    public void SetTeachingRadiusStep(int index)
    {
        if (teachingRadiiMicrometer == null || teachingRadiiMicrometer.Length == 0)
        {
            Debug.LogWarning("[SpraySpawner] Teaching radii list is empty.");
            return;
        }

        currentTeachingRadiusIndex = Mathf.Clamp(index, 0, teachingRadiiMicrometer.Length - 1);
        currentTeachingRadiusMicrometer = teachingRadiiMicrometer[currentTeachingRadiusIndex];

        currentSpawnMode = SpawnMode.TeachingFixedRadius;

        if (resetDropsWhenChangingTeachingRadius)
            ResetAllDrops();

        Debug.Log(
            $"[SpraySpawner] Teaching mode ON. Radius = {currentTeachingRadiusMicrometer:0.00} µm, " +
            $"Charge = {fixedChargeMultipleForTeaching}e, Drops = {teachingDropsPerGroup}"
        );
    }

    public void DisableTeachingRadiusMode()
    {
        currentSpawnMode = SpawnMode.Random;
        Debug.Log("[SpraySpawner] Teaching mode OFF. New drops will be random.");
    }

    public void ReturnToRandomModeAndClearDrops()
    {
        currentSpawnMode = SpawnMode.Random;
        ResetAllDrops();
        Debug.Log("[SpraySpawner] Returned to random mode and cleared all drops.");
    }

    private IEnumerator SpawnBurst(int count)
    {
        float delay = (burstDuration <= 0f || count <= 1)
            ? 0f
            : burstDuration / (count - 1);

        for (int i = 0; i < count; i++)
        {
            SpawnOne();

            if (delay > 0f)
                yield return new WaitForSeconds(delay);
        }

        burstRoutine = null;

        BottomTutorialController tutorial = FindFirstObjectByType<BottomTutorialController>();
        if (tutorial != null)
            tutorial.NotifyDropletTriggered();
    }

    private void SpawnOne()
    {
        OilDrop drop = Instantiate(dropPrefab);

        spawnedDrops.Add(drop);
        spawnedCount++;

        DropProperties props = drop.GetComponent<DropProperties>();
        if (props == null)
            props = drop.GetComponentInChildren<DropProperties>();

        if (props != null)
        {
            if (currentSpawnMode == SpawnMode.TeachingFixedRadius)
            {
                // Tutorial radius comparison:
                // fixed radius + fixed charge
                props.SetTeachingRadiusAndCharge(
                    currentTeachingRadiusMicrometer,
                    fixedChargeMultipleForTeaching
                );
            }
            else
            {
                // Normal experiment:
                // random radius/mass + random charge
                // visual scale is also applied according to the random radius
                props.RandomizeAndApply();
            }
        }
        else
        {
            Debug.LogWarning("[SpraySpawner] Spawned drop has no DropProperties component.");
        }

        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;

        Vector3 spawnPosition =
            spawnOrigin.position +
            spawnOrigin.right * randomOffset.x +
            spawnOrigin.up * randomOffset.y;

        Vector3 baseDirection = (useAimTarget && aimTarget != null)
            ? (aimTarget.position - spawnPosition).normalized
            : spawnOrigin.forward;

        baseDirection = (baseDirection + Vector3.up * upwardBias).normalized;

        Vector3 launchDirection = RandomDirectionInCone(baseDirection, coneAngle);

        float launchSpeed = baseLaunchSpeed *
                            (1f + Random.Range(-speedRandomPercent, speedRandomPercent));

        Vector3 lateralJitter =
            Vector3.ProjectOnPlane(Random.onUnitSphere, launchDirection).normalized *
            lateralJitterSpeed;

        Vector3 launchVelocity = launchDirection * launchSpeed + lateralJitter;

        drop.Launch(spawnPosition, launchVelocity);
    }

    private void PlayNozzleFeedback()
    {
        if (nozzleSfxSource != null && nozzleSfx != null)
            nozzleSfxSource.PlayOneShot(nozzleSfx);

        if (nozzleVfxPrefab == null)
            return;

        Transform effectTransform = nozzlePoint != null
            ? nozzlePoint
            : (spawnOrigin != null ? spawnOrigin : transform);

        ParticleSystem vfx = Instantiate(
            nozzleVfxPrefab,
            effectTransform.position,
            effectTransform.rotation
        );

        vfx.Play();

        float destroyAfter = 2f;
        ParticleSystem.MainModule main = vfx.main;

        if (!main.loop)
        {
            float lifetimeMax = main.startLifetime.constantMax;
            destroyAfter = Mathf.Max(0.1f, main.duration + lifetimeMax + 0.2f);
        }

        Destroy(vfx.gameObject, destroyAfter);
    }

    private static Vector3 RandomDirectionInCone(Vector3 forward, float coneHalfAngleDeg)
    {
        if (coneHalfAngleDeg <= 0.001f)
            return forward.normalized;

        float coneRad = coneHalfAngleDeg * Mathf.Deg2Rad;
        float cosMin = Mathf.Cos(coneRad);

        float z = Random.Range(cosMin, 1f);
        float theta = Random.Range(0f, Mathf.PI * 2f);
        float r = Mathf.Sqrt(1f - z * z);

        Vector3 localDirection = new Vector3(
            r * Mathf.Cos(theta),
            r * Mathf.Sin(theta),
            z
        );

        return Quaternion.FromToRotation(Vector3.forward, forward.normalized) * localDirection;
    }
}
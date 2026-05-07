using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class OilDrop : MonoBehaviour
{
    [Header("Physical Values For Calculation")]
    [Tooltip("Keep this at -9.81 so the hover voltage calculation stays physically correct.")]
    public Vector3 customGravity = new Vector3(0f, -9.81f, 0f);

    [Header("Launch Phase")]
    [Tooltip("How long the droplet keeps its launch movement before slow observation mode starts.")]
    public float launchPhaseDuration = 0.45f;

    [Tooltip("How much of the spray velocity is kept during launch.")]
    public float launchVelocityScale = 0.65f;

    [Tooltip("Maximum speed during launch phase.")]
    public float maxLaunchSpeed = 0.8f;

    [Header("Observation Fall Motion")]
    [Tooltip("Base falling speed for a 1.0 µm droplet after launch phase.")]
    public float baseFallSpeed = 0.08f;

    [Tooltip("Maximum falling speed after launch phase.")]
    public float maxFallSpeed = 0.22f;

    [Tooltip("How fast the droplet changes from launch movement to stable falling.")]
    public float velocitySmoothing = 5f;

    [Header("Radius Speed Difference")]
    public bool radiusAffectsFallSpeed = true;
    public float referenceRadiusMicrometer = 1.0f;
    public float radiusFallSpeedPower = 1.6f;

    [Header("Horizontal Damping After Launch")]
    [Tooltip("How quickly horizontal movement is reduced after the launch phase.")]
    public float horizontalDamping = 4f;

    [Header("Collision")]
    public bool destroyOnCollision = false;

    private Rigidbody rb;
    private Vector3 startPosition;
    private bool activeDrop;

    private float launchStartTime;
    private DropProperties dropProperties;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        dropProperties = GetComponent<DropProperties>();

        if (dropProperties == null)
            dropProperties = GetComponentInChildren<DropProperties>();

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        Collider col = GetComponent<Collider>();
        col.isTrigger = false;

        gameObject.SetActive(false);
    }

    public void Launch(Vector3 worldPos, Vector3 initialVelocity)
    {
        startPosition = worldPos;
        transform.position = worldPos;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 launchVelocity = initialVelocity * Mathf.Max(0f, launchVelocityScale);
        rb.linearVelocity = Vector3.ClampMagnitude(launchVelocity, maxLaunchSpeed);

        launchStartTime = Time.time;
        activeDrop = true;

        gameObject.SetActive(true);
    }

    public void ResetDrop()
    {
        activeDrop = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = startPosition;
        gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (!activeDrop)
            return;

        float timeSinceLaunch = Time.time - launchStartTime;

        if (timeSinceLaunch < launchPhaseDuration)
        {
            LimitLaunchSpeed();
            return;
        }

        ApplyObservationMotion();
    }

    private void LimitLaunchSpeed()
    {
        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, Mathf.Max(0.01f, maxLaunchSpeed));
    }

    private void ApplyObservationMotion()
    {
        Vector3 currentVelocity = rb.linearVelocity;

        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        horizontalVelocity = Vector3.Lerp(
            horizontalVelocity,
            Vector3.zero,
            Time.fixedDeltaTime * horizontalDamping
        );

        float targetFallSpeed = GetTargetFallSpeed();

        Vector3 targetVelocity = new Vector3(
            horizontalVelocity.x,
            -targetFallSpeed,
            horizontalVelocity.z
        );

        rb.linearVelocity = Vector3.Lerp(
            currentVelocity,
            targetVelocity,
            Time.fixedDeltaTime * velocitySmoothing
        );
    }

    private float GetTargetFallSpeed()
    {
        float radiusFactor = 1f;

        if (radiusAffectsFallSpeed && dropProperties != null && dropProperties.RadiusMicrometer > 0f)
        {
            float reference = Mathf.Max(0.01f, referenceRadiusMicrometer);
            radiusFactor = dropProperties.RadiusMicrometer / reference;
            radiusFactor = Mathf.Pow(radiusFactor, Mathf.Max(0f, radiusFallSpeedPower));
        }

        float fallSpeed = baseFallSpeed * radiusFactor;
        return Mathf.Clamp(fallSpeed, 0.01f, maxFallSpeed);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!destroyOnCollision)
            return;

        ResetDrop();
    }
}
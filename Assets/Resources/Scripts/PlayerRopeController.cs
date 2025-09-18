using UnityEngine;

/// <summary>
/// Attach this component to your player. It will:
/// - Allow the player to attach to the nearest grabbable rope segment (press E by default),
///   or you can call AttachToGrabbable(Grabbable g) from your existing Grab flow.
/// - Create a HingeJoint2D and DistanceJoint2D on the player to connect to the rope segment so
///   rope and player influence each other physically.
/// - Allow swinging with SwingLeftKey/SwingRightKey (A/D by default) by applying torque / forces.
/// - Allow climbing up/down the rope by changing the DistanceJoint2D distance with ClimbUp/Down.
/// - Detach with ReleaseKey (Space by default) or automatically when you jump (if JumpWithRelease = true).
/// 
/// Notes:
/// - The player must have a Rigidbody2D and at least one Collider2D.
/// - Player GameObject should be tagged "Player" to let RopeBuilder automatically ignore collisions.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerRopeController : MonoBehaviour
{
    [Header("Input (can be changed)")]
    public KeyCode GrabKey = KeyCode.E;
    public KeyCode ReleaseKey = KeyCode.Space;
    public KeyCode SwingLeftKey = KeyCode.A;
    public KeyCode SwingRightKey = KeyCode.D;
    public KeyCode ClimbUpKey = KeyCode.W;
    public KeyCode ClimbDownKey = KeyCode.S;

    [Header("Swing / Climb tuning")]
    public float swingForce = 5f; // applied as horizontal force to the player when swinging
    public float climbSpeed = 1.2f; // meters per second change in the distance joint
    public float minDistance = 0.3f; // smallest allowed distance (closest to rope)
    public float maxDistance = 10f;  // largest allowed distance

    [Header("Behavior")]
    public bool JumpWithRelease = true; // if player jumps we automatically release (optional)

    // Active joints created at runtime
    private HingeJoint2D hingeJoint;
    private DistanceJoint2D distanceJoint;

    private Rigidbody2D rb;

    // the grabbable segment we are attached to
    private Grabbable attachedGrabbable;
    private Rigidbody2D attachedBody;

    // used to snap player to attachment point once
    public float snapOnAttachDuration = 0.12f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // simple keyboard-driven grabbing for convenience (you can wire into your InputSystem)
        if (Input.GetKeyDown(GrabKey) && attachedGrabbable == null)
        {
            // find nearest Grabbable in range (we look for any Grabbable colliders near player)
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f);
            Grabbable best = null;
            float bestDist = float.MaxValue;
            foreach (var h in hits)
            {
                var g = h.GetComponent<Grabbable>();
                if (g == null) continue;
                Vector2 p = g.GetClosestPoint(transform.position);
                float d = Vector2.SqrMagnitude((Vector2)transform.position - p);
                if (d < bestDist)
                {
                    bestDist = d; best = g;
                }
            }

            if (best != null)
            {
                AttachToGrabbable(best);
            }
        }

        if (Input.GetKeyDown(ReleaseKey) && attachedGrabbable != null)
        {
            Detach();
        }

        if (attachedGrabbable != null)
        {
            HandleSwingInput();
            HandleClimbInput();
            // optional: release on jump if user has jump mapped through other code. This can be triggered externally instead.
            if (JumpWithRelease && Input.GetKeyDown(KeyCode.Space))
                Detach();
        }
    }

    private void HandleSwingInput()
    {
        // apply horizontal force to produce swinging
        if (Input.GetKey(SwingLeftKey))
        {
            rb.AddForce(Vector2.left * swingForce * Mathf.Clamp01(Time.deltaTime * 60f), ForceMode2D.Force);
        }
        if (Input.GetKey(SwingRightKey))
        {
            rb.AddForce(Vector2.right * swingForce * Mathf.Clamp01(Time.deltaTime * 60f), ForceMode2D.Force);
        }
    }

    private void HandleClimbInput()
    {
        if (distanceJoint == null) return;

        float delta = 0f;
        if (Input.GetKey(ClimbUpKey)) delta = -climbSpeed * Time.deltaTime;
        if (Input.GetKey(ClimbDownKey)) delta = climbSpeed * Time.deltaTime;

        if (Mathf.Abs(delta) > 0f)
        {
            float newDist = Mathf.Clamp(distanceJoint.distance + delta, minDistance, maxDistance);
            distanceJoint.distance = newDist;
        }
    }

    /// <summary>
    /// Public attach so your existing Grab() flow can call this: AttachToGrabbable(detector.lookedAtGrabbable)
    /// </summary>
    public void AttachToGrabbable(Grabbable grabbable)
    {
        if (grabbable == null) return;
        if (attachedGrabbable != null) Detach();

        attachedGrabbable = grabbable;
        Collider2D segCollider = grabbable.GetComponent<Collider2D>();
        if (segCollider == null)
        {
            Debug.LogWarning("Grabbable has no Collider2D!");
            return;
        }

        // try to get the rigidbody on the grabbable's root (rope segment)
        attachedBody = segCollider.attachedRigidbody;
        if (attachedBody == null)
        {
            Debug.LogWarning("Grabbable has no attached Rigidbody2D!");
            return;
        }

        // create hinge joint for rotational influence
        hingeJoint = gameObject.AddComponent<HingeJoint2D>();
        hingeJoint.connectedBody = attachedBody;
        Vector2 closest = segCollider.ClosestPoint(transform.position);
        // hinge anchor local on player (keep it at player's center)
        hingeJoint.autoConfigureConnectedAnchor = false;
        hingeJoint.anchor = Vector2.zero;
        // connectedAnchor in connected body local space:
        hingeJoint.connectedAnchor = attachedBody.transform.InverseTransformPoint(closest);

        // create distance joint to enforce rope-length and allow climbing by changing distance
        distanceJoint = gameObject.AddComponent<DistanceJoint2D>();
        distanceJoint.connectedBody = attachedBody;
        distanceJoint.autoConfigureConnectedAnchor = false;
        distanceJoint.anchor = Vector2.zero;
        distanceJoint.connectedAnchor = attachedBody.transform.InverseTransformPoint(closest);

        // compute initial distance
        float initialDist = Vector2.Distance(transform.position, closest);
        distanceJoint.distance = Mathf.Clamp(initialDist, minDistance, maxDistance);
        distanceJoint.maxDistanceOnly = false;
        distanceJoint.enableCollision = false; // player- rope collisions already ignored

        // optionally snap player to the closest point quickly so joint starts in a consistent position
        StartCoroutine(SnapToPointRoutine(closest, Mathf.Clamp01(snapOnAttachDuration)));

        // ensure the player's gravity scale continues to be its normal value (rope joint physics will handle swinging)
    }

    private System.Collections.IEnumerator SnapToPointRoutine(Vector2 worldPoint, float duration)
    {
        if (duration <= 0f)
            yield break;

        Vector2 start = transform.position;
        float t = 0f;
        // temporarily make kinematic-ish by freezing rotation? we'll lerp position using MovePosition
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;

        while (t < duration)
        {
            t += Time.deltaTime;
            Vector2 newPos = Vector2.Lerp(start, worldPoint, t / duration);
            rb.MovePosition(newPos);
            yield return null;
        }

        rb.gravityScale = originalGravity;
    }

    public void Detach()
    {
        if (hingeJoint != null) Destroy(hingeJoint);
        if (distanceJoint != null) Destroy(distanceJoint);
        attachedGrabbable = null;
        attachedBody = null;
    }

    private void OnDestroy()
    {
        Detach();
    }

    // Helpful: expose an API method so your existing Player.Grab(...) can call into this component to attach the player.
    // Example: From your Player.Grab(Grabbable g) method call GetComponent<PlayerRopeController>()?.AttachToGrabbable(g);
}

using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.InputSystem;

public class GrabbableDetector : MonoBehaviour
{
    public float jumpGrabRadius = 2f;
    public float reachGrabRadius = 1f;
    public LayerMask grabbableLayer;
    [HideInInspector] public Grabbable currentGrabbable;
    [HideInInspector] public Grabbable lookedAtGrabbable;
    [HideInInspector] public List<Grabbable> grabbablesInRange = new List<Grabbable>();
    [HideInInspector] public Vector2 moveInput;

    private Rigidbody2D rb;
    private Player player;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<Player>();
    }

    void Update()
    {
        foreach (var grabbable in grabbablesInRange)
        {
            grabbable.ResetHighlightColor();
        }
        if (lookedAtGrabbable)
            lookedAtGrabbable.ResetHighlightColor();

        grabbablesInRange.Clear();
        var radius = (player.isGrounded || player.isGrabbing) ? jumpGrabRadius : reachGrabRadius;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, grabbableLayer);

        foreach (var hit in hits)
        {
            Grabbable grabbable = hit.GetComponent<Grabbable>();
            if (grabbable && grabbable != currentGrabbable)
            {
                grabbablesInRange.Add(grabbable);
            }
        }

        foreach (var grabbable in grabbablesInRange)
        {
            grabbable.SetHighlightColor(Color.yellow);
        }

        lookedAtGrabbable = SelectGrabbableInMoveDirection();
        if (lookedAtGrabbable)
            lookedAtGrabbable.SetHighlightColor(Color.red);
    }

    private Grabbable SelectGrabbableInMoveDirection()
    {
        if (grabbablesInRange == null || grabbablesInRange.Count == 0)
            return null;

        Vector2 moveDir = GetMoveDirection();
        if (moveDir == Vector2.zero)
            return null;

        const float maxAngle = 45f;
        float minSqrDistance = float.MaxValue;
        Grabbable bestGrabbable = null;
        Vector2 origin = rb.position;

        foreach (var grabbable in grabbablesInRange)
        {
            Vector2 closestPoint = grabbable.GetClosestPoint(origin);
            Vector2 grabbableDir = (closestPoint - origin).normalized;
            float angle = Vector2.Angle(moveDir, grabbableDir);

            if (angle <= maxAngle)
            {
                float sqrDist = (closestPoint - origin).sqrMagnitude;
                if (sqrDist < minSqrDistance)
                {
                    minSqrDistance = sqrDist;
                    bestGrabbable = grabbable;
                }
            }
        }

        return bestGrabbable;
    }

    public Vector2 GetMoveDirection()
    {
        Vector2 moveDir = moveInput;

        if (moveDir == Vector2.zero)
            moveDir = rb.velocity.normalized;

        return moveDir;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, jumpGrabRadius);
    }
}
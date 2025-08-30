using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.InputSystem;

public class GrabbableDetector : MonoBehaviour
{
    public float detectionRadius = 0.75f;
    public LayerMask grabbableLayer;
    [HideInInspector] public Grabbable currentGrabbable;
    [HideInInspector] public Grabbable lookedAtGrabbable;
    [HideInInspector] public List<Grabbable> grabbablesInRange = new List<Grabbable>();
    [HideInInspector] public Vector2 moveInput;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, grabbableLayer);

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
        if (grabbablesInRange.Count == 0)
            return null;

        if (grabbablesInRange.Count == 1)
            return grabbablesInRange[0];

        Vector2 moveDir = GetMoveDirection();
        float maxAngle = 45f;
        float closestAngle = maxAngle;
        Grabbable bestGrabbable = null;

        foreach (var grabbable in grabbablesInRange)
        {
            Vector2 grabbableDir = (grabbable.GetClosestPoint(rb.position) - rb.position).normalized;
            float angle = Vector2.Angle(moveDir, grabbableDir);

            if (angle <= maxAngle && angle < closestAngle)
            {
                closestAngle = angle;
                bestGrabbable = grabbable;
            }
        }

        return bestGrabbable;
    }

    public Vector2 GetMoveDirection()
    {
        //Vector2 lookDir = Vector2.zero;

        //// Mouse input
        //Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        //lookDir = (mouseWorld - (Vector2)rb.position).normalized;

        //// Gamepad right stick
        //Vector2 stickInput = Gamepad.current?.rightStick.ReadValue() ?? Vector2.zero;
        //if (stickInput.magnitude > 0.1f)
        //    lookDir = stickInput.normalized;

        //return lookDir;

        Vector2 moveDir = moveInput;

        // Optional: for mouse, convert delta to world direction
        //if (Mouse.current != null)
        //{
        //    Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        //    moveDir = (mouseWorld - (Vector2)rb.position).normalized;
        //}
        //else if (moveInput.magnitude > 0.1f)
        //{
        //    moveDir = moveInput.normalized; // right stick
        //}

        return moveDir;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
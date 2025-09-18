using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float climbSpeed = 3f;
    public float gravityScale = 1f;
    public float groundJumpForce = 50f;
    public float grabJumpForce = 50f;
    public float swingForce = 10f;
    public float snapDurationFactor = 0.3f;

    private Rigidbody2D rb;
    private GrabbableDetector detector;
    private Vector2 moveInput;
    [HideInInspector] public bool isGrabbing = false;
    [HideInInspector] public bool isGrounded = false;
    private Coroutine snapCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        detector = GetComponent<GrabbableDetector>();
    }

    private void Update()
    {
        CheckGroundedStatus();
    }


    void FixedUpdate()
    {
        if (isGrabbing && detector.currentGrabbable && snapCoroutine == null)
        {
            var targetIsAllowed = detector.lookedAtGrabbable && (!detector.currentGrabbable.CompareTag("RopeSegment") || detector.currentGrabbable.transform.parent == detector.lookedAtGrabbable.transform.parent);

            if (targetIsAllowed && moveInput != Vector2.zero && Vector2.Distance(transform.position, detector.lookedAtGrabbable.GetClosestPoint(transform.position)) <= detector.reachGrabRadius)
            {
                // AutoGrab
                Grab(detector.lookedAtGrabbable);
            }
            else
            {
                // Move on Grabbable
                Vector3 desiredPos = rb.position + moveInput * climbSpeed * Time.fixedDeltaTime - detector.currentGrabbable.snapOffset;
                Vector3 closestPoint = detector.currentGrabbable.GetClosestPoint(desiredPos);

                rb.MovePosition(closestPoint);
            }

            if (detector.currentGrabbable.CompareTag("RopeSegment") && moveInput != Vector2.zero)
            {
                // Swing
                detector.currentGrabbable.GetComponent<Rigidbody2D>().AddForce(new Vector2(moveInput.x, 0) * swingForce * Mathf.Clamp01(Time.deltaTime * 60f), ForceMode2D.Force);
            }
        }
        else if (isGrounded)
        {
            //rb.velocity = new Vector2(moveInput.x * moveSpeed, rb.velocity.y);

            float targetSpeed = moveInput.x * moveSpeed;
            float speedDiff = targetSpeed - rb.velocity.x;
            float acceleration = 10f; // You can tweak this value
            float force = speedDiff * rb.mass * acceleration;

            rb.AddForce(new Vector2(force, 0f));
        }
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        detector.moveInput = moveInput;
    }


    public void OnLook(InputValue value)
    {
        //detector.lookInput = value.Get<Vector2>();
    }

    public void OnGrab(InputValue value)
    {
        if (!value.isPressed) return;

        if (detector.lookedAtGrabbable != null)
        {
            Grab(detector.lookedAtGrabbable);
        }
    }

    public void OnRelease(InputValue value)
    {
        if (value.isPressed && isGrabbing)
        {
            Release();
        }
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            if (isGrounded || isGrabbing)
            {
                Jump();
            }
        }
    }



    void Grab(Grabbable grabbable)
    {
        isGrabbing = true;
        detector.currentGrabbable = grabbable;

        if (snapCoroutine != null)
            StopCoroutine(snapCoroutine);

        snapCoroutine = StartCoroutine(SnapToGrabbable(grabbable));
        rb.gravityScale = 0f;
    }

    void Release()
    {
        isGrabbing = false;
        detector.currentGrabbable = null;
        rb.gravityScale = gravityScale;
    }

    void Jump()
    {
        if (isGrabbing)
        {
            rb.AddForce(moveInput * grabJumpForce, ForceMode2D.Impulse);
        }
        else
        {
            rb.AddForce(Vector2.up * groundJumpForce, ForceMode2D.Impulse);
        }

        if (isGrabbing)
        {
            Release();
        }
    }

    private void CheckGroundedStatus()
    {
        float checkRadius = 1f;
        LayerMask groundLayer = LayerMask.GetMask("Solid");
        var isGroundedLeft = Physics2D.Raycast(transform.position - new Vector3(transform.localScale.x * 0.5f, 0f), Vector2.down, checkRadius, groundLayer);
        var isGroundedRight = Physics2D.Raycast(transform.position + new Vector3(transform.localScale.x * 0.5f, 0f), Vector2.down, checkRadius, groundLayer);

        isGrounded = isGroundedLeft || isGroundedRight;
        // Debug.Log($"Grounded: {isGrounded}");
    }

    private IEnumerator SnapToGrabbable(Grabbable grabbable)
    {
        Vector3 startPos = rb.position;
        Vector3 targetPos = grabbable.GetClosestPoint(startPos);
        var dist = (targetPos - startPos).magnitude;
        var snapDuration = Mathf.Min(snapDurationFactor * dist, 0.15f);
        float elapsed = 0f;

        while (elapsed < snapDuration)
        {
            elapsed += Time.fixedDeltaTime;
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, elapsed / snapDuration);
            rb.MovePosition(newPos);
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(targetPos);

        snapCoroutine = null;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float climbSpeed = 3f;
    public float gravityScale = 1f;
    public float jumpForce = 5f;
    public float grabJumpBoost = 5f;
    public GrabbableDetector detector;
    public float snapDuration = 0.15f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isGrabbing = false;
    private bool isGrounded = false;
    private Coroutine snapCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        CheckGroundedStatus();
    }


    void FixedUpdate()
    {
        if (isGrabbing && detector.currentGrabbable && snapCoroutine == null)
        {
            Vector3 desiredPos = rb.position + moveInput * climbSpeed * Time.fixedDeltaTime;
            Vector3 closestPoint = detector.currentGrabbable.GetClosestPoint(desiredPos);

            rb.MovePosition(closestPoint);
        }
        else
        {
            rb.velocity = new Vector2(moveInput.x * moveSpeed, rb.velocity.y);
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
            rb.velocity = new Vector2(rb.velocity.x, grabJumpBoost);
        }
        else
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        if (isGrabbing)
        {
            Release();
        }
    }

    private void CheckGroundedStatus()
    {
        float checkRadius = 1.2f;
        LayerMask groundLayer = LayerMask.GetMask("Solid");
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, checkRadius, groundLayer);
        Debug.Log($"Grounded: {isGrounded}");
    }

    private IEnumerator SnapToGrabbable(Grabbable grabbable)
    {
        Vector3 startPos = rb.position;
        Vector3 targetPos = grabbable.GetClosestPoint(startPos) + grabbable.snapOffset;
        float elapsed = 0f;

        //rb.velocity = Vector2.zero;

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
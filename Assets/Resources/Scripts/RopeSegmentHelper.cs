using UnityEngine;

/// <summary>
/// Small helper attached to each generated rope segment.
/// Implements GetClosestPoint to match your Grabbable expectations
/// and exposes convenience data.
/// Note: the RopeBuilder already adds a Grabbable component; this script
/// simply wraps a few utilities and ensures the segment has required pieces.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class RopeSegmentHelper : MonoBehaviour
{
    private Collider2D col;
    private Rigidbody2D rb;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Returns the closest point on this collider to the provided position in world space.
    /// Kept to mirror the API used earlier.
    /// </summary>
    public Vector2 GetClosestPoint(Vector3 worldPosition)
    {
        if (col == null) col = GetComponent<Collider2D>();
        return col.ClosestPoint(worldPosition);
    }

    public Rigidbody2D Body => rb;
}

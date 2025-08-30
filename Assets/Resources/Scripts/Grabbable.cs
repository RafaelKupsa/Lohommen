using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Grabbable : MonoBehaviour
{
    public Vector2 snapOffset = Vector2.zero;
    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    public Vector2 GetClosestPoint(Vector3 position)
    {
        return col.ClosestPoint(position) + snapOffset;
    }

    void OnDrawGizmos()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
        }
    }

    public void SetHighlightColor(Color color)
    {
        spriteRenderer.color = color;
    }

    public void ResetHighlightColor()
    {
        spriteRenderer.color = originalColor;
    }
}
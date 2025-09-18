using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class RopeBuilder : MonoBehaviour
{
    [Header("Rope geometry")]
    public int segmentCount = 20;
    public float segmentLength = 0.25f;     // distance between segment centers
    public float colliderRadius = 0.06f;
    public float segmentMass = 0.1f;
    public float gravityScale = 1f;
    public float damping = 0.1f;
    public float angularDrag = 0.05f;

    [Header("Anchor")]
    public Transform anchorTransform;
    public Rigidbody2D anchorRigidbody;

    [Header("References")]
    public PhysicsMaterial2D segmentMaterial;
    public GameObject segmentPrefab; // optional
    public LayerMask grabbableLayer;

    [Header("Runtime")]
    public bool hideSegmentsInHierarchy = false;

    [HideInInspector] public List<GameObject> segments = new List<GameObject>();

    private const string segmentNamePrefix = "RopeSegment_";

    [ContextMenu("Build Rope")]
    public void BuildRope()
    {
        ClearExisting();

        if (segmentCount <= 0) segmentCount = 1;

        // base anchor world point (where rope should hang from)
        Vector3 baseAnchorPoint = (anchorTransform != null) ? anchorTransform.position
                                : (anchorRigidbody != null) ? (Vector3)anchorRigidbody.position
                                : transform.position;

        // place the top segment center half a segment length below the anchor point so rope naturally hangs
        Vector3 topCenter = baseAnchorPoint + Vector3.down * (segmentLength * 0.5f);

        GameObject previous = null;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 pos = topCenter + Vector3.down * (i * segmentLength);

            GameObject seg;
            if (segmentPrefab != null)
            {
                seg = Instantiate(segmentPrefab, pos, Quaternion.identity, this.transform);
            }
            else
            {
                seg = new GameObject(segmentNamePrefix + i);
                seg.transform.parent = this.transform;
                seg.transform.position = pos;

#if UNITY_EDITOR
                // small editor visual; remove in your final art pipeline if you want
                var sr = seg.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                sr.color = new Color(0.1698113f, 0.1698113f, 0.1698113f);
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = new Vector2(colliderRadius * 2f, colliderRadius * 2f);
                sr.sortingOrder = 0;
#endif
            }
            seg.layer = LayerMaskToFirstLayerIndex(grabbableLayer);
            seg.tag = "RopeSegment";

            // Rigidbody
            Rigidbody2D rb = seg.GetComponent<Rigidbody2D>();
            if (rb == null) rb = seg.AddComponent<Rigidbody2D>();
            rb.mass = segmentMass;
            rb.angularDrag = angularDrag;
            rb.drag = damping;
            rb.gravityScale = gravityScale;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Collider
            CircleCollider2D col = seg.GetComponent<CircleCollider2D>();
            if (col == null) col = seg.AddComponent<CircleCollider2D>();
            col.radius = colliderRadius;
            col.offset = Vector2.zero;
            col.sharedMaterial = segmentMaterial;

            // Hinge joint
            HingeJoint2D hinge = seg.GetComponent<HingeJoint2D>();
            if (hinge == null) hinge = seg.AddComponent<HingeJoint2D>();
            hinge.autoConfigureConnectedAnchor = false;

            if (i == 0)
            {
                // Top segment: connect to anchorTransform or anchorRigidbody (or world point)
                if (anchorRigidbody != null)
                {
                    hinge.connectedBody = anchorRigidbody;
                    Vector3 worldAnchor = baseAnchorPoint;
                    hinge.connectedAnchor = (Vector2)anchorRigidbody.transform.InverseTransformPoint(worldAnchor);
                    hinge.anchor = (Vector2)seg.transform.InverseTransformPoint(worldAnchor);
                }
                else
                {
                    hinge.connectedBody = null; // world anchor
                    Vector3 worldAnchor = baseAnchorPoint;
                    hinge.connectedAnchor = (Vector2)worldAnchor; // when connectedBody == null this is world-space
                    hinge.anchor = (Vector2)seg.transform.InverseTransformPoint(worldAnchor);
                }
            }
            else
            {
                // connect to previous segment: put the joint at the midpoint between the two centers
                Rigidbody2D prevRb = previous.GetComponent<Rigidbody2D>();
                hinge.connectedBody = prevRb;
                Vector3 midpoint = (seg.transform.position + previous.transform.position) * 0.5f;
                hinge.anchor = (Vector2)seg.transform.InverseTransformPoint(midpoint);
                hinge.connectedAnchor = (Vector2)previous.transform.InverseTransformPoint(midpoint);
            }

            // make it grabbable so your existing detector can find it
            var grabbable = seg.GetComponent<Grabbable>();
            if (grabbable == null) grabbable = seg.AddComponent<Grabbable>();

            segments.Add(seg);
            previous = seg;
        }

#if UNITY_EDITOR
        foreach (var s in segments)
        {
            if (s != null) s.hideFlags = hideSegmentsInHierarchy ? HideFlags.HideInHierarchy : HideFlags.None;
        }
#endif

        if (Application.isPlaying) IgnorePlayerCollisions();

        Debug.Log($"Rope built with {segments.Count} segments (segmentLength={segmentLength}).");
    }

    [ContextMenu("Clear Rope")]
    public void ClearExisting()
    {
        var children = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++) children.Add(transform.GetChild(i).gameObject);

        foreach (var c in children)
        {
            if (Application.isPlaying) Destroy(c);
            else
#if UNITY_EDITOR
                DestroyImmediate(c);
#else
                Destroy(c);
#endif
        }

        segments.Clear();
    }

    private void Awake()
    {
        if (Application.isPlaying && (segments == null || segments.Count == 0))
            BuildRope();

        if (Application.isPlaying)
            IgnorePlayerCollisions();
    }

    private void IgnorePlayerCollisions()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Collider2D[] playerCols = player.GetComponentsInChildren<Collider2D>();
        if (playerCols == null || playerCols.Length == 0) return;

        foreach (var seg in segments)
        {
            if (seg == null) continue;
            var segCols = seg.GetComponentsInChildren<Collider2D>();
            foreach (var sc in segCols)
            {
                foreach (var pc in playerCols)
                {
                    Physics2D.IgnoreCollision(sc, pc, true);
                }
            }
        }
    }

    // helper
    private int LayerMaskToFirstLayerIndex(LayerMask mask)
    {
        int m = mask.value;
        for (int i = 0; i < 32; i++)
            if ((m & (1 << i)) != 0)
                return i;
        return 0; // fallback
    }
}

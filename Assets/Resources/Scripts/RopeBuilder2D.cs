using UnityEngine;

public class RopeBuilder2D : MonoBehaviour
{
    [Header("Setup")]
    public Transform anchor;            // world anchor point
    public GameObject linkPrefab;       // prefab must have Rigidbody2D + HingeJoint2D + DistanceJoint2D
    [Range(2, 200)] public int segmentCount = 25;
    public float linkSpacing = 0.2f;
    public Rigidbody2D endAttach;       // optional payload at rope end

    [Header("Hinge Options")]
    public bool limitHingeAngles = false;
    public float hingeLower = -110f;
    public float hingeUpper = -70f;

    void Start()
    {
        Build();
    }

    void Build()
    {
        Rigidbody2D prevBody = null;
        Vector2 startPos = anchor ? (Vector2)anchor.position : (Vector2)transform.position;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector2 pos = startPos + Vector2.down * (i * linkSpacing);
            var link = Instantiate(linkPrefab, pos, Quaternion.identity, transform);

            var rb = link.GetComponent<Rigidbody2D>();
            var hinge = link.GetComponent<HingeJoint2D>();
            var spring = link.GetComponent<SpringJoint2D>();

            // Connect to world or previous link
            if (i == 0)
            {
                hinge.connectedBody = null;
                hinge.connectedAnchor = startPos;

                spring.connectedBody = null;
                spring.connectedAnchor = startPos;
            }
            else
            {
                hinge.connectedBody = prevBody;
                hinge.connectedAnchor = Vector2.zero;

                spring.connectedBody = prevBody;
                spring.connectedAnchor = Vector2.zero;
            }

            // Hinge limits (optional)
            if (limitHingeAngles)
            {
                hinge.useLimits = true;
                JointAngleLimits2D lim = new();
                lim.min = hingeLower;
                lim.max = hingeUpper;
                hinge.limits = lim;
            }

            spring.autoConfigureDistance = false;
            spring.distance = linkSpacing;
            spring.frequency = 10f;
            spring.dampingRatio = 0f;

            prevBody = rb;
        }

        // Attach payload/player at the end
        if (endAttach != null && prevBody != null)
        {
            var endHinge = endAttach.gameObject.AddComponent<HingeJoint2D>();
            endHinge.autoConfigureConnectedAnchor = false;
            endHinge.connectedBody = prevBody;
            endHinge.connectedAnchor = Vector2.zero;
            endHinge.anchor = Vector2.zero;

            var endSpring = endAttach.gameObject.AddComponent<SpringJoint2D>();
            endSpring.autoConfigureConnectedAnchor = false;
            endSpring.connectedBody = prevBody;
            endSpring.connectedAnchor = Vector2.zero;
            endSpring.distance = linkSpacing;
            endSpring.frequency = 10f;
            endSpring.dampingRatio = 0f;
        }
    }
}

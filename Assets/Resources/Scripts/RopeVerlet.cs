//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;
//using UnityEngine.InputSystem;

//public class RopeVerlet : MonoBehaviour
//{
//    [Header("Rope")]
//    [SerializeField] private int _numOfRopeSegments = 25;
//    [SerializeField] private float _ropeSegmentLength = 0.225f;

//    [Header("Physics")]
//    [SerializeField] private Vector2 _gravityForce = new Vector2(0, -2);
//    [SerializeField] private float _dampingFactor = 0.98f;

//    [Header("Constraints")]
//    [SerializeField] private int _numOfConstraintRuns = 50;

//    [Header("Anchors")]
//    [SerializeField] private Transform _startAnchor;
//    [SerializeField] private Transform _endAnchor;

//    private LineRenderer _lineRenderer;
//    private List<RopeSegment> _ropeSegments = new List<RopeSegment>();
//    private bool[] _pinned; // which segments are fixed
//    private Vector2 _startPosition, _endPosition;
//    private bool _startPinned = false, _endPinned = false;

//    public struct RopeSegment
//    {
//        public Vector2 CurrentPosition;
//        public Vector2 OldPosition;

//        public RopeSegment(Vector2 pos)
//        {
//            CurrentPosition = pos;
//            OldPosition = pos;
//        }
//    }

//    private void Awake()
//    {
//        _lineRenderer = GetComponent<LineRenderer>();
//        _lineRenderer.positionCount = _numOfRopeSegments;

//        _pinned = new bool[_numOfRopeSegments];

//        // default start point under mouse (same as your original)
//        Vector3 worldStart = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
//        worldStart.z = 0f;

//        Vector2 spawn = worldStart;
//        for (var i = 0; i < _numOfRopeSegments; i++)
//        {
//            _ropeSegments.Add(new RopeSegment(spawn));
//            spawn.y -= _ropeSegmentLength;
//        }

//        // set default start/end positions to the current endpoints
//        _startPosition = _ropeSegments[0].CurrentPosition;
//        _endPosition = _ropeSegments[_numOfRopeSegments - 1].CurrentPosition;

//        if (_startAnchor != null && _startAnchor.GetComponent<Rigidbody2D>() != null)
//            ConstrainStartAnchor();

//        if (_endAnchor != null && _endAnchor.GetComponent<Rigidbody2D>() != null)
//            ConstrainEndAnchor();
//    }

//    private void Update()
//    {
//        DrawRope();
//    }

//    private void FixedUpdate()
//    {
//        Simulate();

//        for (var i = 0; i < _numOfConstraintRuns; i++)
//        {
//            ApplyContraints();
//        }

//        if (_startAnchor != null && _startAnchor.GetComponent<Rigidbody2D>() != null)
//        {
//            var joint = _startAnchor.GetComponent<DistanceJoint2D>();
//            joint.connectedAnchor = _ropeSegments.First().CurrentPosition;
//        }

//        if (_endAnchor != null && _endAnchor.GetComponent<Rigidbody2D>() != null)
//        {
//            var joint = _endAnchor.GetComponent<DistanceJoint2D>();
//            joint.connectedAnchor = _ropeSegments.Last().CurrentPosition;
//        }

//        if (_playerHinge != null && _playerSegmentIndex >= 0)
//        {
//            Vector2 target = _ropeSegments[_playerSegmentIndex].CurrentPosition;
//            // smooth the connectedAnchor movement so the player doesn't teleport
//            Vector2 current = _playerHinge.connectedAnchor;
//            Vector2 smoothed = Vector2.Lerp(current, target, Mathf.Clamp01(_climbSmoothSpeed * Time.fixedDeltaTime));
//            _playerHinge.connectedAnchor = smoothed;

//            // Optionally, if you want the player to "pull" the rope:
//            // _ropeSegments[_playerSegmentIndex].CurrentPosition = _playerRb.position;
//            // _ropeSegments[_playerSegmentIndex].OldPosition = _playerRb.position;
//        }

//    }

//    private void DrawRope()
//    {
//        Vector3[] ropePositions = new Vector3[_numOfRopeSegments];
//        for (var i = 0; i < _ropeSegments.Count; i++)
//        {
//            Vector2 p = _ropeSegments[i].CurrentPosition;
//            ropePositions[i] = new Vector3(p.x, p.y, 0f);
//        }

//        _lineRenderer.SetPositions(ropePositions);
//    }

//    private void Simulate()
//    {
//        for (var i = 0; i < _ropeSegments.Count; i++)
//        {
//            RopeSegment segment = _ropeSegments[i];

//            Vector2 velocity = (segment.CurrentPosition - segment.OldPosition) * _dampingFactor;
//            segment.OldPosition = segment.CurrentPosition;
//            segment.CurrentPosition += velocity;
//            segment.CurrentPosition += _gravityForce * Time.fixedDeltaTime;

//            _ropeSegments[i] = segment;
//        }
//    }

//    private void ApplyContraints()
//    {
//        // enforce anchored / pinned endpoints before constraint solving
//        if (_startAnchor != null)
//        {
//            Vector3 p = _startAnchor.position;
//            RopeSegment s0 = _ropeSegments[0];
//            s0.CurrentPosition = new Vector2(p.x, p.y);
//            s0.OldPosition = s0.CurrentPosition;
//            _ropeSegments[0] = s0;
//            _pinned[0] = true;
//        }
//        else if (_startPinned)
//        {
//            RopeSegment s0 = _ropeSegments[0];
//            s0.CurrentPosition = _startPosition;
//            s0.OldPosition = s0.CurrentPosition;
//            _ropeSegments[0] = s0;
//            _pinned[0] = true;
//        }
//        else
//        {
//            _pinned[0] = false;
//        }

//        if (_endAnchor != null)
//        {
//            Vector3 p = _endAnchor.position;
//            int last = _numOfRopeSegments - 1;
//            RopeSegment sl = _ropeSegments[last];
//            sl.CurrentPosition = new Vector2(p.x, p.y);
//            sl.OldPosition = sl.CurrentPosition;
//            _ropeSegments[last] = sl;
//            _pinned[last] = true;
//        }
//        else if (_endPinned)
//        {
//            int last = _numOfRopeSegments - 1;
//            RopeSegment sl = _ropeSegments[last];
//            sl.CurrentPosition = _endPosition;
//            sl.OldPosition = sl.CurrentPosition;
//            _ropeSegments[last] = sl;
//            _pinned[last] = true;
//        }
//        else
//        {
//            _pinned[_numOfRopeSegments - 1] = false;
//        }

//        // solve constraints between each pair
//        for (var i = 0; i < _numOfRopeSegments - 1; i++)
//        {
//            RopeSegment currentSegment = _ropeSegments[i];
//            RopeSegment nextSegment = _ropeSegments[i + 1];

//            float dist = (currentSegment.CurrentPosition - nextSegment.CurrentPosition).magnitude;
//            float difference = (dist - _ropeSegmentLength);
//            Vector2 changeDir = (currentSegment.CurrentPosition - nextSegment.CurrentPosition).normalized;
//            Vector2 changeVector = changeDir * difference;

//            bool pinnedA = _pinned[i];
//            bool pinnedB = _pinned[i + 1];

//            if (pinnedA && pinnedB)
//            {
//                // both pinned -> nothing to do
//            }
//            else if (pinnedA && !pinnedB)
//            {
//                // A pinned, move B fully
//                nextSegment.CurrentPosition += changeVector;
//            }
//            else if (!pinnedA && pinnedB)
//            {
//                // B pinned, move A fully
//                currentSegment.CurrentPosition -= changeVector;
//            }
//            else
//            {
//                // neither pinned, move both half
//                currentSegment.CurrentPosition -= changeVector * 0.5f;
//                nextSegment.CurrentPosition += changeVector * 0.5f;
//            }

//            _ropeSegments[i] = currentSegment;
//            _ropeSegments[i + 1] = nextSegment;
//        }
//    }

//    // --- Public API to pin/unpin endpoints ---

//    /// <summary>
//    /// Pin the start of the rope to a specific world position (or unpin by passing pinned=false).
//    /// Also immediately moves the first segment to the position.
//    /// </summary>
//    public void SetStartPosition(Vector2 worldPosition, bool pinned = true)
//    {
//        _startAnchor = null;
//        _startPosition = worldPosition;
//        _startPinned = pinned;
//        _pinned[0] = pinned;

//        RopeSegment s0 = _ropeSegments[0];
//        s0.CurrentPosition = worldPosition;
//        s0.OldPosition = worldPosition;
//        _ropeSegments[0] = s0;
//    }

//    /// <summary>
//    /// Pin the end of the rope to a specific world position.
//    /// </summary>
//    public void SetEndPosition(Vector2 worldPosition, bool pinned = true)
//    {
//        _endAnchor = null;
//        _endPosition = worldPosition;
//        _endPinned = pinned;

//        int last = _numOfRopeSegments - 1;
//        _pinned[last] = pinned;

//        RopeSegment sl = _ropeSegments[last];
//        sl.CurrentPosition = worldPosition;
//        sl.OldPosition = worldPosition;
//        _ropeSegments[last] = sl;
//    }

//    /// <summary>
//    /// Attach start to a Transform (follow it). Pass null to detach.
//    /// </summary>
//    public void SetStartAnchor(Transform anchor)
//    {
//        _startAnchor = anchor;
//        _startPinned = anchor != null;
//        if (_startAnchor != null && _startAnchor.GetComponent<Rigidbody2D>() != null)
//            ConstrainStartAnchor();
//    }

//    /// <summary>
//    /// Attach end to a Transform (follow it). Pass null to detach.
//    /// </summary>
//    public void SetEndAnchor(Transform anchor)
//    {
//        _endAnchor = anchor;
//        _endPinned = anchor != null;
//        if (_endAnchor != null && _endAnchor.GetComponent<Rigidbody2D>() != null)
//            ConstrainEndAnchor();
//    }

//    private void ConstrainStartAnchor()
//    {
//        var joint = _startAnchor.gameObject.AddComponent<DistanceJoint2D>();
//        Debug.Log($"Adding Distance Joint to {_startAnchor.name}");
//        joint.autoConfigureDistance = false;
//        //joint.connectedAnchor = _ropeSegments.First().CurrentPosition;
//        joint.distance = _ropeSegmentLength * (_numOfRopeSegments - 1);
//    }

//    private void ConstrainEndAnchor()
//    {
//        var joint = _endAnchor.gameObject.AddComponent<DistanceJoint2D>();
//        Debug.Log($"Adding Distance Joint to {_endAnchor.name}");
//        joint.autoConfigureDistance = false;
//        //joint.connectedAnchor = _ropeSegments.Last().CurrentPosition;
//        joint.distance = _ropeSegmentLength * (_numOfRopeSegments - 1);
//    }

//    public void AttachPlayer(Rigidbody2D playerRb)
//    {
//        _playerRb = playerRb;

//        // find nearest segment to player's world position
//        Vector2 playerPos = _playerRb.position;
//        float bestDist = float.MaxValue;
//        int bestIndex = 0;
//        for (int i = 0; i < _ropeSegments.Count; i++)
//        {
//            float d = Vector2.SqrMagnitude(_ropeSegments[i].CurrentPosition - playerPos);
//            if (d < bestDist)
//            {
//                bestDist = d;
//                bestIndex = i;
//            }
//        }

//        _playerSegmentIndex = bestIndex;

//        // remove old hinge if present
//        var old = _playerRb.GetComponent<HingeJoint2D>();
//        if (old != null) Destroy(old);

//        _playerHinge = _playerRb.gameObject.AddComponent<HingeJoint2D>();
//        _playerHinge.autoConfigureConnectedAnchor = false;
//        _playerHinge.connectedBody = null; // connect to world
//        _playerHinge.connectedAnchor = _ropeSegments[_playerSegmentIndex].CurrentPosition;
//        _playerHinge.anchor = Vector2.zero; // adjust if player pivot offset needed

//        // optional: reduce player's gravity while attached for better feel
//        //_playerRb.gravityScale = 0.8f;
//    }

//    public void DetachPlayer()
//    {
//        if (_playerRb == null) return;
//        var hinge = _playerRb.GetComponent<HingeJoint2D>();
//        if (hinge != null) Destroy(hinge);
//        _playerHinge = null;
//        _playerRb = null;
//        _playerSegmentIndex = -1;
//    }
//}


using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Verlet rope implementation with utilities for attaching a player (HingeJoint2D),
/// climbing (discrete or smooth), and optional player influence on the rope.
/// Attach this script to a GameObject that has a LineRenderer component.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class RopeVerlet : Grabbable
{
    [Header("Rope geometry")]
    [SerializeField, Min(2)] private int _numOfRopeSegments = 25;
    [SerializeField, Min(0.01f)] private float _ropeSegmentLength = 0.225f;

    [Header("Physics")]
    [SerializeField] private Vector2 _gravityForce = new Vector2(0f, -9.81f);
    [SerializeField, Range(0f, 1f)] private float _dampingFactor = 0.98f;

    [Header("Constraints")]
    [SerializeField, Range(1, 50)] private int _numOfConstraintRuns = 10;

    [Header("Anchors (optional)")]
    [SerializeField] private Transform _startAnchor;
    [SerializeField] private Transform _endAnchor;

    [Header("Player / Climbing")]
    [SerializeField] private float _climbSmoothSpeed = 10f; // used to lerp hinge anchor for smoothing
    [SerializeField] private bool _playerInfluencesRope = true; // if true, rope will be moved to match player pos of attached segment

    private LineRenderer _lineRenderer;
    private List<RopeSegment> _ropeSegments;
    private bool[] _pinned; // pinned segments (start/end pins)
    private Vector2 _startPosition, _endPosition;
    private bool _startPinned = false, _endPinned = false;

    // anchor joints (added once if anchor has Rigidbody2D)
    private DistanceJoint2D _startAnchorJoint;
    private DistanceJoint2D _endAnchorJoint;

    // player attachment
    private Rigidbody2D _playerRb;
    private HingeJoint2D _playerHinge;
    private float _playerT = -1f; // continuous position along rope: 0..(_numOfRopeSegments - 1); -1 means not attached

    // struct for Verlet segments
    public struct RopeSegment
    {
        public Vector2 CurrentPosition;
        public Vector2 OldPosition;
        public RopeSegment(Vector2 pos)
        {
            CurrentPosition = pos;
            OldPosition = pos;
        }
    }

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        InitializeRope();
    }

    private void InitializeRope()
    {
        // prepare containers
        _ropeSegments = new List<RopeSegment>(_numOfRopeSegments);
        _pinned = new bool[_numOfRopeSegments];

        // Initialize line renderer
        _lineRenderer.positionCount = _numOfRopeSegments;

        // spawn start position (default under camera center if available, otherwise under this transform)
        Vector3 worldStart = Vector3.zero;
        if (Camera.main != null)
        {
            // screen center fallback
            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            worldStart = Camera.main.ScreenToWorldPoint(screenCenter);
            worldStart.z = 0f;
        }
        else
        {
            worldStart = transform.position;
            worldStart.z = 0f;
        }

        Vector2 spawn = worldStart;
        for (int i = 0; i < _numOfRopeSegments; i++)
        {
            _ropeSegments.Add(new RopeSegment(spawn));
            spawn.y -= _ropeSegmentLength;
        }

        _startPosition = _ropeSegments[0].CurrentPosition;
        _endPosition = _ropeSegments[_numOfRopeSegments - 1].CurrentPosition;

        // setup anchor joints if necessary
        if (_startAnchor != null && _startAnchor.GetComponent<Rigidbody2D>() != null)
            SetupStartAnchorJoint();

        if (_endAnchor != null && _endAnchor.GetComponent<Rigidbody2D>() != null)
            SetupEndAnchorJoint();
    }

    private void SetupStartAnchorJoint()
    {
        if (_startAnchor == null) return;
        if (_startAnchorJoint != null) return; // already added

        _startAnchorJoint = _startAnchor.GetComponent<DistanceJoint2D>();
        if (_startAnchorJoint == null)
            _startAnchorJoint = _startAnchor.gameObject.AddComponent<DistanceJoint2D>();

        _startAnchorJoint.autoConfigureDistance = false;
        _startAnchorJoint.connectedBody = null; // connecting to world anchor
        _startAnchorJoint.distance = _ropeSegmentLength * (_numOfRopeSegments - 1);
    }

    private void SetupEndAnchorJoint()
    {
        if (_endAnchor == null) return;
        if (_endAnchorJoint != null) return; // already added

        _endAnchorJoint = _endAnchor.GetComponent<DistanceJoint2D>();
        if (_endAnchorJoint == null)
            _endAnchorJoint = _endAnchor.gameObject.AddComponent<DistanceJoint2D>();

        _endAnchorJoint.autoConfigureDistance = false;
        _endAnchorJoint.connectedBody = null;
        _endAnchorJoint.distance = _ropeSegmentLength * (_numOfRopeSegments - 1);
    }

    private void Update()
    {
        DrawRope();
    }

    private void FixedUpdate()
    {
        Simulate();

        // constraint solves (iterative)
        for (int i = 0; i < _numOfConstraintRuns; i++)
            ApplyConstraints();

        // update anchor joints' connected anchors (follow endpoints of verlet rope)
        if (_startAnchorJoint != null)
        {
            _startAnchorJoint.connectedAnchor = _ropeSegments.First().CurrentPosition;
        }
        if (_endAnchorJoint != null)
        {
            _endAnchorJoint.connectedAnchor = _ropeSegments.Last().CurrentPosition;
        }

        // update player's hinge connected anchor (smoothly)
        if (_playerHinge != null && _playerT >= 0f)
        {
            Vector2 target = SampleRopePosition(_playerT);
            Vector2 current = _playerHinge.connectedAnchor;
            Vector2 smoothed = Vector2.Lerp(current, target, Mathf.Clamp01(_climbSmoothSpeed * Time.fixedDeltaTime));
            _playerHinge.connectedAnchor = smoothed;

            // optionally let the player influence the rope by pinning the segment
            if (_playerInfluencesRope)
            {
                // push the rope segment(s) toward the player position
                float ft = Mathf.Clamp(_playerT, 0f, _numOfRopeSegments - 1);
                int floor = Mathf.FloorToInt(ft);
                float frac = ft - floor;

                // move nearest segment(s) to reduce pop. We set CurrentPosition and OldPosition so verlet uses it.
                Vector2 playerPos = _playerRb.position;
                if (frac <= 0f || floor >= _numOfRopeSegments - 1)
                {
                    // single segment
                    RopeSegment s = _ropeSegments[floor];
                    s.CurrentPosition = playerPos;
                    s.OldPosition = playerPos;
                    _ropeSegments[floor] = s;
                }
                else
                {
                    // influence two segments proportionally
                    RopeSegment a = _ropeSegments[floor];
                    RopeSegment b = _ropeSegments[floor + 1];
                    a.CurrentPosition = Vector2.Lerp(a.CurrentPosition, playerPos, 0.9f);
                    a.OldPosition = a.CurrentPosition;
                    b.CurrentPosition = Vector2.Lerp(b.CurrentPosition, playerPos, 0.9f);
                    b.OldPosition = b.CurrentPosition;
                    _ropeSegments[floor] = a;
                    _ropeSegments[floor + 1] = b;
                }
            }
        }
    }

    private void DrawRope()
    {
        if (_lineRenderer == null) return;
        int count = _ropeSegments.Count;
        _lineRenderer.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            Vector2 p = _ropeSegments[i].CurrentPosition;
            _lineRenderer.SetPosition(i, new Vector3(p.x, p.y, 0f));
        }
    }

    private void Simulate()
    {
        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            RopeSegment seg = _ropeSegments[i];
            Vector2 velocity = (seg.CurrentPosition - seg.OldPosition) * _dampingFactor;
            seg.OldPosition = seg.CurrentPosition;
            seg.CurrentPosition += velocity;
            seg.CurrentPosition += _gravityForce * Time.fixedDeltaTime;
            _ropeSegments[i] = seg;
        }
    }

    private void ApplyConstraints()
    {
        // pinned endpoints / anchors
        if (_startAnchor != null)
        {
            Vector3 p = _startAnchor.position;
            RopeSegment s0 = _ropeSegments[0];
            s0.CurrentPosition = new Vector2(p.x, p.y);
            s0.OldPosition = s0.CurrentPosition;
            _ropeSegments[0] = s0;
            _pinned[0] = true;
        }
        else if (_startPinned)
        {
            RopeSegment s0 = _ropeSegments[0];
            s0.CurrentPosition = _startPosition;
            s0.OldPosition = s0.CurrentPosition;
            _ropeSegments[0] = s0;
            _pinned[0] = true;
        }
        else
        {
            _pinned[0] = false;
        }

        if (_endAnchor != null)
        {
            Vector3 p = _endAnchor.position;
            int last = _numOfRopeSegments - 1;
            RopeSegment sl = _ropeSegments[last];
            sl.CurrentPosition = new Vector2(p.x, p.y);
            sl.OldPosition = sl.CurrentPosition;
            _ropeSegments[last] = sl;
            _pinned[last] = true;
        }
        else if (_endPinned)
        {
            int last = _numOfRopeSegments - 1;
            RopeSegment sl = _ropeSegments[last];
            sl.CurrentPosition = _endPosition;
            sl.OldPosition = sl.CurrentPosition;
            _ropeSegments[last] = sl;
            _pinned[last] = true;
        }
        else
        {
            _pinned[_numOfRopeSegments - 1] = false;
        }

        // pairwise constraints
        for (int i = 0; i < _numOfRopeSegments - 1; i++)
        {
            RopeSegment a = _ropeSegments[i];
            RopeSegment b = _ropeSegments[i + 1];

            Vector2 delta = a.CurrentPosition - b.CurrentPosition;
            float dist = delta.magnitude;

            // guard against zero-distance (avoid NaN when normalizing)
            if (dist <= Mathf.Epsilon)
                continue;

            // compute how much to move: (dist - desiredLength) / dist gives fractional offset
            float diff = (dist - _ropeSegmentLength) / dist;
            Vector2 offset = delta * diff;

            bool pinnedA = _pinned[i];
            bool pinnedB = _pinned[i + 1];

            if (pinnedA && pinnedB)
            {
                // nothing
            }
            else if (pinnedA && !pinnedB)
            {
                b.CurrentPosition += offset;
            }
            else if (!pinnedA && pinnedB)
            {
                a.CurrentPosition -= offset;
            }
            else
            {
                a.CurrentPosition -= offset * 0.5f;
                b.CurrentPosition += offset * 0.5f;
            }

            _ropeSegments[i] = a;
            _ropeSegments[i + 1] = b;
        }
    }

    // --- Public API for pinning endpoints ---
    public void SetStartPosition(Vector2 worldPosition, bool pinned = true)
    {
        _startAnchor = null;
        _startPosition = worldPosition;
        _startPinned = pinned;
        _pinned[0] = pinned;

        RopeSegment s0 = _ropeSegments[0];
        s0.CurrentPosition = worldPosition;
        s0.OldPosition = worldPosition;
        _ropeSegments[0] = s0;
    }

    public void SetEndPosition(Vector2 worldPosition, bool pinned = true)
    {
        _endAnchor = null;
        _endPosition = worldPosition;
        _endPinned = pinned;

        int last = _numOfRopeSegments - 1;
        _pinned[last] = pinned;

        RopeSegment sl = _ropeSegments[last];
        sl.CurrentPosition = worldPosition;
        sl.OldPosition = worldPosition;
        _ropeSegments[last] = sl;
    }

    public void SetStartAnchor(Transform anchor)
    {
        _startAnchor = anchor;
        _startPinned = anchor != null;
        if (_startAnchor != null && _startAnchor.GetComponent<Rigidbody2D>() != null)
            SetupStartAnchorJoint();
    }

    public void SetEndAnchor(Transform anchor)
    {
        _endAnchor = anchor;
        _endPinned = anchor != null;
        if (_endAnchor != null && _endAnchor.GetComponent<Rigidbody2D>() != null)
            SetupEndAnchorJoint();
    }

    // --- Player attach / climb API ---

    /// <summary>
    /// Attach the supplied player Rigidbody2D to the nearest rope segment.
    /// Creates a HingeJoint2D on the player that uses a world-space connectedAnchor to follow the rope.
    /// </summary>
    public void AttachPlayer(Rigidbody2D playerRb, bool snapPlayerToRope = false)
    {
        if (playerRb == null) return;

        _playerRb = playerRb;

        // find nearest segment index
        Vector2 playerPos = _playerRb.position;
        float best = float.MaxValue;
        int bestIndex = 0;
        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            float d = Vector2.SqrMagnitude(_ropeSegments[i].CurrentPosition - playerPos);
            if (d < best)
            {
                best = d;
                bestIndex = i;
            }
        }

        _playerT = bestIndex; // attach to that segment (continuous representation)
        if (snapPlayerToRope)
        {
            _playerRb.position = _ropeSegments[bestIndex].CurrentPosition;
            _playerRb.velocity = Vector2.zero;
        }

        // remove existing hinge if any
        var old = _playerRb.GetComponent<HingeJoint2D>();
        if (old != null) Destroy(old);

        _playerHinge = _playerRb.gameObject.AddComponent<HingeJoint2D>();
        _playerHinge.autoConfigureConnectedAnchor = false;
        _playerHinge.connectedBody = null; // connect to world anchor
        _playerHinge.connectedAnchor = SampleRopePosition(_playerT);
        _playerHinge.anchor = Vector2.zero; // adjust if player sprite pivot requires offset
    }

    /// <summary>
    /// Detach the player from the rope.
    /// </summary>
    public void DetachPlayer()
    {
        if (_playerRb == null) return;
        var hinge = _playerRb.GetComponent<HingeJoint2D>();
        if (hinge != null) Destroy(hinge);
        _playerHinge = null;
        _playerRb = null;
        _playerT = -1f;
    }

    /// <summary>
    /// Discrete climb step: +/-1 moves by one segment.
    /// </summary>
    public void ClimbStep(int delta)
    {
        if (_playerT < 0f) return;
        _playerT = Mathf.Clamp(_playerT + Mathf.Sign(delta), 0f, _numOfRopeSegments - 1);
    }

    /// <summary>
    /// Continuous climb: delta is in segment-units (e.g. +1 = one segment down).
    /// Use small deltas per frame (e.g. from input axis * speed * Time.deltaTime).
    /// </summary>
    public void ClimbContinuous(float delta)
    {
        if (_playerT < 0f) return;
        _playerT = Mathf.Clamp(_playerT + delta, 0f, _numOfRopeSegments - 1);
    }

    /// <summary>
    /// Returns a world-space sample position along the rope for fractional index t (0..n-1).
    /// </summary>
    public Vector2 SampleRopePosition(float t)
    {
        if (_ropeSegments == null || _ropeSegments.Count == 0) return transform.position;

        float ft = Mathf.Clamp(t, 0f, _ropeSegments.Count - 1);
        int i = Mathf.FloorToInt(ft);
        float frac = ft - i;
        if (i >= _ropeSegments.Count - 1) return _ropeSegments[_ropeSegments.Count - 1].CurrentPosition;
        return Vector2.Lerp(_ropeSegments[i].CurrentPosition, _ropeSegments[i + 1].CurrentPosition, frac);
    }
}

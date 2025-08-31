using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RopeVerlet : MonoBehaviour
{
    [Header("Rope")]
    [SerializeField] private int _numOfRopeSegments = 25;
    [SerializeField] private float _ropeSegmentLength = 0.225f;

    [Header("Physics")]
    [SerializeField] private Vector2 _gravityForce = new Vector2(0, -2);
    [SerializeField] private float _dampingFactor = 0.98f;

    [Header("Constraints")]
    [SerializeField] private int _numOfConstraintRuns = 50;

    private LineRenderer _lineRenderer;
    private List<RopeSegment> _ropeSegments = new List<RopeSegment>();

    private Vector3 _ropeStartPoint;

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
        _lineRenderer.positionCount = _numOfRopeSegments;

        _ropeStartPoint = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    
        for (var i = 0; i < _numOfRopeSegments; i++)
        {
            _ropeSegments.Add(new RopeSegment(_ropeStartPoint));
            _ropeStartPoint.y -= _ropeSegmentLength;
        }
    }

    private void Update()
    {
        DrawRope();
    }

    private void FixedUpdate()
    {
        Simulate();

        for (var i = 0; i < _numOfConstraintRuns; i++)
        {
            ApplyContraints();
        }
    }

    private void DrawRope()
    {
        Vector3[] ropePositions = new Vector3[_numOfRopeSegments];
        for (var i = 0; i < _ropeSegments.Count; i++)
        {
            ropePositions[i] = _ropeSegments[i].CurrentPosition;
        }

        _lineRenderer.SetPositions(ropePositions);
    }

    private void Simulate()
    {
        for (var i = 0; i < _ropeSegments.Count; i++)
        {
            RopeSegment segment = _ropeSegments[i];
            Vector2 velocity = (segment.CurrentPosition - segment.OldPosition) * _dampingFactor;

            segment.OldPosition = segment.CurrentPosition;
            segment.CurrentPosition += velocity;
            segment.CurrentPosition += _gravityForce * Time.fixedDeltaTime;
            _ropeSegments[i] = segment;
        }
    }

    private void ApplyContraints()
    {
        RopeSegment firstSegment = _ropeSegments[0];
        firstSegment.CurrentPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        _ropeSegments[0] = firstSegment;

        for (var i = 0; i < _numOfRopeSegments - 1; i++)
        {
            RopeSegment currentSegment = _ropeSegments[i];
            RopeSegment nextSegment = _ropeSegments[i + 1];

            float dist = (currentSegment.CurrentPosition - nextSegment.CurrentPosition).magnitude;
            float difference = (dist - _ropeSegmentLength);

            Vector2 changeDir = (currentSegment.CurrentPosition - nextSegment.CurrentPosition).normalized;
            Vector2 changeVector = changeDir * difference;

            if (i != 0)
            {
                currentSegment.CurrentPosition -= (changeVector * 0.5f);
                nextSegment.CurrentPosition += (changeVector * 0.5f);
            }
            else
            {
                nextSegment.CurrentPosition += changeVector;
            }

            _ropeSegments[i] = currentSegment;
            _ropeSegments[i+1] = nextSegment;
        }
    }
}

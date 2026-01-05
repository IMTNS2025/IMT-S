using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using System.Collections.Generic;

/// <summary>
/// Simulates obstacle input patterns for testing the airflow simulation.
/// Attach this to a GameObject in the scene to override the InteractionInput.
/// </summary>
public class SimulatedInputController : MonoBehaviour
{
    [Header("Pattern Source")]
    [Tooltip("The ScriptableObject pattern to use for simulation.")]
    public SimulatedInputPatternSO patternAsset;

    [Header("Debug Visualization")]
    public bool showGizmos = true;
    public Color pathColor = Color.yellow;
    public Color obstacleColor = Color.red;

    [Header("Control")]
    public bool isRunning = false;

    // Runtime state
    private float2 currentPosition;
    private float currentAngle; // in radians
    private int currentSegmentIndex;
    private float segmentProgress; // 0 to 1 for current segment
    private float pauseTimer;
    private float startDelayTimer;
    private bool isStarting;

    private float2 previousPosition;
    private World world;
    private EntityQuery interactionInputQuery;
    private bool queryCreated;

    // Cached pattern data
    private List<SimulatedInputPatternSO.PatternSegment> activePattern;
    private Vector2 activeStartPosition;
    private float activeStartAngle;
    private bool activeLoopPattern;
    private float activeStartDelay;

    private void Start()
    {
        LoadPatternData();
        ResetPattern();
        
        // If already running (set in Inspector), make sure the flag is set
        if (isRunning)
        {
            InteractionInputSystem.UseSimulatedInput = true;
        }
    }

    private void OnEnable()
    {
        // Signal the system that simulated input may be used
        if (isRunning)
        {
            InteractionInputSystem.UseSimulatedInput = true;
        }
    }

    private void OnDisable()
    {
        // Clear the simulated input flag when disabled
        InteractionInputSystem.UseSimulatedInput = false;
        ClearInput();
        
        // Reset query state
        queryCreated = false;
        interactionInputQuery = default;
    }

    private void LoadPatternData()
    {
        if (patternAsset != null)
        {
            activePattern = patternAsset.CloneSegments();
            activeStartPosition = patternAsset.startPosition;
            activeStartAngle = patternAsset.startAngle;
            activeLoopPattern = patternAsset.loopPattern;
            activeStartDelay = patternAsset.startDelay;
        }
        else
        {
            activePattern = null;
            activeStartPosition = Vector2.zero;
            activeStartAngle = 0f;
            activeLoopPattern = false;
            activeStartDelay = 0f;
            Debug.LogWarning("[SimulatedInputController] No pattern asset assigned. Please assign a SimulatedInputPatternSO.");
        }
    }

    private void Update()
    {
        // Get world reference and create query if needed
        if (world == null || !world.IsCreated)
        {
            world = World.DefaultGameObjectInjectionWorld;
            
            // Reset query when world changes
            queryCreated = false;
            interactionInputQuery = default;
        }

        if (world == null || !world.IsCreated)
            return;

        // Create and cache the query
        if (!queryCreated)
        {
            interactionInputQuery = world.EntityManager.CreateEntityQuery(typeof(InteractionInput));
            queryCreated = true;
        }

        // Update pattern
        if (isRunning)
        {
            UpdatePattern();
        }
    }

    /// <summary>
    /// Apply input in LateUpdate to ensure it happens after the ECS systems have run
    /// and won't be overwritten.
    /// </summary>
    private void LateUpdate()
    {
        if (world == null || !world.IsCreated)
            return;

        if (isRunning)
        {
            ApplyInput();
        }
        else
        {
            ClearInput();
        }
    }

    private void ResetPattern()
    {
        currentPosition = new float2(activeStartPosition.x, activeStartPosition.y);
        currentAngle = math.radians(activeStartAngle);
        currentSegmentIndex = 0;
        segmentProgress = 0f;
        pauseTimer = 0f;
        previousPosition = currentPosition;
        isStarting = true;
        startDelayTimer = activeStartDelay;
    }

    private void UpdatePattern()
    {
        // Handle start delay
        if (isStarting)
        {
            startDelayTimer -= Time.deltaTime;
            if (startDelayTimer > 0f)
                return;
            isStarting = false;
        }

        if (activePattern == null || activePattern.Count == 0)
            return;

        if (currentSegmentIndex >= activePattern.Count)
        {
            if (activeLoopPattern)
            {
                ResetPattern();
                isStarting = true;
                startDelayTimer = activeStartDelay;
            }
            else
            {
                isRunning = false;
                InteractionInputSystem.UseSimulatedInput = false;
            }
            return;
        }

        previousPosition = currentPosition;
        var segment = activePattern[currentSegmentIndex];

        switch (segment.type)
        {
            case SimulatedInputPatternSO.PatternSegment.SegmentType.Straight:
                UpdateStraight(segment);
                break;
            case SimulatedInputPatternSO.PatternSegment.SegmentType.Turn:
                UpdateTurn(segment);
                break;
            case SimulatedInputPatternSO.PatternSegment.SegmentType.Pause:
                UpdatePause(segment);
                break;
        }
    }

    private void UpdateStraight(SimulatedInputPatternSO.PatternSegment segment)
    {
        float distance = segment.value;
        float speed = segment.speed;
        float duration = distance / speed;

        float deltaProgress = (Time.deltaTime / duration);
        segmentProgress += deltaProgress;

        // Move in current direction
        float2 direction = new float2(math.cos(currentAngle), math.sin(currentAngle));
        float moveDistance = speed * Time.deltaTime;
        currentPosition += direction * moveDistance;

        if (segmentProgress >= 1f)
        {
            // Snap to exact end position
            float overshoot = (segmentProgress - 1f) * duration * speed;
            currentPosition -= direction * overshoot;

            AdvanceToNextSegment();
        }
    }

    private void UpdateTurn(SimulatedInputPatternSO.PatternSegment segment)
    {
        float angleChange = math.radians(segment.value);
        float speed = segment.speed;

        // Calculate turn duration based on a reference arc length (matching gizmo calculation)
        float arcLength = math.abs(angleChange) * 1f; // 1 unit radius reference
        float duration = math.max(arcLength / speed, 0.1f);
        float totalDistance = speed * duration;

        float deltaProgress = Time.deltaTime / duration;
        
        // Calculate the angle at the start of this frame
        float direction = segment.turnLeft ? 1f : -1f;
        float startFrameAngle = currentAngle;
        
        // Calculate new progress
        float newProgress = segmentProgress + deltaProgress;
        float clampedProgress = math.min(newProgress, 1f);
        float actualDeltaProgress = clampedProgress - segmentProgress;
        
        // Calculate the angle at the end of this frame
        float endFrameAngle = currentAngle + direction * math.abs(angleChange) * actualDeltaProgress;
        
        // Move in the average direction between start and end angles (matching gizmo)
        float avgAngle = (startFrameAngle + endFrameAngle) * 0.5f;
        float2 moveDir = new float2(math.cos(avgAngle), math.sin(avgAngle));
        float stepDist = totalDistance * actualDeltaProgress;
        
        currentPosition += moveDir * stepDist;
        currentAngle = endFrameAngle;
        segmentProgress = newProgress;

        if (segmentProgress >= 1f)
        {
            AdvanceToNextSegment();
        }
    }

    private void UpdatePause(SimulatedInputPatternSO.PatternSegment segment)
    {
        pauseTimer += Time.deltaTime;

        if (pauseTimer >= segment.value)
        {
            AdvanceToNextSegment();
        }
    }

    private void AdvanceToNextSegment()
    {
        currentSegmentIndex++;
        segmentProgress = 0f;
        pauseTimer = 0f;
    }

    private void ApplyInput()
    {
        if (world == null || !world.IsCreated)
            return;

        if (!queryCreated || interactionInputQuery.IsEmpty)
            return;

        var entityManager = world.EntityManager;
        var entity = interactionInputQuery.GetSingletonEntity();

        float2 velocity = (currentPosition - previousPosition) / math.max(Time.deltaTime, 0.0001f);
        float speed = math.length(velocity);

        var input = new InteractionInput
        {
            position = currentPosition,
            velocity = velocity,
            speed = speed,
            lineStart = previousPosition,
            lineEnd = currentPosition,
            deltaTime = Time.deltaTime,
            isActive = true
        };

        entityManager.SetComponentData(entity, input);
    }

    private void ClearInput()
    {
        if (world == null || !world.IsCreated)
            return;

        if (!queryCreated || interactionInputQuery.IsEmpty)
            return;

        var entityManager = world.EntityManager;
        var entity = interactionInputQuery.GetSingletonEntity();
        entityManager.SetComponentData(entity, new InteractionInput());
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        // Draw the planned path
        DrawPatternPath();

        // Draw current obstacle position if running
        if (Application.isPlaying && isRunning)
        {
            Gizmos.color = obstacleColor;
            Gizmos.DrawWireSphere(new Vector3(currentPosition.x, currentPosition.y, 0f), 0.5f);

            // Draw direction
            float2 dir = new float2(math.cos(currentAngle), math.sin(currentAngle));
            Gizmos.DrawLine(
                new Vector3(currentPosition.x, currentPosition.y, 0f),
                new Vector3(currentPosition.x + dir.x, currentPosition.y + dir.y, 0f)
            );
        }
    }

    private void DrawPatternPath()
    {
        if (patternAsset == null)
            return;

        var patternToDraw = activePattern ?? patternAsset.segments;
        var startPos = patternAsset.startPosition;
        var startAng = patternAsset.startAngle;

        if (patternToDraw == null || patternToDraw.Count == 0)
            return;

        Gizmos.color = pathColor;

        float2 pos = new float2(startPos.x, startPos.y);
        float angle = math.radians(startAng);

        // Draw start point
        Gizmos.DrawWireSphere(new Vector3(pos.x, pos.y, 0f), 0.2f);

        foreach (var segment in patternToDraw)
        {
            switch (segment.type)
            {
                case SimulatedInputPatternSO.PatternSegment.SegmentType.Straight:
                    float2 dir = new float2(math.cos(angle), math.sin(angle));
                    float2 endPos = pos + dir * segment.value;

                    Gizmos.DrawLine(
                        new Vector3(pos.x, pos.y, 0f),
                        new Vector3(endPos.x, endPos.y, 0f)
                    );

                    pos = endPos;
                    break;

                case SimulatedInputPatternSO.PatternSegment.SegmentType.Turn:
                    // Calculate the arc that will be traveled during the turn
                    float angleChange = math.radians(segment.value);
                    float direction = segment.turnLeft ? 1f : -1f;
                    float arcLength = math.abs(angleChange) * 1f; // Reference arc
                    float duration = math.max(arcLength / segment.speed, 0.1f);
                    float totalDistance = segment.speed * duration;
                    
                    int steps = 16;
                    float startAngleRad = angle;

                    for (int i = 1; i <= steps; i++)
                    {
                        float tPrev = (float)(i - 1) / steps;
                        float tCurr = (float)i / steps;
                        
                        // Calculate angles at previous and current step
                        float anglePrev = startAngleRad + direction * angleChange * tPrev;
                        float angleCurr = startAngleRad + direction * angleChange * tCurr;
                        
                        // Move in the average direction between steps
                        float avgAngle = (anglePrev + angleCurr) * 0.5f;
                        float2 moveDir = new float2(math.cos(avgAngle), math.sin(avgAngle));
                        float stepDist = totalDistance / steps;
                        
                        float2 nextPos = pos + moveDir * stepDist;

                        Gizmos.DrawLine(
                            new Vector3(pos.x, pos.y, 0f),
                            new Vector3(nextPos.x, nextPos.y, 0f)
                        );

                        pos = nextPos;
                    }
                    
                    angle = startAngleRad + direction * angleChange;
                    break;

                case SimulatedInputPatternSO.PatternSegment.SegmentType.Pause:
                    // Draw pause indicator
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireCube(new Vector3(pos.x, pos.y, 0f), Vector3.one * 0.3f);
                    Gizmos.color = pathColor;
                    break;
            }
        }

        // Draw end point
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(new Vector3(pos.x, pos.y, 0f), 0.2f);
    }

    /// <summary>
    /// Loads a pattern from a ScriptableObject asset at runtime.
    /// </summary>
    public void LoadPattern(SimulatedInputPatternSO patternSO)
    {
        patternAsset = patternSO;
        LoadPatternData();
        ResetPattern();
    }
}

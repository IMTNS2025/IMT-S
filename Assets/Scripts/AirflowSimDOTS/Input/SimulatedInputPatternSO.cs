using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject that defines a movement pattern for simulated input.
/// </summary>
[CreateAssetMenu(fileName = "NewInputPattern", menuName = "Airflow Simulation/Input Pattern")]
public class SimulatedInputPatternSO : ScriptableObject
{
    [System.Serializable]
    public class PatternSegment
    {
        public enum SegmentType
        {
            Straight,
            Turn,
            Pause
        }

        public SegmentType type = SegmentType.Straight;

        [Tooltip("For Straight: distance to travel. For Turn: angle in degrees. For Pause: duration in seconds.")]
        public float value = 2f;

        [Tooltip("Speed in units per second (not used for Pause)")]
        public float speed = 5f;

        [Tooltip("For Turn: positive = counter-clockwise, negative = clockwise")]
        public bool turnLeft = true;

        public PatternSegment Clone()
        {
            return new PatternSegment
            {
                type = this.type,
                value = this.value,
                speed = this.speed,
                turnLeft = this.turnLeft
            };
        }
    }

    [Header("Starting Configuration")]
    [Tooltip("Starting position in world space")]
    public Vector2 startPosition = new (-5f, 0f);

    [Tooltip("Starting direction (0 = right, 90 = up, 180 = left, 270 = down)")]
    public float startAngle = 0f;

    [Header("Pattern Segments")]
    [Tooltip("List of pattern segments to execute in order")]
    public List<PatternSegment> segments = new ();

    [Header("Pattern Settings")]
    [Tooltip("Loop the pattern when finished")]
    public bool loopPattern = true;

    [Tooltip("Delay before starting/restarting the pattern")]
    public float startDelay = 1f;

    /// <summary>
    /// Creates a deep copy of the segments list.
    /// </summary>
    public List<PatternSegment> CloneSegments()
    {
        var cloned = new List<PatternSegment>();
        foreach (var segment in segments)
        {
            cloned.Add(segment.Clone());
        }
        return cloned;
    }

    #region Helper Methods for Building Patterns in Code

    public void AddStraight(float distance, float speed)
    {
        segments.Add(new PatternSegment
        {
            type = PatternSegment.SegmentType.Straight,
            value = distance,
            speed = speed
        });
    }

    public void AddTurn(float angleDegrees, float speed, bool turnLeft = true)
    {
        segments.Add(new PatternSegment
        {
            type = PatternSegment.SegmentType.Turn,
            value = angleDegrees,
            speed = speed,
            turnLeft = turnLeft
        });
    }

    public void AddPause(float duration)
    {
        segments.Add(new PatternSegment
        {
            type = PatternSegment.SegmentType.Pause,
            value = duration
        });
    }

    public void ClearSegments()
    {
        segments.Clear();
    }

    #endregion
}

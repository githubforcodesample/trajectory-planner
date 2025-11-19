using UnityEngine;
using System.Collections.Generic;

public class TrajectoryPlanner : MonoBehaviour
{
    private const float epsilon = 0.0001f;    
    public Vector3 StartPoint;
    public Vector3 EndPoint;
    [Min(epsilon)]
    public float Acceleration = 1.0f;
    [Min(epsilon)]
    public float Deceleration = 1.0f;
    [Min(epsilon)]
    public float MaxSpeed = 1.0f;
    [Min(epsilon)]
    public float StepDuration = 0.05f;

    private List<Vector3> path;
    private float timeElapsed = 0.0f;

    void Start()
    {
        path = GetPath(StartPoint, EndPoint, Acceleration, Deceleration, MaxSpeed, StepDuration);
        transform.position = StartPoint;
    }

    // Throttle path animation, but no smoothing to show discretized path for large stepDuration
    void Update()
    {
        timeElapsed += Time.deltaTime;
        int pathIndex = Mathf.FloorToInt(timeElapsed / StepDuration);
        if (pathIndex >= path.Count)
        {
            transform.position = EndPoint;
            enabled = false;
            return;
        }
        transform.position = path[pathIndex];
    }
    
    public static List<Vector3> GetPath(Vector3 start, Vector3 end, float acceleration, float deceleration, float maxSpeed, float stepDuration)
    {
        if (acceleration < epsilon || deceleration < epsilon || maxSpeed < epsilon || stepDuration < epsilon)
        {
            Debug.Log("Error! acceleration, deceleration, maxSpeed, and stepDuration must be greater than float.Epsilon");
            return new List<Vector3>();
        }

        float totalDistance = (end - start).magnitude;
        Vector3 unitDirection = (end - start).normalized;
        float accelerationDistance = 0.5f * maxSpeed * maxSpeed / acceleration;
        float decelerationDistance = 0.5f * maxSpeed * maxSpeed / deceleration;
        float constSpeedDistance = totalDistance - accelerationDistance - decelerationDistance;
        float constSpeedDuration = constSpeedDistance / maxSpeed;
        
        if (constSpeedDistance < 0.0f)
        {
            maxSpeed = Mathf.Sqrt(2.0f * totalDistance / (1.0f / acceleration + 1.0f / deceleration));
            accelerationDistance = 0.5f * maxSpeed * maxSpeed / acceleration;
            decelerationDistance = 0.5f * maxSpeed * maxSpeed / deceleration; // Unused, but keeping the value valid
            constSpeedDistance = 0.0f;
            constSpeedDuration = 0.0f;
        }

        float accelerationDuration = maxSpeed / acceleration;
        float decelerationDuration = maxSpeed / deceleration;
        float totalDuration = accelerationDuration + constSpeedDuration + decelerationDuration;
        float timeAfterConstSpeed = accelerationDuration + constSpeedDuration;

        int steps = Mathf.CeilToInt(totalDuration / stepDuration);
        List<Vector3> path = new List<Vector3>(steps + 1); // 1 point per step + the end point

        float time = 0.0f; // Calculate time from timeStep * stepDuration to reduce rounding errors
        for (int timeStep = 0; time < totalDuration; time = ++timeStep * stepDuration)
        {
            if (time < accelerationDuration) // Acceleration
            {
                float distanceTravelled = 0.5f * acceleration * time * time;
                path.Add(start + distanceTravelled * unitDirection);
            }
            else if (time < timeAfterConstSpeed) // Constant speed
            {
                float distanceTravelled = accelerationDistance + (time - accelerationDuration) * maxSpeed;
                path.Add(start + distanceTravelled * unitDirection);
            }
            else // Deceleration
            {
                float timeLeft = totalDuration - time;
                float distanceRemaining = 0.5f * deceleration * timeLeft * timeLeft;
                path.Add(end - distanceRemaining * unitDirection);
            }
        }
        path.Add(end);
        return path;
    }
}

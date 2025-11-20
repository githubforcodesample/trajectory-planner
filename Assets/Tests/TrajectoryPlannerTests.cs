using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TrajectoryPlannerTests
{
    private readonly Vector3 point0 = new Vector3(0.0f, 0.0f, 0.0f);
    private readonly Vector3 point1 = new Vector3(5.0f, 5.0f, 5.0f);

    [Test]
    public void TrajectoryPlannerTestsInvalidInput()
    {
        Assert.IsEmpty(TrajectoryPlanner.GetPath(point0, point1, -1.0f, 1.0f, 1.0f, 0.1f), "Negative acceleration should return empty.");
        Assert.IsEmpty(TrajectoryPlanner.GetPath(point0, point1, 1.0f, -1.0f, 1.0f, 0.1f), "Negative deceleration should return empty.");
        Assert.IsEmpty(TrajectoryPlanner.GetPath(point0, point1, 1.0f, 1.0f, -1.0f, 0.1f), "Negative maxSpeed should return empty.");
        Assert.IsEmpty(TrajectoryPlanner.GetPath(point0, point1, 1.0f, 1.0f, 1.0f, -0.1f), "Negative stepDuration should return empty.");

        // Do not allow float.Epsilon for numbers that are used for calculations. small * small = way too small
        Assert.IsEmpty(TrajectoryPlanner.GetPath(point0, point1, float.Epsilon, 1.0f, 1.0f, 0.1f), "Negligible acceleration should return empty.");
        Assert.IsEmpty(TrajectoryPlanner.GetPath(point0, point1, 1.0f, float.Epsilon, 1.0f, 0.1f), "Negligible deceleration should return empty.");
        Assert.IsEmpty(TrajectoryPlanner.GetPath(point0, point1, 1.0f, 1.0f, float.Epsilon, 0.1f), "Negligible maxSpeed should return empty.");
        Assert.IsEmpty(TrajectoryPlanner.GetPath(point0, point1, 1.0f, 1.0f, 1.0f, float.Epsilon), "Negligible stepDuration should return empty.");
    }

    [Test]
    public void TrajectoryPlannerTestsNoMovement()
    {
        List<Vector3> path = TrajectoryPlanner.GetPath(point1, point1, 1.0f, 1.0f, 1.0f, 0.1f);

        Assert.AreEqual(1, path.Count, "Same start and end should result in 1 point.");
        Assert.AreEqual(point1, path[0], "Path should be same as start/end.");
    }

    [Test]
    public void TrajectoryPlannerTestsTrapezoidalVelocity()
    {
        Vector3 end = new Vector3(0.0f, 5.0f, 0.0f);    
        List<Vector3> path = TrajectoryPlanner.GetPath(point0, end, 1.0f, 1.0f, 1.0f, 1.0f);

        Assert.AreEqual(7, path.Count);
        Assert.AreEqual(point0, path[0], "First point in path should be start.");
        Assert.AreEqual(end, path[^1], "Last point in path should be end.");
        // Check constant velocity in the middle
        Vector3 currentPoint = new Vector3(0.0f, 0.5f, 0.0f);
        for (int i = 1; i <= 5; ++i)
        {
            Assert.AreEqual(currentPoint, path[i], "Middle points should have constant velocity.");
            currentPoint.y += 1.0f;
        }
    }

    [Test]
    public void TrajectoryPlannerTestsTrapezoidalVelocityFinerGrain()
    {  
        List<Vector3> path = TrajectoryPlanner.GetPath(point1, point0, 3.0f, 2.0f, 1.0f, 0.1f);

        Assert.IsNotEmpty(path, "Path should have many points");
        Assert.AreEqual(point1, path[0], "First point in path should be start.");
        Assert.AreEqual(point0, path[^1], "Last point in path should be end.");
        
        // There should be acceleration at the start
        float firstStepDistance = (path[1] - path[0]).magnitude;
        float secondStepDistance = (path[2] - path[1]).magnitude;
        Assert.Less(firstStepDistance, secondStepDistance, "First step should be smaller than second step.");

        // There should be deceleration at the end
        float lastStepDistance = (path[^1] - path[^2]).magnitude;
        float secondLastStepDistance = (path[^2] - path[^3]).magnitude;
        Assert.Less(lastStepDistance, secondLastStepDistance, "Last step should be smaller than second last step.");

        // Check constant velocity in the middle
        int midPoint = path.Count / 2;
        float stepBeforeMid = (path[midPoint] - path[midPoint - 1]).magnitude;
        float stepAfterMid = (path[midPoint + 1] - path[midPoint]).magnitude;
        Assert.AreEqual(stepBeforeMid, stepAfterMid, 0.0001f, "Middle of trajectory should have constant velocity.");
    }

    [Test]
    public void TrajectoryPlannerTestsNoConstantVelocity()
    {
        Vector3 end = new Vector3(0.0f, 0.0f, 6.0f);
        List<Vector3> path = TrajectoryPlanner.GetPath(point0, end, 1.0f, 3.0f, 5.0f, 1.0f);

        // Velocity should peak at path[3], the decelerate rapidly
        Assert.AreEqual(5, path.Count);
        Assert.AreEqual(point0, path[0], "First point in path should be start");
        Assert.AreEqual(new Vector3(0.0f, 0.0f, 0.5f), path[1], "Point should be accelerating");
        Assert.AreEqual(new Vector3(0.0f, 0.0f, 2.0f), path[2], "Point should be accelerating");
        Assert.AreEqual(new Vector3(0.0f, 0.0f, 4.5f), path[3], "Point should be accelerating");
        Assert.AreEqual(end, path[4], "Last point in path should be end");
    }
}

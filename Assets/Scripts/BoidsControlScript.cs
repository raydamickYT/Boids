using UnityEngine;
using UnityEngine.PlayerLoop;
using VSCodeEditor;

public class BoidsControlScript
{
    public BoidSettings settings;
    [HideInInspector]
    public Vector3 velocity, position, forward;
    private Transform target;
    private GameObject boidObject;
    private Bounds PlayArea;
    //to update
    [HideInInspector]
    public Vector3 avgFlockHeading, avgAvoidanceHeading, centreOfFlockmates;

    [HideInInspector]
    public int numPerceivedFlockmates;

    //to cache
    private Transform cachedTransform;

    public BoidsControlScript(GameObject gameObject, Bounds bounds, BoidSettings boidSettings)
    {
        settings = boidSettings;
        PlayArea = bounds;
        boidObject = gameObject;


        Observer.UpdateBoids += UpdateBoids;
        cachedTransform = boidObject.transform;
        position = cachedTransform.position;
        forward = cachedTransform.forward;

        float startSpeed = (settings.minSpeed + settings.maxSpeed) / 2;
        velocity = boidObject.transform.forward * startSpeed;

        SetNewBoidPos();
    }
    private void SetNewBoidPos()
    {
        var spawnPosition = GetRandomPositionOnBoundsEdge(PlayArea);
        boidObject.transform.position = spawnPosition;
        // boidObject.transform.rotation = GetRandomRotationInsideCube(spawnPosition, PlayArea);
        boidObject.transform.forward = Random.insideUnitSphere;


        if (!boidObject.activeSelf)
        {
            boidObject.SetActive(true);
        }
    }
    public void UpdateBoids()
    {
        Vector3 acceleration = Vector3.zero;

        if (target != null)
        {
            Vector3 offsetToTarget = target.position - boidObject.transform.position;
            acceleration = SteerTowards(offsetToTarget) * settings.targetWeight;
        }
        if (numPerceivedFlockmates != 0)
        {
            centreOfFlockmates /= numPerceivedFlockmates;

            Vector3 offsetToFlockmatesCentre = (centreOfFlockmates - position);

            var alignmentForce = SteerTowards(avgFlockHeading) * settings.alignWeight;
            var cohesionForce = SteerTowards(offsetToFlockmatesCentre) * settings.cohesionWeight;
            var seperationForce = SteerTowards(avgAvoidanceHeading) * settings.seperateWeight;

            acceleration += alignmentForce;
            acceleration += cohesionForce;
            acceleration += seperationForce;
        }
        if (IsHeadingForCollision())
        {
            Vector3 collisionAvoidDir = ObstacleRays();
            Vector3 collisionAvoidForce = SteerTowards(collisionAvoidDir) * settings.avoidCollisionWeight;
            acceleration += collisionAvoidForce;
        }


        velocity += acceleration * Time.deltaTime;
        float speed = velocity.magnitude;
        Vector3 dir = velocity / speed;
        speed = Mathf.Clamp(speed, settings.minSpeed, settings.maxSpeed);
        velocity = dir * speed;

        cachedTransform.position += velocity * Time.deltaTime;
        cachedTransform.forward = dir;
        position = cachedTransform.position;
        forward = dir;

    }
    Vector3 ObstacleRays()
    {
        Vector3[] rayDirections = BoidHelper.directions;

        for (int i = 0; i < rayDirections.Length; i++)
        {
            Vector3 dir = cachedTransform.TransformDirection(rayDirections[i]);
            Ray ray = new Ray(position, dir);


            if (!Physics.SphereCast(ray, settings.boundsRadius, settings.collisionAvoidDst, settings.obstacleMask))
            {
                return dir;
            }
        }

        return forward;
    }
    bool IsHeadingForCollision()
    {
        RaycastHit hit;
        if (Physics.SphereCast(position, settings.boundsRadius, boidObject.transform.forward, out hit, settings.collisionAvoidDst, settings.obstacleMask))
        {
            return true;
        }
        else { }
        return false;
    }
    Vector3 SteerTowards(Vector3 vector)
    {
        Vector3 v = vector.normalized * settings.maxSpeed - velocity;
        return Vector3.ClampMagnitude(v, settings.maxSteerForce);
    }


    Quaternion GetRandomRotationInsideCube(Vector3 spawnPosition, Bounds cubeBounds)
    {
        Vector3 directionTowardsCenter = (cubeBounds.center - spawnPosition).normalized;
        Vector3 randomDirection;
        float angle;

        do
        {
            randomDirection = Random.onUnitSphere;

            // Calculate the angle between the random direction and the direction towards the center
            angle = Vector3.Angle(randomDirection, directionTowardsCenter);

        } while (angle > 45);

        return Quaternion.LookRotation(randomDirection);
    }


    Vector3 GetRandomPositionOnBoundsEdge(Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        // Randomly select an axis and edge
        int axis = Random.Range(0, 3); // 0 for x, 1 for y, 2 for z
        bool minEdge = Random.value > 0.5f; // True for min edge, false for max edge

        Vector3 randomPosition = Vector3.zero;

        // Randomly choose a point within the selected face
        if (axis == 0) // X-axis
        {
            randomPosition.x = minEdge ? min.x : max.x;
            randomPosition.y = Random.Range(min.y, max.y);
            randomPosition.z = Random.Range(min.z, max.z);
        }
        else if (axis == 1) // Y-axis
        {
            randomPosition.x = Random.Range(min.x, max.x);
            randomPosition.y = minEdge ? min.y : max.y;
            randomPosition.z = Random.Range(min.z, max.z);
        }
        else // Z-axis
        {
            randomPosition.x = Random.Range(min.x, max.x);
            randomPosition.y = Random.Range(min.y, max.y);
            randomPosition.z = minEdge ? min.z : max.z;
        }

        return randomPosition;
    }

    bool CheckIfBoidIsOutOfBounds(Bounds bounds, Transform boidTransform)
    {
        // Check if the boid's position is outside the bounds
        if (!bounds.Contains(boidTransform.position))
        {
            boidObject.SetActive(false);
            return true; // The boid is out of bounds
        }
        else
        {
            return false; // The boid is within bounds
        }
    }

}

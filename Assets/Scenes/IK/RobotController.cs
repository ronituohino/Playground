using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RobotController : MonoBehaviour
{
    int chainCount;

    InverseKinematics.FABRIKChain[] chains;
    Vector3[] goals;
    Quaternion[] rotations;
    Vector3[] surfaceNormals;

    public InverseKinematics ikRig;
    public GameObject robot;
    public Rigidbody rb;

    [Space]

    int robotLayer;

    Vector3 lastRobotPos;
    Quaternion lastRobotRotation;

    public Transform[] defaults;

    [Space]

    public float heightAdd;
    public float maxDiff;
    public float maxAngularDiff;

    bool[] foundBooleans;

    [Header("Walking")]

    public bool followWalkingRig;
    public GameObject robotBody;
    public float latency = 10f;
    public float bodyWalkingSpeed;
    public float bodyRotationSpeed;
    public AnimationCurve walkingCurve;
    public float stepHeight;

    [Space]

    public float bodySway;
    public float bodySwayRotation;
    public float stabilizationSpeed;

    [Space]

    public GameObject walkingRig;

    public GameObject[] walkingStarts;

    public GameObject[] walkingGoals;
    Vector3[] walkingGoalTemps;
    float[] dists;
    Vector3[] normals;
    float[] percentageDoneSteps;
    Vector3[] walkingHeightAdds;
    bool[] placedFeet;
    RaycastHit[] raycasts;

    [Space]

    public float pace = 1f;
    public float steppingSpeed;
    float timer = 0f;

    int legToStep = 0;

    private void Start()
    {
        lastRobotPos = robot.transform.position;
        lastRobotRotation = robot.transform.rotation;

        robotLayer = LayerMask.GetMask("Robot");

        chainCount = ikRig.ikChains.Length;
        chains = new InverseKinematics.FABRIKChain[chainCount];
        for (int i = 0; i < chainCount; i++)
        {
            chains[i] = ikRig.ikChains[i];
        }

        goals = new Vector3[chainCount];
        rotations = new Quaternion[chainCount];
        walkingGoalTemps = new Vector3[chainCount];
        dists = new float[chainCount];
        normals = new Vector3[chainCount];

        percentageDoneSteps = new float[chainCount];
        percentageDoneSteps.Populate(0f);

        walkingHeightAdds = new Vector3[chainCount];

        placedFeet = new bool[chainCount];
        placedFeet.Populate(true);

        raycasts = new RaycastHit[chainCount];
        foundBooleans = new bool[chainCount];

        surfaceNormals = new Vector3[chainCount];
        surfaceNormals.Populate(Vector3.zero);
    }

    // Update is called once per frame
    void Update()
    {
        //Leg positioning
        for (int i = 0; i < chainCount; i++)
        {
            if (!foundBooleans[i])
            {
                InverseKinematics.FABRIKChain chain = chains[i];

                RaycastHit rch = GetPoint(chain.startPoint, chain.chainTotalLength);
                if (rch.point != Vector3.zero)
                {
                    surfaceNormals[i] = rch.normal;
                    goals[i] = rch.point + (heightAdd * rch.normal);
                    rotations[i] = Quaternion.LookRotation(rch.normal) * Quaternion.Euler(0, -90, 90) * Quaternion.Euler(0, -robot.transform.rotation.eulerAngles.y, 0);
                    foundBooleans[i] = true;
                }
                else
                {
                    goals[i] = defaults[i].position;
                    rotations[i] = defaults[i].rotation;
                }
            }
        }

        //Update leg position if the body is moved enough
        if (EnoughDifference(robot.transform.position, lastRobotPos) || EnoughDifferenceQuat(robot.transform.rotation, lastRobotRotation))
        {
            foundBooleans.Populate(false);

            lastRobotPos = robot.transform.position;
            lastRobotRotation = robot.transform.rotation;
        }

        //Walking rig
        if (followWalkingRig)
        {
            for (int i = 0; i < chainCount; i++)
            {
                //Lerping mainbody toward the rig
                raycasts[i] = GetPoint(walkingStarts[i].transform, chains[i].chainTotalLength);
                if (raycasts[i].point != Vector3.zero)
                {
                    walkingGoals[i].transform.position = raycasts[i].point + (heightAdd * raycasts[i].normal);
                    rotations[i] = Quaternion.LookRotation(raycasts[i].normal) * Quaternion.Euler(0, -90, 90) * Quaternion.Euler(0, -robotBody.transform.rotation.eulerAngles.y, 0);
                }
                else
                {
                    walkingGoals[i].transform.position = defaults[i].position;
                    rotations[i] = defaults[i].rotation;
                }
            }

            //Stepping initiator, switches a leg from static to active according to pace
            timer += Time.deltaTime;
            if (timer > pace)
            {
                timer = 0f;
                if ((walkingGoals[legToStep].transform.position - goals[legToStep]).magnitude > 0.03f)
                {
                    walkingGoalTemps[legToStep] = walkingGoals[legToStep].transform.position;
                    dists[legToStep] = (walkingGoalTemps[legToStep] - goals[legToStep]).magnitude;
                    normals[legToStep] = raycasts[legToStep].normal;

                    placedFeet[legToStep] = false;

                    legToStep++;
                    if (legToStep == chainCount)
                    {
                        legToStep = 0;
                    }
                }
            }

            //Foot raising up and movement, lerping
            for (int i = 0; i < chainCount; i++)
            {
                if (dists[i] > 0 && !placedFeet[i])
                {
                    goals[i] = Vector3.Lerp(goals[i], walkingGoalTemps[i], Time.deltaTime * steppingSpeed);
                    percentageDoneSteps[i] = walkingCurve.Evaluate(((walkingGoalTemps[i] - goals[i]).magnitude / dists[i]));
                    walkingHeightAdds[i] = normals[i] * percentageDoneSteps[i] * stepHeight;

                    placedFeet[i] = percentageDoneSteps[i] > 0.98f;
                    if (placedFeet[i])
                    {
                        goals[i] = walkingGoalTemps[i];
                        walkingHeightAdds[i] = Vector3.zero;
                        dists[i] = 0f;
                    }
                }
            }
        }

        //Set goal points
        for (int i = 0; i < chainCount; i++)
        {
            chains[i].goalPoint.position = goals[i] + walkingHeightAdds[i];
            chains[i].goalPoint.rotation = rotations[i];
        }
    }

    //Physics
    private void FixedUpdate()
    {
        if (followWalkingRig)
        {
            //Fake physics applied
            //Stabilize robot when all legs are on ground
            if (placedFeet.AreAllTrue())
            {
                //rb.useGravity = false;
                //Debug.Log("all placedd");
                rb.AddForce(-rb.velocity * stabilizationSpeed, ForceMode.Acceleration);
                rb.AddTorque(-rb.angularVelocity * stabilizationSpeed, ForceMode.Acceleration);
            }
            //Swing robot around if a leg is raised
            else
            {
                //rb.useGravity = true;
                for (int i = 0; i < chainCount; i++)
                {
                    //Debug.Log(placedFeet[i] + " " + i);
                    if (!placedFeet[i])
                    {
                        Vector3 bodyToFoot = (chains[i].goalPoint.transform.position - robotBody.transform.position);
                        rb.AddForce(bodyToFoot.normalized * bodySway, ForceMode.Acceleration);

                        /*if(i == 0)
                        {
                            Debug.DrawRay(chains[i].goalPoint.transform.position, bodyToFoot, Color.red, 0.1f);
                            Debug.DrawRay(robotBody.transform.position, robot.transform.forward, Color.blue, 0.1f);
                            Debug.DrawRay(robotBody.transform.position, Vector3.Cross(bodyToFoot, robot.transform.up), Color.green, 0.1f);
                        }*/

                        rb.AddTorque(Quaternion.Euler(0, Vector3.Angle(Vector3.Cross(bodyToFoot, robot.transform.up), robotBody.transform.forward), 0) * (-robotBody.transform.forward * bodySwayRotation), ForceMode.Acceleration);
                    }
                }
            }

            //Apply physics movements
            rb.MovePosition(Vector3.Lerp(robotBody.transform.position, walkingRig.transform.position, Time.deltaTime * bodyWalkingSpeed));
            rb.MoveRotation(Quaternion.Lerp(robotBody.transform.rotation, walkingRig.transform.rotation, Time.deltaTime * bodyRotationSpeed));
        }
    }

    //Gets the point to place IK target
    RaycastHit GetPoint(Transform startPoint, float chainLength)
    {
        RaycastHit rch1;
        RaycastHit rch2;

        //Debug.DrawRay(startPoint.position, Vector3.down * chainLength, Color.red, .1f);
        if (Physics.Raycast(startPoint.position, Vector3.down, out rch1, chainLength, ~robotLayer)) //Directly below start point
        {
            if (Physics.Raycast(startPoint.position, startPoint.forward, out rch2, chainLength, ~robotLayer)) //Raycast from startpoint along robot body
            {
                float angle1 = Mathf.Abs(Vector3.Angle(rch1.normal, Vector3.up));
                float angle2 = Mathf.Abs(Vector3.Angle(rch2.normal, Vector3.up));

                //Check which point is more flat, 
                if (!SignificantDifference(angle1, angle2))
                {
                    //This is usually better
                    return rch2;
                }
                else
                {
                    if (angle2 > angle1)
                    {
                        return rch1;
                    }
                    else
                    {
                        return rch2;
                    }
                }
            }
            else
            {
                return rch1;
            }
        }
        else if (Physics.Raycast(startPoint.position, startPoint.forward, out rch2, chainLength, ~robotLayer))
        {
            return rch2;
        }
        else
        {
            //No point found, retract legs to default points
            return rch1;
        }
    }

    bool EnoughDifference(Vector3 a, Vector3 b)
    {
        if (Mathf.Abs((a - b).magnitude) > maxDiff)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    bool EnoughDifferenceQuat(Quaternion a, Quaternion b)
    {
        if (Mathf.Abs(Quaternion.Angle(a, b)) > maxAngularDiff)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    bool SignificantDifference(float f1, float f2)
    {
        if (Mathf.RoundToInt(f1 * 10) == Mathf.RoundToInt(f2 * 10))
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}

static class Extensions
{
    public static void Populate<T>(this T[] arr, T value)
    {
        int len = arr.Length;
        for (int i = 0; i < len; i++)
        {
            arr[i] = value;
        }
    }

    /// <summary>Checks if all the booleans in arr are true.</summary>
    public static bool AreAllTrue(this bool[] arr)
    {
        int len = arr.Length;
        for (int i = 0; i < len; i++)
        {
            if (!arr[i])
            {
                return false;
            }
        }
        return true;
    }

    public static float Max(this float[] arr)
    {
        float max = 0f;
        foreach (float f in arr)
        {
            if (f > max)
            {
                max = f;
            }
        }
        return max;
    }

    public static float Min(this float[] arr)
    {
        float min = float.MaxValue;
        foreach (float f in arr)
        {
            if (f < min)
            {
                min = f;
            }
        }
        return min;
    }
}


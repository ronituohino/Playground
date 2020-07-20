using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//Based on a FABRIK model (Forwards And Backwards Reaching Inverse Kinematics)
//Implemented by Roni Tuohino
public class InverseKinematics : MonoBehaviour
{
    public FABRIKChain[] ikChains;

    [System.Serializable]
    public class FABRIKChain
    {
        //The actual chain points
        public Transform[] chainPoints;

        //The positions to slerp towards
        [HideInInspector]
        public Vector3[] calculatedPoints;
        [HideInInspector]
        public Vector3[] rotatedPoints;

        [HideInInspector]
        public Vector3 lastPolePoint = Vector3.zero;

        [HideInInspector]
        public float[] chainLengths;
        [HideInInspector]
        public float chainTotalLength;

        [Space]

        public bool fixedChain = true;
        public bool copyLastPointRotation = true; //Make the last point have the same rotation as the goal.
        public float movementSpeed = 1f;
        public float rotationSpeed = 1f;

        [HideInInspector]
        public float debugRotation = 0f;

        [Space]

        public bool usePoleTarget;
        public Transform poleTarget;

        [Space]

        public Transform goalPoint;
        [HideInInspector]
        public Vector3 lastGoalPoint = Vector3.zero;

        public Transform startPoint;
        [HideInInspector]
        public Vector3 lastStartPoint = Vector3.zero;

        

        [HideInInspector]
        public Vector3 startToGoalVector;

        [HideInInspector]
        public bool solved = false;
    }

    private void Start()
    {
        //Calculate chain lengths, they can't change
        //Also setup some arrays
        foreach (FABRIKChain fbc in ikChains)
        {
            fbc.chainLengths = GetChainLengths(fbc.chainPoints);
            fbc.chainTotalLength = fbc.chainLengths.Sum();

            int len = fbc.chainPoints.Length;
            fbc.calculatedPoints = new Vector3[len];
            for (int i = 0; i < len; i++)
            {
                fbc.calculatedPoints[i] = fbc.chainPoints[i].position;
            }

            fbc.rotatedPoints = new Vector3[len];
            for (int i = 0; i < len; i++)
            {
                fbc.rotatedPoints[i] = fbc.chainPoints[i].position;
            }
        }
    }

    void Update()
    {
        foreach (FABRIKChain fbc in ikChains)
        {
            fbc.startToGoalVector = fbc.goalPoint.position - fbc.startPoint.position;

            //Check if the goal moves, if so then start solving!
            if (fbc.goalPoint.position != fbc.lastGoalPoint || fbc.startPoint.position != fbc.lastStartPoint)
            {
                fbc.solved = false;
            }

            //Calculate the new points
            if (!fbc.solved)
            {
                //Set points linearly if the goal is farther away than the chain length (unless not fixed)
                if (fbc.fixedChain && fbc.startToGoalVector.magnitude > fbc.chainTotalLength)
                {
                    SetChainPointsLinear(fbc.calculatedPoints, fbc.chainLengths, fbc.startToGoalVector.normalized, fbc.startPoint.position);
                    fbc.solved = true;
                }
                //FABRIK!
                else
                {
                    ComputeBackwards(fbc.calculatedPoints, fbc.chainLengths, fbc.goalPoint);
                    if (fbc.fixedChain)
                    {
                        ComputeForwards(fbc.calculatedPoints, fbc.chainLengths, fbc.startPoint);
                    }

                    fbc.solved = IsSolved(fbc.calculatedPoints[fbc.calculatedPoints.Length - 1], fbc.goalPoint.position);
                }
            }

            

            //Pole target code
            int len = fbc.chainPoints.Length;
            if (fbc.usePoleTarget && fbc.solved && (fbc.lastPolePoint != fbc.poleTarget.position || fbc.goalPoint.position != fbc.lastGoalPoint || fbc.startPoint.position != fbc.lastStartPoint))
            {
                //Get the pole target vectors
                Vector3 poleTargetVector = Quaternion.AngleAxis(-90f, fbc.startToGoalVector) * Vector3.Cross(fbc.startToGoalVector, fbc.poleTarget.position - fbc.startPoint.position);
                Vector3 staticPointOfViewVector = Vector3.Cross(fbc.startToGoalVector, Vector3.forward);
                float angle = Vector3.SignedAngle(staticPointOfViewVector, poleTargetVector, fbc.startToGoalVector);

                //Calculate angles for the chains, how should they be rotated?
                float[] chainAngles = new float[len - 2];
                for (int i = 1; i < len - 1; i++)
                {
                    //Combined all three of the above things to get the chainPoint angle
                    chainAngles[i - 1] = Vector3.SignedAngle(staticPointOfViewVector, Quaternion.AngleAxis(-90f, fbc.startToGoalVector) * Vector3.Cross(fbc.startToGoalVector, fbc.calculatedPoints[i] - fbc.startPoint.position), fbc.startToGoalVector);
                }

                //Apply pole target angle to calculated points
                fbc.rotatedPoints[0] = fbc.calculatedPoints[0];
                fbc.rotatedPoints[len - 1] = fbc.calculatedPoints[len - 1];
                for (int i = 1; i < len - 1; i++)
                {
                    //fbc.rotatedPoints[i] = fbc.calculatedPoints[i];
                    fbc.rotatedPoints[i] = RotateToPole(fbc.calculatedPoints[i], fbc.startPoint.position, fbc.startToGoalVector, angle - chainAngles[i - 1] + fbc.debugRotation);
                }

                //Debug.DrawLine(fbc.startPoint.position, fbc.goalPoint.position, Color.green, 1f);
                //Debug.DrawLine(fbc.startPoint.position, poleTargetVector, Color.red, 1f);
                //Debug.DrawLine(fbc.startPoint.position, staticPointOfViewVector, Color.blue, 1f);

                fbc.lastPolePoint = fbc.poleTarget.position;
            }

            //Lerp toward the calculated/rotated points
            for (int i = 0; i < len; i++)
            {
                if (fbc.usePoleTarget)
                {
                    fbc.chainPoints[i].position = Vector3.Lerp(fbc.chainPoints[i].position, fbc.rotatedPoints[i], Time.deltaTime * fbc.movementSpeed);
                }
                else
                {
                    fbc.chainPoints[i].position = Vector3.Lerp(fbc.chainPoints[i].position, fbc.calculatedPoints[i], Time.deltaTime * fbc.movementSpeed);
                }
            }

            //Rotate the chainpoints accordingly
            if (fbc.copyLastPointRotation)
            {
                fbc.chainPoints[len - 1].rotation = fbc.goalPoint.rotation;
            }

            for (int i = 0; i < len - 1; i++)
            {
                if (fbc.usePoleTarget)
                {
                    Vector3 forward = fbc.rotatedPoints[i + 1] - fbc.rotatedPoints[i];
                    fbc.chainPoints[i].rotation = Quaternion.Lerp(fbc.chainPoints[i].rotation, Quaternion.LookRotation(forward, Quaternion.AngleAxis(-90f, forward) * Vector3.Cross(forward, fbc.poleTarget.position - fbc.rotatedPoints[i])) * Quaternion.Euler(90, 0, 0), Time.deltaTime * fbc.rotationSpeed);
                    //fbc.chainPoints[i].rotation = Quaternion.LookRotation(forward, Quaternion.AngleAxis(-90f, forward) * Vector3.Cross(forward, fbc.poleTarget.position - fbc.rotatedPoints[i])) * Quaternion.Euler(90, 0, 0);
                }
                else
                {
                    Vector3 forward = fbc.calculatedPoints[i + 1] - fbc.calculatedPoints[i];
                    fbc.chainPoints[i].rotation = Quaternion.Lerp(fbc.chainPoints[i].rotation, Quaternion.LookRotation(forward) * Quaternion.Euler(90, 0, 0), Time.deltaTime * fbc.rotationSpeed);
                    //fbc.chainPoints[i].rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(90, 0, 0);
                }
            }

            fbc.lastGoalPoint = fbc.goalPoint.position;
            fbc.lastStartPoint = fbc.startPoint.position;
        }
    }

    float[] GetChainLengths(Transform[] points)
    {
        int len = points.Length;
        float[] lengths = new float[len - 1];

        for (int i = 0; i < len - 1; i++)
        {
            lengths[i] = (points[i + 1].position - points[i].position).magnitude;
        }

        return lengths;
    }

    void SetChainPointsLinear(Vector3[] points, float[] lengths, Vector3 startToGoalVector, Vector3 start) //startToGoalVector is normalized
    {
        int len = points.Length;

        points[0] = start;
        for (int i = 1; i < len; i++) //Skip the start point, (it's fixed)
        {
            points[i] = points[i - 1] + startToGoalVector * lengths[i - 1];
        }
    }


    //Backwards Reaching Inverse Kinematics, computed first in FABRIK
    void ComputeBackwards(Vector3[] points, float[] lengths, Transform goalPoint)
    {
        int len = points.Length;

        points[len - 1] = goalPoint.position;
        for (int i = len - 2; i > -1; i--) //Backwards
        {
            points[i] = points[i + 1] - ((points[i + 1] - points[i]).normalized * lengths[i]);
        }
    }

    //Forwards Reaching Inverse Kinematics, computed second in FABRIK
    void ComputeForwards(Vector3[] points, float[] lengths, Transform startPoint)
    {
        int len = points.Length;

        points[0] = startPoint.position;
        for (int i = 1; i < len - 1; i++) //Forwards
        {
            points[i] = points[i - 1] + ((points[i] - points[i - 1]).normalized * lengths[i - 1]);
        }
    }

    //Check if the point positions are valid
    bool IsSolved(Vector3 a, Vector3 b)
    {
        if (Mathf.RoundToInt(a.x * 10) == Mathf.RoundToInt(b.x * 10) && Mathf.RoundToInt(a.y * 10) == Mathf.RoundToInt(b.y * 10) && Mathf.RoundToInt(a.z * 10) == Mathf.RoundToInt(b.z * 10))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //Rotates a point to the pole target
    Vector3 RotateToPole(Vector3 point, Vector3 startPoint, Vector3 startToGoalVector, float angles)
    {
        return Quaternion.AngleAxis(angles, startToGoalVector) * (point - startPoint) + startPoint;
    }
}

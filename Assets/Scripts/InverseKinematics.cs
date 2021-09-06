using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InverseKinematics : MonoBehaviour
{
    public Transform Target;
    [Range(1, 10)]
    public int chainLength;
    [Range(1, 100)]
    public int Iterations;
    public float maxRotationAngle = 90f;
    public float delta = 0.01f;
    public Transform poleVector;
    private float[] boneLengths;
    private Transform[] bones;
    private Vector3[] positions;
    private Vector3[] initialForwards;
    private Quaternion[] rotations;
    private float TotalBoneLength;
    private bool IKInitError;
    private Vector3 targetPrevPos;
    private Vector3 polePrevPos;

    void Init()
    {
        // One extra virtual bone is present which is the tip of the chain
        bones = new Transform[chainLength + 1];
        positions = new Vector3[chainLength + 1];
        rotations = new Quaternion[chainLength];
        boneLengths = new float[chainLength];
        initialForwards = new Vector3[chainLength];
        TotalBoneLength = 0;
        Transform current = transform;
        for (int i = bones.Length - 1; i >= 0; i--)
        {
            bones[i] = current;

            if (i == bones.Length - 1)
            {
            }
            else
            {
                boneLengths[i] = (bones[i + 1].position - current.position).magnitude;
                initialForwards[i] = current.forward;
                TotalBoneLength += boneLengths[i];
            }

            current = current.parent;
            if (current == null && i > 0)
            {
                Debug.LogError("Chain length greater than actual game objects present!");
                IKInitError = true;
                return;
            }
        }
        IKInitError = false;
    }

    void Start()
    {
        Init();
        polePrevPos = poleVector.position;
        targetPrevPos = Target.position;
    }

    private void LateUpdate()
    {
        ResolveIK();
    }


    void ResolveIK()
    {
        if (Target == null)
        {
            return;
        }
        if (bones.Length != chainLength + 1)
        {
            Init();
        }
        if (IKInitError || bones.Length == 0) { return; }
        //for(int i=0; i<bones.Length; i++) { positions[i] = bones[i].position; }
        // if the target is outside the reach of the chain then this condition
        if ((Target.position - bones[0].position).sqrMagnitude >= TotalBoneLength * TotalBoneLength)
        {
            //Vector3 dir = (Target.position - bones[0].position).normalized;
            //// dir = Vector3.RotateTowards(initialForwards[0], dir, maxRotationAngle * Mathf.Deg2Rad, 0);
            //for (int i = 1; i < bones.Length; i++)
            //{
            //    positions[i] = positions[i - 1] + (dir * boneLengths[i - 1]);
            //    rotations[i - 1] = ResolveIKRotation(positions[i], positions[i - 1], bones[i - 1].parent, initialForwards[i-1]);
            //}
            for(int i=1; i<bones.Length; i++)
            {
                float r = (Target.position - positions[i-1]).magnitude;
                float lambda = boneLengths[i-1] / r;
                positions[i] = (1-lambda) * positions[i-1] + lambda * Target.position;
                rotations[i - 1] = ResolveIKRotation(positions[i], positions[i - 1], bones[i - 1].parent, initialForwards[i - 1]);
            }
        }
        else
        {
            bool displaceLittle=false;
            float angle11 = Vector3.SignedAngle(Vector3.right, Target.position-positions[0], Vector3.forward);
            angle11 = angle11 >= 0 ? angle11 : 360 + angle11;
            float angle22 = Vector3.SignedAngle(Vector3.forward, Target.position-positions[0], Vector3.right);
            angle22 = angle22 >= 0 ? angle22 : 360 + angle22;
            for(int i=1; i<positions.Length; i++)
            {
                float angle1 = Vector3.SignedAngle(Vector3.right, positions[i] - positions[0], Vector3.forward);
                angle1 = angle1 >= 0 ? angle1 : 360 + angle1;
                float angle2 = Vector3.SignedAngle(Vector3.forward, positions[i] - positions[0], Vector3.right);
                angle2 = angle2 >= 0 ? angle2 : 360 + angle2;
                if (angle1!=angle11 || angle2 != angle22)
                {
                    displaceLittle = false;
                    break;
                }
                if (i == positions.Length - 1) { displaceLittle = true; }
            }
            int iterations = Iterations;
            while (iterations > 0)
            {
                Vector3 rootPosition = positions[0];
                // Bone tip to root
                for (int i = positions.Length - 1; i > 0; i--)
                {
                    if (i == bones.Length - 1) positions[i] = Target.position;
                    else {
                        float r = (positions[i + 1] - positions[i]).magnitude;
                        float lambda = boneLengths[i] / r;
                        positions[i] = (1 - lambda) * positions[i + 1] + lambda * positions[i];
                        // positions[i] = positions[i + 1] + (positions[i] - positions[i + 1]).normalized * boneLengths[i];
                        if (displaceLittle)
                        {
                            positions[i] += transform.right * 0.5f; // When the pole is ready then this movement should be done towards the pole vector
                            displaceLittle = false;
                        }
                    }
                }

                // Root to bone tip
                positions[0] = rootPosition;
                for (int i = 1; i < bones.Length; i++)
                {
                    float r = (positions[i] - positions[i - 1]).magnitude;
                    float lambda = boneLengths[i - 1] / r;
                    positions[i] = (1 - lambda) * positions[i - 1] + lambda * positions[i];
                    // positions[i] = positions[i - 1] + (positions[i] - positions[i - 1]).normalized * boneLengths[i - 1];
                }

                // If very close to target
                if ((Target.position - positions[positions.Length - 1]).sqrMagnitude < delta * delta)
                {
                    break;
                }
                iterations--;
            }
            if (poleVector != null)
            {
                for(int i=1; i<positions.Length-1; i++)
                {
                    Plane polePlane = new Plane((positions[i + 1] - positions[i]), positions[i - 1]);
                    // DrawPlane(positions[i - 1], polePlane.normal);
                    Plane polePlaneClose = new Plane((positions[i + 1] - positions[i]), positions[i]);
                    var projectedPole = polePlane.ClosestPointOnPlane(poleVector.position);
                    var projectedBone = polePlane.ClosestPointOnPlane(positions[i]);
                    Debug.DrawRay(positions[i - 1], (projectedBone - positions[i - 1]).normalized, Color.black);
                    Debug.DrawRay(positions[i - 1], (projectedPole - positions[i - 1]).normalized, Color.blue);
                    Debug.DrawRay(positions[i - 1], polePlane.normal * 10, Color.green);
                    float angle = Vector3.SignedAngle((projectedBone - positions[i - 1]).normalized, (projectedPole - positions[i - 1]).normalized, polePlane.normal);
                    Vector3 temp = polePlane.ClosestPointOnPlane(Quaternion.AngleAxis(angle, polePlane.normal) * (positions[i] - positions[i - 1]));
                    Debug.DrawRay(positions[i - 1], temp, Color.cyan);
                    //Debug.Log(i + " : " + angle);
                    Vector3 projectedRectifiedBone = Quaternion.AngleAxis(angle, polePlane.normal) * (projectedBone - positions[i - 1]);
                    // positions[i] = polePlaneClose.ClosestPointOnPlane(projectedRectifiedBone);
                    positions[i] = Quaternion.AngleAxis(angle, polePlane.normal) * (positions[i] - positions[i - 1]) + positions[i - 1];
                }
            }
            for (int i = 0; i < positions.Length - 1; i++)
            {
                rotations[i] = ResolveIKRotation(positions[i + 1], positions[i], bones[i].parent, initialForwards[i]);
            }
        }
        for (int i = 0; i < bones.Length; i++)
        {
            //if (i > 0) { positions[i] = positions[i-1] + (bones[i - 1].forward).normalized * boneLengths[i - 1]; }
            bones[i].position = positions[i];
            if (i < bones.Length - 1) bones[i].rotation = rotations[i];
        }
        polePrevPos = poleVector.position;
        targetPrevPos = Target.position;
    }

    Quaternion ResolveIKRotation(Vector3 target, Vector3 headBone, Transform headBoneParent, Vector3 initialForwardDir)
    {
        Vector3 targetRotationDir = target - headBone;
        // Debug.DrawRay(headBone, headBone.forward * 20, Color.green);
        /// Vector3 targetLocalRotationDir = headBone.parent.InverseTransformDirection(targetRotationDir);
        Vector3 forwardDir = Vector3.forward;
        if (headBoneParent)
        {
            forwardDir = headBoneParent.forward;
        }
        Vector3 targetClampedRotationDir = Vector3.RotateTowards(forwardDir, targetRotationDir, maxRotationAngle * Mathf.Deg2Rad, 0);
        // Vector3 targetClampedLocalRotationDir = Vector3.RotateTowards(Vector3.forward, targetLocalRotationDir, Mathf.Deg2Rad * maxRotationAngle, 0);
        // Debug.Log(targetClampedLocalRotationDir);
        Quaternion targetRotation = Quaternion.LookRotation(targetRotationDir, Vector3.up);
        // headBone.rotation = targetRotation;
        return targetRotation;
        // headBone.localRotation = Quaternion.LookRotation(targetClampedLocalRotationDir, Vector3.up);
    }


    void DrawPlane(Vector3 position, Vector3 normal)
    {

        Vector3 v3;

        if (normal.normalized != Vector3.forward)
            v3 = Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude;
        else
            v3 = Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude; ;

        var corner0 = position + v3 * 10f;
        var corner2 = position - v3 * 10f;
        var q = Quaternion.AngleAxis(90.0f, normal);
        v3 = q * v3;
        var corner1 = position + v3 * 10f;
        var corner3 = position - v3 * 10f;

        Debug.DrawLine(corner0, corner2, Color.green);
        Debug.DrawLine(corner1, corner3, Color.green);
        Debug.DrawLine(corner0, corner1, Color.green);
        Debug.DrawLine(corner1, corner2, Color.green);
        Debug.DrawLine(corner2, corner3, Color.green);
        Debug.DrawLine(corner3, corner0, Color.green);
        Debug.DrawRay(position, normal, Color.red);
    }
}

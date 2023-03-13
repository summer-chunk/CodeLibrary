using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PersonMoveUAV : MonoBehaviour
{

    public float moveSpeed;

    //public GameObject routePoints;

    [HideInInspector]
    public Transform[] weightPoints;

    [HideInInspector]
    public bool isCenterPerson = false;
    [HideInInspector]
    public GameObject centerPerson;

    bool isRunningToCenter = false;
    float nextBoundTime = 1.0f;

    int currentPointsIndex;

    bool hasUpdateMeshCollider = false;

    CameraMove mainCameraScript;

    private void Awake()
    {
        PresetWeightPoints();
    }

    private void Start()
    {
        mainCameraScript = GameObject.FindWithTag("UAV").GetComponent<CameraMove>();
    }

    void Update()
    {

        if (mainCameraScript.gameIsPaused)
        {
            return;
        }

        if (mainCameraScript.isWalkCircle && !isCenterPerson)
        {
            WalkInCircle();
        }
        else
        {
            WalkAlongRoute();
        }
        //WalkAlongRoute();

    }

    private void LateUpdate()
    {
        if (!hasUpdateMeshCollider)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject partObject = transform.GetChild(i).gameObject;
                if (partObject.GetComponent<SkinnedMeshRenderer>() != null)
                {
                    Mesh colliderMesh = new Mesh();
                    partObject.GetComponent<SkinnedMeshRenderer>().BakeMesh(colliderMesh, true);
                    partObject.GetComponent<MeshCollider>().sharedMesh = colliderMesh;
                }
            }
            hasUpdateMeshCollider = true;
        }
    }

    void WalkAlongRoute()
    {
        int nextIndex = (currentPointsIndex + 1) % weightPoints.Length;
        Vector3 targetPosition = weightPoints[nextIndex].position;

        Vector3 targetDirection = (targetPosition - transform.position).normalized;
        //Vector3 targetDirection = (targetPosition - transform.position);
        //targetDirection.y = 0;

        transform.Translate(targetDirection.normalized * moveSpeed * Time.deltaTime, Space.World);

        if (Vector3.Distance(transform.position, targetPosition) < 0.2f)
        {
            currentPointsIndex = (currentPointsIndex + 1) % weightPoints.Length;
        }

        transform.rotation = Quaternion.LookRotation(targetDirection, Vector3.up);
    }

    void WalkInCircle()
    {
        float distance2Center = Vector3.Distance(transform.position, centerPerson.transform.position);

        if (isRunningToCenter)
        {
            moveSpeed += Time.deltaTime * 1f * CameraMove.runtimeRatio;
            moveSpeed = Mathf.Min(moveSpeed, centerPerson.GetComponent<PersonMoveUAV>().moveSpeed);

            Vector3 targetDirection = (centerPerson.transform.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(targetDirection, Vector3.up);

            if (distance2Center < mainCameraScript.circleRadius)
            {
                isRunningToCenter = false;
            }

        }
        else if (distance2Center > mainCameraScript.circleRadius)
        {

            if (distance2Center > mainCameraScript.circleRadius * 0.8f)
            {
                isRunningToCenter = true;
            }

            if (nextBoundTime < Time.time)
            {
                nextBoundTime = Time.time + 0.2f * CameraMove.runtimeRatio;

                float rotationOffset = Random.value * 60f - 30f;
                transform.rotation = Quaternion.Euler(centerPerson.transform.rotation.eulerAngles + Vector3.up * rotationOffset);

                float personAngle = Vector3.Angle(centerPerson.transform.forward, transform.position - centerPerson.transform.position);
                var centerPersonMoveUAV = centerPerson.GetComponent<PersonMoveUAV>();
                if (personAngle >= 90)
                {
                    moveSpeed = (Random.value * 0.2f + 1.2f) * centerPersonMoveUAV.moveSpeed;
                }
                else
                {
                    moveSpeed = (Random.value * 0.2f + 0.6f) * centerPersonMoveUAV.moveSpeed;
                }
            }
        }
        else
        {
            //print("in circle");

        }
        transform.Translate(transform.forward * moveSpeed * Time.deltaTime, Space.World);
    }

    void PresetWeightPoints()
    {
        weightPoints = new Transform[2];
    }
    public void ResetPosition()
    {
        transform.position = Vector3.Lerp(weightPoints[0].position, weightPoints[1].position, Random.value);
        if (Random.value < 0.5f)
        {
            currentPointsIndex = 0;
        }
        else
        {
            currentPointsIndex = 1;
        }
        WalkAlongRoute();
    }

    public void ResetPosition(int personIndex)
    {
        switch (personIndex)
        {
            case 0:
                transform.position = centerPerson.transform.position - centerPerson.transform.right * 0.8f;
                //transform.position = centerPerson.transform.position - centerPerson.transform.right * 1.2f;
                break;
            case 1:
                transform.position = centerPerson.transform.position - centerPerson.transform.right * 0.4f;
                //transform.position = centerPerson.transform.position - centerPerson.transform.right * 0.8f;
                break;
            case 2:
                transform.position = centerPerson.transform.position + centerPerson.transform.right * 0.4f;
                //transform.position = centerPerson.transform.position + centerPerson.transform.right * 0.8f;
                break;
            case 3:
                transform.position = centerPerson.transform.position + centerPerson.transform.right * 0.8f;
                //transform.position = centerPerson.transform.position + centerPerson.transform.right * 1.2f;
                break;
            case 4:
                transform.position = centerPerson.transform.position - centerPerson.transform.right * 0.6f + centerPerson.transform.forward * 0.4f;
                //transform.position = centerPerson.transform.position - centerPerson.transform.right * 0.6f + centerPerson.transform.forward * 0.6f;
                break;
            case 5:
                transform.position = centerPerson.transform.position + centerPerson.transform.right * 0.6f - centerPerson.transform.forward * 0.4f;
                //transform.position = centerPerson.transform.position + centerPerson.transform.right * 0.6f + centerPerson.transform.forward * 0.6f;
                break;
            default:
                print("ResetPosition Error. No Index:  " + personIndex.ToString());
                break;
        }

        float rotationOffset = Random.value * 60f - 30f;
        transform.rotation = Quaternion.Euler(centerPerson.transform.rotation.eulerAngles + Vector3.up * rotationOffset);
    }

    // private void OnCollisionEnter(Collision collision) {
    //     if (collision.gameObject.name == "floor") {
    //         //GetComponent<Rigidbody>().useGravity = false;
    //     }
    // }

    IEnumerator rotate(Quaternion targetRotation)
    {
        Quaternion beginRotation = transform.rotation;
        float rotatePercentage = 0f;
        yield return 0.02f;
    }

    private void OnDrawGizmos()
    {
        if (isCenterPerson)
        {
            Gizmos.color = Color.green;
            //Gizmos.DrawWireSphere(transform.position, mainCameraScript.circleRadius);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(centerPerson.transform.position, mainCameraScript.circleRadius);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(centerPerson.transform.position, mainCameraScript.circleRadius * 1.2f);
        }
    }
}

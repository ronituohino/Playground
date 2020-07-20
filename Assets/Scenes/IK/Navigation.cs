using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Navigation : MonoBehaviour
{
    public Camera cam;
    public NavMeshAgent navMeshAgent;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray r = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit rch;
            Physics.Raycast(r, out rch);
            navMeshAgent.SetDestination(rch.point);
        }
    }
}

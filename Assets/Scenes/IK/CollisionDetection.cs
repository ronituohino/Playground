using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    public bool colliding = false;
    public Collision collision;

    private void OnCollisionEnter(Collision collision)
    {
        colliding = true;
        this.collision = collision;
    }

    private void OnCollisionExit(Collision collision)
    {
        colliding = false;
        this.collision = collision;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackFollows : MonoBehaviour
{
    public Transform target;

    public float maxDistance;

    void Update()
    {
        float dist = Vector3.Distance(target.position, transform.position);
        if(dist > maxDistance) {
            transform.position = target.position + (transform.position - target.position)/dist * maxDistance;
        }
    }
}

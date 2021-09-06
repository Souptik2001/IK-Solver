using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxMove : MonoBehaviour
{
    public Transform target;
    void Start()
    {
        
    }


    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, target.position, 0.002f);
    }
}

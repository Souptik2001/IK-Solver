using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRot : MonoBehaviour
{
    [Range(0, 5)]
    public float speed = 2;
    void Start()
    {
        
    }


    void Update()
    {
        transform.rotation = Quaternion.AngleAxis(speed, -Vector3.up) * transform.rotation;
    }
}

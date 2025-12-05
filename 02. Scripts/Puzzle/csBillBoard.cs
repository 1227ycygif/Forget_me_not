using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class csBillBoard : MonoBehaviour
{
    public Transform target;    // XR Origin 내부 카메라 위치
    private void Start()
    {
        
    }

    private void OnEnable()
    {
        target = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if(target)  transform.LookAt(target.position);
        transform.Rotate(0, 180f, 0);
    }
}

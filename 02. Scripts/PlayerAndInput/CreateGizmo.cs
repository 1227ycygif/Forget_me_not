using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateGizmo : MonoBehaviour
{
    public Color myColor = Color.red;
    public float myRadius = 0.05f;

    // Start is called before the first frame update
    void Start()
    {
        myColor.a = 128.0f;     // 반투명
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = myColor;
        Gizmos.DrawSphere(transform.position, myRadius);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotArrowClick : MonoBehaviour
{
    public bool RightHand;

    [SerializeField] GameObject gear;
    

    private void OnMouseDown()
    {
        if (gear == null) return;

        if (RightHand)
        {
            gear.transform.parent.gameObject.GetComponent<PuzzleLock>().RotateRight(gear.transform);
        }
        else
        {
            gear.transform.parent.gameObject.GetComponent<PuzzleLock>().RotateLeft(gear.transform);
        }
    }
}

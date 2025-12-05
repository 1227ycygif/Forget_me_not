using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiftPlatform : MonoBehaviour
{
    [Header("위/아래 목표 위치")]
    [SerializeField] private Transform bottomPoint;
    [SerializeField] private Transform topPoint;

    [Header("이동 속도")]
    [SerializeField] private float moveSpeed = 2f;

    private Vector3 targetPos;
    private bool isMoving;

    private void Start()
    {
        // 시작 위치를 바닥으로 고정
        if (bottomPoint)
        {
            transform.position = bottomPoint.position;
            targetPos = bottomPoint.position;
        }
    }

    private void Update()
    {
        //   마스터만 실제로 움직이고
        //   다른 클라들은 PhotonTransformView로 위치만 동기화 받게 함
        if (!PhotonNetwork.isMasterClient)
            return;

        if (!isMoving)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            transform.position = targetPos;
            isMoving = false;
        }
    }

    public void MoveUp()
    {
        if (!topPoint) return;

        targetPos = topPoint.position;
        isMoving = true;
    }

    public void MoveDown()
    {
        if (!bottomPoint) return;

        targetPos = bottomPoint.position;
        isMoving = true;
    }
}
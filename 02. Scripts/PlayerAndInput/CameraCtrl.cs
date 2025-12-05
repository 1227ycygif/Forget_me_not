using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCtrl : MonoBehaviour
{
    [SerializeField] Transform _playerPos; // 플레이어 캠 위치, 원점
    [SerializeField] float speed; // 캠 이동 속도

    //[HideInInspector] 
    public Transform _targetPos;

    string _Slide = "GameController";
    string _Padlock = "Finish";


    private void Awake()
    {
        _playerPos = GameObject.FindGameObjectWithTag("CamPosPlayer").transform;

        //// 임시
        //_targetPos = GameObject.FindGameObjectWithTag(_Slide).transform;

        // 임시
        // 시작하면 카메라는 플레이어 위치로
        _targetPos = _playerPos;
    }

    private void Start()
    {
        
    }

    private void FixedUpdate()
    {
        if (_targetPos == null)
            return;

        // 위치 이동
        transform.position = Vector3.Lerp(transform.position, _targetPos.position, Time.deltaTime * speed);

        // 회전 이동
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetPos.rotation, Time.deltaTime * speed);
    }

    public void CamPosBack()
    {
        // 원점으로 돌아온다
        _targetPos = _playerPos;
    }

    public void SetCamPos(Transform target)
    {
        if (target == null) return;

        _targetPos = target;
    }
    public void SetInitPos(Transform player)
    {
        if (player == null) return;

        _playerPos = player;
        _targetPos = _playerPos;
        // 기본 속도 세팅도 같이 해주자
        speed = 4f;
    }


}

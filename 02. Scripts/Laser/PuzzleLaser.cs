#define CBT_MODE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 퍼즐은 포톤 뷰가무조건 필요하다
[RequireComponent(typeof(PhotonView))]

public class PuzzleLaser : MonoBehaviour
{
    [SerializeField] GameObject mirrorLeft;
    [SerializeField] GameObject mirrorRight;
    [SerializeField] Transform targetPos_L;
    [SerializeField] Transform targetPos_R;

    PhotonView pv;

    [SerializeField]
    bool isSolved = false;      // 정답 판정용 변수

    public float lerpSpeed;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    private void Update()
    {
        if (isSolved)
        {
            mirrorLeft.transform.position = Vector3.Lerp(mirrorLeft.transform.position, targetPos_L.position, Time.deltaTime * lerpSpeed);
            mirrorRight.transform.position = Vector3.Lerp(mirrorRight.transform.position, targetPos_R.position, Time.deltaTime * lerpSpeed);
        }
    }

    // 퍼즐 클리어 판정 함수, 외부 호출로 호출할 수 있어서 public 설정
    public void SetPuzzleClear()
    {
#if CBT_MODE
        isSolved = true;
#else
        pv.RPC("SetSolved", PhotonTargets.All, null);
#endif
    }

    [PunRPC]
    void SetSolved()
    {
        isSolved = true;
    }
}

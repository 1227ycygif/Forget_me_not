using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LiftLever : Photon.MonoBehaviour
{
    [Header("연결된 리프트")]
    [SerializeField] private LiftPlatform lift;

    [Header("조작 키")]
    [SerializeField] private KeyCode interactKey = KeyCode.G;

    private PhotonView pv;
    private bool playerInRange;
    private bool isTop;     // 리프트가 현재 위에 있는지 여부

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            // 여기서 UI 띄우고 싶으면 "E키: 레버 조작" 같은 텍스트 활성화
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            // UI 비활성화
        }
    }

    private void Update()
    {
        if (!playerInRange)
            return;

        // 어떤 클라에서든 G를 누르면 → 마스터에게 "토글해달라" 요청
        if (Input.GetKeyDown(interactKey))
        {
            pv.RPC(nameof(RPC_RequestToggleLever), PhotonTargets.MasterClient);
        }
    }

    /// <summary>
    /// 클라이언트 → 마스터로 보내는 요청
    /// </summary>
    [PunRPC]
    private void RPC_RequestToggleLever(PhotonMessageInfo info)
    {
        // 마스터만 실행
        if (!PhotonNetwork.isMasterClient)
            return;

        ToggleLever();
    }

    /// <summary>
    /// 실제로 레버 상태를 바꾸고 리프트를 움직이는 쪽 (마스터에서만 호출)
    /// </summary>
    private void ToggleLever()
    {
        isTop = !isTop;

        if (lift != null)
        {
            if (isTop) lift.MoveUp();
            else lift.MoveDown();
        }

        // 레버 애니/사운드 등도 여기서 처리하고
        // 그 상태를 모든 클라에 브로드캐스트
        pv.RPC(nameof(RPC_SyncLeverState), PhotonTargets.Others, isTop);
    }

    /// <summary>
    /// 다른 클라들에 레버 상태만 동기화 (애니메이션용)
    /// </summary>
    [PunRPC]
    private void RPC_SyncLeverState(bool top)
    {
        isTop = top;
        // 여기서 레버 애니메이션 bool/trigger 세팅해주면 됨
        // 예: anim.SetBool("IsUp", isTop);
    }
}
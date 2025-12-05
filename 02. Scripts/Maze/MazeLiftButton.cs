using UnityEngine;

public class MazeLiftButton : Photon.MonoBehaviour
{
    [Header("연결된 그룹 매니저")]
    [SerializeField] private MazeLiftGroup group;

    [Header("이 버튼이 담당하는 큐브 인덱스 (0 = A, 1 = B, ...)")]
    [SerializeField] private int liftIndex = 0;

    [Header("조작 키")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private PhotonView pv;
    private bool playerInRange;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            // 여기서 "E키: 버튼 누르기" UI 표시 가능
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            // UI 숨기기
        }
    }

    private void Update()
    {
        if (!playerInRange)
            return;
        if (group == null)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            // 어떤 클라에서든 → 마스터에게 "이 인덱스 활성화" 요청
            pv.RPC(nameof(RPC_RequestActivate), PhotonTargets.MasterClient, liftIndex);
        }
    }

    [PunRPC]
    private void RPC_RequestActivate(int index, PhotonMessageInfo info)
    {
        // 마스터에서만 처리
        if (!PhotonNetwork.isMasterClient)
            return;

        if (group != null)
        {
            group.Activate(index);
        }

        // 버튼 애니/상태를 동기화하고 싶으면
        // 여기서 다른 클라로도 RPC 쏴서 UI만 맞춰주면 됨.
    }
}
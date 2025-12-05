using UnityEngine;

public class MazeGoalButton : Photon.MonoBehaviour
{
    [SerializeField] private MazeLiftGroup group;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool oneTimeUse = true;

    private PhotonView pv;
    private bool playerInRange;
    private bool used;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    private void Update()
    {
        // 디버깅용, 지울지 살릴 지 정할 것
        if (Input.GetKeyDown(KeyCode.F2))
        {
            pv.RPC(nameof(RPC_RequestOpenAll), PhotonTargets.MasterClient);
        }

        if (!playerInRange)
            return;
        if (group == null)
            return;
        if (oneTimeUse && used)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            pv.RPC(nameof(RPC_RequestOpenAll), PhotonTargets.MasterClient);
        }
    }

    [PunRPC]
    private void RPC_RequestOpenAll(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        if (oneTimeUse && used)
            return;

        used = true;

        group.OpenAll();

        // 버튼 상태를 다른 클라에도 알려주고 싶으면 여기서 브로드캐스트
        pv.RPC(nameof(RPC_SyncUsed), PhotonTargets.Others, used);
    }

    [PunRPC]
    private void RPC_SyncUsed(bool value)
    {
        used = value;
        // 여기서 버튼 비활성화, 색 변경 등 UI 싱크 가능
    }
}
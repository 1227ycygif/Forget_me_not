using UnityEngine;
using UnityEngine.UI;
using Photon;

public class RoomDebugger : PunBehaviour
{
    [Header("UI 요소들")]
    public Button testButton;
    public Text statusText;

    private PhotonView pv;

    void Start()
    {
        // PhotonView 컴포넌트 가져오기
        pv = GetComponent<PhotonView>();

        if (pv == null)
        {
            Debug.LogError("PhotonView 컴포넌트가 없습니다! GameObject에 PhotonView를 추가해주세요.");
            return;
        }

        // 버튼 이벤트 연결
        if (testButton != null)
        {
            testButton.onClick.AddListener(OnClickTestButton);
        }

        // 초기 상태 텍스트 설정
        if (statusText != null)
        {
            statusText.text = "동기화 테스트 대기 중...";
            statusText.color = Color.white;
        }

        // 방 정보 출력
        PrintRoomInfo();
    }

    // ========== 1번: Debug 로그 출력 ==========
    void PrintRoomInfo()
    {
        Debug.Log("========================================");
        Debug.Log("====== scRoom 진입 확인 (DEBUG) ======");
        Debug.Log("========================================");

        Debug.Log("🏠 방 이름: " + PhotonNetwork.room.Name);

        // 방 코드 출력 (CustomProperties에서 가져오기)
        if (PhotonNetwork.room.CustomProperties.ContainsKey("RoomCord"))
        {
            Debug.Log("🔑 방 코드: " + PhotonNetwork.room.CustomProperties["RoomCord"]);
        }

        Debug.Log("👥 현재 인원: " + PhotonNetwork.room.PlayerCount + "/" + PhotonNetwork.room.MaxPlayers);
        Debug.Log("👤 내 닉네임: " + PhotonNetwork.player.NickName);
        Debug.Log("👑 현재 방장: " + PhotonNetwork.masterClient.NickName);

        Debug.Log("\n=== 방에 있는 모든 플레이어 ===");
        foreach (PhotonPlayer player in PhotonNetwork.playerList)
        {
            string role = player.IsMasterClient ? "[방장]" : "[참가자]";
            Debug.Log(role + " " + player.NickName + " (ID: " + player.ID + ")");
        }
        Debug.Log("========================================\n");
    }

    // 플레이어가 들어올 때
    void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        Debug.Log("✅ 플레이어 입장: " + newPlayer.NickName);
        Debug.Log("현재 인원: " + PhotonNetwork.room.PlayerCount + "/2");
    }

    // 플레이어가 나갈 때
    void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        Debug.Log("❌ 플레이어 퇴장: " + otherPlayer.NickName);
        Debug.Log("현재 인원: " + PhotonNetwork.room.PlayerCount + "/2");
    }

    // ========== 3번: 동기화 테스트 버튼 ==========
    void OnClickTestButton()
    {
        Debug.Log("🔵 동기화 테스트 버튼 클릭!");

        // RPC로 모든 플레이어에게 신호 전송
        pv.RPC("ReceiveSyncTest", PhotonTargets.All, PhotonNetwork.player.NickName);
    }

    [PunRPC]
    void ReceiveSyncTest(string senderName)
    {
        Debug.Log("📡 RPC 수신: " + senderName + "님이 테스트 버튼을 눌렀습니다!");

        if (statusText != null)
        {
            statusText.text = "✅ " + senderName + "님이 버튼을 눌렀습니다!";
            statusText.color = Color.green;
        }

        // 2초 후 원래대로
        Invoke("ResetStatus", 2f);
    }

    void ResetStatus()
    {
        if (statusText != null)
        {
            statusText.text = "동기화 테스트 대기 중...";
            statusText.color = Color.white;
        }
    }

    // ========== 추가: 수동으로 방 정보 다시 출력 (테스트용) ==========
    void Update()
    {
        // F5 키를 누르면 방 정보 다시 출력
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log("\n[F5] 방 정보 새로고침");
            PrintRoomInfo();
        }
    }
}
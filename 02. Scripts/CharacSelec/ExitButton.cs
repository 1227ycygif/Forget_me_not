using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExitButton : MonoBehaviour
{
    // 로그 메시지를 표시할 Text (선택 사항)
    public Text logText;

    void Start()
    {
        // 버튼 컴포넌트 가져오기
        Button btn = GetComponent<Button>();

        if (btn != null)
        {
            // 버튼 클릭 이벤트에 함수 연결
            btn.onClick.AddListener(OnClickExitRoom);
        }
        else
        {
            Debug.LogError("Button 컴포넌트를 찾을 수 없습니다!");
        }
    }

    // 방 나가기 버튼 클릭 시 호출
    void OnClickExitRoom()
    {
        // PhotonView를 가진 오브젝트 찾기 (CharacterSelectManager 등)
        PhotonView[] photonViews = FindObjectsOfType<PhotonView>();

        if (photonViews.Length > 0)
        {
            PhotonView pv = photonViews[0];
            string msg = "\n<color=#ff0000>[" + PhotonNetwork.player.NickName + "] 퇴장</color>";
            pv.RPC("LogMsg", PhotonTargets.AllBuffered, msg);
        }

        Debug.Log("방 나가기: " + PhotonNetwork.room.Name);
        PhotonNetwork.LeaveRoom();
    }

    // 방을 나간 후 호출되는 콜백
    void OnLeftRoom()
    {
        Debug.Log("방을 나갔습니다. 로비로 돌아갑니다.");

        // 로비 씬으로 이동
        SceneManager.LoadScene("scLobby");
    }

    // 포톤 연결이 끊겼을 때 호출되는 콜백
    void OnDisconnectedFromPhoton()
    {
        Debug.Log("포톤 서버 연결 끊김");

        // 로비 씬으로 이동
        SceneManager.LoadScene("scLobby");
    }

    // 연결 실패 시 호출되는 콜백
    void OnConnectionFail(DisconnectCause cause)
    {
        Debug.LogError("연결 실패: " + cause);

        // 로비 씬으로 이동
        SceneManager.LoadScene("scLobby");
    }
}
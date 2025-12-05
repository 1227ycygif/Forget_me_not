using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PhotonView))]

public class InGameChat : MonoBehaviour
{
    [Header("채팅 로그창")]
    public Text txtChatLog;
    [Header("채팅 입력창")]
    public Text txtInput;

    public InputField inputField;

    bool Entered = false;

    PhotonView pv;

    // 입력 메세지 전송을 위한 중계 변수
    string message;



    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        // Enter 키로 로그인
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (inputField.interactable)
            {
                OnClickSend();
            }
        }
    }

    // 매세지 버튼 클릭 시 실행되는 이벤트 구독시키기
    public void OnClickSend()
    {
        // 입력창의 메세지 풀링
        message = "[" + PhotonNetwork.player.NickName + "] " + txtInput.text;
        
        // 메세지 RPC 전송하기
        pv.RPC(nameof(LogMsg), PhotonTargets.All, message);

        // 입력창 비우기
        inputField = txtInput.transform.parent.gameObject.GetComponent<InputField>();
        inputField.text = string.Empty;
        inputField.ForceLabelUpdate();

        // 탈출 구문 
        inputField.DeactivateInputField();
        EventSystem.current.SetSelectedGameObject(null);
    }

    [PunRPC]
    void LogMsg(string msg)
    { 
        // 채팅은 쓸때마다 갱신하기, += 으로 바꾸면 누적식으로 변환
        txtChatLog.text = msg;
        // 채팅이 갱신됐음을 알리는 알림음 출력
        // 마스터로 적은 이유?  안하면 플레이어 수만큼 중첩 출력되더라고 ㅇㅇ
        if (PhotonNetwork.isMasterClient)
            SoundManager.manager.SFXPlay(6, this.transform.position);
    }
}

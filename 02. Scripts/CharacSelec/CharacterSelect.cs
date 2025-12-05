using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelect : MonoBehaviour
{
    // 캐릭터 인덱스 상수
    private const int FEMALE_CHARACTER = 1; // 여자 캐릭터
    private const int MALE_CHARACTER = 2;   // 남자 캐릭터

    // 준비 버튼 (2개 - 각 캐릭터 아래)
    public Button leftReadyButton;    // 왼쪽 준비 버튼
    public Button rightReadyButton;   // 오른쪽 준비 버튼

    // 플레이어 1(여자), 2(남자)의 선택 상태를 표시할 텍스트 또는 이미지
    public Text player1StatusText;
    public Text player2StatusText;

    // 캐릭터 이미지 (상대 위치 기준)
    public GameObject myCharacterImg;      // 내 캐릭터 이미지 (왼쪽)
    public GameObject otherCharacterImg;   // 상대방 캐릭터 이미지 (오른쪽)

    // 실제 캐릭터 스프라이트 또는 프리팹 (Inspector에서 할당)
    public Sprite girlCharacterSprite;     // 여자 캐릭터 스프라이트
    public Sprite boyCharacterSprite;      // 남자 캐릭터 스프라이트

    // PhotonView 컴포넌트 참조
    private PhotonView pv;

    // 로그 메시지를 표시할 Text UI
    public Text txtLogMsg;

    // 인원 수 표시
    public Text playerCountText;

    // 방 나가기 버튼 참조
    public Button exitButton;

    // 내 준비 버튼과 상대방 준비 버튼 참조
    private Button myReadyButton;
    private Button otherReadyButton;

    // 현재 로컬 플레이어가 선택한 캐릭터 (자동 배정)
    private int mySelectedCharacter = 0;

    // 다른 플레이어가 선택한 캐릭터
    private int otherPlayerCharacter = 0;

    // 준비 완료 상태
    private bool isReady = false;
    private bool otherPlayerReady = false;

    // GUI 스타일 (OnGUI용)
    private GUIStyle titleStyle;
    private GUIStyle normalStyle;
    private GUIStyle highlightStyle;

    // 테스트용, 나중에 수정할 것
    [Header("불러올 씬 이름")]
    [SerializeField]
    string nextSceneName;


    void Start()
    {
        // PhotonView 컴포넌트 가져오기
        pv = GetComponent<PhotonView>();

        if (pv == null)
        {
            Debug.LogError("PhotonView 컴포넌트가 없습니다! GameObject에 PhotonView를 추가해주세요.");
            return;
        }

        // 씬 로딩 완료 후 네트워크 메시지 수신 재개
        PhotonNetwork.isMessageQueueRunning = true;

        // 자동 씬 동기화 활성화 (중요!)
        PhotonNetwork.automaticallySyncScene = true;

        Debug.Log("=== 캐릭터 선택 씬 시작 ===");
        Debug.Log("현재 방: " + PhotonNetwork.room.Name);
        Debug.Log("방 인원: " + PhotonNetwork.room.PlayerCount + "/" + PhotonNetwork.room.MaxPlayers);
        Debug.Log("내가 방장인가? " + PhotonNetwork.isMasterClient);
        Debug.Log("현재 방장: " + PhotonNetwork.masterClient.NickName);
        Debug.Log("자동 씬 동기화: " + PhotonNetwork.automaticallySyncScene);

        // 자동으로 캐릭터 배정 및 버튼 설정
        AutoAssignCharacter();

        // GUI 스타일 초기화
        InitializeGUIStyles();

        // 초기 인원 수
        GetConnectPlayerCount();

        // 입장 메시지 전송
        string msg = "\n<color=#00ff00>[" + PhotonNetwork.player.NickName + "] 입장</color>";
        pv.RPC("LogMsg", PhotonTargets.AllBuffered, msg);
    }

    // GUI 스타일 초기화
    void InitializeGUIStyles()
    {
        // 제목 스타일 (큰 글씨, 색, Bold)
        titleStyle = new GUIStyle();
        titleStyle.fontSize = 22;
        titleStyle.normal.textColor = new Color(1f, 0.8f, 0f);
        //titleStyle.normal.textColor = Color.yellow;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.UpperLeft;

        // 일반 텍스트 스타일 (중간 크기, 색)
        normalStyle = new GUIStyle();
        normalStyle.fontSize = 18;
        normalStyle.normal.textColor = Color.black;
        normalStyle.alignment = TextAnchor.UpperLeft;

        // 강조 텍스트 스타일 (중간 크기, 색)
        highlightStyle = new GUIStyle();
        highlightStyle.fontSize = 18;
        //highlightStyle.normal.textColor = new Color(1f, 0.8f, 0f);
        highlightStyle.normal.textColor = Color.cyan;
        highlightStyle.fontStyle = FontStyle.Bold;
        highlightStyle.alignment = TextAnchor.UpperLeft;
    }

    // 캐릭터 자동 배정 (방장 = 여자, 참가자 = 남자)
    void AutoAssignCharacter()
    {
        // 방 인원수 확인
        int playerCount = PhotonNetwork.room.PlayerCount;
        Debug.Log("현재 방 인원수: " + playerCount);

        // 방에 나 혼자만 있으면 무조건 방장(여자 캐릭터)
        if (playerCount == 1)
        {
            mySelectedCharacter = FEMALE_CHARACTER;
            Debug.Log("방에 혼자 있음 - 여자 캐릭터 자동 배정 (방장)");
        }
        // 방에 2명 이상이면 MasterClient 여부로 판단
        else if (PhotonNetwork.isMasterClient)
        {
            mySelectedCharacter = FEMALE_CHARACTER;
            Debug.Log("방장으로 여자 캐릭터 자동 배정");
        }
        else
        {
            mySelectedCharacter = MALE_CHARACTER;
            Debug.Log("참가자로 남자 캐릭터 자동 배정");
        }

        // 내 준비 버튼과 상대방 준비 버튼 설정
        SetupReadyButtons();

        // 다른 플레이어에게 내 선택 정보 전송
        pv.RPC("ReceiveCharacterSelection", PhotonTargets.OthersBuffered, mySelectedCharacter);

        UpdateUI();
    }

    // 준비 버튼 설정 (내 버튼은 클릭 가능, 상대방 버튼은 표시만)
    void SetupReadyButtons()
    {
        // 항상 왼쪽이 내 캐릭터, 오른쪽이 상대방 캐릭터
        myReadyButton = leftReadyButton;
        otherReadyButton = rightReadyButton;

        // 내 버튼 설정
        if (myReadyButton != null)
        {
            myReadyButton.onClick.RemoveAllListeners();
            myReadyButton.onClick.AddListener(OnClickReady);
            myReadyButton.interactable = true;

            Text myBtnText = myReadyButton.GetComponentInChildren<Text>();
            if (myBtnText != null)
            {
                myBtnText.text = "준비 완료";
                myBtnText.color = Color.white;
            }
        }

        // 상대방 버튼 설정 (클릭 불가)
        if (otherReadyButton != null)
        {
            otherReadyButton.interactable = false;

            Text otherBtnText = otherReadyButton.GetComponentInChildren<Text>();
            if (otherBtnText != null)
            {
                otherBtnText.text = "대기 중";
                otherBtnText.color = Color.gray;
            }
        }

        Debug.Log("버튼 설정 완료 - 내 캐릭터: " + (mySelectedCharacter == FEMALE_CHARACTER ? "여자" : "남자"));
    }

    // 준비 완료 버튼 클릭 시 호출
    void OnClickReady()
    {
        if (mySelectedCharacter == 0)
        {
            Debug.LogError("캐릭터가 배정되지 않았습니다!");
            return;
        }

        // 이미 준비 완료 상태면 취소
        if (isReady)
        {
            // 방장이고 상대방도 준비 완료 상태면 게임 시작
            if (PhotonNetwork.isMasterClient && otherPlayerReady)
            {
                Debug.Log("게임 시작!");
                StartCoroutine(StartGameCountdown());
                return;
            }

            // 준비 취소
            CancelReady();
            return;
        }

        // 준비 완료
        isReady = true;
        Debug.Log("준비 완료!");

        string msg = "\n<color=#yellow>[" + PhotonNetwork.player.NickName + "] 준비 완료!</color>";
        pv.RPC("LogMsg", PhotonTargets.AllBuffered, msg);

        // 다른 플레이어에게 준비 완료 전송
        pv.RPC("ReceiveReadyStatus", PhotonTargets.OthersBuffered, true);

        // 내 준비 버튼 업데이트
        UpdateMyReadyButton();

        UpdateUI();

        // 두 플레이어 모두 준비 완료 시 방장 버튼을 "게임 시작"으로 변경
        CheckBothPlayersReady();
    }

    // 준비 취소
    void CancelReady()
    {
        isReady = false;
        Debug.Log("준비 취소");

        string msg = "\n<color=#gray>[" + PhotonNetwork.player.NickName + "] 준비 취소</color>";
        pv.RPC("LogMsg", PhotonTargets.AllBuffered, msg);

        // 다른 플레이어에게 준비 취소 전송
        pv.RPC("ReceiveReadyStatus", PhotonTargets.OthersBuffered, false);

        // 내 준비 버튼 업데이트
        if (myReadyButton != null)
        {
            myReadyButton.interactable = true;

            Text myBtnText = myReadyButton.GetComponentInChildren<Text>();
            if (myBtnText != null)
            {
                myBtnText.text = "준비 완료";
                myBtnText.color = Color.white;
            }
        }

        UpdateUI();
    }

    // 내 준비 버튼 업데이트
    void UpdateMyReadyButton()
    {
        if (myReadyButton == null) return;

        Text myBtnText = myReadyButton.GetComponentInChildren<Text>();

        if (isReady)
        {
            // 준비 완료 상태
            myReadyButton.interactable = true; // 취소 가능하게

            if (myBtnText != null)
            {
                myBtnText.text = "준비 취소";
                myBtnText.color = Color.green;
            }
        }
        else
        {
            // 준비 전 상태
            myReadyButton.interactable = true;

            if (myBtnText != null)
            {
                myBtnText.text = "준비 완료";
                myBtnText.color = Color.white;
            }
        }
    }

    // 게임 시작 카운트다운
    IEnumerator StartGameCountdown()
    {
        // 버튼 비활성화
        if (myReadyButton != null)
        {
            myReadyButton.interactable = false;
        }

        // 방 나가기 버튼 비활성화 (카운트다운 중 나가기 방지)
        if (exitButton != null)
        {
            exitButton.interactable = false;
            Debug.Log("게임 시작 - 방 나가기 버튼 비활성화");
        }

        // 모든 플레이어에게 게임 시작 알림
        pv.RPC("ReceiveGameStarting", PhotonTargets.All);

        // 3초 카운트다운
        for (int i = 3; i > 0; i--)
        {
            Debug.Log("게임 시작까지: " + i + "초");

            // 버튼 텍스트 업데이트
            if (myReadyButton != null)
            {
                Text btnText = myReadyButton.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.text = i + "초 후 시작...";
                }
            }

            yield return new WaitForSeconds(1f);
        }

        // 게임 씬으로 이동
        LoadGameScene();
    }

    // 게임 시작 알림을 받는 RPC
    [PunRPC]
    void ReceiveGameStarting()
    {
        Debug.Log("게임이 곧 시작됩니다!");

        // 참가자도 방 나가기 버튼 비활성화
        if (exitButton != null)
        {
            exitButton.interactable = false;
            Debug.Log("게임 시작 - 방 나가기 버튼 비활성화 (참가자)");
        }

        // 참가자 화면에서도 카운트다운 표시
        if (!PhotonNetwork.isMasterClient)
        {
            StartCoroutine(ShowCountdownForGuest());
        }
    }

    // 참가자용 카운트다운 표시
    IEnumerator ShowCountdownForGuest()
    {
        if (myReadyButton != null)
        {
            myReadyButton.interactable = false;
        }

        for (int i = 3; i > 0; i--)
        {
            if (myReadyButton != null)
            {
                Text btnText = myReadyButton.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.text = i + "초 후 시작...";
                    btnText.color = Color.yellow;
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    // 다른 플레이어의 캐릭터 선택을 받는 RPC
    [PunRPC]
    void ReceiveCharacterSelection(int characterIndex)
    {
        otherPlayerCharacter = characterIndex;
        Debug.Log("다른 플레이어 캐릭터 배정 받음: " + characterIndex);
        Debug.Log("현재 방 인원: " + PhotonNetwork.room.PlayerCount);

        // 중복 체크: 내 캐릭터와 같으면 경고
        if (otherPlayerCharacter == mySelectedCharacter)
        {
            Debug.LogWarning("⚠️ 캐릭터 중복 감지! 내 캐릭터: " + mySelectedCharacter + ", 상대방: " + otherPlayerCharacter);
            Debug.LogWarning("방 인원: " + PhotonNetwork.room.PlayerCount + ", 내가 방장? " + PhotonNetwork.isMasterClient);
        }

        UpdateUI();
    }

    // 다른 플레이어의 준비 상태를 받는 RPC
    [PunRPC]
    void ReceiveReadyStatus(bool ready)
    {
        otherPlayerReady = ready;
        Debug.Log("다른 플레이어 준비 상태: " + (ready ? "완료" : "취소"));

        // 상대방 준비 버튼 업데이트
        if (otherReadyButton != null)
        {
            Text otherBtnText = otherReadyButton.GetComponentInChildren<Text>();
            if (otherBtnText != null)
            {
                if (ready)
                {
                    otherBtnText.text = "준비 완료!";
                    otherBtnText.color = Color.green;
                }
                else
                {
                    otherBtnText.text = "대기 중";
                    otherBtnText.color = Color.gray;
                }
            }
        }

        UpdateUI();

        // 두 플레이어 준비 상태 확인
        CheckBothPlayersReady();
    }

    // 두 플레이어 모두 준비 완료 확인
    void CheckBothPlayersReady()
    {
        // 상대방이 없으면 게임 시작 불가
        if (otherPlayerCharacter == 0)
        {
            Debug.Log("상대방이 없습니다. 게임 시작 불가");

            if (PhotonNetwork.isMasterClient && myReadyButton != null)
            {
                Text btnText = myReadyButton.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    if (isReady)
                    {
                        btnText.text = "준비 취소";
                        btnText.color = Color.green;
                    }
                    else
                    {
                        btnText.text = "준비 완료";
                        btnText.color = Color.white;
                    }
                }
            }
            return; // ← 여기서 리턴
        }

        // 두 플레이어가 모두 존재할 때
        if (isReady && otherPlayerReady)
        {
            Debug.Log("모든 플레이어 준비 완료!");

            // 방장이면 버튼을 "게임 시작"으로 변경
            if (PhotonNetwork.isMasterClient && myReadyButton != null)
            {
                myReadyButton.interactable = true;

                Text btnText = myReadyButton.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.text = "게임 시작!";
                    btnText.color = Color.cyan; // 하늘색으로 강조
                }

                Debug.Log("방장: 게임 시작 버튼 활성화");
            }
        }
        else
        {
            // 한 명이라도 준비 취소하면 버튼 원래대로
            if (PhotonNetwork.isMasterClient && isReady && myReadyButton != null)
            {
                Text btnText = myReadyButton.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.text = "준비 취소";
                    btnText.color = Color.green;
                }
            }
        }
    }

    /* ==========================================
     * 방장 권한 양도 관련 코드 (현재 비활성화)
     * 필요 시 주석 해제하여 사용
     * ==========================================
     
    // 여자 캐릭터를 선택한 플레이어에게 방장 권한 양도
    void TransferMasterClientIfNeeded()
    {
        // 내가 여자 캐릭터를 선택했고, 아직 방장이 아닌 경우
        if (mySelectedCharacter == FEMALE_CHARACTER && !PhotonNetwork.isMasterClient)
        {
            Debug.Log("여자 캐릭터 선택 - 방장 권한 요청");
            // 현재 방장에게 권한 양도 요청
            pv.RPC("RequestMasterClientTransfer", PhotonTargets.MasterClient);
        }
        // 내가 현재 방장이고, 상대방이 여자 캐릭터를 선택한 경우
        else if (otherPlayerCharacter == FEMALE_CHARACTER && PhotonNetwork.isMasterClient)
        {
            Debug.Log("상대방이 여자 캐릭터 선택 - 방장 권한 양도 시작");
            TransferMasterClientToOther();
        }
        else
        {
            Debug.Log("방장 권한 양도 불필요 (현재 방장: " + PhotonNetwork.masterClient.NickName + ")");
        }
    }

    // 방장 권한 양도 요청을 받는 RPC (방장만 받음)
    [PunRPC]
    void RequestMasterClientTransfer()
    {
        if (PhotonNetwork.isMasterClient)
        {
            Debug.Log("방장 권한 양도 요청 받음 - 권한 양도 시작");
            TransferMasterClientToOther();
        }
    }

    // 방장 권한을 다른 플레이어에게 양도
    void TransferMasterClientToOther()
    {
        // 방에 있는 다른 플레이어 찾기
        foreach (PhotonPlayer player in PhotonNetwork.playerList)
        {
            if (player != PhotonNetwork.player)
            {
                Debug.Log("방장 권한 양도: " + PhotonNetwork.player.NickName + " → " + player.NickName);
                PhotonNetwork.SetMasterClient(player);
                return;
            }
        }
    }
    
    ========================================== */

    // 방장이 바뀌었을 때 호출되는 콜백
    void OnMasterClientSwitched(PhotonPlayer masterClient)
    {
        Debug.Log("⚠️ 방장이 변경되었습니다: " + masterClient.NickName);

        // 내가 새로운 방장이 되었다면
        if (PhotonNetwork.isMasterClient)
        {
            Debug.Log("내가 새 방장이 되었습니다!");

            // CustomProperties의 HostID를 내 ID로 업데이트
            UpdateRoomHostId();

            // 캐릭터도 여자로 변경 (방장은 항상 여자)
            if (mySelectedCharacter != FEMALE_CHARACTER)
            {
                mySelectedCharacter = FEMALE_CHARACTER;
                Debug.Log("방장이 되어 여자 캐릭터로 변경");

                // 다른 플레이어에게 변경 사항 전송
                pv.RPC("ReceiveCharacterSelection", PhotonTargets.OthersBuffered, mySelectedCharacter);

                // 버튼 재설정
                SetupReadyButtons();
                UpdateUI();
            }
        }
    }

    // 방의 HostID를 현재 방장의 ID로 업데이트
    void UpdateRoomHostId()
    {
        if (!PhotonNetwork.isMasterClient)
        {
            Debug.LogWarning("방장이 아니므로 HostID를 업데이트할 수 없습니다");
            return;
        }

        // 현재 방장의 NickName을 HostID로 설정
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["HostID"] = PhotonNetwork.player.NickName;

        PhotonNetwork.room.SetCustomProperties(props);

        Debug.Log("방의 HostID 업데이트 완료: " + PhotonNetwork.player.NickName);
    }

    // 게임 씬으로 이동하는 함수
    // 방장만 호출
    void LoadGameScene()
    {
        if (PhotonNetwork.isMasterClient)
            pv.RPC("BeginLoad", PhotonTargets.All, nextSceneName);
    }

    [PunRPC]
    void BeginLoad(string sceneName)
    {
        // 메시지 큐는 로딩 시작 직전에만 일시정지
        StartCoroutine(Co_LoadScene(sceneName));
    }

    IEnumerator Co_LoadScene(string sceneName)
    {
        // RPC 핑 호출 차이 방지
        yield return new WaitForSeconds(1f);

        PhotonNetwork.isMessageQueueRunning = false;
        PhotonNetwork.networkingPeer.loadingLevelAndPausedNetwork = true;
        PhotonNetwork.networkingPeer.AsynchLevelLoadCall = true;

        SceneMoveManager.Instance.LoadScene(sceneName, CutsceneName.Intro);
        yield break;
    }

    // UI 업데이트
    void UpdateUI()
    {
        // 내 캐릭터 이미지 설정 (왼쪽)
        if (myCharacterImg != null)
        {
            Image myImg = myCharacterImg.GetComponent<Image>();
            if (myImg != null)
            {
                // 내가 여자면 여자 스프라이트, 남자면 남자 스프라이트
                if (mySelectedCharacter == FEMALE_CHARACTER)
                {
                    myImg.sprite = girlCharacterSprite;
                }
                else if (mySelectedCharacter == MALE_CHARACTER)
                {
                    myImg.sprite = boyCharacterSprite;
                }
            }
            myCharacterImg.SetActive(mySelectedCharacter != 0);
        }

        // 상대방 캐릭터 이미지 설정 (오른쪽)
        if (otherCharacterImg != null)
        {
            Image otherImg = otherCharacterImg.GetComponent<Image>();
            if (otherImg != null)
            {
                // 상대방이 여자면 여자 스프라이트, 남자면 남자 스프라이트
                if (otherPlayerCharacter == FEMALE_CHARACTER)
                {
                    otherImg.sprite = girlCharacterSprite;
                }
                else if (otherPlayerCharacter == MALE_CHARACTER)
                {
                    otherImg.sprite = boyCharacterSprite;
                }
            }
            otherCharacterImg.SetActive(otherPlayerCharacter != 0);
        }

        // 플레이어 상태 텍스트 업데이트
        if (player1StatusText != null)
        {
            // 왼쪽은 항상 "나"
            string myCharName = mySelectedCharacter == FEMALE_CHARACTER ? "여자" : "남자";
            player1StatusText.text = "나 - " + myCharName + " 캐릭터" + (isReady ? " (준비 완료)" : "");
        }

        if (player2StatusText != null)
        {
            // 오른쪽은 항상 "상대방"
            if (otherPlayerCharacter != 0)
            {
                string otherCharName = otherPlayerCharacter == FEMALE_CHARACTER ? "여자" : "남자";
                player2StatusText.text = "상대방 - " + otherCharName + " 캐릭터" + (otherPlayerReady ? " (준비 완료)" : "");
            }
            else
            {
                player2StatusText.text = "상대방 대기 중...";
            }
        }
    }

    void GetConnectPlayerCount()
    {
        if (playerCountText != null)
        {
            Room currRoom = PhotonNetwork.room;
            playerCountText.text = currRoom.PlayerCount.ToString() + "/" + currRoom.MaxPlayers.ToString();
        }
    }

    // 선택한 캐릭터 정보를 다음 씬으로 전달하기 위한 함수
    public static int GetMyCharacter()
    {
        return PlayerPrefs.GetInt("MY_CHARACTER", 0);
    }

    // 여자 캐릭터 여부 확인
    public static bool IsFemaleCharacter()
    {
        return GetMyCharacter() == FEMALE_CHARACTER;
    }

    [PunRPC]
    void LogMsg(string msg)
    {
        if (txtLogMsg != null)
        {
            txtLogMsg.text = txtLogMsg.text + msg;
        }
        Debug.Log("LogMsg: " + msg);
    }



    // 네트워크 플레이어가 방에 접속했을 때 호출
    void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        Debug.Log(newPlayer.NickName + " 님이 입장했습니다.");
        GetConnectPlayerCount();

        string msg = "\n<color=#00ff00>[" + newPlayer.NickName + "] 입장</color>";
        pv.RPC("LogMsg", PhotonTargets.Others, msg);
    }

    // 네트워크 플레이어가 방에서 나갔을 때 호출
    void OnPhotonPlayerDisconnected(PhotonPlayer outPlayer)
    {
        Debug.Log(outPlayer.NickName + " 님이 퇴장했습니다.");

        string msg = "\n<color=#ff0000>[" + outPlayer.NickName + "] 퇴장</color>";
        pv.RPC("LogMsg", PhotonTargets.Others, msg);

        // 상대방 정보 초기화
        otherPlayerCharacter = 0;
        otherPlayerReady = false;

        // UI 업데이트
        UpdateUI();

        // 상대방 버튼 초기화
        if (otherReadyButton != null)
        {
            otherReadyButton.interactable = false;

            Text otherBtnText = otherReadyButton.GetComponentInChildren<Text>();
            if (otherBtnText != null)
            {
                otherBtnText.text = "대기 중";
                otherBtnText.color = Color.gray;
            }
        }

        // 내 준비 상태 확인 및 버튼 업데이트
        if (isReady)
        {
            // 나는 준비 완료 상태이지만 상대방이 나갔으므로
            // 버튼을 "준비 취소"로 유지하되, 게임 시작 불가능하게
            UpdateMyReadyButton();
        }

        // 게임 시작 버튼 비활성화 (상대방이 없으므로)
        CheckBothPlayersReady();

        Debug.Log("상대방 퇴장 처리 완료 - 캐릭터 및 준비 상태 초기화");
        GetConnectPlayerCount();
    }

    void OnDestroy()
    {
        // 선택한 캐릭터 정보 저장
        PlayerPrefs.SetInt("MY_CHARACTER", mySelectedCharacter);
    }

    private void OnGUI()
    {
        // 스타일이 초기화되지 않았으면 기본 스타일 사용
        if (titleStyle == null)
        {
            return;
        }

        GUILayout.Label("현재 방: " + PhotonNetwork.room.Name, normalStyle);
        //GUILayout.Label("현재 방장: " + PhotonNetwork.masterClient.NickName + (PhotonNetwork.isMasterClient ? " (나)" : ""), highlightStyle);
        //GUILayout.Label("");

        //GUILayout.Label("=== 자동 배정 ===", titleStyle);
        //GUILayout.Label("내 캐릭터: " + (mySelectedCharacter == FEMALE_CHARACTER ? "여자 (방장)" : mySelectedCharacter == MALE_CHARACTER ? "남자 (참가자)" : "배정 중..."), normalStyle);
        //GUILayout.Label("상대방 캐릭터: " + (otherPlayerCharacter == FEMALE_CHARACTER ? "여자 (방장)" : otherPlayerCharacter == MALE_CHARACTER ? "남자 (참가자)" : "대기 중..."), normalStyle);
        //GUILayout.Label("");

        //GUILayout.Label("내 준비: " + (isReady ? "완료" : "대기 중"), isReady ? highlightStyle : normalStyle);
        //GUILayout.Label("상대방 준비: " + (otherPlayerReady ? "완료" : "대기 중"), otherPlayerReady ? highlightStyle : normalStyle);
    }
}
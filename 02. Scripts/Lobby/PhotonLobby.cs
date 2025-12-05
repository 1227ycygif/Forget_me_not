//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.SceneManagement;
//using UnityEngine.UI;

//public class PhotonLobby : MonoBehaviour
//{
//    //App의 버전 정보
//    public string version = "Ver 0.1.0";

//    public PhotonLogLevel LogLevel = PhotonLogLevel.Full;

//    //룸 코드를 입력받을 UI 항목 연결 레퍼런스
//    public InputField roomCord;

//    // 친구 ID를 입력 받을 UI 항목 연결 레퍼런스
//    public InputField friendId;

//    // 친구 찾기 모드 전환 Toggle
//    public Toggle friendToggle;

//    //RoomList - 방 목록이 표시될 ScrollView
//    public GameObject roomScrollContent;

//    //FriendList - 친구 목록이 표시될 ScrollView
//    public GameObject friendScrollContent;

//    //RoomList Panel - 활성/비활성 제어용 (Panel 전체)
//    public GameObject roomListPanel;

//    //FriendList Panel - 활성/비활성 제어용 (Panel 전체)
//    public GameObject friendListPanel;

//    //룸 목록만큼 생성될 RoomItem 프리팹 연결 레퍼런스
//    public GameObject roomItem;

//    // 실제로 사용되는 유저 ID (변경 불가능하도록 내부 저장)
//    private string myUserId = "";

//    // 마지막으로 검색한 친구 ID 저장
//    private string lastSearchedFriendId = "";

//    private void Awake()
//    {
//        if (!PhotonNetwork.connected)
//        {
//            PhotonNetwork.ConnectUsingSettings(version);
//            PhotonNetwork.logLevel = LogLevel;
//            PhotonNetwork.playerName = "GUEST " + Random.Range(1, 9999);
//        }

//        //roomCord.text = "ROOM_" + Random.Range(0, 999).ToString("000");
//        roomCord.text = "";

//        if (UserData.Instance != null && UserData.Instance.isLoggedIn)
//        {
//            myUserId = UserData.Instance.username;
//            Debug.Log("로그인된 사용자" + myUserId);
//        }
//        else
//        {
//            Debug.LogError("UserData를 찾을 수 없거나 로그인되지 않았습니다!");
//        }

//        // RoomList와 FriendList의 pivot 설정
//        //roomList.GetComponent<RectTransform>().pivot = new Vector2(0.0f, 1.0f);
//        //friendList.GetComponent<RectTransform>().pivot = new Vector2(0.0f, 1.0f);

//        // 기본값: RoomList Panel 활성화, FriendList Panel 비활성화
//        if (roomListPanel != null)
//        {
//            roomListPanel.SetActive(true);
//        }
//        if (friendListPanel != null)
//        {
//            friendListPanel.SetActive(false);
//        }

//        // Toggle 이벤트 리스너 등록
//        if (friendToggle != null)
//        {
//            friendToggle.onValueChanged.AddListener(OnFriendToggleChanged);
//        }

//    }

//    void OnJoinedLobby()
//    {
//        Debug.Log("로비 입장 완료");

//        if (roomListPanel != null && roomListPanel.activeSelf)
//        {
//            RefreshRoomList();
//        }
//    }

//    // Toggle 상태가 변경될 때 호출되는 함수
//    void OnFriendToggleChanged(bool isOn)
//    {
//        if (isOn)
//        {
//            // 친구 찾기 모드: FriendList Panel 활성화, RoomList Panel 비활성화
//            if (roomListPanel != null)
//            {
//                roomListPanel.SetActive(false);
//            }
//            if (friendListPanel != null)
//            {
//                friendListPanel.SetActive(true);
//            }
//            Debug.Log("친구 찾기 모드 활성화");
//        }
//        else
//        {
//            // 방 찾기 모드: RoomList Panel 활성화, FriendList Panel 비활성화
//            if (roomListPanel != null)
//            {
//                roomListPanel.SetActive(true);
//            }
//            if (friendListPanel != null)
//            {
//                friendListPanel.SetActive(false);
//            }
//            Debug.Log("방 찾기 모드 활성화");
//        }
//    }

//    // 친구 ID 검색 버튼 클릭 시 호출
//    public void OnClickSearchFriend()
//    {
//        string searchId = friendId.text;

//        if (string.IsNullOrEmpty(searchId))
//        {
//            Debug.Log("친구 ID를 입력해주세요");
//            return;
//        }

//        // 검색한 친구 ID 저장 (자동 갱신용)
//        lastSearchedFriendId = searchId;

//        Debug.Log("친구 검색: " + searchId);

//        // 친구 목록 갱신
//        RefreshFriendList();
//    }

//    // 친구 목록을 갱신하는 함수
//    void RefreshFriendList()
//    {
//        if (string.IsNullOrEmpty(lastSearchedFriendId))
//        {
//            Debug.Log("검색된 친구가 없습니다");
//            return;
//        }

//        // 기존 친구 목록 UI 삭제
//        foreach (Transform child in friendScrollContent.transform)
//        {
//            Destroy(child.gameObject);
//        }

//        int rowCount = 0;

//        // 포톤 서버의 방 목록에서 해당 친구가 만든 방을 찾기
//        foreach (RoomInfo _room in PhotonNetwork.GetRoomList())
//        {
//            // 방이 삭제되었으면 건너뛰기 (플레이어 수가 음수면 삭제된 방)
//            if (_room.PlayerCount < 0)
//            {
//                continue;
//            }

//            // 방의 CustomProperties에서 MasterClientID 확인
//            if (_room.CustomProperties != null && _room.CustomProperties.ContainsKey("MasterClientID"))
//            {
//                string masterClientId = _room.CustomProperties["MasterClientID"].ToString();

//                // 검색한 친구 ID와 일치하는 방만 표시
//                if (masterClientId == lastSearchedFriendId)
//                {
//                    Debug.Log("친구의 방 발견: " + _room.Name + " (MasterClient: " + masterClientId + ")");

//                    GameObject room = (GameObject)Instantiate(roomItem);
//                    room.transform.SetParent(friendScrollContent.transform, false);

//                    // 태그 설정
//                    room.tag = "ROOM_ITEM";

//                    //생성한 RoomItem에 룸 정보를 표시하기 위한 텍스트 정보 전달
//                    RoomData roomData = room.GetComponent<RoomData>();
//                    if (roomData != null)
//                    {
//                        roomData.roomCord = _room.Name;
//                        roomData.connectPlayer = _room.PlayerCount;
//                        roomData.maxPlayers = _room.MaxPlayers;

//                        //텍스트 정보를 표시 
//                        roomData.DisplayRoomData();

//                        //RoomItem의 Button 컴포넌트에 클릭 이벤트를 동적으로 연결
//                        Button btn = roomData.GetComponent<UnityEngine.UI.Button>();
//                        if (btn != null)
//                        {
//                            // 기존 리스너 제거 후 새로 추가
//                            btn.onClick.RemoveAllListeners();
//                            string roomNameCopy = _room.Name; // 클로저 문제 방지
//                            btn.onClick.AddListener(
//                                delegate {
//                                    OnClickRoomItem(roomNameCopy);
//                                }
//                            );
//                        }
//                    }

//                    rowCount++;
//                }
//            }
//        }

//        if (rowCount == 0)
//        {
//            Debug.Log("해당 친구의 방을 찾을 수 없습니다");
//        }
//        else
//        {
//            Debug.Log("친구의 방 " + rowCount + "개를 찾았습니다");
//        }
//    }

//    void OnPhotonCreateRoomFailed(object[] codeAndMsg)
//    {
//        //오류 코드
//        Debug.Log(codeAndMsg[0].ToString());
//        //오류 메시지
//        Debug.Log(codeAndMsg[1].ToString());
//        Debug.Log("Create Room Failed = " + codeAndMsg[1]);
//    }

//    void OnJoinedRoom()
//    {
//        Debug.Log("방 입장 완료");

//        //룸 씬으로 전환하는 코루틴 실행
//        StartCoroutine(this.LoadStage());
//    }

//    //룸 씬으로 이동하는 코루틴 함수
//    IEnumerator LoadStage()
//    {
//        //씬을 전환하는 동안 포톤 클라우드 서버로부터 네트워크 메시지 수신 중단
//        PhotonNetwork.isMessageQueueRunning = false;

//        //캐릭터 선택 씬으로 로딩 (게임 방 씬이 아닌 캐릭터 선택 씬으로)
//        AsyncOperation ao = SceneManager.LoadSceneAsync("scCharacterSelect");

//        // 씬 로딩이 완료 될때까지 대기
//        yield return ao;

//        Debug.Log("캐릭터 선택 씬 로딩 완료");
//    }

//    // 방 코드로 입장 버튼 클릭 시 호출
//    public void OnClickJoinByRoomCord()
//    {
//        string _roomCord = roomCord.text;

//        if (string.IsNullOrEmpty(_roomCord))
//        {
//            Debug.Log("방 코드를 입력해주세요");
//            return;
//        }

//        //로컬 플레이어의 이름을 설정 (내부 저장된 ID 사용)
//        PhotonNetwork.player.NickName = myUserId;

//        //플레이어 이름을 저장
//        PlayerPrefs.SetString("USER_ID", myUserId);

//        //입력한 방 코드로 직접 입장
//        PhotonNetwork.JoinRoom(_roomCord);
//        Debug.Log("방 코드로 입장 시도: " + _roomCord + " (플레이어: " + myUserId + ")");
//    }

//    // 방 생성 버튼 클릭 시 호출
//    public void OnClickCreateRoom()
//    {
//        string _roomName = roomCord.text.Trim();

//        //룸 이름이 없거나 Null일 경우 룸 이름 지정
//        if (string.IsNullOrEmpty(roomCord.text))
//        {
//            Debug.Log("방 제목을 입력해주세요");
//            return;
//        }

//        if (_roomName.Length > 20)
//        {
//            Debug.Log("방 제목은 한글 10자, 영어 20자 이내로 입력해주세요!");
//            return;
//        }

//        //로컬 플레이어의 이름을 설정 (내부 저장된 ID 사용)
//        PhotonNetwork.player.NickName = myUserId;

//        //플레이어의 이름을 로컬 저장
//        PlayerPrefs.SetString("USER_ID", myUserId);

//        //방탈출 게임용 룸 옵션 설정 (최대 2명)
//        RoomOptions roomOptions = new RoomOptions();
//        roomOptions.IsOpen = true;      // 방 입장 가능
//        roomOptions.IsVisible = true;   // 로비 목록에 표시
//        roomOptions.MaxPlayers = 2;     // 최대 2명으로 제한

//        // 방장의 ID를 CustomProperties에 저장 (중복 검사용, 내부 ID 사용)
//        ExitGames.Client.Photon.Hashtable customProps = new ExitGames.Client.Photon.Hashtable();
//        customProps["MasterClientID"] = myUserId;
//        roomOptions.CustomRoomProperties = customProps;
//        roomOptions.CustomRoomPropertiesForLobby = new string[] { "MasterClientID" };

//        //지정한 조건에 맞는 룸 생성 함수 
//        PhotonNetwork.CreateRoom(_roomName, roomOptions, TypedLobby.Default);

//        Debug.Log("방 생성 시도: " + _roomName + " (MasterClient: " + myUserId + ")");
//    }

//    // 방 목록이 업데이트될 때 자동으로 호출되는 콜백 함수
//    void OnReceivedRoomListUpdate()
//    {
//        Debug.Log("방 목록 업데이트 감지");

//        // 친구 찾기 모드일 때
//        if (friendToggle != null && friendToggle.isOn)
//        {
//            // 친구 검색을 한 적이 있으면 자동으로 친구 목록 갱신
//            if (!string.IsNullOrEmpty(lastSearchedFriendId))
//            {
//                Debug.Log("친구 목록 자동 갱신: " + lastSearchedFriendId);
//                RefreshFriendList();
//            }
//            return;
//        }

//        // 방 찾기 모드일 때는 방 목록 갱신
//        RefreshRoomList();
//    }

//    // 방 목록을 갱신하는 함수
//    void RefreshRoomList()
//    {
//        // 기존 방 목록 UI 삭제
//        foreach (Transform child in roomScrollContent.transform)
//        {
//            Destroy(child.gameObject);
//        }

//        int rowCount = 0;

//        // 포톤 서버로부터 받은 방 목록을 순회하며 UI 생성
//        foreach (RoomInfo _room in PhotonNetwork.GetRoomList())
//        {
//            // 방이 꽉 찼거나, 닫혀있거나, 플레이어가 0명(삭제된 방)이면 목록에 표시하지 않음
//            if (_room.PlayerCount >= _room.MaxPlayers || !_room.IsOpen || _room.PlayerCount < 0)
//            {
//                Debug.Log("제외된 방: " + _room.Name + " (인원: " + _room.PlayerCount + " / " + _room.MaxPlayers + ", 열림: " + _room.IsOpen + ")");
//                continue;
//            }

//            Debug.Log("입장 가능한 방: " + _room.Name + _room.PlayerCount + " / " + _room.MaxPlayers);

//            GameObject room = (GameObject)Instantiate(roomItem);
//            room.transform.SetParent(roomScrollContent.transform, false);

//            // 태그 설정 (나중에 삭제할 때 찾기 위해)
//            room.tag = "ROOM_ITEM";

//            //생성한 RoomItem에 룸 정보를 표시하기 위한 텍스트 정보 전달
//            RoomData roomData = room.GetComponent<RoomData>();
//            if (roomData != null)
//            {
//                roomData.roomCord = _room.Name;
//                roomData.connectPlayer = _room.PlayerCount;
//                roomData.maxPlayers = _room.MaxPlayers;

//                //텍스트 정보를 표시 
//                roomData.DisplayRoomData();

//                //RoomItem의 Button 컴포넌트에 클릭 이벤트를 동적으로 연결
//                Button btn = roomData.GetComponent<Button>();
//                Debug.Log("🔍 Button 찾기 시도: " + _room.Name);

//                if (btn != null)
//                {
//                    Debug.Log("✅ Button 찾음! 이벤트 등록 중...");
//                    // 기존 리스너 제거 후 새로 추가
//                    btn.onClick.RemoveAllListeners();
//                    string roomNameCopy = _room.Name; // 클로저 문제 방지
//                    btn.onClick.AddListener(
//                        delegate {
//                            OnClickRoomItem(roomNameCopy);
//                        }
//                    );
//                }
//            }
//            else
//            {
//                Debug.LogError("RoomData 컴포넌트를 찾을 수 없습니다!");
//                Debug.LogError("❌ Button을 찾을 수 없습니다!");
//            }

//            rowCount++;
//        }

//        if (rowCount == 0)
//        {
//            Debug.Log("입장 가능한 방이 없습니다");
//        }
//        else
//        {
//            Debug.Log("총 " + rowCount + "개의 방이 표시되었습니다");
//        }
//    }

//    // 방 목록에서 특정 방을 클릭했을 때 호출
//    void OnClickRoomItem(string roomName)
//    {
//        Debug.Log("방 클릭됨! 방 이름 : " + roomName);
     
//        //로컬 플레이어의 이름을 설정 (내부 저장된 ID 사용)
//        PhotonNetwork.player.NickName = myUserId;

//        //플레이어 이름을 저장
//        PlayerPrefs.SetString("USER_ID", myUserId);

//        //인자로 전달된 이름에 해당하는 룸으로 입장
//        PhotonNetwork.JoinRoom(roomName);

//        Debug.Log("방 입장 시도 : " + roomName + " (플레이어 : " + myUserId + ")");
//    }

//    private void OnGUI()
//    {
//        GUILayout.Label(PhotonNetwork.connectionStateDetailed.ToString());
//    }
//}
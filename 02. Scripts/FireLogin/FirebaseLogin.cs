using Firebase;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirebaseLogin : MonoBehaviour
{
    [Header("UI")]
    public InputField idInput;
    public InputField passwordInput;
    public Text Text_Msg;
    public Button loginButton;

    [Header("Scene 이름")]
    public string lobbySceneName = "scLobby";

    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private bool isInitialized = false;

    void Start()
    {
        loginButton.interactable = false;
        InitializeFirebaseAsync();
    }

    void Update()
    {
        // Tab 키로 입력 필드 이동
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            HandleTabNavigation();
        }

        // Enter 키로 로그인
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (loginButton.interactable)
            {
                OnLoginClicked();
            }
        }
    }

    void HandleTabNavigation()
    {
        Selectable current = EventSystem.current.currentSelectedGameObject?.GetComponent<Selectable>();

        if (current == null)
        {
            // 아무것도 선택되지 않았으면 첫 번째 필드 선택
            idInput.Select();
            return;
        }

        Selectable next;

        // Shift+Tab: 역방향 이동
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            next = current.FindSelectableOnUp();
            if (next == null)
            {
                // 첫 번째 필드에서 역방향이면 마지막으로
                next = loginButton;
            }
        }
        // Tab: 순방향 이동
        else
        {
            next = current.FindSelectableOnDown();
            if (next == null)
            {
                // 마지막 필드에서 순방향이면 첫 번째로
                next = idInput;
            }
        }

        if (next != null)
        {
            InputField inputField = next.GetComponent<InputField>();
            if (inputField != null)
            {
                inputField.Select();
            }
            else
            {
                next.Select();
            }
        }
    }

    async void InitializeFirebaseAsync()
    {
        Text_Msg.text = "Firebase 초기화 중...";

        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                isInitialized = true;
                Debug.Log("Firebase 초기화 완료");

                if (FirebaseDBManager.Instance != null)
                {
                    FirebaseDBManager.Instance.Init(auth);
                }
                Debug.Log("Firebase DB 초기화 완료");

                Text_Msg.text = "로그인 가능";
                loginButton.interactable = true;
            }
            else
            {
                Debug.LogError("Firebase 초기화 실패: " + dependencyStatus);
                Text_Msg.text = "Firebase 초기화 실패";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Firebase 초기화 예외: " + e.Message);
            Text_Msg.text = "Firebase 초기화 오류";
        }
    }

    public void OnLoginClicked()
    {
        if (!isInitialized || auth == null)
        {
            Text_Msg.text = "Firebase가 준비되지 않았습니다";
            return;
        }

        string email = idInput.text.Trim();
        string pw = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pw))
        {
            Text_Msg.text = "이메일과 비밀번호를 입력하세요";
            return;
        }

        if (!email.Contains("@"))
        {
            Text_Msg.text = "이메일 형식이 올바르지 않습니다";
            return;
        }

        LoginAsync(email, pw);
    }

    async void LoginAsync(string email, string pw)
    {
        Text_Msg.text = "로그인 중...";
        loginButton.interactable = false;

        try
        {
            var result = await auth.SignInWithEmailAndPasswordAsync(email, pw);
            currentUser = result.User;

            Debug.Log($"로그인 성공: {currentUser.Email} / UID: {currentUser.UserId}");

            SaveUserData();

            Text_Msg.text = "로그인 성공! Photon 연결 중...";

            ConnectToPhoton();
            
        }
        catch (System.Exception e)
        {
            Debug.LogError("로그인 실패: " + e.Message);
            Text_Msg.text = GetFirebaseErrorMessage(e);
        }
        finally
        {
            loginButton.interactable = true;
        }
    }


    void SaveUserData()
    {
        if (UserData.Instance != null)
        {
            string userId = currentUser.UserId;
            string username = currentUser.DisplayName;

            if (string.IsNullOrEmpty(username))
            {
                username = currentUser.Email.Split('@')[0];
            }

            UserData.Instance.SetUserInfo(userId, username, 0);

            Debug.Log($"UserData 저장 완료: {userId} / {username}");
        }
        else
        {
            Debug.LogError("UserData Instance를 찾을 수 없습니다");
        }
    }

    void ConnectToPhoton()
    {
        if (currentUser ==  null)
        {
            Debug.LogError("currentUser NULL");
            Text_Msg.text = "사용자 정보 오류";
            return;
        }

        string nickname = currentUser.DisplayName;
        if (string.IsNullOrEmpty(nickname))
        {
            nickname = currentUser.Email.Split('@')[0];
        }

        // 이미 연결 중이면 이름만 변경
        if (PhotonNetwork.connected)
        {
            PhotonNetwork.playerName = nickname;
            Debug.Log("Photon 이미 연결됨 - playerName 변경 : " + nickname);

            // 로비가 없으면 입장
            if (!PhotonNetwork.insideLobby)
            {
                PhotonNetwork.JoinLobby();
            }
            else
            {
                // 로비라면, 씬 전환
                Invoke("LoadLobbyScene", 0.5f);
            }
        }
        else
        {
            // 미연결 중일 시 연결
            PhotonNetwork.playerName = nickname;
            Debug.Log($"Photon 연결 시도... playerName : {PhotonNetwork.playerName}, userId : {currentUser.UserId}");
            PhotonNetwork.ConnectUsingSettings("1.0");
        }  
    }

    void OnConnectedToMaster()
    {
        Debug.Log("Photon Master 서버 연결 성공!");
        Text_Msg.text = "Photon 연결 성공! 로비 입장 중...";

        // 로비 입장
        PhotonNetwork.JoinLobby();
    }

    void OnJoinedLobby()
    {
        Debug.Log("Photon 로비 입장 성공!");
        Text_Msg.text = "로비 입장 완료!";

        // 1초 후 로비 씬으로 전환
        Invoke("LoadLobbyScene", 1f);
    }

    void LoadLobbyScene()
    {
        Debug.Log("로비 씬으로 이동: " + lobbySceneName);
        SceneManager.LoadScene(lobbySceneName);
    }

    void OnDisconnectedFromPhoton()
    {
        Debug.LogError("Photon 연결 끊김");
        Text_Msg.text = "Photon 연결 실패";
        loginButton.interactable = true;
    }

    void OnFailedToConnectToPhoton(DisconnectCause cause)
    {
        Debug.LogError($"Photon 연결 실패: {cause}");
        Text_Msg.text = $"Photon 연결 실패: {cause}";
        loginButton.interactable = true;
    }

    string GetFirebaseErrorMessage(System.Exception exception)
    {
        if (exception is Firebase.FirebaseException firebaseEx)
        {
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            switch (errorCode)
            {
                case AuthError.EmailAlreadyInUse:
                    return "이미 사용 중인 이메일입니다";
                case AuthError.InvalidEmail:
                    return "이메일 형식이 올바르지 않습니다";
                case AuthError.WrongPassword:
                    return "비밀번호가 틀렸습니다";
                case AuthError.WeakPassword:
                    return "비밀번호가 너무 약합니다 (6자 이상)";
                case AuthError.NetworkRequestFailed:
                    return "네트워크 연결을 확인하세요";
                default:
                    return "로그인 실패: " + errorCode.ToString();
            }
        }

        return "알 수 없는 오류: " + exception.Message;
    }
}
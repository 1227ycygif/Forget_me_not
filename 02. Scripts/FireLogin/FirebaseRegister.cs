using Firebase;
using Firebase.Auth;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FirebaseRegister : MonoBehaviour
{
    [Header("UI")]
    public InputField IdInput;
    public InputField passwordInput;
    public InputField passwordConfirmInput;
    public InputField usernameInput;
    public Text Text_Msg;
    public Button registerButton;

    private FirebaseAuth auth;
    private bool isInitialized = false;

    void Start()
    {
        // 버튼 비활성화
        registerButton.interactable = false;
        InitializeFirebaseAsync();
    }

    void Update()
    {
        // Tab 키로 입력 필드 이동
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            HandleTabNavigation();
        }

        // Enter 키로 회원가입
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (registerButton.interactable)
            {
                OnRegisterClicked();
            }
        }
    }

    void HandleTabNavigation()
    {
        Selectable current = EventSystem.current.currentSelectedGameObject?.GetComponent<Selectable>();

        if (current == null)
        {
            // 아무것도 선택되지 않았으면 첫 번째 필드 선택
            IdInput.Select();
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
                next = registerButton;
            }
        }
        // Tab: 순방향 이동
        else
        {
            next = current.FindSelectableOnDown();
            if (next == null)
            {
                // 마지막 필드에서 순방향이면 첫 번째로
                next = IdInput;
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
                Text_Msg.text = "회원가입 가능";
                registerButton.interactable = true;
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


    public void OnRegisterClicked()
    {
        if (!isInitialized || auth == null)
        {
            Text_Msg.text = "Firebase가 준비되지 않았습니다";
            return;
        }

        string email = IdInput.text.Trim();
        string pw = passwordInput.text;
        string pwConfirm = passwordConfirmInput.text;
        string username = usernameInput.text.Trim();

        // 입력값 검증
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pw) || string.IsNullOrEmpty(username))
        {
            Text_Msg.text = "모든 항목을 입력하세요";
            return;
        }

        if (email.Length > 26)
        {
            Text_Msg.text = "이메일은 26자 이내로 입력하세요";
            return;
        }

        if (!email.Contains("@"))
        {
            Text_Msg.text = "이메일 형식이 올바르지 않습니다";
            return;
        }

        if (pw.Length < 8)
        {
            Text_Msg.text = "비밀번호는 8자 이상이어야 합니다";
            return;
        }

        if (pw.Length > 15)
        {
            Text_Msg.text = "비밀번호는 15자 이내로 입력하세요";
            return;
        }

        if (pw != pwConfirm)
        {
            Text_Msg.text = "비밀번호가 일치하지 않습니다";
            return;
        }

        RegisterAsync(email, pw, username);
    }


    async void RegisterAsync(string email, string pw, string username)
    {
        Text_Msg.text = "회원가입 중...";
        registerButton.interactable = false;

        try
        {
            // 회원가입
            var result = await auth.CreateUserWithEmailAndPasswordAsync(email, pw);
            FirebaseUser newUser = result.User;

            Debug.Log($"회원가입 성공: {newUser.Email} / UID: {newUser.UserId}");

            // 닉네임 설정
            UserProfile profile = new UserProfile
            {
                DisplayName = username
            };

            await newUser.UpdateUserProfileAsync(profile);
            Debug.Log("닉네임 설정 완료: " + username);

            Text_Msg.text = "회원가입 성공!";
        }
        catch (System.Exception e)
        {
            Debug.LogError("회원가입 실패: " + e.Message);
            Text_Msg.text = GetFirebaseErrorMessage(e);
        }
        finally
        {
            registerButton.interactable = true;
        }
    }

    // Firebase 에러 메시지 한글로 변환
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
                case AuthError.WeakPassword:
                    return "비밀번호가 너무 약합니다 (6자 이상)";
                case AuthError.NetworkRequestFailed:
                    return "네트워크 연결을 확인하세요";
                default:
                    return "회원가입 실패: " + errorCode.ToString();
            }
        }
        return "알 수 없는 오류" + exception.Message;
    }
}
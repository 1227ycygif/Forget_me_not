using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections.Generic;

public class FirebaseDBManager : MonoBehaviour
{
    public static FirebaseDBManager Instance { get; private set; }

    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    public DatabaseReference reference;

    public bool IsInitialized { get; private set; } = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //void Start()
    //{
    //    Init();
    //}

    //public void Init()
    //{
    //    FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
    //    {
    //        if (task.Result == DependencyStatus.Available)
    //        {
    //            auth = FirebaseAuth.DefaultInstance;
    //            currentUser = auth.CurrentUser;
    //            reference = FirebaseDatabase.DefaultInstance.RootReference;

    //            IsInitialized = true;

    //            Debug.Log("Firebase Database 초기화 성공!");
    //        }
    //        else
    //        {
    //            Debug.LogError("Firebase 초기화 실패: " + task.Result);
    //        }
    //    });
    //}

    public void Init(FirebaseAuth authInstance)
    {
        if (IsInitialized) return;

        auth = authInstance;
        reference = FirebaseDatabase.DefaultInstance.RootReference;
        IsInitialized = true;

        Debug.Log("FirebaseDBManager 초기화 완료");
    }

    public string GetUserId()
    {
        if (auth != null && auth.CurrentUser != null)
        {
            return auth.CurrentUser.UserId;
        }

        if (UserData.Instance != null && UserData.Instance.isLoggedIn)
        {
            return UserData.Instance.userId;
        }

        Debug.LogWarning("로그인된 사용자가 없습니다! 테스트 모드로 전환");
        return "TEST_USER_" + SystemInfo.deviceUniqueIdentifier;
    }

    // ==========================================
    // PlayerData 저장 - Dictionary 직접 저장 방식!
    // ==========================================

    public void SavePlayerData(PlayerData data)
    {
        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return;

        DatabaseReference userRef = reference.Child("users").Child(userId);

        // 1. UID 저장
        userRef.Child("uid").SetValueAsync(data.uid);

        // 2. 컬렉션 비트 저장
        userRef.Child("collectionBits").SetValueAsync(data.collectionBits);

        // 3. 세이브 포인트 저장
        userRef.Child("currentSavePoint").SetValueAsync(data.stageProgress.currentSavePoint);

        // 4. 여성 인벤토리 저장 (Dictionary → Firebase가 자동 변환)
        if (data.femaleInventory.Count > 0)
        {
            userRef.Child("femaleInventory").SetValueAsync(data.femaleInventory);
        }

        // 5. 남성 인벤토리 저장
        if (data.maleInventory.Count > 0)
        {
            userRef.Child("maleInventory").SetValueAsync(data.maleInventory);
        }

        // 6. 클리어한 퍼즐 저장
        if (data.stageProgress.clearedPuzzles.Count > 0)
        {
            userRef.Child("clearedPuzzlesList").SetValueAsync(data.stageProgress.clearedPuzzles);
        }

        Debug.Log("플레이어 데이터 저장 완료!");
    }

    // ==========================================
    // PlayerData 불러오기
    // ==========================================

    public void LoadPlayerData(Action<PlayerData> onSuccess)
    {
        if (!IsInitialized)
        {
            Debug.LogError("Firebase가 초기화되지 않았습니다!");
            return;
        }

        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return;

        reference.Child("users").Child(userId).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    PlayerData data = new PlayerData(userId);

                    // UID 불러오기
                    if (snapshot.Child("uid").Exists)
                    {
                        data.uid = snapshot.Child("uid").Value.ToString();
                    }

                    // 컬렉션 비트 불러오기
                    if (snapshot.Child("collectionBits").Exists)
                    {
                        data.collectionBits = int.Parse(snapshot.Child("collectionBits").Value.ToString());
                    }

                    // 세이브 포인트 불러오기
                    if (snapshot.Child("currentSavePoint").Exists)
                    {
                        data.stageProgress.currentSavePoint = int.Parse(snapshot.Child("currentSavePoint").Value.ToString());
                    }

                    // 여성 인벤토리 불러오기
                    if (snapshot.Child("femaleInventory").Exists)
                    {
                        foreach (var child in snapshot.Child("femaleInventory").Children)
                        {
                            string key = child.Key;
                            int value = int.Parse(child.Value.ToString());
                            data.femaleInventory[key] = value;
                        }
                    }

                    // 남성 인벤토리 불러오기
                    if (snapshot.Child("maleInventory").Exists)
                    {
                        foreach (var child in snapshot.Child("maleInventory").Children)
                        {
                            string key = child.Key;
                            int value = int.Parse(child.Value.ToString());
                            data.maleInventory[key] = value;
                        }
                    }

                    // 클리어한 퍼즐 불러오기
                    if (snapshot.Child("clearedPuzzlesList").Exists)
                    {
                        foreach (var child in snapshot.Child("clearedPuzzlesList").Children)
                        {
                            string key = child.Key;
                            bool value = bool.Parse(child.Value.ToString());
                            data.stageProgress.clearedPuzzles[key] = value;
                        }
                    }

                    Debug.Log("플레이어 데이터 불러오기 완료!");
                    onSuccess?.Invoke(data);
                }
                else
                {
                    Debug.Log("저장된 데이터가 없습니다. 새로 생성합니다.");

                    PlayerData newData = new PlayerData(userId);
                    SavePlayerData(newData);

                    onSuccess?.Invoke(newData);
                }
            }
            else
            {
                Debug.LogError("데이터 불러오기 실패: " + task.Exception);
            }
        });
    }

    // ==========================================
    // 개별 데이터 저장
    // ==========================================

    public void SaveCollectionBits(int bits)
    {
        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return;

        reference.Child("users").Child(userId)
            .Child("collectionBits")
            .SetValueAsync(bits)
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log($"컬렉션 비트 저장 완료: {bits}");
                }
            });
    }

    public void SaveCurrentSavePoint(int savePoint)
    {
        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return;

        reference.Child("users").Child(userId)
            .Child("currentSavePoint")
            .SetValueAsync(savePoint)
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log($"세이브 포인트 저장: {savePoint}");
                }
            });
    }

    public void AddClearedPuzzle(string puzzleKey)
    {
        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return;

        reference.Child("users").Child(userId)
            .Child("clearedPuzzlesList")
            .Child(puzzleKey)
            .SetValueAsync(true)
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log($"퍼즐 클리어 추가: {puzzleKey}");
                }
            });
    }

    public void SaveFemaleInventoryItem(string itemKey, int count)
    {
        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return;

        reference.Child("users").Child(userId)
            .Child("femaleInventory")
            .Child(itemKey)
            .SetValueAsync(count)
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log($"여성 인벤토리 아이템 저장: {itemKey} = {count}");
                }
            });
    }

    public void SaveMaleInventoryItem(string itemKey, int count)
    {
        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return;

        reference.Child("users").Child(userId)
            .Child("maleInventory")
            .Child(itemKey)
            .SetValueAsync(count)
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log($"남성 인벤토리 아이템 저장: {itemKey} = {count}");
                }
            });
    }

    // 전체 인벤토리 저장
    public void SaveFemaleInventory(Dictionary<string, int> inventory)
    {
        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return;

        reference.Child("users").Child(userId)
            .Child("femaleInventory")
            .SetValueAsync(inventory)
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("여성 인벤토리 전체 저장 완료");
                }
            });
    }

    public void SaveMaleInventory(Dictionary<string, int> inventory)
    {
        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return;

        reference.Child("users").Child(userId)
            .Child("maleInventory")
            .SetValueAsync(inventory)
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("남성 인벤토리 전체 저장 완료");
                }
            });
    }
}
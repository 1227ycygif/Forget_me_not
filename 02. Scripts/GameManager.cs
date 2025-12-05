using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Normal,
    SolvingPuzzle,
    InMenu,
    Cutscene
}

//public enum DamageStack 
//{ 
//    NoHit, 
//    FristHit, 
//    SecoundHit, 
//    ThirdHit, 
//    FourthHit 
//};

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.Normal;
    //public DamageStack DamageStack { get; private set; } = DamageStack.NoHit;

    public PlayerData currentPlayerData;

    //public float invincibleTime = 3f;
    //public bool isHiting;      // 무적 여부
    //public float hitCooldown;  // 지난 시간

    //public int hp = 5;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            TestSaveAllData();
        }

        //// 무적 시간 처리
        //if (isHiting)
        //{
        //    hitCooldown += Time.deltaTime;
        //    if (hitCooldown >= invincibleTime)
        //    {
        //        isHiting = false;
        //        hitCooldown = 0f;
        //    }
        //}

        //// 테스트용 피격 입력
        //if (Input.GetKeyDown(KeyCode.Y))        // Y키 할당, 나중에 꼭 뺄것
        //{
        //    Debug.Log("공격");
        //    Attacked();
        //}
    }

    //void Attacked()
    //{
    //    if (isHiting || hp <= 0)
    //        return;

    //    hp--;
    //    UpdateDamageStack();

    //    isHiting = true;
    //    hitCooldown = 0f;

    //    Debug.Log($"HP: {hp}, Stack: {DamageStack}");
    //}

    //void UpdateDamageStack()
    //{
    //    switch (hp)
    //    {
    //        case 5: DamageStack = DamageStack.NoHit; break;
    //        case 4: DamageStack = DamageStack.FristHit; break;
    //        case 3: DamageStack = DamageStack.SecoundHit; break;
    //        case 2: DamageStack = DamageStack.ThirdHit; break;
    //        case 1: DamageStack = DamageStack.FourthHit; break;
    //        default:
    //            // TODO: 사망 처리
    //            break;
    //    }
    //}

    void TestSaveAllData()
    {
        if (currentPlayerData == null)
        {
            Debug.LogError("데이터가 없습니다!");
            return;
        }

        Debug.Log("=== 테스트 데이터 추가 시작 ===");

        // 테스트 데이터 추가
        currentPlayerData.femaleInventory["test_key"] = 1;
        currentPlayerData.femaleInventory["test_flower"] = 3;

        currentPlayerData.maleInventory["test_rope"] = 2;
        currentPlayerData.maleInventory["test_battery"] = 5;

        currentPlayerData.stageProgress.clearedPuzzles["puzzle_test_01"] = true;
        currentPlayerData.stageProgress.clearedPuzzles["puzzle_test_02"] = true;

        currentPlayerData.collectionBits = 7;  // 비트: 0111 (3개 해금)
        currentPlayerData.stageProgress.currentSavePoint = 5;

        // Firebase에 저장
        SaveGameData();

        Debug.Log("=== 테스트 데이터 저장 완료 ===");
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        OnStateChanged(newState);
    }

    void OnStateChanged(GameState state)
    {
        Debug.Log($"Game State changed to {state}");
    }

    void Start()
    {
        StartCoroutine(WaitForFirebaseAndLoad());
    }

    IEnumerator WaitForFirebaseAndLoad()
    {
        while (FirebaseDBManager.Instance == null)
        {
            yield return null;
        }

        yield return new WaitUntil(() => FirebaseDBManager.Instance.IsInitialized);

        Debug.Log("FirebaseDBManager 준비 완료 - 데이터 로딩 시작");

        LoadFirebaseData();
    }

    void LoadFirebaseData()
    {
        FirebaseDBManager.Instance.LoadPlayerData((data) =>
        {
            currentPlayerData = data;

            Debug.Log("=== Firebase 데이터 로드 완료 ===");
            Debug.Log("UID: " + data.uid);
            Debug.Log("세이브 포인트: " + data.stageProgress.currentSavePoint);
            Debug.Log("컬렉션 비트: " + data.collectionBits);
            Debug.Log("클리어한 퍼즐 수: " + data.stageProgress.clearedPuzzles.Count);
            Debug.Log("여성 인벤토리 아이템 수: " + data.femaleInventory.Count);
            Debug.Log("남성 인벤토리 아이템 수: " + data.maleInventory.Count);
        });
    }

    // ==========================================
    // 데이터 저장
    // ==========================================

    public void SaveGameData()
    {
        if (currentPlayerData != null)
        {
            FirebaseDBManager.Instance.SavePlayerData(currentPlayerData);
            Debug.Log("게임 데이터 저장 완료");
        }
        else
        {
            Debug.LogWarning("저장할 데이터가 없습니다!");
        }
    }

    // ==========================================
    // 퍼즐 & 진행도 관련
    // ==========================================

    public void OnPuzzleCleared(string puzzleKey)
    {
        if (currentPlayerData == null) return;

        currentPlayerData.stageProgress.clearedPuzzles[puzzleKey] = true;

        FirebaseDBManager.Instance.AddClearedPuzzle(puzzleKey);

        Debug.Log($"퍼즐 클리어: {puzzleKey}");
    }

    public void UpdateSavePoint(int newSavePoint)
    {
        if (currentPlayerData == null) return;

        currentPlayerData.stageProgress.currentSavePoint = newSavePoint;

        FirebaseDBManager.Instance.SaveCurrentSavePoint(newSavePoint);

        Debug.Log($"세이브 포인트 갱신: {newSavePoint}");
    }

    public void UnlockCutscene(int bitIndex)
    {
        if (currentPlayerData == null) return;

        currentPlayerData.collectionBits |= (1 << bitIndex);

        FirebaseDBManager.Instance.SaveCollectionBits(currentPlayerData.collectionBits);

        Debug.Log($"컷씬 해금: {bitIndex}");
    }

    public bool IsCutsceneUnlocked(int bitIndex)
    {
        if (currentPlayerData == null) return false;

        return (currentPlayerData.collectionBits & (1 << bitIndex)) != 0;
    }

    public bool IsPuzzleCleared(string puzzleKey)
    {
        if (currentPlayerData == null) return false;

        return currentPlayerData.stageProgress.clearedPuzzles.ContainsKey(puzzleKey)
            && currentPlayerData.stageProgress.clearedPuzzles[puzzleKey];
    }

    // ==========================================
    // 인벤토리 관련 함수들 (새로 추가!)
    // ==========================================

    // 여성 인벤토리에 아이템 추가
    public void AddFemaleInventoryItem(string itemKey, int count)
    {
        if (currentPlayerData == null) return;

        // 로컬 데이터 업데이트
        if (currentPlayerData.femaleInventory.ContainsKey(itemKey))
        {
            currentPlayerData.femaleInventory[itemKey] += count;
        }
        else
        {
            currentPlayerData.femaleInventory[itemKey] = count;
        }

        // Firebase에 저장
        FirebaseDBManager.Instance.SaveFemaleInventoryItem(itemKey, currentPlayerData.femaleInventory[itemKey]);

        Debug.Log($"여성 인벤토리 추가: {itemKey} x{count} (총: {currentPlayerData.femaleInventory[itemKey]})");
    }

    // 남성 인벤토리에 아이템 추가
    public void AddMaleInventoryItem(string itemKey, int count)
    {
        if (currentPlayerData == null) return;

        // 로컬 데이터 업데이트
        if (currentPlayerData.maleInventory.ContainsKey(itemKey))
        {
            currentPlayerData.maleInventory[itemKey] += count;
        }
        else
        {
            currentPlayerData.maleInventory[itemKey] = count;
        }

        // Firebase에 저장
        FirebaseDBManager.Instance.SaveMaleInventoryItem(itemKey, currentPlayerData.maleInventory[itemKey]);

        Debug.Log($"남성 인벤토리 추가: {itemKey} x{count} (총: {currentPlayerData.maleInventory[itemKey]})");
    }

    // 여성 인벤토리 아이템 사용 (개수 감소)
    public bool UseFemaleInventoryItem(string itemKey, int count)
    {
        if (currentPlayerData == null) return false;

        if (!currentPlayerData.femaleInventory.ContainsKey(itemKey))
        {
            Debug.LogWarning($"여성 인벤토리에 {itemKey} 없음");
            return false;
        }

        if (currentPlayerData.femaleInventory[itemKey] < count)
        {
            Debug.LogWarning($"여성 인벤토리 {itemKey} 부족: 필요 {count}, 보유 {currentPlayerData.femaleInventory[itemKey]}");
            return false;
        }

        // 로컬 데이터 업데이트
        currentPlayerData.femaleInventory[itemKey] -= count;

        // Firebase에 저장
        FirebaseDBManager.Instance.SaveFemaleInventoryItem(itemKey, currentPlayerData.femaleInventory[itemKey]);

        Debug.Log($"여성 인벤토리 사용: {itemKey} x{count} (남은: {currentPlayerData.femaleInventory[itemKey]})");
        return true;
    }

    // 남성 인벤토리 아이템 사용 (개수 감소)
    public bool UseMaleInventoryItem(string itemKey, int count)
    {
        if (currentPlayerData == null) return false;

        if (!currentPlayerData.maleInventory.ContainsKey(itemKey))
        {
            Debug.LogWarning($"남성 인벤토리에 {itemKey} 없음");
            return false;
        }

        if (currentPlayerData.maleInventory[itemKey] < count)
        {
            Debug.LogWarning($"남성 인벤토리 {itemKey} 부족: 필요 {count}, 보유 {currentPlayerData.maleInventory[itemKey]}");
            return false;
        }

        // 로컬 데이터 업데이트
        currentPlayerData.maleInventory[itemKey] -= count;

        // Firebase에 저장
        FirebaseDBManager.Instance.SaveMaleInventoryItem(itemKey, currentPlayerData.maleInventory[itemKey]);

        Debug.Log($"남성 인벤토리 사용: {itemKey} x{count} (남은: {currentPlayerData.maleInventory[itemKey]})");
        return true;
    }

    // 여성 인벤토리 아이템 개수 확인
    public int GetFemaleInventoryItemCount(string itemKey)
    {
        if (currentPlayerData == null) return 0;

        if (currentPlayerData.femaleInventory.ContainsKey(itemKey))
        {
            return currentPlayerData.femaleInventory[itemKey];
        }

        return 0;
    }

    // 남성 인벤토리 아이템 개수 확인
    public int GetMaleInventoryItemCount(string itemKey)
    {
        if (currentPlayerData == null) return 0;

        if (currentPlayerData.maleInventory.ContainsKey(itemKey))
        {
            return currentPlayerData.maleInventory[itemKey];
        }

        return 0;
    }

    // 여성 인벤토리 전체 저장
    public void SaveFemaleInventory()
    {
        if (currentPlayerData == null) return;

        FirebaseDBManager.Instance.SaveFemaleInventory(currentPlayerData.femaleInventory);
    }

    // 남성 인벤토리 전체 저장
    public void SaveMaleInventory()
    {
        if (currentPlayerData == null) return;

        FirebaseDBManager.Instance.SaveMaleInventory(currentPlayerData.maleInventory);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 디버그용   다 지울 것 ////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    public void test1(string itemKey)
    {
        AddFemaleInventoryItem(itemKey, 1);
    }

    public void test2(string itemKey)
    {
        AddMaleInventoryItem(itemKey, 1);
    }

    public void test3(int bitIndex)
    {
        UnlockCutscene(bitIndex);
    }

    public void test4(int newSavePoint)
    {
        UpdateSavePoint(newSavePoint);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////

}
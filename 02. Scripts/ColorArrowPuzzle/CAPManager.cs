using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화살표-문자 매칭 정보
/// </summary>
[System.Serializable]
public class ArrowLetterMapping
{
    public int arrowID;      // 1~9
    public string letterID;  // a~i

    public ArrowLetterMapping(int arrow, string letter)
    {
        arrowID = arrow;
        letterID = letter;
    }
}

/// <summary>
/// Color Arrow Puzzle 전체 관리자
/// - 화살표-문자 매칭 관리
/// - 단서 획득 관리
/// - 세션 관리
/// </summary>
public class CAPManager : MonoBehaviour
{
    [Header("Mapping (세션 동안 유지)")]
    public CAPPieceData[] currentMapping;

    [Header("Clues")]
    public bool clue1Obtained = false;  // Player2가 입력 성공
    public bool clue2Obtained = false;  // Player1이 입력 성공

    private static CAPManager instance;

    void Awake()
    {
        // 싱글톤 패턴 (세션 동안 유지)
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            if (currentMapping == null || currentMapping.Length == 0 || currentMapping[0] == null)
            {
                GenerateNewMapping();
            }

            Debug.Log("CAPManager 초기화 완료");
        }
        else if (instance != this)
        {
            Debug.Log("CAPManager 중복 인스턴스 제거");
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        // OnEnable에서도 확인 (혹시 모를 경우 대비)
        if (currentMapping == null || currentMapping.Length == 0 || currentMapping[0] == null)
        {
            Debug.LogWarning("OnEnable: 매핑이 없어서 재생성!");
            GenerateNewMapping();
        }
    }

    /// <summary>
    /// 싱글톤 인스턴스 가져오기
    /// </summary>
    public static CAPManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CAPManager>();
            }
            return instance;
        }
    }

    /// <summary>
    /// 새로운 화살표-문자 매칭 생성
    /// (지금은 랜덤이 아니라, 고정 9쌍: A D E G I L M O S)
    /// </summary>
    public void GenerateNewMapping()
    {
        // 고정 문자 순서 (CAPPiece의 letterOrder 와 반드시 동일해야 함)
        string letterOrder = "ADEGILMOS";

        if (currentMapping == null || currentMapping.Length != 9)
            currentMapping = new CAPPieceData[9];

        for (int i = 0; i < 9; i++)
        {
            int arrowID = i + 1;                  // 화살표 1~9
            string letterID = letterOrder[i].ToString(); // A,D,E,G,I,L,M,O,S

            if (currentMapping[i] == null)
                currentMapping[i] = new CAPPieceData();

            currentMapping[i].pieceID = i;
            currentMapping[i].arrowID = arrowID;
            currentMapping[i].letterID = letterID;
            currentMapping[i].gridIndex = -1;
        }

        // 디버그 출력
        string log = "[CAPManager] 새 매핑(고정): ";
        for (int i = 0; i < currentMapping.Length; i++)
        {
            log += $"(Arrow {currentMapping[i].arrowID} ↔ {currentMapping[i].letterID}) ";
        }
        Debug.Log(log);
    }

    /// <summary>
    /// 문자 ID로 화살표 ID 찾기
    /// </summary>
    public int GetArrowForLetter(string letterID)
    {
        if (currentMapping == null)
        {
            Debug.LogWarning("GetArrowForLetter: currentMapping 이 null 입니다.");
            return 0;
        }

        foreach (var mapping in currentMapping)
        {
            if (mapping.letterID == letterID)
                return mapping.arrowID;
        }

        Debug.LogWarning($"문자 {letterID}에 대한 매칭을 찾을 수 없습니다!");
        return 1; // 기본값
    }

    /// <summary>
    /// 화살표 ID로 문자 ID 찾기
    /// </summary>
    public string GetLetterForArrow(int arrowID)
    {
        if (currentMapping == null)
        {
            Debug.LogWarning("GetLetterForArrow: currentMapping 이 null 입니다.");
            return "";
        }

        foreach (var mapping in currentMapping)
        {
            if (mapping != null && mapping.arrowID == arrowID)
                return mapping.letterID;
        }

        Debug.LogWarning($"화살표 {arrowID}에 대한 매칭을 찾을 수 없습니다!");
        return "";
    }

    /// <summary>
    /// 단서 1 획득 (Player2가 입력 성공)
    /// </summary>
    public void OnClue1Obtained(string playerName)
    {
        if (clue1Obtained) return;

        clue1Obtained = true;
        Debug.Log($"단서 1 획득! ({playerName})");

        // TODO: UI 표시, 서버 저장 등
    }

    /// <summary>
    /// 단서 2 획득 (Player1이 입력 성공)
    /// </summary>
    public void OnClue2Obtained(string playerName)
    {
        if (clue2Obtained) return;

        clue2Obtained = true;
        Debug.Log($"단서 2 획득! ({playerName})");

        // TODO: UI 표시, 서버 저장 등

        CheckAllCluesObtained();
    }

    /// <summary>
    /// 모든 단서 획득 확인
    /// </summary>
    void CheckAllCluesObtained()
    {
        if (clue1Obtained && clue2Obtained)
        {
            Debug.Log("모든 단서 획득! 퍼즐 완료!");
            // TODO: 게임 클리어 처리
        }
    }

    /// <summary>
    /// 퍼즐 재시작 (매칭 유지!)
    /// </summary>
    public void RestartPuzzle()
    {
        Debug.Log("퍼즐 재시작 - 매칭은 유지됩니다!");

        clue1Obtained = false;
        clue2Obtained = false;

        // 매칭은 유지!
    }

    /// <summary>
    /// 세션 종료 (Room 나가기, 로그아웃 등)
    /// 매칭 리셋!
    /// </summary>
    public void OnSessionEnd()
    {
        Debug.Log("세션 종료 - 매칭 리셋!");

        GenerateNewMapping();
        clue1Obtained = false;
        clue2Obtained = false;
    }

    /// <summary>
    /// 현재 매칭 상태 출력 (디버그용)
    /// </summary>
    [ContextMenu("Print Current Mapping")]
    public void PrintCurrentMapping()
    {
        Debug.Log("=== 현재 화살표-문자 매칭 ===");
        for (int i = 0; i < 9; i++)
        {
            Debug.Log($"화살표 {currentMapping[i].arrowID} ↔ 문자 {currentMapping[i].letterID}");
        }
    }

    /// <summary>
    /// 새 매칭 강제 생성 (디버그용)
    /// </summary>
    [ContextMenu("Generate New Mapping")]
    public void DebugGenerateNewMapping()
    {
        GenerateNewMapping();
    }

    // RPC로 매핑 동기화
    public void SyncMappingToClients()
    {
        if (!PhotonNetwork.inRoom || !PhotonNetwork.isMasterClient)
            return;

        // 매핑 데이터를 배열로 변환
        int[] arrowIDs = new int[9];
        string[] letterIDs = new string[9];

        for (int i = 0; i < 9; i++)
        {
            arrowIDs[i] = currentMapping[i].arrowID;
            letterIDs[i] = currentMapping[i].letterID;
        }

        // RPC로 전송
        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null)
        {
            pv.RPC("RPC_ReceiveMapping", PhotonTargets.Others, arrowIDs, letterIDs);
        }
    }

    [PunRPC]
    void RPC_ReceiveMapping(int[] arrowIDs, string[] letterIDs)
    {
        Debug.Log("[RPC] 매핑 수신!");

        if (currentMapping == null || currentMapping.Length != 9)
            currentMapping = new CAPPieceData[9];

        for (int i = 0; i < 9; i++)
        {
            if (currentMapping[i] == null)
                currentMapping[i] = new CAPPieceData();

            currentMapping[i].pieceID = i;
            currentMapping[i].arrowID = arrowIDs[i];
            currentMapping[i].letterID = letterIDs[i];
            currentMapping[i].gridIndex = -1;
        }

        PrintCurrentMapping();
    }
}
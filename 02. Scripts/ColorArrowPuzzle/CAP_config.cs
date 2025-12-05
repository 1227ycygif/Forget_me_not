using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using static Unity.Burst.Intrinsics.X86.Avx;

/// <summary>
/// Color Arrow Puzzle 조각 설정
/// CAPManager의 매칭 정보를 기반으로 조각 데이터 생성
/// </summary>
public class CAP_config : MonoBehaviour
{
    [Header("Piece Configuration")]
    public CAPPieceData[] pieceDataArray = new CAPPieceData[9];

    [Header("References")]
    public CAPManager capManager;

    void Awake()
    {
        Debug.Log("=== CAP_config Awake 시작 ===");

        // CAPManager가 준비될 때까지 기다리기
        StartCoroutine(InitializeWhenReady());
    }

    IEnumerator InitializeWhenReady()
    {
        // CAPManager 찾기
        capManager = CAPManager.Instance;

        // CAPManager의 매핑이 준비될 때까지 대기
        int maxWait = 10;  // 최대 10프레임 대기
        int waited = 0;

        while (waited < maxWait)
        {
            if (capManager != null &&
                capManager.currentMapping != null &&
                capManager.currentMapping.Length == 9 &&
                capManager.currentMapping[0] != null)
            {
                Debug.Log("CAPManager 준비 완료! 초기화 시작");
                break;
            }

            Debug.Log($"CAPManager 대기 중... ({waited + 1}/{maxWait})");
            yield return null;  // 다음 프레임까지 대기
            waited++;
        }

        if (waited >= maxWait)
        {
            Debug.LogError("CAPManager 초기화 실패!");
            yield break;
        }

        // 이제 초기화
        InitializePieceData();
    }

    /// <summary>
    /// CAPManager의 매칭 정보로 조각 데이터 초기화
    /// </summary>
    void InitializePieceData()
    {
        if (capManager == null)
        {
            Debug.LogError("CAPManager를 찾을 수 없습니다!");
            return;
        }

        Debug.Log("=== 조각 데이터 초기화 시작 ===");

        for (int i = 0; i < 9; i++)
        {
            int arrowID = i + 1;  // 화살표 1~9
            string letterID = capManager.GetLetterForArrow(arrowID);

            pieceDataArray[i] = new CAPPieceData(i, arrowID, letterID, -1);

            Debug.Log($"조각 {i}: 화살표 {arrowID} ↔ 문자 {letterID}");
        }

        Debug.Log("=== 조각 데이터 초기화 완료 ===");
    }

    /// <summary>
    /// 특정 화살표 ID의 조각 데이터 가져오기
    /// </summary>
    public CAPPieceData GetPieceByArrowID(int arrowID)
    {
        foreach (var data in pieceDataArray)
        {
            if (data.arrowID == arrowID)
                return data;
        }

        Debug.LogWarning($"화살표 {arrowID}에 해당하는 조각을 찾을 수 없습니다!");
        return null;
    }

    /// <summary>
    /// 특정 문자 ID의 조각 데이터 가져오기
    /// </summary>
    public CAPPieceData GetPieceByLetterID(string letterID)
    {
        foreach (var data in pieceDataArray)
        {
            if (data.letterID == letterID)
                return data;
        }

        Debug.LogWarning($"문자 {letterID}에 해당하는 조각을 찾을 수 없습니다!");
        return null;
    }

    /// <summary>
    /// 조각 데이터 재초기화 (매칭 변경 시)
    /// </summary>
    [ContextMenu("Reinitialize Piece Data")]
    public void ReinitializePieceData()
    {
        InitializePieceData();
    }

    /// <summary>
    /// 현재 조각 데이터 출력 (디버그용)
    /// </summary>
    [ContextMenu("Print Piece Data")]
    public void PrintPieceData()
    {
        Debug.Log("=== 현재 조각 데이터 ===");
        for (int i = 0; i < 9; i++)
        {
            Debug.Log(pieceDataArray[i].ToString());
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 3x3 그리드 시스템 (3D 월드 좌표 사용)
public class CAPGrid : MonoBehaviour
{
    [Header("Grid Settings - 3D")]
    [SerializeField]
    private float gridSpacing = 1.2f;     // 그리드 간격

    [SerializeField]
    private Vector3 gridStartPosition = new Vector3(-1.2f, 0f, 1.2f);  // 좌측 상단 시작점

    [Header("Grid Visualization")]
    [SerializeField]
    private bool showGridGizmos = true;
    private Vector3[] gridPositions = new Vector3[9];  // 9개 그리드 위치
    private CAPPiece[] currentPieces = new CAPPiece[9];  // 현재 각 그리드에 있는 조각

    void Awake()
    {
        CalculateGridPositions3D();
    }


    // 3x3 그리드 위치 계산
    void CalculateGridPositions3D()
    {
        for (int i = 0; i < 9; i++)
        {
            int row = i / 3;  // 0, 0, 0, 1, 1, 1, 2, 2, 2
            int col = i % 3;  // 0, 1, 2, 0, 1, 2, 0, 1, 2

            float x = gridStartPosition.x + (col * gridSpacing);
            float y = 0f;  // Y축은 0으로 고정 (바닥)
            float z = gridStartPosition.z - (row * gridSpacing);

            gridPositions[i] = new Vector3(x, y, z);

            Debug.Log($"Grid {i}: ({x:F2}, {y:F2}, {z:F2})");
        }
    }


    // 특정 그리드 인덱스의 3D 위치 반환
    public Vector3 GetGridPosition3D(int gridIndex)
    {
        if (gridIndex < 0 || gridIndex >= 9)
        {
            Debug.LogError($"Invalid grid index: {gridIndex}");
            return Vector3.zero;
        }

        return gridPositions[gridIndex];
    }


    // 월드 포지션에서 가장 가까운 그리드 인덱스 찾기 (3D)
    public int GetNearestGridIndex3D(Vector3 worldPosition)
    {
        float minDistance = float.MaxValue;
        int nearestIndex = 0;

        for (int i = 0; i < 9; i++)
        {
            // XZ 평면에서만 거리 계산
            Vector3 gridPos = gridPositions[i];
            float dx = worldPosition.x - gridPos.x;
            float dz = worldPosition.z - gridPos.z;

            float distance = Mathf.Sqrt(dx * dx + dz * dz);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = i;
            }
        }

        if (minDistance > gridSpacing * 2f)
        {
            Debug.LogWarning($"그리드에서 너무 멀리 떨어짐: {minDistance:F2}");
            return -1;
        }

        return nearestIndex;
    }


    // 그리드 영역 기반으로 인덱스 찾기 (더 정확!)
    public int GetGridIndexByRegion(Vector3 worldPosition)
    {
        // 그리드 시작점 기준으로 상대 위치 계산
        float relativeX = worldPosition.x - gridStartPosition.x;
        float relativeZ = gridStartPosition.z - worldPosition.z;  // Z는 반대 방향

        // 그리드 칸 계산
        int col = Mathf.RoundToInt(relativeX / gridSpacing);
        int row = Mathf.RoundToInt(relativeZ / gridSpacing);

        // 범위 제한 (0~2)
        col = Mathf.Clamp(col, 0, 2);
        row = Mathf.Clamp(row, 0, 2);

        // 인덱스 계산
        int gridIndex = row * 3 + col;

        Debug.Log($"위치: ({worldPosition.x:F2}, {worldPosition.z:F2})");
        Debug.Log($"상대 위치: ({relativeX:F2}, {relativeZ:F2})");
        Debug.Log($"Row: {row}, Col: {col}");
        Debug.Log($"Grid Index: {gridIndex}");

        return gridIndex;
    }


    // ============================================================
    // 조각 등록 (개선 버전 - 중복 방지 + 자동 동기화)
    // ============================================================
    public void RegisterPiece(int gridIndex, CAPPiece piece)
    {
        if (gridIndex < 0 || gridIndex >= 9)
        {
            Debug.LogError($"잘못된 gridIndex: {gridIndex}");
            return;
        }

        // ---------------------------------
        // 0) piece == null 이면 해당 칸 비우기
        //    (초기화/리셋 용도)
        // ---------------------------------
        if (piece == null)
        {
            if (currentPieces[gridIndex] != null)
            {
                Debug.Log(
                    $"Grid {gridIndex} 비우기 (기존 Piece {currentPieces[gridIndex].pieceData.pieceID})"
                );
                // 그 칸에 있던 조각의 gridIndex 를 -1로
                currentPieces[gridIndex].pieceData.gridIndex = -1;
            }

            currentPieces[gridIndex] = null;
            return;
        }

        // ---------------------------------
        // 1) 이 gridIndex 칸에 이미 있던 조각 제거
        // ---------------------------------
        CAPPiece oldPiece = currentPieces[gridIndex];
        if (oldPiece != null && oldPiece != piece)
        {
            Debug.Log(
                $"Grid {gridIndex} 기존 조각 교체: {oldPiece.pieceData.pieceID}"
            );
            oldPiece.pieceData.gridIndex = -1;   // 더 이상 그리드에 없음
        }

        // ---------------------------------
        // 2) 다른 칸에 있던 같은 piece 제거
        //    (겹쳐서 이동해 온 경우, 예전 위치를 null로)
        // ---------------------------------
        for (int i = 0; i < 9; i++)
        {
            if (currentPieces[i] == piece && i != gridIndex)
            {
                currentPieces[i] = null;
                Debug.Log(
                    $"Piece {piece.pieceData.pieceID}가 Grid {i}에서 이동됨 → null 처리"
                );
                break;
            }
        }

        // ---------------------------------
        // 3) 새 칸에 등록
        // ---------------------------------
        currentPieces[gridIndex] = piece;
        piece.pieceData.gridIndex = gridIndex;   // 조각 쪽 상태도 동기화

        Debug.Log($"RegisterPiece: Grid {gridIndex} ← Piece {piece.pieceData.pieceID}");
    }


    // 특정 그리드가 비어있는지 확인
    public bool IsGridEmpty(int gridIndex)
    {
        return currentPieces[gridIndex] == null;
    }

    public CAPPiece GetPieceAt(int gridIndex)
    {
        if (gridIndex >= 0 && gridIndex < 9)
        {
            return currentPieces[gridIndex];
        }
        return null;
    }

    public int GetFirstEmptyGridIndex()
    {
        for (int i = 0; i < 9; i++)
        {
            if (currentPieces[i] == null)
            {
                return i;
            }
        }
        return -1;
    }


    // 현재 배열 상태 가져오기 (Player1 기준 - 화살표)
    public int[] GetCurrentArrowArrangement()
    {
        int[] arrangement = new int[9];

        for (int i = 0; i < 9; i++)
        {
            if (currentPieces[i] != null)
            {
                arrangement[i] = currentPieces[i].pieceData.arrowID;
            }
            else
            {
                arrangement[i] = 0; // 비어있음
            }
        }

        return arrangement;
    }


    // 현재 배열 상태 가져오기 (Player2 기준 - 문자)
    public string[] GetCurrentLettersArrangement()
    {
        string[] arrangement = new string[9];

        for (int i = 0; i < 9; i++)
        {
            if (currentPieces[i] != null)
            {
                arrangement[i] = currentPieces[i].pieceData.letterID;
            }
            else
            {
                arrangement[i] = ""; // 비어있음
            }
        }

        return arrangement;
    }

    // Player1 기준(화살표 ID) 패턴 체크
    public bool CheckArrowPattern(int[] answerPattern, bool verbose = true)
    {
        if (answerPattern == null || answerPattern.Length != 9)
        {
            Debug.LogError("[CAPGrid] CheckArrowPattern: answerPattern이 null 이거나 길이가 9가 아닙니다.");
            return false;
        }

        int[] arrangement = GetCurrentArrowArrangement();

        if (verbose)
        {
            Debug.Log("=== 화살표 패턴 체크 ===");
            Debug.Log($"현재 배치: [{string.Join(", ", arrangement)}]");
            Debug.Log($"정답 패턴: [{string.Join(", ", answerPattern)}]");
        }

        for (int i = 0; i < 9; i++)
        {
            if (arrangement[i] != answerPattern[i])
            {
                if (verbose)
                    Debug.Log($"칸 {i} 불일치: 현재={arrangement[i]}, 정답={answerPattern[i]}");
                return false;
            }
        }

        if (verbose)
            Debug.Log("화살표 패턴 일치!");

        return true;
    }

    // Player2 기준(문자 ID) 패턴 체크
    public bool CheckLetterPattern(string[] answerPattern, bool verbose = true)
    {
        if (answerPattern == null || answerPattern.Length != 9)
        {
            Debug.LogError("[CAPGrid] CheckLetterPattern: answerPattern이 null 이거나 길이가 9가 아닙니다.");
            return false;
        }

        string[] arrangement = GetCurrentLettersArrangement();

        if (verbose)
        {
            Debug.Log("=== 문자 패턴 체크 ===");
            Debug.Log($"현재 배치: [{string.Join(", ", arrangement)}]");
            Debug.Log($"정답 패턴: [{string.Join(", ", answerPattern)}]");
        }

        for (int i = 0; i < 9; i++)
        {
            if (arrangement[i] != answerPattern[i])
            {
                if (verbose)
                    Debug.Log($"칸 {i} 불일치: 현재={arrangement[i]}, 정답={answerPattern[i]}");
                return false;
            }
        }

        if (verbose)
            Debug.Log("문자 패턴 일치!");

        return true;
    }


    // 디버그용: 현재 배열 상태 출력
    public void PrintCurrentState()
    {
        Debug.Log("=== Current Grid State ===");

        // 화살표 배열
        int[] arrows = GetCurrentArrowArrangement();
        Debug.Log($"Arrows: {arrows[0]}{arrows[1]}{arrows[2]} / {arrows[3]}{arrows[4]}{arrows[5]} / {arrows[6]}{arrows[7]}{arrows[8]}");

        // 색깔 배열
        string[] colors = GetCurrentLettersArrangement();
        Debug.Log($"Colors: {colors[0]}{colors[1]}{colors[2]} / {colors[3]}{colors[4]}{colors[5]} / {colors[6]}{colors[7]}{colors[8]}");
    }

    // ============================================================
    // 부분 정답 체크 (피드백 시스템용)
    // ============================================================

    /// <summary>
    /// 현재 배치에서 올바른 위치에 있는 조각 개수 (문자 기준)
    /// </summary>
    public int GetCorrectLetterCount(string[] answerPattern)
    {
        if (answerPattern == null || answerPattern.Length != 9)
        {
            Debug.LogError("[CAPGrid] GetCorrectLetterCount: answerPattern이 null이거나 길이가 9가 아닙니다.");
            return 0;
        }

        string[] current = GetCurrentLettersArrangement();
        int correctCount = 0;

        for (int i = 0; i < 9; i++)
        {
            if (!string.IsNullOrEmpty(current[i]) && current[i] == answerPattern[i])
            {
                correctCount++;
            }
        }

        return correctCount;
    }

    /// <summary>
    /// 현재 배치에서 올바른 위치에 있는 조각 개수 (화살표 기준)
    /// </summary>
    public int GetCorrectArrowCount(int[] answerPattern)
    {
        if (answerPattern == null || answerPattern.Length != 9)
        {
            Debug.LogError("[CAPGrid] GetCorrectArrowCount: answerPattern이 null이거나 길이가 9가 아닙니다.");
            return 0;
        }

        int[] current = GetCurrentArrowArrangement();
        int correctCount = 0;

        for (int i = 0; i < 9; i++)
        {
            if (current[i] > 0 && current[i] == answerPattern[i])
            {
                correctCount++;
            }
        }

        return correctCount;
    }

    /// <summary>
    /// 그리드에 배치된 조각 개수
    /// </summary>
    public int GetPlacedPieceCount()
    {
        int count = 0;
        for (int i = 0; i < 9; i++)
        {
            if (currentPieces[i] != null)
                count++;
        }
        return count;
    }

    // Gizmo 그리기 

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showGridGizmos)
        {
            return;
        }

        // Awake가 실행 안 됐을 수도 있으니 여기서도 계산
        if (gridPositions == null || gridPositions.Length == 0)
        {
            CalculateGridPositions3D();
        }

        // 그리드 위치 시각화
        for (int i = 0; i < 9; i++)
        {
            Vector3 pos = gridPositions[i];

            // 구체로 표시
            Gizmos.DrawWireSphere(pos, 0.2f);
            // 그리드 번호 표시
            UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, $"{i}");
        }

        // 그리드 테두리 선 그리기
        Gizmos.color = Color.green;
        // 시작점 계산
        Vector3 topLeft = gridStartPosition + new Vector3(-gridSpacing * 0.5f, 0, gridSpacing * 0.5f);

        // 가로선
        for (int row = 0; row < 4; row++)
        {
            Vector3 start = topLeft + new Vector3(0, 0, -row * gridSpacing);
            Vector3 end = start + new Vector3(gridSpacing * 3, 0, 0);
            Gizmos.DrawLine(start, end);
        }

        // 세로선
        for (int col = 0; col < 4; col++)
        {
            Vector3 start = topLeft + new Vector3(col * gridSpacing, 0, 0);
            Vector3 end = start + new Vector3(0, 0, -gridSpacing * 3);
            Gizmos.DrawLine(start, end);
        }
    }
#endif
}

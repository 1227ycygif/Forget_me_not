using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlidePuzzle : MonoBehaviour
{
    // === 퍼즐 조각 클래스 ===
    [System.Serializable]
    public class PuzzlePiece
    {
        public int id;                         // 조각 식별자 (인스펙터에서 지정 가능)
        public Vector2Int position;            // 현재 격자 좌표
        public Vector2Int correctPosition;     // 맞춰야 하는 정답 좌표

        [SerializeField] public GameObject pieceObject; // 실제 게임 오브젝트
    }

    [Header("퍼즐 설정")]
    [SerializeField] private int cols = 3;
    [SerializeField] private int rows = 3;
    [SerializeField] private float cellSize = 1f; // 로컬 좌표 스케일(타일 간 간격)

    [Header("퍼즐 조각 목록(왼->오, 위->아래 정답 순서로 배치)")]
    public List<PuzzlePiece> pieces;

    // 빈칸 조각
    private PuzzlePiece emptyPiece;

    void Start()
    {
        CorrectPositions();           // 인스펙터 순서를 정답 좌표로 기록
        ShufflePieces();              // 퍼즐 섞기
        ApplyAllTransforms();         // 트랜스폼 반영
    }

    // 인스펙터에 나열된 순서를 정답 좌표로 기록 (0,0)부터 (cols-1, rows-1)
    void CorrectPositions()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            int x = i % cols;
            int y = i / cols;
            pieces[i].correctPosition = new Vector2Int(x, y);
        }
    }

    // 퍼즐 섞기

    void ShufflePieces()
    {
        // 모든 정답 좌표를 뽑아 섞고 그 좌표를 현재 위치로 할당
        var allPositions = new List<Vector2Int>(pieces.Count);
        for (int i = 0; i < pieces.Count; i++)
            allPositions.Add(pieces[i].correctPosition);

        // 셔플
        for (int i = 0; i < allPositions.Count; i++)
        {
            int r = Random.Range(0, allPositions.Count);
            (allPositions[i], allPositions[r]) = (allPositions[r], allPositions[i]);
        }

        // 섞인 좌표를 현재 위치로 배정
        for (int i = 0; i < pieces.Count; i++)
            pieces[i].position = allPositions[i];

        // 마지막 조각을 빈칸으로: 오브젝트 비활성 + 참조 보관
        emptyPiece = pieces[pieces.Count - 1];
        emptyPiece.pieceObject.SetActive(false);

        // 나머지는 활성화
        for (int i = 0; i < pieces.Count - 1; i++)
            pieces[i].pieceObject.SetActive(true);
    }

    private bool IsSolvable(int[] shuffleIdx)
    {
        int inv = 0;
        for (int i = 0; i < 9; i++)
        {
            if (shuffleIdx[i] == 0) continue;
            for (int j = i + 1; j < 9; j++)
            {
                if (shuffleIdx[j] == 0) continue;
                if (shuffleIdx[i] > shuffleIdx[j]) inv++;
            }
        }
        return (inv % 2) == 0;
    }

    // 클릭 이벤트(버튼/콜라이더에서 이 메서드 호출)
    public void OnPieceClicked(PuzzlePiece piece)
    {
        if (piece == null || piece == emptyPiece)
            return;

        // 빈칸과 인접(맨해튼 거리 1)인지 확인
        if (!AreAdjacent(piece.position, emptyPiece.position))
        {
            Debug.Log("빈 공간과 인접하지 않음");
            return;
        }

        Debug.Log($"퍼즐 조각 클릭됨: {piece.id}");

        // 위치 스왑(오브젝트 활성/비활성은 변경하지 않음: 빈칸은 계속 비활성)
        Vector2Int temp = piece.position;
        piece.position = emptyPiece.position;
        emptyPiece.position = temp;

        // 트랜스폼 반영
        ApplyTransform(piece);
        ApplyTransform(emptyPiece);

        // 퍼즐 완성 체크(필요하면 사용)
        if (PuzzleComplete()) { Debug.Log("퍼즐 완성"); }
    }

    // 맨해튼 거리 1칸 인접 판정
    bool AreAdjacent(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return (dx + dy) == 1;
    }

    // 격자 좌표 → 로컬 좌표
    Vector3 GridToLocal(Vector2Int gridPos)
    {
        // (0,0)이 좌하단/좌상단인지는 레이아웃에 맞춰 조정
        return new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, 0f);
    }

    // 단일 조각 트랜스폼 반영
    void ApplyTransform(PuzzlePiece piece)
    {
        if (piece.pieceObject != null)
            piece.pieceObject.transform.localPosition = GridToLocal(piece.position);
    }

    // 전 조각 트랜스폼 반영
    void ApplyAllTransforms()
    {
        for (int i = 0; i < pieces.Count; i++)
            ApplyTransform(pieces[i]);
    }

    // 퍼즐 완성 여부(원하면 사용)
    bool PuzzleComplete()
    {
        for (int i = 0; i < pieces.Count; i++)
            if (pieces[i].position != pieces[i].correctPosition)
                return false;
        return true;
    }


    void PuzzleReset() { }

    void PuzzleClear() { }

    void SolveAction() { }
}

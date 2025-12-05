
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 퍼즐 조각의 데이터를 저장하는 클래스
/// Player1은 arrowID만, Player2는 colorID만 볼 수 있음
/// </summary>
[System.Serializable]
public class CAPPieceData
{
    [Header("Piece Identity")]
    public int pieceID;          // 고유 ID (0~8)
    public int arrowID;          // 1~9 (Player1이 보는 화살표 번호)
    public string letterID;       // 정해진 문자 (Player2가 보는 문자)

    [Header("Position")]
    public int gridIndex;        // 현재 그리드 위치 (0~8)

    // 생성자
    public CAPPieceData(int id, int arrow, string letter, int index)
    {
        pieceID = id;
        arrowID = arrow;
        letterID = letter;
        gridIndex = index;
    }

    // 기본 생성자
    public CAPPieceData()
    {
        pieceID = 0;
        arrowID = 1;
        letterID = "a";
        gridIndex = 0;
    }

    // 디버그용 출력
    public override string ToString()
    {
        return $"Piece#{pieceID}: Arrow={arrowID}, Letter={letterID}, Grid={gridIndex}";
    }
}
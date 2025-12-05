using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PieceClick : MonoBehaviour 
{ 
    PuzzleSlide puzzle;
    public int pieceId; // = SlidePuzzle.PuzzlePiece.id와 동일하게 설정

    private void Start()
    {
        this.AddComponent<BoxCollider2D>();
        puzzle = GetComponentInParent<PuzzleSlide>();
    }

    private void OnMouseDown()
    {
        if (puzzle == null) return;

        puzzle.OnPieceClicked(pieceId);
    }
}
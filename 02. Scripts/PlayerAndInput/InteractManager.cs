using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    [SerializeField] IInteractable puzzleNowSolving;     // 풀기 시작한 퍼즐 정보 임시 저장

    public void TryInteractByHover(GameObject interactor, IInteractable hovered, RaycastHit hit)
    {
        if (hovered == null) return;
        if (!hovered.CanInteract(interactor)) return;
        if (GameManager.Instance.CurrentState == GameState.SolvingPuzzle)
        {
            Debug.Log("퍼즐은 찾음, 퍼즐이 풀렸는지 확인, 플레이어가 퍼즐 푸는중임을 확인");
            return;
        }

        hovered.Interact(interactor);
        // 탈출을 위해 잠시 저장
        puzzleNowSolving = hovered;

        Debug.Log($"상호작용 판별 완료 : {puzzleNowSolving}");
    }

    public void TryGetOutInteract()
    {
        if (puzzleNowSolving == null) return;
        if(GameManager.Instance.CurrentState != GameState.SolvingPuzzle)
        {
            Debug.LogWarning("오류 발생!! ESC 눌렸고 퍼즐은 풀러 들어갔는데, 플레이어가 퍼즐 푸는 상태가 아님");
            return;
        }

        puzzleNowSolving.ExitPuzzle();

        puzzleNowSolving = null;
    }
}
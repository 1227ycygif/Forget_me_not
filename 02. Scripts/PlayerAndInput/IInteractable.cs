using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    //void Activate();
    bool CanInteract(GameObject interactor);     // 상호작용 가능 여부 반환
    void Interact(GameObject interactor);        // 상호작용시 할 동작 구현

    void ExitPuzzle();                           // 탈출 버튼 눌러서 상호작용 종료

}

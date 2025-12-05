using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorOpen : MonoBehaviour, IInteractable
{
    public bool CanInteract(GameObject interactor)
    {
        //if (isSolved)
        //{
        //    Debug.Log($"이미 푼 퍼즐입니다 : 호출자 {player.name}");
        //    return false;
        //}
        //if (nowSolving)
        //{
        //    Debug.Log($"이미 상대방이 퍼즐을 풀고 있습니다.");
        //    return false;
        //}

        return true;
    }

    public void ExitPuzzle()
    {
        throw new System.NotImplementedException();
        // Nothing
    }

    public void Interact(GameObject interactor)
    {
        if (GameManager.Instance.CurrentState != GameState.Normal)
        {
            return;
        }
        //if (nowSolving || isSolved) return;
        //nowSolving = true;
        //pv.RPC("NowSolving", PhotonTargets.All, null);      // 둘 중 하나만 하면 되는거 아닌가? 타겟 All 이잖아

        //Debug.Log($"nowSolving ? {nowSolving}");

        //GameManager.Instance.SetState(GameState.SolvingPuzzle);

        GameManager.Instance.SetState(GameState.Cutscene);
        interactor.GetComponent<PlayerCtrl>().MoveNextScene();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

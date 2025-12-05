using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1. 버튼은 전부 이 스크립트를 공유
/// 2. 이 버튼은 마우스 클릭으로 상호작용 할 예정
/// 3. 내부 클리어판정 변수인 isCleared로 상호작용 차단
/// 4. 기능 : 할당된 벽을 올리고(LiftUp) 내린다(FallDown).
/// 5. 
/// </summary>


public class ButtonClick : MonoBehaviour, IInteractable
{
    [SerializeField] GameObject[] walls;        // 올리고 내릴 벽s 지정
    [SerializeField] bool isEndBtutton = false; // 마지막 버튼에 달 때 true로 적용

    //// 벽의 스크립트에 접근해서 함수를 호출하는 형태로
    //// 이동 위치를 설정하는건 벽 자체 스크립트로 수행 (카메라랑 구조 동일)
    //[SerializeField] Transform originPos;       // 벽s 원래 위치
    //[SerializeField] Transform targetPos;       // 벽s 올라갈 위치
    //// 할당된 모든 벽 들어올리기, Interact에서 호출됨
    //void LiftUpWall()
    //{

    //}

    //// 할당된 모든 벽 내리기, 자신 제외 다른 외부 버튼에서 호출됨
    //public void FallDownWall()
    //{

    //}

    bool isCleared = false; // true면 버튼 상호작용 차단

    PhotonView pv;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    public bool CanInteract(GameObject interactor)
    {
        if (isCleared)
        {
            Debug.Log("미로 다 풀었어");
            return false;
        }

        return true;
        //throw new System.NotImplementedException();
    }

   

    public void Interact(GameObject interactor)
    {
        // 상호작용 판정에서 막았으니, 여기서도 방어코드를 적용할 필요없다


        //throw new System.NotImplementedException();
    }

    public void ExitPuzzle()
    {
        // 필요가 없음!! 단발성 동작만 수행하니까

        //// 퍼즐 초기화
        //pv.RPC("PuzzleReset", PhotonTargets.All, null);
        //// 플레이어 상태 갱신
        //GameManager.Instance.SetState(GameState.Normal);
        //// 자기 자신의 카메라 되돌리기  
        //cam.CamPosBack();

        //throw new System.NotImplementedException();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    

    // 버튼 상호작용을 차단하기, 모든 버튼에 한번에 하달
    public void Deactivate()
    {

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    [SerializeField] Camera cam;
    LayerMask interactMask;   // Player를 제외한 모든 레이어 조회
    float maxDistance = 3f;
    
    public bool InteractPressed { get; private set; }   // E키 눌림
    public bool EquipItemUse { get; private set; }      // F키 눌림
    public bool MovePressed { get; private set; }       // H & V
    public bool RunPressed { get; private set; }        // Shift
    public bool JumpPressed { get; private set; }       // Space
    public bool EscPressed { get; private set; }        // ESC
    public float H { get; private set; }                // Horizontal Move
    public float V { get; private set; }                // Vertical Move
    public bool IsInventory { get; private set; }       // I
    public bool AnyKey { get; private set; }            // 어떤 키라도 눌렸는지 확인


    public bool CanInteractNow { get; private set; }    // 상호작용 가능!
    public IInteractable Hovered { get; private set; }  // 
    public RaycastHit HitInfo { get; private set; }     //

    private void Start()
    {
        // 캠 연결
        if (!cam)
            cam = Camera.main;
        // 플레이어를 제외하는 Ray LayerMask 제작
        interactMask = ~(1 << LayerMask.NameToLayer("Player"));
    }

    void Update()
    { 
        // Mouse Control /////////////////////////////////////////////

        //// UI에 올려지면 비활성화
        //if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        //{
        //    Hovered = null; CanInteractNow = false; return;
        //}

        // 마우스 → 월드 레이
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hit, maxDistance, interactMask, QueryTriggerInteraction.Collide))
        {
            //Debug.Log("상호작용 가능");
            HitInfo = hit;
            Hovered = hit.collider.GetComponentInParent<IInteractable>();
            CanInteractNow = Hovered != null;
        }
        else
        {
            Hovered = null;
            CanInteractNow = false;
        }

        // Key Binding ///////////////////////////////////////////////
        InteractPressed = Input.GetKey(KeyCode.E);
        EquipItemUse = Input.GetKey(KeyCode.F);
        JumpPressed = Input.GetKey(KeyCode.Space);
        RunPressed = Input.GetKey(KeyCode.LeftShift);
        EscPressed = Input.GetKeyDown(KeyCode.Escape);
        IsInventory = Input.GetKeyDown(KeyCode.I);

        H = Input.GetAxis("Horizontal");
        V = Input.GetAxis("Vertical");
        AnyKey = Input.anyKey; 

        MovePressed = (H != 0f) || (V != 0f);


        // 지금 퍼즐 중이면 입력 안받음
        // 임시, 수정 필요
        if (GameManager.Instance.CurrentState == GameState.SolvingPuzzle)
        {
            InteractPressed = false;
            EquipItemUse = false;
            MovePressed = false;
            JumpPressed = false;
            RunPressed = false;
            IsInventory = false;
            AnyKey = false;

            H = 0f;
            V = 0f;

            return;
        }
    }
}

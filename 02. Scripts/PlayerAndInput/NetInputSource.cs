using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetInputSource : IInputSource
{
    public bool InteractPressed { get; private set; }   // E키 눌림
    public bool EquipItemUse { get; private set; }      // F키 눌림
    public bool MovePressed { get; private set; }       // H & V
    public bool RunPressed { get; private set; }        // Shift
    public bool JumpPressed { get; private set; }       // Space
    public bool EscPressed { get; private set; }        // ESC
    public float H { get; private set; }                // Horizontal Move
    public float V { get; private set; }                // Vertical Move
    public bool AnyKey { get; private set; }            // AnyKEy
    public bool IsInventory { get; private set; }       // I
    public bool CanInteractNow { get; private set; }    // 상호작용 가능!
    public IInteractable Hovered => null;
    public RaycastHit HitInfo => default;

    

    // 수신 적용
    public void Apply(float h, float v, bool anyKey ,bool run, bool move, bool jumpPulse, bool esc, bool interactPulse, bool canInteract , bool isInventory, bool equipItemUse)
    {
        H = h; V = v;
        AnyKey = anyKey;
        RunPressed = run; MovePressed = move; CanInteractNow = canInteract;
        JumpPressed = jumpPulse;
        EscPressed = esc;
        InteractPressed = interactPulse;
        EquipItemUse = equipItemUse;
        IsInventory = isInventory;
    }
}

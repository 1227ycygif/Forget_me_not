using UnityEngine;

public interface IInputSource
{
    bool InteractPressed { get; }   // F키 눌림
    bool EquipItemUse { get; }      // E키 눌림
    bool MovePressed { get;  }       // H & V
    bool RunPressed { get; }        // Shift
    bool JumpPressed { get; }       // Space
    bool EscPressed { get; }        // ESC
    float H { get; }                // Horizontal Move
    float V { get; }                // Vertical Move
    bool AnyKey { get; }            // anyKey only Animator Blender


    bool CanInteractNow { get; }    // 상호작용 가능!
    IInteractable Hovered { get; }  // 
    RaycastHit HitInfo { get; }     //
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalInputSource : IInputSource
{
    private readonly InputManager _input;
    public LocalInputSource(InputManager input) { _input = input; }
    public bool InteractPressed => _input.InteractPressed;  // E키 눌림
    public bool EquipItemUse => _input.EquipItemUse;        // F키 눌림
    public bool MovePressed => _input.MovePressed;       // H & V
    public bool RunPressed => _input.RunPressed;        // Shift
    public bool EscPressed => _input.EscPressed;        // Shift
    public bool JumpPressed => _input.JumpPressed;      // Space
    public float H => _input.H;                         // Horizontal Move
    public float V => _input.V;                         // Vertical Move
    public bool AnyKey => _input.AnyKey;                // anyKey only Animator Blender
    public bool IsInventory => _input.IsInventory;       // I

    public bool CanInteractNow => _input.CanInteractNow;    // 상호작용 가능!
    public IInteractable Hovered => _input.Hovered;  // 
    public RaycastHit HitInfo => _input.HitInfo;     //

    
}

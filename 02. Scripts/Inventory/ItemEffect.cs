using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public struct ItemEffectContext
{
    public GameObject user;
    public InventoryStore store;
    public MonoBehaviour runner;
    public GameObject itemPrefabs;
    public string itemKey;
    public ItemUseSource source; // 어디서 사용했는지 출처
}

public enum ItemUseSource
{
    None,
    UI,        // 인벤토리에서 클릭
    Equipped,  // 손에 들고 F 누름
    Aiming,    // 조준 상태에서 사용
}

















// // 여기에 효과 이름 적으면 됨
// public enum ItemEffect { Hina, Floor, Test, Flash }

// public class ItemEffectHandler
// {
//     public static readonly ItemEffectHandler Instance = new ItemEffectHandler();

//     public void Apply(ItemEffect fx, ItemEffectContext ctx)
//     {
//         switch (fx)
//         {
//             case ItemEffect.Hina:
//                 Debug.Log("Hina 효과!"); break;
//             case ItemEffect.Floor:
//                 Debug.Log("타일 아이템 소환!"); break;
//             case ItemEffect.Flash:
//                 FlashEffect(fx, ctx);
//                 break;

//             default:
//                 Debug.LogWarning($"미지원 효과: {fx}");
//                 break;

//         }
//     }

//     private void FlashEffect(ItemEffect fx, ItemEffectContext ctx)
//     {
//         Debug.Log("Flash 아이템 장착!");
//         var equip = ctx.user.GetComponent<PlayerItemEquick>();
//         if (equip == null) { Debug.LogWarning("PlayerItemEquick 없음"); return; }
//         if (ctx.itemPrefabs == null) { Debug.LogWarning("itemPrefabs null"); return; }
//         //equip.Equip(ctx.itemPrefabs);
//     }
// }

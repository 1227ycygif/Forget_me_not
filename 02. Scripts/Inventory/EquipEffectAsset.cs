using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ItemEffects/Equip")]
public class EquipEffectAsset : ItemEffectAsset
{
    public GameObject equipPrefab;

    public override bool Execute(ItemEffectContext ctx)
    {
        // UI에서 쓸 때만 장착. F에서 들어오면 무시.
        if (ctx.source != ItemUseSource.UI)
            return false;

        var equip = ctx.user ? ctx.user.GetComponent<PlayerItemEquick>() : null;
        if (!equip)
        {
            Debug.LogWarning("[EquipEffect] PlayerItemEquick 없음");
            return false;
        }

        var prefab = equipPrefab ? equipPrefab : ctx.itemPrefabs;
        if (!prefab)
        {
            Debug.LogWarning("[EquipEffect] prefab 없음");
            return false;
        }

        Debug.Log("[EquipEffect] 장착 시도");
        equip.Equip(prefab, ctx.itemKey); // 이 함수 안에서 포톤 네트워크 처리됨
        return false; // 장착만, 소모 X
    }
}

[CreateAssetMenu(menuName = "ItemEffects/Charge")]
public class ChargeEffectAsset : ItemEffectAsset
{
    [Min(1)] public int amount = 10;

    public override bool Execute(ItemEffectContext ctx)
    {
        return true;
    }
}

// [CreateAssetMenu(menuName = "ItemEffects/Spawn")]
// public class SpawnEffectAsset : ItemEffectAsset
// {
//     public GameObject prefab;
//     public Vector3 localOffset = new Vector3(0, 0, 1);

//     public override bool Execute(ItemEffectContext ctx)
//     {
//         if (!prefab || !ctx.user) return false;
//         var p = ctx.user.transform.TransformPoint(localOffset);
//         Object.Instantiate(prefab, p, Quaternion.identity);
//         return true;
//     }
// }

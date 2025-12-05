using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ItemEffects/Flash")]
public class FlashEffectAsset : ItemEffectAsset
{
    public override bool Execute(ItemEffectContext ctx)
    {
        var equip = ctx.user ? ctx.user.GetComponent<PlayerItemEquick>() : null;
        var pv = ctx.user ? ctx.user.GetComponent<PhotonView>() : null;

        if (!equip || !pv)
        {
            Debug.LogWarning("[Flash] PlayerItemEquick 또는 PhotonView 없음");
            return false;
        }

        if (!pv.isMine)
            return false;

        if (!equip.equippedInstance)
        {
            Debug.LogWarning("[Flash] 장착된 아이템 없음");
            return false;
        }

        var light = equip.equippedInstance.GetComponentInChildren<Light>();
        if (!light)
        {
            Debug.LogWarning("[Flash] Light 없음");
            return false;
        }

        // 1) 로컬 토글
        light.enabled = !light.enabled;

        // 2) 다른 클라에 동기화
        pv.RPC(nameof(PlayerItemEquick.RpcSetFlashLight), PhotonTargets.Others, light.enabled);

        Debug.Log($"[Flash] Light {(light.enabled ? "ON" : "OFF")} (Photon Sync)");
        return false; // 소모 X
    }
}
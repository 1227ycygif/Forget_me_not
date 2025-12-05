using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ItemEffects/StunTalisman")]
public class StunTalismanEffectAsset : ItemEffectAsset
{
    public GameObject talismanPrefab;
    public float throwForce = 12f;   // PlayerCtrl에서 같이 씀
    public float arcAngle = 0f;      // 필요 없으면 0
    public string pendingFieldName = "pendingTalisman"; // PlayerCtrl 필드명

    // public override bool Execute(ItemEffectContext ctx)
    // {
    //     if (!ctx.user || !talismanPrefab) return false;

    //     PhotonView pv = ctx.user.GetComponent<PhotonView>();
    //     if (!pv || !pv.isMine) return false;  // isMine 동일

    //     PlayerCtrl playerCtrl = ctx.user.GetComponent<PlayerCtrl>();
    //     if (!playerCtrl) return false;

    //     if (playerCtrl.pendingTalisman != null) return true;

    //     Transform muzzle = playerCtrl.throwMuzzle ? playerCtrl.throwMuzzle : ctx.user.transform;

    //     string prefabPath = "ItemPrefabs/Talisman";  // Resources/Talisman/Talisman.prefab
    //     // PUN Classic: PhotonNetwork.Instantiate 동일
    //     GameObject go = PhotonNetwork.Instantiate(prefabPath, muzzle.position, muzzle.rotation, 0);
    //     StunTalisman talisman = go.GetComponent<StunTalisman>();
    //     if (!talisman)
    //     {
    //         PhotonNetwork.Destroy(go);
    //         return false;
    //     }

    //     talisman.AttachTo(muzzle);
    //     playerCtrl.pendingTalisman = talisman;
    //     return true;
    // }
    public override bool Execute(ItemEffectContext ctx)
    {
        var pv = ctx.user.GetComponent<PhotonView>();
        if (!pv || !pv.isMine) return false;

        var playerCtrl = ctx.user.GetComponent<PlayerCtrl>();
        if (!playerCtrl || playerCtrl.pendingTalisman) return true;

        Transform muzzle = playerCtrl.throwMuzzle ? playerCtrl.throwMuzzle : ctx.user.transform;
        Debug.Log(muzzle.position + "========  " + muzzle.name);
        var go = PhotonNetwork.Instantiate("ItemPrefabs/Talisman", muzzle.position, muzzle.rotation, 0);
        var talisman = go.GetComponent<StunTalisman>();
        if (!talisman) return false;

        talisman.AttachTo(muzzle);  // 이 한 줄로 모든 클라에서 손에 붙음!
        playerCtrl.pendingTalisman = talisman;

        return true;
    }
}
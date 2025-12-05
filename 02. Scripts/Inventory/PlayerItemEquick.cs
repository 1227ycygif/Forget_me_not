using System;
using UnityEngine;

public class PlayerItemEquick : MonoBehaviour
{
    public PhotonView pv;
    public Transform rightHand;

    public GameObject equippedInstance;   // 씬 인스턴스
    public GameObject equippedPrefab;     // 장착 프리팹(에셋 레퍼런스)

    public string equippedKey;      // 현재 장착 아이템 key
    public bool equippedConsumable; // 소모 여부



    void Awake()
    {
        if (!pv)
            pv = GetComponent<PhotonView>() ?? GetComponentInParent<PhotonView>();
    }
    /// <summary>
    /// 같은 프리팹이면 토글(해제), 다르면 교체
    /// </summary>
    public GameObject Equip(GameObject prefab, string key = null)
    {
        if (!rightHand || !prefab) return null;

        // key 없으면 prefab.name 사용
        if (string.IsNullOrEmpty(key))
            key = prefab.name;

        if (equippedInstance == null)
        {
            var go = EquipNew(prefab, key);
            RequestEquip(key);
            return go;
        }
        else if (equippedPrefab == prefab && equippedKey == key)
        {
            // 같은 아이템 다시 누르면 해제(토글)
            Unequip();
            RequestUnequip();
            return null;
        }
        else
        {
            Unequip();
            var go = EquipNew(prefab, key);
            RequestEquip(key);
            return go;
        }
    }

    GameObject EquipNew(GameObject prefab, string key)
    {
        var go = Instantiate(prefab, rightHand);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        equippedInstance = go;
        equippedPrefab = prefab;
        equippedKey = key;
        // equippedConsumable = consumable;

        return go;
    }

    public void Unequip()
    {
        if (equippedInstance) Destroy(equippedInstance);
        equippedInstance = null;
        equippedPrefab = null;
    }

    // === 네트워크 브로드캐스트(소유자만) ===
    void RequestEquip(string key)
    {
        Debug.Log(key);

        if (!pv || !pv.isMine) return;
        pv.RPC(nameof(RpcEquipByKey), PhotonTargets.OthersBuffered, key); // OthersBuffered
    }

    public void RequestUnequip()
    {
        if (!pv || !pv.isMine) return;
        pv.RPC(nameof(RpcUnequip), PhotonTargets.OthersBuffered);
    }

    // === 수신측: 로컬 적용만 ===
    [PunRPC]
    void RpcEquipByKey(string key, PhotonMessageInfo info)
    {
        var prefab = Resources.Load<GameObject>($"ItemPrefabs/{key}"); // 규칙: Resources/<key>.prefab
        if (!rightHand || !prefab)
        {
            Debug.LogWarning($"[RpcEquipByKey] 키 못 찾음 : {key}");
            return;
        }

        if (equippedPrefab == prefab) // 에셋 비교
        {
            // 이미 같은 거면 무시(혹은 토글을 원하면 아래처럼 해제)
            // Unequip();
            return;
        }

        Unequip();
        EquipNew(prefab, key);
    }

    [PunRPC]
    public void RpcUnequip(PhotonMessageInfo info)
    {
        Unequip();
    }

    // === 아이템 포톤 네트워크 처리 함수 ===
     [PunRPC]
    public void RpcSetFlashLight(bool enabled)
    {
        if (!equippedInstance) return;


        var light = equippedInstance.GetComponentInChildren<Light>();
        if (!light) return;

        light.enabled = enabled;
    }
}

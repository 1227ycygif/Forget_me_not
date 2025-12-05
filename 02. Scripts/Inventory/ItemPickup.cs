using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [SerializeField] string key;
    [SerializeField] InventoryStore store;
    PhotonView pv;
    bool picked; // 중복 방지

    void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    void OnMouseDown()
    {
        if (picked) return;
        store = GameObject.FindWithTag("Player").GetComponent<InventoryStore>();
        if (!store) { Debug.LogWarning("[Pickup] store null"); return; }

        // 동시 클릭 방지
        picked = true;
        GetComponent<Collider>().enabled = false;

        int before = store.GetCount(key);
        store.Add(key, 1);

        if (store.GetCount(key) > before)
        {
            // 권한자만 파괴 트리거 (소유자 또는 마스터가 담당하도록)
            if (pv && (pv.isMine || PhotonNetwork.isMasterClient))
            {
                PhotonNetwork.Destroy(gameObject); // 모든 클라에서 사라짐
            }
            else
            {
                // 소유권 없으면 요청하거나, 마스터만 파괴하는 정책으로
                PhotonView.Get(this).TransferOwnership(PhotonNetwork.player);
                PhotonNetwork.Destroy(gameObject);
            }
        }
        else
        {
            // 실패했으면 다시 활성화
            picked = false;
            GetComponent<Collider>().enabled = true;
        }
    }
}

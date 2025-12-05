using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquippedItemUser : MonoBehaviour
{
    [SerializeField] private InventoryStore store;
    [SerializeField] private ItemUseRegistry registry;
    [SerializeField] private PlayerItemEquick equick;

    void Awake()
    {
        if (!store) store = GetComponent<InventoryStore>();
        if (!equick) equick = GetComponent<PlayerItemEquick>();
        if (!registry) registry = ItemUseRegistry.Instance ?? FindObjectOfType<ItemUseRegistry>(true);
        
    }

    /// <summary>
    /// 현재 손에 든(Equip된) 아이템을 1회 사용 + 필요시 소모 처리
    /// PlayerCtrl에서는 이 함수만 부르면 됨.
    /// </summary>
    public bool TryUseEquipped()
    {
        if (!equick) return false;

        var key = equick.equippedKey;
        if (string.IsNullOrEmpty(key)) return false;
        if (!store || !registry) return false;

        // 남은 수량 체크 (소모형만 의미 있지만, 통합 처리)
        if (store.GetCount(key) <= 0)
        {
            equick.Unequip();
            return false;
        }

        var ctx = new ItemEffectContext
        {
            user = gameObject,
            store = store,
            runner = this,
            itemKey = key,
            itemPrefabs = equick.equippedInstance,
            source = ItemUseSource.Equipped
        };

        if (!registry.TryUse(key, ctx, out bool consumable))
            return false;

        if (consumable)
        {
            store.Remove(key, 1);
            if (store.GetCount(key) <= 0)
                equick.Unequip();
        }

        return true;
    }

        void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("[EquippedItemUser] F 눌림");
            TryUseEquippedItem();
        }
    }

    void TryUseEquippedItem()
    {
        Debug.Log("[EquippedItemUser] 손에 든 아이템 사용 시도");
        if (!equick) return;
        if (string.IsNullOrEmpty(equick.equippedKey)) return;
        if (!registry || !store) return;

        if (store.GetCount(equick.equippedKey) <= 0)
        {
            equick.Unequip();
            return;
        }

        var ctx = new ItemEffectContext
        {
            user        = this.gameObject,
            store       = store,
            runner      = this,
            itemPrefabs = equick.equippedInstance,
            itemKey     = equick.equippedKey,
        };

        if (!registry.TryUse(equick.equippedKey, ctx, out bool consumable))
        {

            Debug.LogWarning("[EquippedItemUser] 핸들러 없음: " + equick.equippedKey);
            return;
        }

        if (consumable)
        {
            store.Remove(equick.equippedKey, 1);
            if (store.GetCount(equick.equippedKey) <= 0)
                equick.Unequip();
        }
    }
}

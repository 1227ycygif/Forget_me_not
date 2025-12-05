using System;
using System.Collections.Generic;
using UnityEngine;

public interface IItemUse
{
    string Key { get; }                 // 이 스크립트가 처리할 아이템 키
    bool Consumable { get; }            // 사용 시 소모 여부
    void Use(ItemEffectContext ctx);    // 실제 동작
}

public class ItemUseRegistry : MonoBehaviour
{
    // 싱글턴
    public static ItemUseRegistry Instance { get; private set; }

    [Header("여기에 item_use 스크립트 프리팹 넣기")]
    [SerializeField] MonoBehaviour[] handlers; // IItemUse 구현들 Drag&Drop
    Dictionary<string, IItemUse> map;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬 넘어가도 유지 (원하면 제거)

        BuildMap();
    }

    void BuildMap()
    {
        map = new Dictionary<string, IItemUse>(StringComparer.Ordinal);
        foreach (var mb in handlers)
        {
            if (mb is IItemUse h && !string.IsNullOrEmpty(h.Key))
                map[h.Key] = h;
        }
    }

    public bool TryUse(string key, ItemEffectContext ctx, out bool consumable)
    {
        consumable = false;
        if (string.IsNullOrEmpty(key) || !map.TryGetValue(key, out var h)) return false;
        h.Use(ctx);
        consumable = h.Consumable;
        return true;
    }
}

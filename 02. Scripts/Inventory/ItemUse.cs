using UnityEngine;

public class ItemUse : MonoBehaviour, IItemUse
{
    [Header("아이템 키 직접 입력")]
    [SerializeField] string key = "";

    [Header("소모 여부(모든 이펙트 성공 시 차감)")]
    [SerializeField] bool consumable = true;

    [Header("이 아이템이 실행할 이펙트 목록(순차 실행)")]
    [SerializeField] ItemEffectAsset[] effects;

    public string Key => key;
    public bool Consumable => consumable;

    public void Use(ItemEffectContext ctx)
    {
        if (string.IsNullOrEmpty(key))                  { Debug.LogWarning("[ItemUse] key 미입력"); return; }
        if (effects == null || effects.Length == 0)     { Debug.LogWarning($"[ItemUse:{key}] 효과 미지정"); return; }
        if (!ctx.user)                                  { Debug.LogWarning($"[ItemUse:{key}] ctx.user null"); return; }

        bool anySuccess = false;
        foreach (var fx in effects)
        {
            if (!fx) continue;
            // 하나라도 성공하면 true
            anySuccess |= fx.Execute(ctx);
        }

        // 소모형이면 UI/Store 쪽에서 차감(Registry → UIController에서 처리)
        // 여기선 "성공 여부"만 반환할 수도 있음(필요하면 시그니처 변경)
    }
}

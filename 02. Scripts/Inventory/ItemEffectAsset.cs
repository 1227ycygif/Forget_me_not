using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ItemEffectAsset : ScriptableObject
{
    // true를 리턴하면 "사용 성공"으로 간주(소모형이면 차감)
    public abstract bool Execute(ItemEffectContext ctx);
}

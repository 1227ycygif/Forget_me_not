using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInventoryItemUse
{
    void Use(ItemEffectContext ctx);
}

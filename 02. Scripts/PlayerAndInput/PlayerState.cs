using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    public enum DamageStack { NoHit, FristHit, SecoundHit, ThirdHit, FourthHit };
    public DamageStack damageStack;

    public float invincibleTime = 3f;
    public bool isHiting;      // 무적 여부
    public float hitCooldown;  // 지난 시간

    public int hp = 5;

    void Update()
    {
        // 무적 시간 처리
        if (isHiting)
        {
            hitCooldown += Time.deltaTime;
            if (hitCooldown >= invincibleTime)
            {
                isHiting = false;
                hitCooldown = 0f;
            }
        }

        // 테스트용 피격 입력
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log("공격");
            Attacked();
        }
    }

    public void Attacked()
    {
        if (isHiting || hp <= 0)
            return;

        SoundManager.manager.SFXPlay(2, this.gameObject.transform.position);
        
        hp--;
        UpdateDamageStack();

        isHiting = true;
        hitCooldown = 0f;

        Debug.Log($"HP: {hp}, Stack: {damageStack}");
    }

    void UpdateDamageStack()
    {
        switch (hp)
        {
            case 5: damageStack = DamageStack.NoHit;      break;
            case 4: damageStack = DamageStack.FristHit;   break;
            case 3: damageStack = DamageStack.SecoundHit; break;
            case 2: damageStack = DamageStack.ThirdHit;   break;
            case 1: damageStack = DamageStack.FourthHit;  break;
            default:
                // TODO: 사망 처리
                break;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CAPPlayerRoleBridge : MonoBehaviour
{
    public static bool IsPlayer1View;  // 남주(Non-Master)
    public static bool IsPlayer2View;  // 여주(Master)

    private void Awake()
    {
        if (!PhotonNetwork.connected)
        {
            // 오프라인 테스트 시 기본값
            IsPlayer1View = true;
            IsPlayer2View = false;
            Debug.Log("오프라인: Player1 시점으로 테스트");
            return;
        }

        if (PhotonNetwork.isMasterClient)
        {
            // 여주 = Master = Player2
            IsPlayer2View = true;
            IsPlayer1View = false;
            Debug.Log("MasterClient → Player2 시점 (문자)");
        }
        else
        {
            // 남주 = Non-master = Player1
            IsPlayer1View = true;
            IsPlayer2View = false;
            Debug.Log("Non-Master → Player1 시점 (화살표)");
        }
    }
}

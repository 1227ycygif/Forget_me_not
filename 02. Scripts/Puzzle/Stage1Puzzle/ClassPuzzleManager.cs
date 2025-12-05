using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(BoxCollider))]
public class ClassPuzzleManager : MonoBehaviour
{
    [Header("총 퍼즐 개수")]
    public int totalPuzzleCount;
    public int solvedCount = 0;

    Collider clear;
    PhotonView pv;

    public static ClassPuzzleManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        clear = GetComponent<Collider>();
        pv = GetComponent<PhotonView>();

    }
    /// <summary>
    /// 맵에 있는 퍼즐들을 리스트에 넣음
    /// 그리고 << solvedCheck 배열이랑 길이 비교 >>
    /// 길이가 같아지면 스테이지 클리어 
    /// .....
    /// 하려고 했는데 그냥 이렇게만 해도 될듯
    /// </summary>

    /// <summary>
    /// 퍼즐 하나가 풀릴 때마다 이 함수 호출
    /// </summary>
    
    public void AddSolved()
    {
        Debug.Log("[ClassPuzzleManager] AddSolved");
        pv.RPC(nameof(RPCAddSolved), PhotonTargets.All, null);
    }

    [PunRPC]
    public void RPCAddSolved()
    {
        Debug.Log("[ClassPuzzleManager] RPCAddSolved");
        solvedCount++;
        Debug.Log(solvedCount);

        if (solvedCount >= totalPuzzleCount) { StageClear(); }
    }

    void StageClear()
    {
        this.gameObject.SetActive(false);
        Debug.Log("class 스테이지 클리어");
    }

}

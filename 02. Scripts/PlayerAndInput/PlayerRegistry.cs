using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRegistry : MonoBehaviour
{
    public static PlayerRegistry Instance { get; private set; }

    // 씬에 존재하는 모든 플레이어
    private readonly List<PlayerCtrl> players = new List<PlayerCtrl>();

    public IReadOnlyList<PlayerCtrl> Players => players;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 필요하면 DontDestroyOnLoad(this.gameObject);
    }

    public void Register(PlayerCtrl player)
    {
        if (!player) return;
        if (!players.Contains(player))
            players.Add(player);
    }

    public void Unregister(PlayerCtrl player)
    {
        if (!player) return;
        players.Remove(player);
    }
    
    /// <summary>
    /// 기준 위치에서 가장 가까운 플레이어 Transform 리턴 (없으면 null)
    /// </summary>
    public Transform GetClosestTarget(Vector3 fromPos)
    {
        PlayerCtrl best = null;
        float bestSqr = float.MaxValue;

        foreach (var p in players)
        {
            if (!p) continue;
            var tr = p.transform;
            float sqr = (tr.position - fromPos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = p;
            }
        }

        return best ? best.transform : null;
    }
}

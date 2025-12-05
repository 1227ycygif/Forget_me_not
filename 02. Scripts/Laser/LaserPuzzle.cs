using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]

public class LaserPuzzle : MonoBehaviour
{
    [Header("Refs")]
    public Grid2D grid;
    public LaserAStar laser;
    public LaserPoint laserPoint;
    public MirrorClearCheck mirrorPuzzle;

    PhotonView pv;

    [Header("Start / Goal")]
    public Node startNode;          // 인스펙터에서 직접 드래그
    public LaserDir startDir = LaserDir.Right;
    public Node goalNode;           // 인스펙터에서 직접 드래그

    void Awake()
    {
        if (!pv) pv = GetComponent<PhotonView>();
        if (!grid) grid = FindObjectOfType<Grid2D>();
        if (!laser) laser = FindObjectOfType<LaserAStar>();
        if (!laserPoint) laserPoint = FindObjectOfType<LaserPoint>();
        if (!mirrorPuzzle) mirrorPuzzle = FindObjectOfType<MirrorClearCheck>();


        if (!grid || !laser || !laserPoint)
            Debug.LogError("[LaserPuzzle] grid / laser / laserPoint 참조 누락");
    }

    void Update()
    {
    }

    /// <summary>
    /// 외부에서 호출하는 퍼즐 판정 함수.
    /// - 현재 거울 상태 기준으로 레이저 경로 계산
    /// - LaserPoint로 시각화
    /// - MirrorSwitch에서 TurnOnce 계산 할 때 안에 함수 넣어서 
    /// 
    /// - Update안에 안 넣고 실시간 동기화 처럼 보임.
    /// </summary>
    [PunRPC]
    public bool CheckSolutionPhoton()
    {
        if (!startNode || !goalNode)
        {
            Debug.LogWarning("[LaserPuzzle] Start / Goal 노드가 설정되어 있지 않음");
            return false;
        }

        // 1) 레이저를 끝까지 쏴본다 (정답 여부 상관 X)
        var path = laser.TraceLaser(startNode, startDir);

        Debug.Log($"[LaserPuzzle] TraceLaser 결과: {(path == null ? "null" : path.Count.ToString())}");

        // 2) 레이저 그리기
        DrawLaser(path);

        // 3) "마지막 노드 == goalNode" 이면 정답
        bool ok = path != null && path.Count > 0 && path[path.Count - 1].node == goalNode;

        if (ok && !mirrorPuzzle.isSolved)
        {
            Debug.Log("SetPuzzleClear 함수 호출");
            mirrorPuzzle.SetPuzzleClear();   // RPC 포함된 함수 쓰는게 더 안전
            laserPoint.gameObject.SetActive(false);
        }

        return ok;
    }

    public void CheckSolution()
    {
        pv.RPC(nameof(CheckSolutionPhoton), PhotonTargets.All, null);
    }


    void DrawLaser(List<LaserState> path)
    {
        if (!laserPoint) return;

        if (path == null || path.Count == 0)
        {
            // 경로 없으면 시작점만 남기기
            laserPoint.ResetAllPointList(new List<Vector3>());
            return;
        }

        var pts = new List<Vector3>();
        foreach (var st in path)
        {
            // 보드 위로 살짝 띄우기
            pts.Add(st.node.transform.position);
        }

        laserPoint.ResetAllPointList(pts);
        Debug.Log($"[LaserPuzzle] DrawLaser: pathCount={path.Count}, pts={pts.Count + 1}");
    }


}

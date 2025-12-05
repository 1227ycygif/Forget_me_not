using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레이저 전용 A*
/// - 상태 = LaserState (Node + 방향)
/// - 지금 단계: 거울 무시, "직진만 가능 + 벽에서 멈춤"
/// </summary>
public class LaserAStar : MonoBehaviour
{
    [Header("References")]
    public Grid2D grid;          // 격자
    public MirrorClearCheck mirrorClear;
    public LaserPuzzle laserPuzzle;

    [Header("Cost")]
    public int moveCost = 10;    // 한 칸 이동 비용
    public int stepLimit = 444;  // 안전장치

    void Awake()
    {
        
    }

    /// <summary>
    /// 레이저 경로 찾기 (거울 없는 버전)
    /// </summary>
    public List<LaserState> FindLaserPath(Node start, LaserDir startDir, Node goal)
    {
        if (start == null || goal == null || grid == null)
        {
            Debug.LogError("[LaserAStar] start/goal/grid 누락");
            return null;
        }

        var open = new List<LaserState>();                          // 후보 상태들
        var closed = new HashSet<(int r, int c, LaserDir d)>();       // 처리 완료된 상태 키

        // 시작 상태
        var s = new LaserState
        {
            node = start,
            dir = startDir,
            g = 0,
            h = Heuristic(start, goal),
            parent = null
        };
        open.Add(s);

        int steps = 0;

        while (open.Count > 0 && steps++ < stepLimit)
        {
            // 1) F가 가장 작은 상태 꺼내기
            open.Sort((a, b) =>
            {
                int fComp = a.f.CompareTo(b.f);
                return (fComp != 0) ? fComp : a.h.CompareTo(b.h);
            });
            var cur = open[0];
            open.RemoveAt(0);

            // 2) 목표 노드 도달
            if (cur.node == goal)
                return Reconstruct(cur);

            // 3) 현재 상태를 closed에 등록
            var key = (cur.node.Row, cur.node.Col, cur.dir);
            if (closed.Contains(key)) continue;
            closed.Add(key);

            // 4) 이웃 상태(다음 칸) 생성
            foreach (var nb in Neighbours(cur))
            {
                if (nb == null) continue;

                // 벽이면 스킵
                if (nb.node.NType == NodeType.Wall)
                    continue;

                var nbKey = (nb.node.Row, nb.node.Col, nb.dir);
                if (closed.Contains(nbKey))
                    continue;

                int cost = moveCost;
                int tentativeG = cur.g + cost;

                // open에 같은 상태 있나 확인
                int idx = open.FindIndex(
                    s2 => s2.node == nb.node && s2.dir == nb.dir);

                if (idx >= 0)
                {
                    // 더 싼 경로면 갱신
                    if (tentativeG < open[idx].g)
                    {
                        open[idx].g = tentativeG;
                        open[idx].h = Heuristic(open[idx].node, goal);
                        open[idx].parent = cur;
                    }
                }
                else
                {
                    nb.g = tentativeG;
                    nb.h = Heuristic(nb.node, goal);
                    nb.parent = cur;
                    open.Add(nb);
                }
            }
            laserPuzzle.CheckSolution();
        }

        // 못 찾음
        return null;
    }
    /// <summary>
    /// A* 말고, 그냥 레이저를 직진/반사시키면서 끝까지 따라가는 함수.
    /// - goal 상관없음
    /// - 범위밖/벽/흡수 거울 만나면 거기서 종료
    /// - 항상 "지나간 모든 칸" 경로를 리턴 (0개일 수도 있음)
    /// </summary>
     // A* 말고 단순 “레이저 따라가기”
    public List<LaserState> TraceLaser(Node start, LaserDir startDir)
    {
        var path = new List<LaserState>();
        if (start == null || grid == null)
        {
            Debug.LogError("[Trace] start 또는 grid가 null");
            return path;
        }

        var cur = start;
        var dir = startDir;

        for (int step = 0; step < stepLimit; ++step)
        {
            path.Add(new LaserState { node = cur, dir = dir });

            Debug.Log($"[Trace] step={step}, pos=({cur.Row},{cur.Col}), dir={dir}");

            int dr = 0, dc = 0;
            switch (dir)
            {
                case LaserDir.Up:    dr = -1; dc =  0; break;
                case LaserDir.Down:  dr =  1; dc =  0; break;
                case LaserDir.Left:  dr =  0; dc = -1; break;
                case LaserDir.Right: dr =  0; dc =  1; break;
            }

            int nr = cur.Row + dr;
            int nc = cur.Col + dc;

            Debug.Log($"[Trace] next ({nr},{nc}), node={grid.m_nodeArr[nr, nc]}");  

            // 1) 범위 밖
            if (nr < 0 || nr >= grid.rows || nc < 0 || nc >= grid.cols)
            {
                Debug.Log($"[Trace] out of bounds -> ({nr},{nc}) / grid={grid.rows}x{grid.cols}");
                break;
            }

            var next = grid.m_nodeArr[nr, nc];

            // 2) 배열에 노드 없음
            if (next == null)
            {
                Debug.Log($"[Trace] NULL node at ({nr},{nc})");
                break;
            }

            // 3) 벽
            if (next.NType == NodeType.Wall)
            {
                Debug.Log($"[Trace] Wall at ({nr},{nc})");
                break;
            }

            // 4) 거울
            var mirror = next.GetComponent<MirrorCell>();
            if (mirror != null)
            {
                if (!mirror.TryReflect(dir, out var outDir))
                {
                    Debug.Log($"[Trace] Absorbed by mirror at ({nr},{nc}), state={mirror.mirrorState}");
                    break;
                }
                dir = outDir;
            }

            // 5) 정답
            if(next.NType == NodeType.Goal)
            {
                
                mirrorClear.SetPuzzleClear();
            }

            cur = next;
        }

        return path;
    }

    int Heuristic(Node a, Node b)
    {
        int dx = Mathf.Abs(a.Col - b.Col);
        int dy = Mathf.Abs(a.Row - b.Row);
        return (dx + dy) * 10;
    }

    List<LaserState> Reconstruct(LaserState s)
    {
        var path = new List<LaserState>();
        while (s != null)
        {
            path.Add(s);
            s = s.parent;
        }
        path.Reverse();
        return path;
    }

    /// <summary>
    /// 거울을 고려한 이웃 상태 생성:
    /// - 현재 dir 방향으로 1칸 전진
    /// - 벽이면 막힘
    /// - 그 칸에 MirrorCell이 있으면 TryReflect로 방향 변경/흡수
    /// </summary>
    IEnumerable<LaserState> Neighbours(LaserState cur)
    {
        int dr = 0, dc = 0;
        switch (cur.dir)
        {
            case LaserDir.Up: dr = -1; dc = 0; break;
            case LaserDir.Down: dr = 1; dc = 0; break;
            case LaserDir.Left: dr = 0; dc = -1; break;
            case LaserDir.Right: dr = 0; dc = 1; break;
        }

        int nr = cur.node.Row + dr;
        int nc = cur.node.Col + dc;

        // 그리드 밖이면 더 이상 진행 불가
        if (nr < 0 || nr >= grid.rows) yield break;
        if (nc < 0 || nc >= grid.cols) yield break;

        Node next = grid.m_nodeArr[nr, nc];
        if (next == null) yield break;

        // 벽이면 통과 불가
        if (next.NType == NodeType.Wall) yield break;

        // 기본 방향 = 직진
        var nextDir = cur.dir;

        // 이 칸이 거울이면 반사 시도
        var mirror = next.GetComponent<MirrorCell>();
        if (mirror != null)
        {
            LaserDir reflected;
            if (!mirror.TryReflect(cur.dir, out reflected))
            {
                // 흡수된 경우 → 이 방향으로 이웃 상태 없음
                yield break;
            }
            nextDir = reflected;
        }

        // 다음 상태 반환
        yield return new LaserState
        {
            node = next,
            dir = nextDir
        };
    }
}

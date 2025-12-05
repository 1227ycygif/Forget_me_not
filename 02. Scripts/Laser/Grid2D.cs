#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid2D : MonoBehaviour
{
    [Header("수동 배치용 설정")]
    public int rows = 9;
    public int cols = 9;
    public float cellSize = 1f;   // 노드 간 간격(그리드 스냅 단위)

    [Header("Prefab")]
    public Node m_nodePrefab;

    [Header("Generated Grid")]
    public Node[,] m_nodeArr;
    public Transform m_root;

    private List<Node> m_neighbours = new List<Node>();


    void Awake()
    {
        if (!m_root) m_root = transform.Find("Root");

        // 실행 시 m_nodeArr 채워주기
        m_nodeArr = new Node[rows, cols];
        var nodes = GetComponentsInChildren<Node>();

        foreach (var node in nodes)
        {
            int r = node.Row;
            int c = node.Col;

            if (r < 0 || r >= rows || c < 0 || c >= cols)
                continue;

            m_nodeArr[r, c] = node;
        }

        foreach (var node in nodes)
    {
        // 위치 기준으로 Row/Col 다시 계산
        Vector3 lp = node.transform.localPosition;

        int col = Mathf.RoundToInt(lp.x / cellSize);
        int row = Mathf.RoundToInt(-lp.z / cellSize);

        // Node 쪽에도 값 다시 세팅
        node.SetNode(row, col);

        if (row < 0 || row >= rows || col < 0 || col >= cols)
        {
            Debug.LogWarning($"[Grid2D] {node.name} pos={lp} -> ({row},{col}) 범위 밖");
            continue;
        }

        m_nodeArr[row, col] = node;
    }
    }

    void Start()
{
    Debug.Log($"[Grid2D] m_nodeArr[0,1] = {m_nodeArr[0,1]}");

    Debug.Log($"[Grid2D] Start on {name}, rows={rows}, cols={cols}");
    if (m_nodeArr != null)
    {
        Debug.Log($"[Grid2D] m_nodeArr[0,0]={m_nodeArr[0,0]}");
        Debug.Log($"[Grid2D] m_nodeArr[0,1]={m_nodeArr[0,1]}");
    }
}

    // =========================================
    //  에디터에서 호출하는 자동 넘버링 함수
    // =========================================
    [ContextMenu("Auto Number Nodes (from local position)")]
    void AutoNumberNodes()
    {
        if (!m_root) m_root = transform.Find("Root");
        if (!m_root)
        {
            Debug.LogWarning("[Grid2D] Root 자식을 찾을 수 없음");
            return;
        }

        var nodes = m_root.GetComponentsInChildren<Node>();

        foreach (var node in nodes)
        {
            // Grid2D 기준 localPosition 사용
            Vector3 lp = node.transform.localPosition;

            int col = Mathf.RoundToInt(lp.x / cellSize);
            int row = Mathf.RoundToInt(-lp.z / cellSize);

            node.SetNode(row, col);

            // 에디터에서 보기 좋게 이름도 바꿔줌
            node.gameObject.name = $"Node_{row}_{col}";

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(node.gameObject);
#endif
        }

        Debug.Log($"[Grid2D] AutoNumberNodes 완료. 노드 개수 = {nodes.Length}");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
    }

    // =============================
    //  Node Click (3D Raycast)
    // =============================
    public Node ClickNode()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.collider.GetComponent<Node>();
        }

        return null;
    }

    // =============================
    //  Grid 생성
    // =============================
    void CreateGrid(int count)
    {
        m_nodeArr = new Node[count, count];

        //int id = 0;

        for (int row = 0; row < count; ++row)
        {
            for (int col = 0; col < count; ++col)
            {
                // Node 생성
                Node node = Instantiate(
                    m_nodePrefab,
                    Vector3.zero,
                    Quaternion.identity,
                    m_root
                );

                m_nodeArr[row, col] = node;

                node.name = $"Node_{row}_{col}";
                node.SetNode(row, col);

                // 위치 배치 (X=col, Z=-row)
                node.transform.localPosition = new Vector3(col * cellSize, 0, -row * cellSize);
            }
        }
    }


    // =============================
    //  좌표 범위 체크
    // =============================
    private bool CheckNode(int row, int col)
    {
        return row >= 0 && row < rows &&
               col >= 0 && col < cols;
    }


    // =============================
    //  Grid 기반 Neighbours (8방향)
    // =============================
    public Node[] Neighbours(Node node)
    {
        m_neighbours.Clear();

        int r = node.Row;
        int c = node.Col;

        TryAdd(r - 1, c - 1); // 좌상단
        TryAdd(r - 1, c);     // 상
        TryAdd(r - 1, c + 1); // 우상단

        TryAdd(r, c - 1);     // 좌
        TryAdd(r, c + 1);     // 우

        TryAdd(r + 1, c - 1); // 좌하단
        TryAdd(r + 1, c);     // 하
        TryAdd(r + 1, c + 1); // 우하단

        return m_neighbours.ToArray();
    }

    void TryAdd(int r, int c)
    {
        if (CheckNode(r, c))
            m_neighbours.Add(m_nodeArr[r, c]);
    }


    // =============================
    // 전체 노드 리셋
    // =============================
    public void ResetNode()
    {
        for (int r = 0; r < rows; ++r)
        {
            for (int c = 0; c < cols; ++c)
            {

                if (m_nodeArr[r, c].NType != NodeType.Mirror)

                    m_nodeArr[r, c].Reset();
            }
        }
    }

    // =============================
    //  에디터용: 프리팹으로 그리드 자동 생성
    // =============================
#if UNITY_EDITOR
    [ContextMenu("Generate Grid (Rows x Cols)")]
    void GenerateGridEditor()
    {
        if (!m_root)
        {
            // Root 없으면 자동 생성
            var rootObj = new GameObject("Root");
            rootObj.transform.SetParent(transform);
            rootObj.transform.localPosition = Vector3.zero;
            rootObj.transform.localRotation = Quaternion.identity;
            rootObj.transform.localScale = Vector3.one;
            m_root = rootObj.transform;
        }

        if (!m_nodePrefab)
        {
            Debug.LogError("[Grid2D] m_nodePrefab이 비어 있음");
            return;
        }

        // 기존 자식 Node들 전부 삭제
        for (int i = m_root.childCount - 1; i >= 0; --i)
        {
            var child = m_root.GetChild(i);
            Undo.DestroyObjectImmediate(child.gameObject);
        }

        m_nodeArr = new Node[rows, cols];

        for (int r = 0; r < rows; ++r)
        {
            for (int c = 0; c < cols; ++c)
            {
                // 프리팹 인스턴스 생성 (Undo 지원)
                Node node =
                    (Node)PrefabUtility.InstantiatePrefab(m_nodePrefab, m_root);

                node.transform.localPosition =
                    new Vector3(c * cellSize, 0f, -r * cellSize);

                node.transform.localRotation = Quaternion.identity;
                node.transform.localScale = Vector3.one;

                node.SetNode(r, c);
                node.gameObject.name = $"Node_{r}_{c}";

                m_nodeArr[r, c] = node;

                Undo.RegisterCreatedObjectUndo(node.gameObject, "Create Node");
            }
        }

        EditorUtility.SetDirty(gameObject);
        Debug.Log($"[Grid2D] GenerateGridEditor 완료: {rows}x{cols}");
    }
#endif
}

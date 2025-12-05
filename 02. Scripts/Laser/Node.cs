using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 노드에 대한 속성 값 : "없음", "벽(장애물)".
public enum NodeType
{
    None,
    Wall,
    Mirror,
    Goal
}

public class Node : MonoBehaviour
{
    [SerializeField] private NodeType m_nodeType = NodeType.None;

    [SerializeField] private int m_row;
    [SerializeField] private int m_col;

    private Node m_parent;

    private Collider m_collider;          // 3D Collider 기준
    private SpriteRenderer m_renderer;    // 색상 표시용

    public int Row => m_row;
    public int Col => m_col;
    public NodeType NType => m_nodeType;
    public Node Parent => m_parent;

    public void MarkAsStart() => SetColor(Color.green);
    public void MarkAsGoal()  => SetColor(Color.blue);

    public void Awake()
    {
        m_collider = GetComponent<Collider>();        // BoxCollider 등
        m_renderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // string s = this.gameObject.name;
        // string nodeCol = s.Substring(s.Length - 3);
        // string nodeRow = s.Substring(s.Length - 1);

        // m_row = (int.Parse(nodeCol[0].ToString()));
        // m_col = (int.Parse(nodeRow));
    }

    public void Reset()
    {
        m_nodeType = NodeType.None;
        m_parent = null;

        if (m_renderer)
            m_renderer.color = Color.white;
    }

    public void SetNode(int row, int col)
    {
        m_row = row;
        m_col = col;
    }

    public void SetNodeType(NodeType type)
    {
        m_nodeType = type;

        // 디버그용 색상 분기 (원하면)
        if (!m_renderer) return;

        switch (type)
        {
            case NodeType.None:   m_renderer.color = Color.white; break;
            case NodeType.Wall:   m_renderer.color = Color.gray;  break;
            case NodeType.Mirror: m_renderer.color = Color.cyan;  break;
        }
    }

    public void SetColor(Color c)
    {
        if (m_nodeType == NodeType.Wall) return;
        if (m_renderer) m_renderer.color = c;
    }
}


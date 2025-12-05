using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 3x3 그리드 선 그리기
/// </summary>
public class GridLineDrawer : MonoBehaviour
{
    public Vector3 gridStartPosition = new Vector3(-1.2f, 0f, 1.2f);
    public float gridSpacing = 1.2f;
    public float lineWidth = 0.03f;
    public Color lineColor = Color.white;

    void Start()
    {
        DrawLines();
    }

    void DrawLines()
    {
        Vector3 topLeft = gridStartPosition + new Vector3(-gridSpacing * 0.5f, 0.05f, gridSpacing * 0.5f);

        // 가로선 4개
        for (int i = 0; i <= 3; i++)
        {
            CreateLine(
                topLeft + new Vector3(0, 0, -i * gridSpacing),
                topLeft + new Vector3(gridSpacing * 3, 0, -i * gridSpacing)
            );
        }

        // 세로선 4개
        for (int i = 0; i <= 3; i++)
        {
            CreateLine(
                topLeft + new Vector3(i * gridSpacing, 0, 0),
                topLeft + new Vector3(i * gridSpacing, 0, -gridSpacing * 3)
            );
        }
    }

    void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject obj = new GameObject("GridLine");
        obj.transform.parent = transform;

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = lineWidth;
        lr.startColor = lr.endColor = lineColor;

        // ===== 중요! 게임 뷰에서 보이게 하는 설정 =====
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        // Unlit Shader 사용 (항상 보임)
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = lineColor;
        lr.material = mat;

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }
}

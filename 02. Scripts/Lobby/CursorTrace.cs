using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CursorTrace : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GraphicRaycaster raycaster;
    [SerializeField] private RectTransform cursor;
    [SerializeField] private Vector2 offset = new Vector2(-357f, 0f);

    [Header("Hover 허용 버튼 목록")]
    public Button[] buttons; // 이 버튼들만 Hover 인식됨

    private HashSet<Button> allowed; // 내부 필터용
    private PointerEventData pointerEventData;
    private EventSystem eventSystem;

    void Awake()
    {
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (!raycaster) raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (!cursor) cursor = GetComponent<RectTransform>();
        eventSystem = EventSystem.current;

        // 빠른 검색을 위한 해시셋화
        if (buttons != null && buttons.Length > 0)
            allowed = new HashSet<Button>(buttons);
        else
            allowed = new HashSet<Button>();
    }

    void Update()
    {
        if (eventSystem == null || raycaster == null) return;

        pointerEventData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        raycaster.Raycast(pointerEventData, results);

        Button hoveredButton = null;

        foreach (var r in results)
        {
            Button btn = r.gameObject.GetComponentInParent<Button>();
            if (btn != null && allowed.Contains(btn))
            {
                hoveredButton = btn;
                break;
            }
        }

        if (hoveredButton != null)
        {
            // cursor.gameObject.SetActive(true);       
            RectTransform btnRect = hoveredButton.GetComponent<RectTransform>();
            cursor.anchoredPosition = btnRect.anchoredPosition + offset;
        }
        else
        {
            // cursor.gameObject.SetActive(false);
            // 취소, 생각이 안일했음, 비활성화하면 스크립트도 동작을 멈추잖아
        }
    }
}

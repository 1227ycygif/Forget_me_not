using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class Book : MonoBehaviour
{
    [Header("이 책의 인덱스 (BookPuzzle.books 순서와 일치)")]
    public int bookIndex = 0;

    [Header("드래그 제한 (로컬 X 기준)")]
    public Vector2 minPos = new Vector2(-3f, 0.06f);
    public Vector2 maxPos = new Vector2(3f, 0.1f);

    [Header("드래그 기준 좌표계 (비우면 부모 사용)")]
    [SerializeField] Transform dragRoot;

    Camera cam;
    BookPuzzle puzzle;

    bool isDragging;
    Vector3 startWorldPos;

    // 로컬 드래그용 저장값
    float startLocalX;          // 드래그 시작 시 책의 local X
    float startMouseLocalX;     // 드래그 시작 시 마우스의 local X

    float zDepth;

    void Awake()
    {
        cam    = Camera.main;
        puzzle = FindObjectOfType<BookPuzzle>();
        if (!dragRoot) dragRoot = transform.parent; // 기본은 부모 기준
    }

    void Start()
    {
        // 회전 고정은 기존 그대로
        transform.rotation = Quaternion.Euler(0, 90, 0);
    }
    void OnMouseDown()
    {
        if (!cam || !dragRoot)
        {
            Debug.LogWarning("[Book] 카메라 또는 dragRoot 없음");
            return;
        }

        isDragging   = true;
        startWorldPos = transform.position;

        // 드래그 시작 시점의 localPosition.x 저장
        Vector3 localPos = dragRoot.InverseTransformPoint(transform.position);
        startLocalX = localPos.x;

        // 마우스의 월드 위치 → 로컬로 변환해서 X만 사용
        zDepth = cam.WorldToScreenPoint(transform.position).z;
        Vector3 mouseScreen = new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDepth);
        Vector3 mouseWorld  = cam.ScreenToWorldPoint(mouseScreen);
        Vector3 mouseLocal  = dragRoot.InverseTransformPoint(mouseWorld);

        startMouseLocalX = mouseLocal.x;

        // 슬롯 점유 해제
        puzzle?.ReleaseByBook(bookIndex);
    }

    void OnMouseDrag()
    {
        if (!isDragging || !cam || !dragRoot) return;

        // 현재 마우스 위치 → 월드 → 로컬
        Vector3 mouseScreen = new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDepth);
        Vector3 mouseWorld  = cam.ScreenToWorldPoint(mouseScreen);
        Vector3 mouseLocal  = dragRoot.InverseTransformPoint(mouseWorld);

        // 마우스 로컬 X의 변화량만큼 책 로컬 X를 이동
        float deltaX       = mouseLocal.x - startMouseLocalX;
        float targetLocalX = startLocalX + deltaX;

        // 로컬 X를 minPos.x ~ maxPos.x 사이로 Clamp
        targetLocalX = Mathf.Clamp(targetLocalX, minPos.x, maxPos.x);

        // 최종 로컬 위치 구성 (Y/Z는 그대로 유지)
        Vector3 localPos = dragRoot.InverseTransformPoint(transform.position);
        localPos.x = targetLocalX;

        // 다시 월드로 변환해서 적용
        Vector3 worldPos = dragRoot.TransformPoint(localPos);
        transform.position = worldPos;
        transform.rotation = Quaternion.Euler(0, 90, 0);
    }

    void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        if (puzzle != null &&
            puzzle.TryGetSnap(transform.position, out int slotIdx, out Vector3 snapPos))
        {
            // 슬롯 위치는 월드 좌표이므로 그대로 사용
            transform.position = new Vector3(snapPos.x, snapPos.y, transform.position.z);
            transform.rotation = Quaternion.Euler(0, 90, 0);
            puzzle.Occupy(slotIdx, bookIndex);
        }
        else
        {
            // 필요하면 원래 위치로 되돌리고 싶을 때:
            // transform.position = startWorldPos;
            Debug.Log($"[Drop] Book {bookIndex} 스냅 실패");
        }
    }
}

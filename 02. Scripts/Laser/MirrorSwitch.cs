using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



public class MirrorSwitch : MonoBehaviour
{
    // 이 버튼으로 회전시킬 거울
    public MirrorCell[] mirrors;
    public LaserPuzzle laserPuzzle;

    public void OnMouseDown()
    {
        if(IsPointerOverUI())
        {
            Debug.Log("UI Object 클릭됨");
            return;
        }

        Debug.Log("클릭 들어옴");
        OnPushButton();
    }

    // 이 버튼을 눌렀다면 호출해줄 함수
    void OnPushButton()
    {
        // 포톤 처리 안함
        // 이미 거울 스크립트 안에서 해둠
        foreach(var mirror in mirrors)
        {
            mirror.TurnOnce();
            // 레이저 동기화
            
        }
        laserPuzzle.CheckSolution();
    }

    bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        var eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            // Image, Button, Text, TMP 등 Graphic이 있는 애들만 UI로 취급
            Graphic g = r.gameObject.GetComponent<Graphic>();
            if (g != null && g.raycastTarget)
            {
                // 여기 걸리면 "UI가 있다"라고 본다
                return true;
            }
        }

        return false;
    }
}

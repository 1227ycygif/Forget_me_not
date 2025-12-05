using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// 3D 버튼 (키패드 느낌)
/// </summary>
public class CAPButton3D : MonoBehaviour
{
    [Header("Button Settings")]
    public string buttonText = "버튼";
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color pressColor = Color.gray;

    [Header("References")]
    public TextMeshPro textMesh;
    public MeshRenderer buttonRenderer;

    [Header("Events")]
    public UnityEvent onButtonClick;

    private Material buttonMaterial;
    private Color currentColor;

    void Start()
    {
        // 머티리얼 복사 (인스턴스)
        if (buttonRenderer != null)
        {
            buttonMaterial = buttonRenderer.material;
            currentColor = normalColor;
            buttonMaterial.color = normalColor;
        }

        // 텍스트 설정
        if (textMesh != null)
        {
            textMesh.text = buttonText;
        }
    }

    void OnMouseEnter()
    {
        Debug.Log("마우스 호출");
        if (buttonMaterial != null)
        {
            buttonMaterial.color = hoverColor;
        }
    }

    void OnMouseExit()
    {
        if (buttonMaterial != null)
        {
            buttonMaterial.color = normalColor;
        }
    }

    void OnMouseDown()
    {
        Debug.Log("마우스 호출22");
        if (buttonMaterial != null)
        {
            buttonMaterial.color = pressColor;
        }
    }

    void OnMouseUp()
    {
        Debug.Log("마우스 호출33");
        if (buttonMaterial != null)
        {
            buttonMaterial.color = hoverColor;
        }

        // 버튼 클릭 이벤트 발생!
        Debug.Log($"버튼 클릭: {buttonText}");
        onButtonClick?.Invoke();
    }
}

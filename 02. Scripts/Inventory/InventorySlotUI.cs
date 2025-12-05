//using System;
//using UnityEngine;
//using UnityEngine.UI;

//public class InventorySlotUI : MonoBehaviour
//{
//    [Header("참조")]
//    public Button coverButton;   // Cover1
//    public Button useButton;     // [사용]1
//    public Image  itemImage;     // Item1 의 Image

//    private string itemId;
//    private Action<string> onUse;

//    void Awake()
//    {
//        if (!coverButton)
//            coverButton = GetComponent<Button>();

//        // 처음엔 [사용] 버튼 숨기기
//        if (useButton)
//            useButton.gameObject.SetActive(false);
//    }

//    public void Clear()
//    {
//        itemId = null;
//        onUse  = null;

//        if (itemImage)
//        {
//            itemImage.sprite  = null;
//            itemImage.enabled = false;
//        }

//        if (coverButton)
//        {
//            coverButton.interactable = false;
//            coverButton.onClick.RemoveAllListeners();
//        }

//        if (useButton)
//        {
//            useButton.gameObject.SetActive(false);
//            useButton.onClick.RemoveAllListeners();
//        }
//    }

//    public void Bind(string id, Sprite icon, Action<string> onUseCallback)
//    {
//        itemId = id;
//        onUse  = onUseCallback;

//        if (itemImage)
//        {
//            itemImage.sprite  = icon;
//            itemImage.enabled = icon != null;
//        }

//        if (coverButton)
//        {
//            coverButton.interactable = true;
//            coverButton.onClick.RemoveAllListeners();
//            coverButton.onClick.AddListener(OnCoverClick);
//        }

//        if (useButton)
//        {
//            useButton.gameObject.SetActive(false);
//            useButton.onClick.RemoveAllListeners();
//            useButton.onClick.AddListener(OnUseClick);
//        }
//    }

//    void OnCoverClick()
//    {
//        if (useButton)
//            useButton.gameObject.SetActive(true);

//        Debug.Log($"[Slot] Cover Click: {name}, item={itemId}");
//    }

//    void OnUseClick()
//    {
//        Debug.Log($"[Slot] Use Click: {name}, item={itemId}");
//        onUse?.Invoke(itemId);
//        if (useButton)
//            useButton.gameObject.SetActive(false);
//    }
//}
using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;
public class InventorySlotUI : MonoBehaviour
{
    [Header("참조")]
    public Button coverButton;   // Cover1
    public Button useButton;     // [사용]1
    public Image itemImage;     // Item1 의 Image
    

    private string itemId;
    private Action<string> onUse;
    Action<string, Sprite> onSelect;

    void Awake()
    {
        if (!coverButton)
            coverButton = GetComponent<Button>();

        // 처음엔 [사용] 버튼 숨기기
        if (useButton)
            useButton.gameObject.SetActive(false);
    }

    public void Clear()
    {
        itemId = null;
        onUse = null;
        onSelect = null;

        if (itemImage)
        {
            itemImage.sprite  = null;
            itemImage.enabled = false;
        }

        if (coverButton)
        {
            coverButton.interactable = false;
            coverButton.onClick.RemoveAllListeners();
        }

        if (useButton)
        {
            useButton.gameObject.SetActive(false);
            useButton.onClick.RemoveAllListeners();
        }
    }

    public void Bind(string id, Sprite icon,
                     Action<string> onUseCallback,
                     Action<string, Sprite> onSelectCallback)
    {
        itemId  = id;
        onUse   = onUseCallback;
        onSelect = onSelectCallback;

        if (itemImage)
        {
            itemImage.sprite  = icon;
            itemImage.enabled = (icon != null);
        }

        if (coverButton)
        {
            coverButton.interactable = true;
            coverButton.onClick.RemoveAllListeners();
            coverButton.onClick.AddListener(OnCoverClick);
        }

        if (useButton)
        {
            useButton.gameObject.SetActive(false);
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(OnUseClick);
        }
    }

    void OnCoverClick()
    {
        onSelect?.Invoke(itemId, itemImage ? itemImage.sprite : null);
        
        if (useButton)
            useButton.gameObject.SetActive(true);

        Debug.Log($"[Slot] Cover Click: {name}, item={itemId}");
    }

    void OnUseClick()
    {
        Debug.Log($"[Slot] Use Click: {name}, item={itemId}");
        onUse?.Invoke(itemId);
        if (useButton)
            useButton.gameObject.SetActive(false);

        /* Hard Code */
        GameObject inventoryPnl = transform.parent.parent.parent.parent.gameObject;
        if (inventoryPnl.activeSelf == true)
            inventoryPnl.SetActive(false);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


//public class InventoryAmount : MonoBehaviour
//{
//    public int amount; // 아이템 수량
//    public string[] itemKey; // 받아올 키
//    public Text[] amountText; // 수량 표시 텍스트
//    public Image[] itemImage; // 아이템 이미지에서 이름 가져오기

//    [Header("인벤토리 스토어 레퍼런스")]
//    [SerializeField] InventoryStore inventoryStore;


//    void Awake()
//    {
//        amountText = GetComponentsInChildren<Text>();
//        itemImage = GetComponentsInChildren<Image>();
//        itemKey = new string[itemImage.Length-1]; // 첫번째는 배경 이미지라 제외
//    }

//    void Update()
//    {
//        //if (GameManager.Instance.CurrentState != GameState.InMenu) return;

//        for (int i = 0; i < itemKey.Length; i++)
//        {
//            itemKey[i] = itemImage[i+1].sprite.name; // 이미지 이름을 키로 사용

//            amount = inventoryStore.GetCount(itemKey[i]);
//            amountText[i].text = amount.ToString() ?? null;
            
//            Debug.Log(amountText[i].text);
//            Debug.Log(itemImage[i+3].sprite.name);

//            Debug.Log($"[InventoryAmount] {itemKey[i]} 수량: {amount}");
//        }
//    }

//}

public class InventoryAmount : MonoBehaviour
{
    public int amount; // 아이템 수량
    public string[] itemKey; // 받아올 키
    public Text[] amountText; // 수량 표시 텍스트

    // 인스펙터에서 할당
    [SerializeField] public Image[] itemImage; // 아이템 이미지에서 이름 가져오기

    [Header("인벤토리 스토어 레퍼런스")]
    [SerializeField] InventoryStore inventoryStore;


    void Awake()
    {
        amountText = GetComponentsInChildren<Text>();
        //itemImage = GetComponentsInChildren<Image>();
        itemKey = new string[itemImage.Length]; // 첫번째는 배경 이미지라 제외
    }

    void Update()
    {
        //if (GameManager.Instance.CurrentState != GameState.InMenu) return;

        for (int i = 0; i < itemKey.Length; i++)
        {
            itemKey[i] = itemImage[i].sprite.name; // 이미지 이름을 키로 사용

            amount = inventoryStore.GetCount(itemKey[i]);
            amountText[i].text = amount.ToString() ?? null;
        }
    }

}
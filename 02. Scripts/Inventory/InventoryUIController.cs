//using UnityEngine;
//using UnityEngine.UI;

//// ItemUseRegistry: IItemUse[] 등록해서 키별로 Use 라우팅
//public class InventoryUIController : MonoBehaviour
//{
//    public InventoryStore store;
//    public ItemUseRegistry registry;    // DB 대신 사용
//    public Button[] itemSlot;
//    public GameObject player;

//    void Awake()
//    {
//        // 1) 인스펙터 주입
//        if (!registry) registry = ItemUseRegistry.Instance;

//        // 2) 그래도 없으면 씬에서 탐색 (비활성 포함)
//        if (!registry) registry = FindObjectOfType<ItemUseRegistry>(true);
//    }

//    void Start()
//    {
//        // 찐 Last 체크
//        if (!registry)
//        {
//            Debug.LogError("[UI] ItemUseRegistry 못 찾음");
//            return;
//        }
//    }
//    void OnEnable()
//    {
//        // 3) 인스펙터 주입
//        if (!registry) registry = ItemUseRegistry.Instance;

//        Debug.Log(registry.name);
//        if (store != null) store.OnInventoryChanged += HandleInventoryChanged;
//        Refresh();
//    }

//    void OnDisable()
//    {
//        if (store != null) store.OnInventoryChanged -= HandleInventoryChanged;
//    }

//    void HandleInventoryChanged(string itemId, int newCount) => Refresh();

//    public void Refresh()
//    {
//        if (store == null || itemSlot == null) return;

//        // 1) 초기화
//        foreach (var btn in itemSlot)
//        {
//            btn.onClick.RemoveAllListeners();
//            btn.image.sprite = null;
//            btn.image.enabled = false;
//            btn.interactable = false;
//        }

//        // 2) 스냅샷 -> 슬롯 바인딩
//        int i = 0;
//        var snap = store.Snapshot(); // IReadOnlyDictionary<string,int>
//        foreach (var kv in snap)
//        {
//            if (i >= itemSlot.Length) break;

//            string id = kv.Key;
//            //Debug.Log($"[UI] 아이템 슬롯 {i}: {id} x{kv.Value}, {kv.Key}");
//            var btn = itemSlot[i];

//            // 아이콘: Resources/Icons/{key}.png (없으면 이미지 비활성)
//            var icon = Resources.Load<Sprite>($"Icons/{id}");
//            if (icon != null)
//            {
//                btn.image.sprite = icon;
//                //Debug.Log($"[UI] 아이콘 로드 성공: {icon.name}");
//                btn.image.enabled = true;
//            }

//            btn.onClick.AddListener(() => OnClickUse(id));
//            btn.interactable = true;

//            i++;
//        }
//    }

//    void OnClickUse(string itemId)
//    {
//        if (registry == null)
//        {
//            Debug.LogWarning("[UI] registry 미할당");
//            return;
//        }

//        var ctx = new ItemEffectContext
//        {
//            user = player,
//            store = store,
//            runner = this,
//            itemPrefabs = null,
//            itemKey = itemId,
//            source = ItemUseSource.UI
//        };

//        if (!registry.TryUse(itemId, ctx, out bool consumable))
//        {
//            Debug.LogWarning($"[UI] 핸들러 없음: {itemId}");
//            return;
//        }

//        Debug.Log($"[UI] TryUse 성공! consumable={consumable}");  // ← 추가

//        if (consumable)
//        {
//            Debug.Log($"[UI] 소모 처리 전 수량: {store.GetCount(itemId)}");  // ← 추가
//            store.Remove(itemId, 1);
//            Debug.Log($"[UI] 소모 후 수량: {store.GetCount(itemId)}");  // ← 추가
//        }

//        Refresh();
//    }
//}

//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//public class InventoryUIController : MonoBehaviour
//{
//    public InventoryStore store;
//    public ItemUseRegistry registry;
//    public InventorySlotUI[] slots;   // Button[] 대신 이걸 사용
//    public GameObject player;

//    void Awake()
//    {
//        if (!registry) registry = ItemUseRegistry.Instance;
//        if (!registry) registry = FindObjectOfType<ItemUseRegistry>(true);
//    }

//    void Start()
//    {
//        if (!registry)
//        {
//            Debug.LogError("[UI] ItemUseRegistry 못 찾음");
//            return;
//        }
//    }

//    void OnEnable()
//    {
//        if (!registry) registry = ItemUseRegistry.Instance;

//        if (store != null)
//            store.OnInventoryChanged += HandleInventoryChanged;

//        Refresh();
//    }

//    void OnDisable()
//    {
//        if (store != null)
//            store.OnInventoryChanged -= HandleInventoryChanged;
//    }

//    void HandleInventoryChanged(string itemId, int newCount) => Refresh();

//    public void Refresh()
//    {
//        if (store == null || slots == null) return;

//        // 슬롯 먼저 비우기
//        foreach (var slot in slots)
//            if (slot != null) slot.Clear();

//        var snap = store.Snapshot();

//        Debug.Log($"[UI] Snapshot count={snap.Count}");

//        int i = 0;
//        foreach (var kv in snap)
//        {
//            if (i >= slots.Length) break;

//            string id = kv.Key;
//            var slot = slots[i];

//            if (slot == null)
//            {
//                i++;
//                continue;
//            }

//            var icon = Resources.Load<Sprite>($"Icons/{id}");

//            Debug.Log($"[UI] Slot {i} = {id}, icon={(icon ? icon.name : "null")}");

//            slot.Bind(id, icon, OnClickUse);

//            i++;
//        }
//    }

//    void OnClickUse(string itemId)
//    {
//        if (registry == null)
//        {
//            Debug.LogWarning("[UI] registry 미할당");
//            return;
//        }

//        var ctx = new ItemEffectContext
//        {
//            user = player,
//            store = store,
//            runner = this,
//            itemPrefabs = null,
//            itemKey = itemId,
//            source = ItemUseSource.UI
//        };

//        if (!registry.TryUse(itemId, ctx, out bool consumable))
//        {
//            Debug.LogWarning($"[UI] 핸들러 없음: {itemId}");
//            return;
//        }

//        Debug.Log($"[UI] TryUse 성공! consumable={consumable}");

//        if (consumable)
//        {
//            store.Remove(itemId, 1);
//        }

//        Refresh();
//    }
//}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class InventoryUIController : MonoBehaviour
{
    public InventoryStore store;
    public ItemUseRegistry registry;
    public InventorySlotUI[] slots;   // Button[] 대신 이걸 사용
    public GameObject player;

    public GameObject detailRoot;  // 오른쪽 패널 전체 (없으면 null로 두고 무시해도 됨)
    public Image detailImage;
    public Text detailName;   // TextMeshPro 쓰면 TMP_Text로 바꿔
    public Text detailDesc;

    void Awake()
    {
        if (!registry) registry = ItemUseRegistry.Instance;
        if (!registry) registry = FindObjectOfType<ItemUseRegistry>(true);
    }

    void Start()
    {
        if (!registry)
        {
            Debug.LogError("[UI] ItemUseRegistry 못 찾음");
            return;
        }
    }

    void OnEnable()
    {
        if (!registry) registry = ItemUseRegistry.Instance;

        if (store != null)
            store.OnInventoryChanged += HandleInventoryChanged;

        Refresh();
    }

    void OnDisable()
    {
        if (store != null)
            store.OnInventoryChanged -= HandleInventoryChanged;
    }

    void HandleInventoryChanged(string itemId, int newCount) => Refresh();

    public void Refresh()
    {
        if (store == null || slots == null) return;

        // 슬롯 먼저 비우기
        foreach (var slot in slots)
            if (slot != null) slot.Clear();

        var snap = store.Snapshot();

        Debug.Log($"[UI] Snapshot count={snap.Count}");

        int i = 0;
        foreach (var kv in snap)
        {
            if (i >= slots.Length) break;

            string id = kv.Key;
            var slot = slots[i];

            if (slot == null)
            {
                i++;
                continue;
            }

            var icon = Resources.Load<Sprite>($"Icons/{id}");

            Debug.Log($"[UI] Slot {i} = {id}, icon={(icon ? icon.name : "null")}");

            if (slot != null)
                slot.Bind(id, icon, OnClickUse, OnSelectSlot);

            i++;
        }
    }

    void OnClickUse(string itemId)
    {
        if (registry == null)
        {
            Debug.LogWarning("[UI] registry 미할당");
            return;
        }

        var ctx = new ItemEffectContext
        {
            user = player,
            store = store,
            runner = this,
            itemPrefabs = null,
            itemKey = itemId,
            source = ItemUseSource.UI
        };

        if (!registry.TryUse(itemId, ctx, out bool consumable))
        {
            Debug.LogWarning($"[UI] 핸들러 없음: {itemId}");
            return;
        }

        Debug.Log($"[UI] TryUse 성공! consumable={consumable}");

        if (consumable)
        {
            store.Remove(itemId, 1);
        }

        Refresh();
    }

    void OnSelectSlot(string itemId, Sprite icon)
    {
        Debug.Log($"[UI] 상세보기 선택: {itemId}");

        if (detailRoot)
            detailRoot.SetActive(true);

        if (detailImage)
        {
            detailImage.sprite = icon;
            detailImage.enabled = (icon != null);
        }

        if (detailName)
            detailName.text = itemId;   // 임시: 나중에 DB에서 한글 이름 뽑으면 교체

        if (detailDesc)
            detailDesc.text = GetDescription(itemId); // 아래에서 정의
    }

    // 설명 텍스트 가져오는 함수 (DB 있으면 거기서 꺼내고, 없으면 대충)
    string GetDescription(string itemId)
    {
        // var meta = store.database?.FindById(itemId);
        // return meta ? meta.description : "";

        switch (itemId)
        {
            case "Flash":
                return "손전등이다. 매우 강한 빛을 방출한다.";
            case "Talisman":
                return "예로부터 내려오는 신사의 어떤 부적. 강력한 주력이 담겨있어 던질 수 있다.";
            default:
                return "";
        }
    }
}

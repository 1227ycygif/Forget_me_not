using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 캐릭터 정보 구조체
public class ItemData
{
    public string item_ID;     // Key랑 혼용중          >> DB로 간다
    //public int item_ID_DB;     // DB에 정보 입력용    (>> DB로 간다)
    public Sprite item_Sprite; // 이미지 스프라이트
    public int item_Count;     // 아이템 소지 개수       >> DB로 간다
    //public string item_Info;   // 아이템 정보
    //public string item_Detail; // 아이템 상세
}

public class InventoryData : MonoBehaviour
{
    public static InventoryData Instance { private set; get; }
    
    // 아이템을 저장할 리스트
    // 포톤 처리 안할거임, 걍 각자 저장하게 할거임
    // 줍는 아이템을 포톤처리 명확하게 했다면, 애초에 여기서 나눌 필요가 없다.
    [SerializeField] public Dictionary<string, int> itemBox;

    InventoryStore store;

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        itemBox = new();
    }

    // 현 시점 InventoryStore 저장, 씬 변경 전, 인벤토리 추가, 제거 시에 호출될 예정
    public bool SaveData()
    {
        // 코루틴 따로 안쓰고 그냥 찾기, 맵에 이미 플레이어가 있음, 못찾을수가 없음
        store = GameObject.FindWithTag("Player").GetComponent<InventoryStore>();

        // 스냅샷, 그대로 이동
        itemBox = store.Snapshot();

        return true;    // 저장 성공
    }

    // 씬 로드 후 인벤토리 불러오기, 새로 만들어진 Player.InventoryStore에서 호출될 예정
    // >> Player.InventoryStore.Awake에서 호출되고 있음
    public bool LoadData()
    {
        // 코루틴 따로 안쓰고 그냥 찾기, 어차피 InventoryStore 혹은 SceneMoveManager에서 호출할거임, 플레이어를 못찾을수가 없음
        store = GameObject.FindWithTag("Player").GetComponent<InventoryStore>();

        // itemBox 전달
        store.DataFullLoad(itemBox);
        
        return true;    // 로드 성공
    }
}

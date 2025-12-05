using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class BookPuzzle : MonoBehaviour, IInteractable
{
    [Header("책, 슬롯 배열 (직접 할당)")]
    [SerializeField] private Transform[] books;   // 책 트랜스폼들 (Book 컴포넌트 함께)
    [SerializeField] private Transform[] slots;   // 슬롯 트랜스폼들 (빈 오브젝트/마커 OK)

    [Header("스냅 설정 (판정 거리)")]
    [SerializeField, Min(0.01f)] private float snapRadius = 0.2f;

    [Header("정답: 슬롯 인덱스를 책 인덱스기준(제로베이스)")]
    [Tooltip("예) slots가 3개면 [2,0,1] 은 0번 책→2번 슬롯, 1번 책→0번 슬롯, 2번 책→1번 슬롯")]
    [SerializeField] private List<int> answer = new List<int>();

    // 슬롯 점유 상태: 슬롯 i를 어느 책이 점유 중인지 (없으면 -1)
    private int[] slotOwner;
    // 책이 어느 슬롯을 점유 중인지 (없으면 -1)
    private int[] bookAtSlot;

    // 풀렸는지
    [SerializeField] bool isSolved;
    [SerializeField] bool nowSolving = false;
    [SerializeField] CameraCtrl cam;
    [SerializeField] Transform camPos;

    PhotonView pv;

    void Awake()
    {
        StartCoroutine(FindCam());

        if (books == null || books.Length == 0)
        {
            Debug.LogError("[BookPuzzle] books 배열을 인스펙터에서 설정하세요.");
        }
        if (slots == null || slots.Length == 0)
        {
            Debug.LogError("[BookPuzzle] slots 배열을 인스펙터에서 설정하세요.");
        }
        if (answer.Count != books.Length)
        {
            Debug.LogWarning("[BookPuzzle] answer 길이가 books 길이와 다릅니다.");
        }

        slotOwner = Enumerable.Repeat(-1, slots.Length).ToArray();
        bookAtSlot = Enumerable.Repeat(-1, books.Length).ToArray();
        pv = GetComponent<PhotonView>();

    }

    IEnumerator FindCam()
    {
        while (cam == null)
        {
            yield return new WaitForSeconds(0.5f);

            cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraCtrl>();
        }

        yield break;
    }

    void Update()
    {
        // 퍼즐이 이미 풀려있다면 즉시 탈출, 아무 동작 안하겠다는 뜻
        if (isSolved) return;

        if (Input.GetKeyDown(KeyCode.V))
        {
            RPCRestPuzzle();
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            PrintMapping();
        }
    }






    // 상호작용 가능 여부 조회
    public bool CanInteract(GameObject player)
    {
        if (isSolved)
        {
            Debug.Log($"이미 푼 퍼즐입니다 : 호출자 {player.name}");
            return false;
        }
        if (nowSolving)
        {
            Debug.Log($"이미 상대방이 퍼즐을 풀고 있습니다.");
            return false;
        }

        return true;
    }

    [PunRPC]
    void RpcSetSolving(bool value)
    {
        nowSolving = value;
    }

    public void Interact(GameObject player)
    {
        if (nowSolving || isSolved) return;

        pv.RPC(nameof(RpcSetSolving), PhotonTargets.AllBuffered, true);

        GameManager.Instance.SetState(GameState.SolvingPuzzle);
        GetComponent<BoxCollider>().enabled = false;
        cam.SetCamPos(camPos);
    }

    public void ExitPuzzle()
    {
        GameManager.Instance.SetState(GameState.Normal);
        cam.CamPosBack();
        pv.RPC(nameof(RpcSetSolving), PhotonTargets.AllBuffered, false);
    }



    [PunRPC]
    void NowSolving()
    {
        nowSolving = true;
    }

    void PuzzleClear()
    {
        SoundManager.manager.SFXPlay(5, this.gameObject.transform.position);
        // 퍼즐별 클리어 시 특수처리할 거 있으면 여기서

        // 액자 충돌박스 활성화
        gameObject.GetComponent<BoxCollider>().enabled = true;

        // 게임 상태 전환
        GameManager.Instance.SetState(GameState.Normal);

        // 카메라 이동 >> 플레이어
        cam.CamPosBack();

        //Reward();
    }

























    // 가장 가까운데 비어있는 슬롯을 찾고, 스냅 가능 여부 반환
    public bool TryGetSnap(Vector3 from, out int slotIdx, out Vector3 snapPos)
    {
        slotIdx = -1;
        snapPos = Vector3.zero;
        float bestDist = float.MaxValue;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slotOwner[i] != -1) continue; // 이미 점유됨

            float d = Vector3.Distance(from, slots[i].position);
            if (d < bestDist)
            {
                bestDist = d;
                slotIdx = i;
                snapPos = slots[i].position;
            }
        }

        // 반경 안에 들어올 때만 스냅 허용
        return (slotIdx != -1 && bestDist <= snapRadius);
    }

    // 책이 드롭되기 전에, 기존 점유가 있으면 해제
    public void ReleaseByBook(int bookIdx)
    {
        int current = bookAtSlot[bookIdx];
        if (current >= 0 && current < slotOwner.Length)
        {
            slotOwner[current] = -1;
            bookAtSlot[bookIdx] = -1;
        }
    }

    // 스냅이 확정되면 점유 갱신
    public void Occupy(int slotIdx, int bookIdx)
    {
        // 안전장치: 해당 슬롯 비워두기
        if (slotOwner[slotIdx] != -1)
        {
            // 이미 누가 있으면 밀어내지 않음
            Debug.LogWarning($"[BookPuzzle] 슬롯 {slotIdx}는 이미 점유됨");
            return;
        }
        slotOwner[slotIdx] = bookIdx;
        bookAtSlot[bookIdx] = slotIdx;

        CheckAnswer(); // 놓는 순간 정답 확인
    }

    // 정답 체크: answer가 "책 i가 들어가야 하는 슬롯 인덱스"를 담고 있다고 가정
    public bool CheckAnswer()
    {
        if (answer.Count != books.Length) return false;
        if (isSolved) return true; // 이미 풀렸다면 다시 처리 X

        for (int i = 0; i < books.Length; i++)
        {
            if (bookAtSlot[i] != answer[i])
                return false;
        }

        // 정답이면 마스터만 클리어를 방송
        if (PhotonNetwork.isMasterClient)
            pv.RPC(nameof(RpcPuzzleClear), PhotonTargets.AllBuffered);

        return true;
    }

    [PunRPC]
    void RpcPuzzleClear()
    {
        if (isSolved) return; // 중복 방지

        isSolved = true;
        nowSolving = false;

        SoundManager.manager.SFXPlay(5, transform.position);
        GetComponent<BoxCollider>().enabled = true;
        GameManager.Instance.SetState(GameState.Normal);
        cam.CamPosBack();
        
        ClassPuzzleManager.Instance.AddSolved();

        // 여기서 문 열기, 보상 지급 등 퍼즐 결과 처리
    }

    // public void OnClickCheckAnswer()
    // {
    //     for (int i = 0; i < books.Length; i++)
    //         Debug.Log($"[Mapping] Book {i} -> Slot {bookAtSlot[i]}  (Answer:{(i < answer.Count ? answer[i] : -1)})");

    //     bool ok = CheckAnswer();
    //     Debug.Log(ok ? "<color=green>정답!</color>" : "<color=red>오답</color>");
    // }

    // 씬에서 반경 시각화(편의)
    void OnDrawGizmosSelected()
    {
        if (slots == null) return;
        Gizmos.color = Color.yellow;
        foreach (var s in slots)
            if (s) Gizmos.DrawWireSphere(s.position, snapRadius);
    }

    // 외부에서 안전히 얻기 위한 읽기용 프로퍼티
    public Transform[] Books => books;

    // 디버그용: 현재 매핑 출력
    [ContextMenu("Print Mapping")]
    void PrintMapping()
    {
        for (int i = 0; i < books.Length; i++)
        {
            Debug.Log($"Book {i} -> Slot {bookAtSlot[i]}");
        }
    }

    public void ResetPuzzle()
    {
        for (int i = 0; i < slotOwner.Length; i++)
            slotOwner[i] = -1;

        for (int i = 0; i < bookAtSlot.Length; i++)
            bookAtSlot[i] = -1;

        isSolved = false;
        nowSolving = false;
    }

    public void RPCRestPuzzle()
    {
        ResetPuzzle();
        pv.RPC(nameof(NowSolving), PhotonTargets.AllBuffered, false);
    }

    // 정답 확인 버튼 // 디버그용
    // void OnGUI()
    // {
    //     if (GUILayout.Button("정답 확인"))
    //     {
    //         if (CheckAnswer())
    //         {
    //             OnClickCheckAnswer();
    //             Debug.Log("정답!");
    //         }
    //         else
    //         {
    //             Debug.Log("오답!");
    //         }
    //     }
    // }
}

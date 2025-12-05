using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleSlide : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject piecePrefab; // 스프라이트를 표시할 프리팹 (SpriteRenderer 또는 Image 포함)

    [SerializeField] private string resourcePath = "Img/Slide"; // Resources/Img/Slide
    [SerializeField, Min(2)] private int cols;
    [SerializeField, Min(2)] private int rows;
    [SerializeField] private Transform[] targetArea;
    [SerializeField] private Transform camPos;

    [SerializeField] CameraCtrl cam;

    Sprite[] slices;

    List<GameObject> pieces;
    List<Vector3> piecePos;
    List<int> pieceOrder;

    [SerializeField] bool isSolved = false;
    [SerializeField] bool nowSolving = false;
    int emptyIdx;

    PhotonView pv = null;
    List<GameObject> currPieces;


    private void Awake()
    {
        StartCoroutine(FindCam());

        pv = GetComponent<PhotonView>();
        pv.ObservedComponents[0] = this;
        pv.synchronization = ViewSynchronization.UnreliableOnChange;
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

    private void Start()
    {
        pieces = new List<GameObject>();
        piecePos = new List<Vector3>();
        pieceOrder = new List<int>();

        emptyIdx = cols * rows - 1;

        // 퍼즐 조각 위치 잡기, piecePos 에 저장
        InitPiecePos();
        // 퍼즐 조각 나열 순서 리스트 섞기, pieceOrder 에 저장
        ShuffleOrder();

        // Resources 에서 이미지 불러와 조각조각
        slices = SliceImg();
        // 조각난 Sprite들로 GameObject 생성, pieces 에 저장
        GeneratePiece(slices);

        // 만들어진 pieces들을 piecePos에 놓기
        SetPiecePos();
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

    // 활성화
    public void Interact(GameObject player)
    {
        //if (!PhotonNetwork.isMasterClient) return;
        if (nowSolving || isSolved) return;
        nowSolving = true;

        pv.RPC("NowSolving", PhotonTargets.AllBuffered, null);

        Debug.Log($"nowSolving ? {nowSolving}");

        GameManager.Instance.SetState(GameState.SolvingPuzzle);

        gameObject.GetComponent<BoxCollider>().enabled = false;

        cam.SetCamPos(camPos);

        Debug.Log($"카메라 위치 조정 호출됨 : {camPos.name}");
    }

    public void ExitPuzzle()
    {
        // 퍼즐 초기화
        pv.RPC("PuzzleReset", PhotonTargets.AllBuffered, null);
        // 플레이어 상태 갱신
        GameManager.Instance.SetState(GameState.Normal);
        // 자기 자신의 카메라 되돌리기
        cam.CamPosBack();
    }

    [PunRPC]
    void NowSolving()
    {
        nowSolving = true;
    }



    // 가로 세로에 맞게 퍼즐이 배열될 위치를 잡아주는 함수
    void InitPiecePos()
    {
        // int cols, rows 에 따라 Transform[] piecePos 생성
        // >> Transform 배열이 동적 생성이 벌로라면, List<Transform> piecePos로 만들 생각
        // cols = rows 인 상황만 작성

        // 생성 영역을 Transform[] targetArea로 제한
        // targetArea[0] : LeftTop,   targetPos[1] : RightBottom

        piecePos = new List<Vector3>(cols * rows);

        // 좌상단/우하단
        Vector3 lt = targetArea[0].position;
        Vector3 rb = targetArea[1].position;

        // 폭/높이 (2D 기준: z는 평균으로 고정)
        float width = rb.x - lt.x;
        float height = rb.y - lt.y;
        float depth = rb.z - lt.z;

        // 각 칸의 "중심"에 배치: (c+0.5)/cols, (r+0.5)/rows
        for (int r = 0; r < rows; r++)          // rows == cols
        {
            for (int c = 0; c < cols; c++)
            {
                float tx = (c + 0.5f) / cols;
                float ty = (r + 0.5f) / rows;
                float tz = (r + 0.5f) / rows;

                Vector3 pos = new Vector3(
                    lt.x + width * tx,
                    lt.y + height * ty,
                    lt.z + depth * tz
                );

                // piecePos 설정
                piecePos.Add(pos);
            }
        }
    }

    private void ShuffleOrder()
    {
        // List<int> pieceOrder 에  0~14까지의 숫자를 셔플해서 넣는다
        while (true)
        {
            // 초기화
            pieceOrder.Clear();

            for (int v = 0; v < cols * rows - 1; v++) pieceOrder.Add(v);

            // Fisher-Yates 셔플
            for (int i = pieceOrder.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1); // [0, i]
                (pieceOrder[i], pieceOrder[j]) = (pieceOrder[j], pieceOrder[i]);
            }

            // 풀이 가능인지 점검 후 탈출
            if (IsSolvable(pieceOrder)) break;
        }
        // 빈공간 체크용 변수
        pieceOrder.Add(-99);
    }

    // 풀이 가능 여부 점검 함수 : 퍼즐 순서 정렬 시 역전수(inv)가 홀수 개면 풀이 불가능
    private bool IsSolvable(List<int> shuffleIdx)
    {
        int inv = 0;

        for (int i = 0; i < shuffleIdx.Count; i++)
        {
            for (int j = i + 1; j < shuffleIdx.Count; j++)
            {
                if (shuffleIdx[i] > shuffleIdx[j]) inv++;
            }
        }

        return (inv % 2) == 0;
    }

    // 
    public Sprite[] SliceImg()
    {
        // 원본 텍스처 로드
        Texture2D tex = Resources.Load<Texture2D>(resourcePath);
        if (tex == null)
        {
            Debug.LogError($"[SliceImg] Resources.Load 실패: {resourcePath}");
            return null;
        }

        int cellW = tex.width / cols;
        int cellH = tex.height / rows;
        int pieceCount = 0;

        var list = new List<Sprite>(cols * rows);

        float areaWidth = Mathf.Abs(targetArea[0].position.x - targetArea[1].position.x);
        //float areaHeight = Mathf.Abs(targetArea[0].position.y - targetArea[1].position.y);

        float ppu = tex.width / areaWidth;
        //float ppuH = tex.height / areaHeight;

        // 텍스쳐 슬라이싱
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int x = c * cellW;
                int y = (rows - 1 - r) * cellH; // 상단부터 내려오도록 정렬
                Rect rect = new Rect(x, y, cellW, cellH);

                Sprite spr = Sprite.Create(
                    tex, rect, new Vector2(0.5f, 0.5f),
                    ppu, 0, SpriteMeshType.FullRect
                );
                spr.name = $"slice_{pieceCount++}";
                list.Add(spr);
            }
        }

        return list.ToArray(); // 순서: (0,0) ~ (rows-1, cols-1)
    }


    // 2. 조각 개수에 따라 조각 프리팹 생성
    void GeneratePiece(Sprite[] slices)
    {
        // 1. List<Sprite>(4 * 4) 인 slices를 기반으로
        // List 요소 개수만큼 Prefab Instantiate
        // 2. 각 오브젝트별로 Sprite 대응시키기
        // 3. 자신의 자식객체로 만들 것

        if (piecePrefab == null || slices == null || slices.Length == 0)
        {
            Debug.LogWarning("GeneratePiece: 설정/입력이 비어있습니다.");
            return;
        }

        // 기존 자식 정리(원하면 유지)
        //for (int i = transform.childCount - 1; i >= 0; --i)
        //    DestroyImmediate(transform.GetChild(i).gameObject);

        if (pieces != null) pieces.Clear();

        for (int i = 0; i < slices.Length; i++)
        {
            var obj = Instantiate(piecePrefab, transform);
            obj.name = $"Piece_{i}";
            obj.transform.localScale = Vector3.one;
            obj.GetComponent<PieceClick>().pieceId = i;

            // 스프라이트 할당 (SpriteRenderer 우선, 없으면 UI Image)
            if (obj.TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.sprite = slices[i];
            }
            else if (obj.TryGetComponent<Image>(out var img))
            {
                img.sprite = slices[i];
                //img.SetNativeSize(); // 필요 시
            }
            else
            {
                Debug.LogWarning($"{obj.name}: SpriteRenderer/Image가 없습니다.");
            }

            pieces.Add(obj);
        }
    }

    // 이건 마스터가 해주고 뿌려
    void SetPiecePos()
    {
        if (pieces.Count != piecePos.Count)
        {
            Debug.LogError("치명적 결함 : 조각 개수와 배치 자리 개수의 불일치");
            return;
        }

        // 15개 조각만 배치
        for (int i = 0; i < pieceOrder.Count - 1; i++)
        {
            pieces[pieceOrder[i]].transform.position = piecePos[i];
            //pieces[pieceOrder[i]].transform.rotation = this.transform.rotation;
            Debug.Log(i + "번째 조각 배치 완료");
        }

        if (pieces[pieces.Count - 1].activeSelf == true)
            pieces[pieces.Count - 1].SetActive(false);
    }

    // 얘는 조작할 때 마스터한테 허락 받아
    public void OnPieceClicked(int id)
    {
        if (pieces == null) return;

        if (isSolved)
        {
            Debug.Log("이 퍼즐은 이미 다 풀었습니다.");
            return;
        }

        int gridId = 0;

        for (int i = 0; i < pieceOrder.Count; i++)
        {
            // 퍼즐 조각 배열 위치랑 
            if (pieceOrder[i] == id)
            {
                gridId = i;
                break;
            }
        }

        // 빈칸과 인접(맨해튼 거리 1)인지 확인
        if (!AreAdjacent(gridId, emptyIdx))
        {
            Debug.Log("빈 공간과 인접하지 않음");
            return;
        }

        // 위치 이동
        pieces[id].transform.position = piecePos[emptyIdx];

        //pieceOrder 스왑
        int temp = pieceOrder[gridId];
        pieceOrder[gridId] = pieceOrder[emptyIdx];
        pieceOrder[emptyIdx] = temp;

        // 공백 인덱스 갱신
        emptyIdx = gridId;

        Debug.Log("빈공간 갱신: " + emptyIdx);
        SoundManager.manager.SFXPlay(6, this.gameObject.transform.position);
        if (CheckAnswer()) PuzzleClear();
    }

    // 맨해튼 거리 1칸 인접 판정
    bool AreAdjacent(int clicked, int blank)
    {
        int cRow = clicked / cols;
        int cCol = clicked % cols;
        int bRow = blank / cols;
        int bCol = blank % cols;

        int dr = Mathf.Abs(cRow - bRow);
        int dc = Mathf.Abs(cCol - bCol);

        // 같은 행에서 좌우 1칸 또는 같은 열에서 상하 1칸
        return (dr == 0 && dc == 1) || (dr == 1 && dc == 0);
    }

    // 정답 체크
    private bool CheckAnswer()
    {
        for (int i = 0; i < pieceOrder.Count - 1; i++)
        {
            if (pieceOrder[i] != i) return false;
        }

        return (pieceOrder[pieceOrder.Count - 1] == -99);
    }

    [PunRPC]
    void PuzzleReset()
    {

        // 순서 배열 초기화
        ShuffleOrder();

        // 만들어진 pieces들을 piecePos에 놓기
        SetPiecePos();

        nowSolving = false;
    }

    void PuzzleClear()
    {
        // 퍼즐별 클리어 시 특수처리할 거 있으면 여기서
        pv.RPC("SolveAction", PhotonTargets.AllBuffered, null);
        Debug.Log("풀었다!");

        // 액자 충돌박스 활성화
        gameObject.GetComponent<BoxCollider>().enabled = true;

        // 게임 상태 전환
        GameManager.Instance.SetState(GameState.Normal);

        // 카메라 이동 >> 플레이어
        cam.CamPosBack();
    }

    void RepositionPiecesFromOrder()
    {
        // 마지막(-99) 조각은 비활성 처리 or 빈칸용
        for (int i = 0; i < pieceOrder.Count - 1; i++)
        {
            int pieceId = pieceOrder[i];
            var p = pieces[pieceId];
            p.transform.position = piecePos[i];
        }

        // 마지막 조각(빈칸)은 강제 활성화
        if (!pieces[pieces.Count - 1].activeSelf)
            pieces[pieces.Count - 1].SetActive(true);
    }


    [PunRPC]
    void SolveAction()
    {
        isSolved = true;
        // 얘는 근데 의미가 없는 코드긴 함

        // 마지막 조각 활성화 및 맨 끝 칸으로 이동
        var lastPiece = pieces[pieces.Count - 1];
        if (!lastPiece.activeSelf) lastPiece.SetActive(true);
        lastPiece.transform.position = piecePos[pieces.Count - 1];

        // 현 배열을 강제 재배치(동기화 보정)
        RepositionPiecesFromOrder();
    }


    void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        //로컬 플레이어의 위치 정보를 송신
        if (stream.isWriting)
        {
            // 박싱해서 전송
            // 보낼 데이터 최소화 (조각 순서, 빈칸 인덱스, 클리어 여부)
            stream.SendNext(pieceOrder.ToArray()); // int[]
            stream.SendNext(emptyIdx);
            stream.SendNext(nowSolving);            // int
            stream.SendNext(isSolved);

        }
        //원격 플레이어의 동작 정보를 수신
        else
        {
            // 언박싱 해서 받기
            pieceOrder = (List<int>)stream.ReceiveNext();
            emptyIdx = (int)stream.ReceiveNext();
            nowSolving = (bool)stream.ReceiveNext();
            isSolved = (bool)stream.ReceiveNext();


            // 로컬 반영: pieceOrder 갱신 후, 위치 재배치
            RepositionPiecesFromOrder(); // 아래 함수 구현

            //// 위치/ 회전
            //currPos = (Vector3)stream.ReceiveNext();
            //currRot = (Quaternion)stream.ReceiveNext();
            //// 입력 파라미터
            //float h = (float)stream.ReceiveNext();
            //float v = (float)stream.ReceiveNext();
            //bool run = (bool)stream.ReceiveNext();
            //bool move = (bool)stream.ReceiveNext();
            //bool jumpPulse = (bool)stream.ReceiveNext();
            //bool interactPulse = (bool)stream.ReceiveNext();
            //bool canInteract = (bool)stream.ReceiveNext();

            //// 원격 입력 소스에 반영
            //(inputSrc as NetInputSource)?.Apply(h, v, run, move, jumpPulse, interactPulse, canInteract);

            //// 애니 값은 여기서도 바로 반영 가능
            //if (anim)
            //{
            //    anim.SetBool("Run", run);

            //    anim.SetFloat("MoveLR", h > 0.05 ? -h : 0, damp, Time.deltaTime);
            //    anim.SetFloat("MoveFB", v > 0.05 ? v : 0, damp, Time.deltaTime);
            //}
        }
    }
}
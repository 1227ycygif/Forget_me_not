using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;

// ============================================================
// CAPPiece - 퍼즐 조각 스크립트
// 역할: 드래그 앤 드롭, 그리드 스냅, Phase별 권한 체크
// ============================================================
public class CAPPiece : Photon.MonoBehaviour
{
    // ============================================================
    // 필드 선언
    // ============================================================

    [Header("Piece Data")]
    public CAPPieceData pieceData;

    [Header("Visual Components")]
    public SpriteRenderer arrowSpriteRenderer;
    public SpriteRenderer letterSpriteRenderer;
    public SpriteRenderer backgroundRenderer;

    [Header("Sprites")]
    public int pieceID;
    public int arrowID;       // 1~9
    public string letterID;   // A ~ I
    public Sprite[] arrowSprites;
    public Sprite[] letterSprites;
    public Sprite backgroundSprite;
    public string letterOrder = "ADEGILMOS";

    [Header("Settings")]
    public float hoverHeight = 0.2f;
    public float snapSpeed = 10f;
    public bool localTestMode = false;

    [Header("References")]
    private CAPGrid capGrid;
    private CAPGameFlow gameFlow;  // Phase별 권한 체크용
    private Vector3 targetPosition;
    private Vector3 originalPosition;
    private bool isDragging = false;
    private bool isSnapping = false;
    private bool isPlayer1;
    private int savedGridIndex = -1;

    // 동시 드래그 방지
    // 동시 드래그 방지 (static으로 모든 조각이 공유)
    private static CAPPiece currentlyDragging = null;




    // ============================================================
    // Awake - CAPGameFlow 자동 찾기 (Phase별 권한 체크용)
    // ============================================================
    void Awake()
    {
        // CAPGameFlow를 씬에서 자동으로 찾기
        if (gameFlow == null)
        {
            gameFlow = FindObjectOfType<CAPGameFlow>();

            if (gameFlow == null)
                Debug.LogWarning("[CAPPiece] CAPGameFlow를 찾을 수 없습니다!");
        }
    }

    // ============================================================
    // Update - 드래그 중일 때 마우스 따라가기 & 스냅 애니메이션
    // ============================================================
    void Update()
    {
        // 드래그 중: 마우스 위치 따라가기
        if (isDragging)
        {
            Vector3 mousePos = GetMouseWorldPosition();
            transform.position = new Vector3(mousePos.x, hoverHeight, mousePos.z);
        }

        // 스냅 중: 목표 위치로 부드럽게 이동 (Lerp)
        if (isSnapping)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * snapSpeed);

            // 목표 위치에 거의 도달하면 스냅 종료
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isSnapping = false;
            }
        }
    }

    // ============================================================
    // Initialize - 조각 초기화 (그리드 위치에 배치)
    // ============================================================
    public void Initialize(CAPPieceData data, bool isPlayer1View, CAPGrid grid)
    {
        pieceData = data;
        isPlayer1 = isPlayer1View;
        capGrid = grid;
        originalPosition = transform.position;

        SetupVisuals(isPlayer1View);

        // 그리드 위치가 지정되어 있으면 해당 위치로 이동
        if (capGrid != null && data.gridIndex >= 0)
        {
            Vector3 gridPos = capGrid.GetGridPosition3D(data.gridIndex);
            transform.position = gridPos;
            capGrid.RegisterPiece(data.gridIndex, this);
        }
    }

    // ============================================================
    // 네트워크 생성 시 데이터 초기화용 RPC
    // ============================================================
    [PunRPC]
    void InitializePieceData(int pieceID, int arrowID, string letterID)
    {
        CAPPieceData data = new CAPPieceData();
        data.pieceID = pieceID;
        data.arrowID = arrowID;
        data.letterID = letterID;
        data.gridIndex = -1;


        bool isP1View = !PhotonNetwork.isMasterClient;
        //InitializeAtWaitPosition(data, isP1View, this.capGrid);
        CAPGameFlow gameFlow = FindObjectOfType<CAPGameFlow>();
        CAPGrid targetGrid = null;

        if (gameFlow != null)
        {
            // Phase1, 3: mainGrid / Phase2, 4: inputGrid
            switch (gameFlow.currentPhase)
            {
                case CAPGameFlow.PuzzlePhase.Phase1_Player2_MainLetters:
                case CAPGameFlow.PuzzlePhase.Phase3_Player1_MainArrows:
                    targetGrid = gameFlow.mainGrid;
                    break;

                case CAPGameFlow.PuzzlePhase.Phase2_Player1_InputArrows:
                case CAPGameFlow.PuzzlePhase.Phase4_Player2_InputLetters:
                    targetGrid = gameFlow.inputGrid;
                    break;
            }
        }

        InitializeAtWaitPosition(data, isP1View, targetGrid);

        Debug.Log($"[RPC] Piece {pieceID} 초기화: arrow={arrowID}, letter={letterID}, isP1={isP1View}");
    }

    // ============================================================
    // InitializeAtWaitPosition - 대기 줄에 조각 생성
    // ============================================================
    public void InitializeAtWaitPosition(CAPPieceData data, bool isPlayer1View, CAPGrid grid)
    {
        pieceData = data;
        isPlayer1 = isPlayer1View;
        capGrid = grid;
        originalPosition = transform.position;
        SetupVisuals(isPlayer1View);
    }

    // ============================================================
    // SetupVisuals - 플레이어 시점에 맞게 화살표/문자 표시
    // Player1: 화살표 보임 / Player2: 문자 보임
    // ============================================================
    void SetupVisuals(bool isPlayer1View)
    {
        if (backgroundRenderer != null && backgroundSprite != null)
        {
            backgroundRenderer.sprite = backgroundSprite;
        }

        // Player1 (남주): 화살표만 보임
        if (isPlayer1View)
        {
            arrowSpriteRenderer.gameObject.SetActive(true);
            letterSpriteRenderer.gameObject.SetActive(false);

            int index = pieceData.arrowID - 1;
            if (index >= 0 && index < arrowSprites.Length)
            {
                arrowSpriteRenderer.sprite = arrowSprites[index];
            }
        }
        else  // Player2 (여주): 문자만 보임
        {
            arrowSpriteRenderer.gameObject.SetActive(false);
            letterSpriteRenderer.gameObject.SetActive(true);

            int index = -1;

            if (!string.IsNullOrEmpty(pieceData.letterID) && letterSprites != null && letterSprites.Length > 0)
            {
                // letterOrder와 letterID를 대문자 기준으로 비교
                char c = char.ToUpper(pieceData.letterID[0]);

                if (!string.IsNullOrEmpty(letterOrder))
                {
                    string orderUpper = letterOrder.ToUpper();
                    index = orderUpper.IndexOf(c);   // ADEGILMOS 안에서 위치 찾기
                }
            }

            if (index >= 0 && index < letterSprites.Length)
            {
                letterSpriteRenderer.sprite = letterSprites[index];
            }
            else
            {
                Debug.LogWarning($"[CAPPiece] letterID '{pieceData.letterID}' 에 해당하는 스프라이트를 찾지 못했습니다.");
            }
        }
    }

    public bool IsPlayer1View => isPlayer1;
    public void RefreshVisual()
    {
        SetupVisuals(isPlayer1);
    }

    // ============================================================
    // OnMouseDown - 드래그 시작
    // 체크 순서:
    // 1) photonView.isMine 체크 (내 조각인지)
    // 2) Phase별 권한 체크 (지금 Phase에서 내가 잡을 수 있는지)
    // 3) 동시 드래그 방지 (다른 조각이 드래그 중인지)
    // 4) 드래그 시작
    // ============================================================
    void OnMouseDown()
    {
        // 1) 로컬 테스트가 아니면 photonView.isMine 체크
        if (!localTestMode && PhotonNetwork.inRoom)
        {
            if (!photonView.isMine)
            {
                photonView.RequestOwnership();
                return; // 다음 프레임에 다시 시도
            }
        }

        // 2) Phase별 권한 체크 추가!
        if (gameFlow != null && !gameFlow.CanGrabPiece())
        {
            Debug.Log(gameFlow.GetCannotGrabMessage());
            return;  // 권한 없으면 드래그 시작 차단!
        }

        // 3) 다른 조각이 드래그 중이면 차단
        if (currentlyDragging != null && currentlyDragging != this)
        {
            Debug.LogWarning($"Piece {currentlyDragging.pieceData.pieceID}가 이미 드래그 중!");
            return;
        }

        currentlyDragging = this;
        isDragging = true;
        savedGridIndex = (pieceData != null) ? pieceData.gridIndex : -1;

        //// 기존 그리드 위치를 비우기 (다른 곳으로 이동할 예정)
        //if (capGrid != null && savedGridIndex >= 0)
        //{
        //    capGrid.RegisterPiece(savedGridIndex, null);
        //    // pieceData.gridIndex는 RegisterPiece에서 -1로 자동 설정됨
        //}

        transform.position = new Vector3(transform.position.x, hoverHeight, transform.position.z);
    }

    // ============================================================
    // OnMouseUp - 드래그 종료 & 그리드 스냅
    // ============================================================
    void OnMouseUp()
    {
        if (!localTestMode && !photonView.isMine)
        {
            return;
        }

        if (!isDragging)
        {
            return;
        }

        isDragging = false;
        currentlyDragging = null;  // 드래그 종료 - 전역 변수 해제

        //if (capGrid == null) return;

        // 가장 가까운 그리드 칸 찾기
        int nearestIndex = capGrid.GetNearestGridIndex3D(transform.position);

        // 그리드 밖으로 드롭했으면 원래 위치로
        if (nearestIndex < 0)
        {
            ReturnToSaved();
            return;
        }

        // 로컬 테스트: 직접 호출 / 멀티플레이: RPC로 동기화
        if (localTestMode)
        {
            MoveToGrid(nearestIndex);
        }
        else
        {
            photonView.RPC("MoveToGrid", PhotonTargets.AllBuffered, nearestIndex);
        }
    }

    // ============================================================
    // ReturnToSaved - 원래 위치로 돌아가기 (그리드 밖으로 드롭했을 때)
    // ============================================================
    void ReturnToSaved()
    {
        if (savedGridIndex >= 0 && capGrid != null)
        {
            // RegisterPiece가 자동으로 pieceData.gridIndex 설정
            capGrid.RegisterPiece(savedGridIndex, this);
            targetPosition = capGrid.GetGridPosition3D(savedGridIndex);
        }
        else
        {
            targetPosition = originalPosition;
        }
        isSnapping = true;
        savedGridIndex = -1;
    }

    // ============================================================
    // MoveToGrid (개선 버전 - RegisterPiece에 책임 집중)
    // ============================================================
    // ============================================================
    // MoveToGrid - 그리드로 이동 (RPC로 동기화)
    // 경우의 수:
    // 1) 빈 칸으로 이동 → 그냥 배치
    // 2) 내가 이미 있는 칸 → 위치 보정
    // 3) 다른 조각이 있는 칸 → 스왑 (서로 위치 교환)
    // ============================================================
    [PunRPC]
    void MoveToGrid(int targetGridIndex)
    {
        if (capGrid == null) return;
        if (targetGridIndex < 0 || targetGridIndex >= 9) return;

        // 모든 클라이언트가 같게
        int fromIndex = (pieceData != null) ? pieceData.gridIndex : -1;

        // 목표 위치에 이미 조각이 있는지 확인
        CAPPiece existingPiece = capGrid.GetPieceAt(targetGridIndex);

        // 경우 1) 빈 칸으로 이동 - 단순 배치
        if (existingPiece == null)
        {
            capGrid.RegisterPiece(targetGridIndex, this);   // pieceData.gridIndex 자동 설정
            targetPosition = capGrid.GetGridPosition3D(targetGridIndex);
            isSnapping = true;
            savedGridIndex = -1;
            return;
        }

        // 경우 2) 이미 그 칸에 나 자신이 있는 경우 - 위치 보정만
        if (existingPiece == this)
        {
            targetPosition = capGrid.GetGridPosition3D(targetGridIndex);
            isSnapping = true;
            savedGridIndex = -1;
            return;
        }

        // 경우 3) 다른 조각이 있는 칸 - 스왑 (서로 위치 교환)
        if (fromIndex >= 0)
        {
            // existingPiece 를 savedGridIndex 로 이동
            // (RegisterPiece가 자동으로 targetGridIndex에서 제거함)
            capGrid.RegisterPiece(fromIndex, existingPiece);
            existingPiece.targetPosition = capGrid.GetGridPosition3D(fromIndex);
            existingPiece.isSnapping = true;
        }

        // 자신을 targetGridIndex 에 등록
        // (RegisterPiece가 자동으로 savedGridIndex에서 제거함)
        capGrid.RegisterPiece(targetGridIndex, this);
        targetPosition = capGrid.GetGridPosition3D(targetGridIndex);
        isSnapping = true;
        savedGridIndex = -1;
    }

    // ============================================================
    // GetMouseWorldPosition - 마우스 위치를 3D 월드 좌표로 변환
    // ============================================================
    Vector3 GetMouseWorldPosition()
    {
        if (Camera.main == null) return transform.position;

        // XZ 평면(바닥)에 레이캐스트
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        float dist;
        if (plane.Raycast(ray, out dist))
        {
            return ray.GetPoint(dist);
        }

        return transform.position;
    }

#if UNITY_EDITOR
    // ============================================================
    // 디버그용: 데이터 일관성 검증
    // ============================================================
    private void OnValidate()
    {
        if (Application.isPlaying && capGrid != null && pieceData != null && pieceData.gridIndex >= 0)
        {
            var registered = capGrid.GetPieceAt(pieceData.gridIndex);
            if (registered != this)
            {
                Debug.LogWarning($"데이터 불일치 감지! Piece {pieceData.pieceID}: " +
                    $"pieceData.gridIndex={pieceData.gridIndex}, " +
                    $"실제 등록={registered?.pieceData.pieceID ?? -1}");
            }
        }
    }
#endif
}

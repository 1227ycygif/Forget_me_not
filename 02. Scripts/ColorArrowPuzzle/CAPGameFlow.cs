using System.Collections;
using System.Collections.Generic;
using Photon;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class CAPGameFlow : Photon.MonoBehaviour
{
    // 퍼즐 4단계 상태
    public enum PuzzlePhase
    {
        Phase1_Player2_MainLetters,   // Player2: 문자 메인 그리드 배치
        Phase2_Player1_InputArrows,   // Player1: 화살표 입력 그리드 배치
        Phase3_Player1_MainArrows,    // Player1: 화살표 메인 그리드 배치
        Phase4_Player2_InputLetters,  // Player2: 문자 입력 그리드 배치
        Completed                     // 퍼즐 완료
    }


    [Header("Grids")]
    public CAPGrid mainGrid;       // 메인 그리드 (문자/화살표 모두 사용)
    public CAPInputGrid inputGrid; // 입력 그리드 (CAPGrid 상속)


    [Header("Spawners")]
    public CAPPieceSpawner arrowPieceSpawner;   // 화살표 조각용 스포너 (isPlayer1View = true)
    public CAPPieceSpawner letterPieceSpawner;  // 문자 조각용 스포너 (isPlayer1View = false)


    [Header("Manager / Config")]
    public CAPManager capManager;


    [Header("Settings")]
    public bool localTestMode = false;   // 테스트용 치트 허용 여부
    public bool isPlayer1 = true;       // 나중에 네트워크 쓸 때 구분용 (지금은 안 써도 됨)


    [Header("State (Debug)")]
    public PuzzlePhase currentPhase = PuzzlePhase.Phase1_Player2_MainLetters;
    private const string PHASE_KEY = "CurrentPhase";


    [Header("Answer Pattern")]
    [SerializeField]
    // Phase1 정답 (문자)
    string[] phase1_letters = new string[9] { "M", "S", "D", "L", "I", "O", "E", "A", "G" };

    // Phase2 정답 (화살표)
    int[] phase2_arrows = { 5, 6, 7, 4, 9, 8, 3, 2, 1 };

    // Phase3 정답 (화살표)
    int[] phase3_arrows = new int[9] { 3, 4, 5, 2, 9, 6, 1, 8, 7 };


    [SerializeField]
    private CAPButton3D submitButton;

    // PhotonNetwork 
    bool IsPlayer2 => PhotonNetwork.isMasterClient;   // 여주 - 엘마
    bool IsPlayer1 => !PhotonNetwork.isMasterClient;  // 남주 - 호타로



    // ------------------------------------------------------------------
    // 초기화
    // ------------------------------------------------------------------
    void Start()
    {
        if (capManager == null)
            capManager = CAPManager.Instance;

        if (arrowPieceSpawner != null)
            arrowPieceSpawner.isPlayer1View = true;   // 화살표 모드
        if (letterPieceSpawner != null)
            letterPieceSpawner.isPlayer1View = false; // 문자 모드

        // Phase 동기화 초기화
        if (PhotonNetwork.isMasterClient)
        {
            capManager.SyncMappingToClients();
            SetPhase(PuzzlePhase.Phase1_Player2_MainLetters);
        }
        else
        {
            LoadPhaseFromRoom();
        }

        SetupPhase1();
        //UpdateSubmitButtonInteractable();
    }


    // ============================================================
    // Phase 동기화 메서드들
    // ============================================================
    private void SetPhase(PuzzlePhase newPhase)
    {
        currentPhase = newPhase;

        if (PhotonNetwork.inRoom)
        {
            Hashtable props = new Hashtable();
            props[PHASE_KEY] = (int)newPhase;
            PhotonNetwork.room.SetCustomProperties(props);

            Debug.Log($"[Phase 동기화] Phase 설정됨: {newPhase}");
        }
    }

    private void LoadPhaseFromRoom()
    {
        if (PhotonNetwork.inRoom && PhotonNetwork.room.CustomProperties.ContainsKey(PHASE_KEY))
        {
            int phaseInt = (int)PhotonNetwork.room.CustomProperties[PHASE_KEY];
            currentPhase = (PuzzlePhase)phaseInt;

            Debug.Log($"[Phase 동기화] Phase 로드됨: {currentPhase}");
        }
    }

    void OnPhotonCustomRoomPropertiesChanged(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(PHASE_KEY))
        {
            int newPhaseInt = (int)propertiesThatChanged[PHASE_KEY];
            PuzzlePhase newPhase = (PuzzlePhase)newPhaseInt;

            if (currentPhase != newPhase)
            {
                PuzzlePhase oldPhase = currentPhase;
                currentPhase = newPhase;
                Debug.Log($"[Phase 동기화] Phase 업데이트: {oldPhase} → {newPhase}");
            }
        }
    }


    // ------------------------------------------------------------------
    // 버튼 엔트리 포인트
    // ------------------------------------------------------------------

    // ============================================================
    // 체크 버튼과 정렬 버튼 통합 버전
    // ============================================================

    /// <summary>
    /// 통합 버튼 (확인/정렬) - 스마트하게 동작
    /// </summary>
    public void OnCompleteButtonClicked()
    {
        Debug.Log($"CompleteButton 클릭 / Phase = {currentPhase}");

        switch (currentPhase)
        {
            case PuzzlePhase.Phase1_Player2_MainLetters:
                // Phase1: 여주만 사용 가능
                if (!CanUseButton_Phase1())
                {
                    Debug.Log("이 버튼을 사용할 수 없습니다. (엘마 차례)");
                    return;
                }
                HandlePhase1_Smart();  // ← 스마트 처리
                break;

            case PuzzlePhase.Phase3_Player1_MainArrows:
                // Phase3: 남주만 사용 가능
                if (!CanUseButton_Phase3())
                {
                    Debug.Log("이 버튼을 사용할 수 없습니다. (호타로 차례)");
                    return;
                }
                HandlePhase3_Smart();  // ← 스마트 처리
                break;

            default:
                Debug.Log("이 버튼을 사용할 수 없습니다.");
                break;
        }
    }

    /// <summary>
    /// Phase1 스마트 처리: 피드백 → 완성 시 자동 진행
    /// </summary>
    void HandlePhase1_Smart()
    {
        if (mainGrid == null)
        {
            Debug.LogError("mainGrid가 없습니다.");
            return;
        }

        // 배치된 조각 개수 확인
        int placedCount = mainGrid.GetPlacedPieceCount();

        if (placedCount == 0)
        {
            Debug.Log("조각을 먼저 배치해주세요!");
            ShowFeedback("조각을 먼저 배치해주세요!", FeedbackType.Warning);
            return;
        }

        // 올바른 위치에 있는 조각 개수
        int correctCount = mainGrid.GetCorrectLetterCount(phase1_letters);

        Debug.Log($"Phase1 진행도: {correctCount}/9 개 정답!");

        // 9/9가 아니면 → 피드백만 제공
        if (correctCount < 9)
        {
            if (correctCount >= 7)
                ShowFeedback($"{correctCount}/9 개 정답! 거의 다 왔어요!", FeedbackType.Good);
            else if (correctCount >= 4)
                ShowFeedback($"{correctCount}/9 개 정답. 조금만 더!", FeedbackType.Normal);
            else
                ShowFeedback($"{correctCount}/9 개 정답. 다시 시도해보세요!", FeedbackType.TryAgain);

            return;  // ← 여기서 종료 (다음 단계로 안 감)
        }

        // 9/9 완성! → 정답 체크 후 다음 단계로
        Debug.Log("Phase1 완성! 정답 체크 시작...");
        ShowFeedback("완벽합니다! 다음 단계로 이동!", FeedbackType.Perfect);

        // 잠시 후 다음 단계로 (피드백 보여주기)
        StartCoroutine(GoToNextPhaseAfterDelay(1.5f, () => {
            HandlePhase1();  // 기존 정답 체크 로직
        }));
    }

    /// <summary>
    /// Phase3 스마트 처리: 피드백 → 완성 시 자동 진행
    /// </summary>
    void HandlePhase3_Smart()
    {
        if (mainGrid == null)
        {
            Debug.LogError("mainGrid가 없습니다.");
            return;
        }

        // 배치된 조각 개수 확인
        int placedCount = mainGrid.GetPlacedPieceCount();

        if (placedCount == 0)
        {
            Debug.Log("조각을 먼저 배치해주세요!");
            ShowFeedback("조각을 먼저 배치해주세요!", FeedbackType.Warning);
            return;
        }

        // 올바른 위치에 있는 조각 개수
        int correctCount = mainGrid.GetCorrectArrowCount(phase3_arrows);

        Debug.Log($"Phase3 진행도: {correctCount}/9 개 정답!");

        // 9/9가 아니면 → 피드백만 제공
        if (correctCount < 9)
        {
            if (correctCount >= 7)
                ShowFeedback($"{correctCount}/9 개 정답! 거의 다 왔어요!", FeedbackType.Good);
            else if (correctCount >= 4)
                ShowFeedback($"{correctCount}/9 개 정답. 조금만 더!", FeedbackType.Normal);
            else
                ShowFeedback($"{correctCount}/9 개 정답. 다시 시도해보세요!", FeedbackType.TryAgain);

            return;  // ← 여기서 종료 (다음 단계로 안 감)
        }

        // 9/9 완성! → 정답 체크 후 다음 단계로
        Debug.Log("Phase3 완성! 정답 체크 시작...");
        ShowFeedback("완벽합니다! 다음 단계로 이동!", FeedbackType.Perfect);

        // 잠시 후 다음 단계로 (피드백 보여주기)
        StartCoroutine(GoToNextPhaseAfterDelay(1.5f, () => {
            HandlePhase3();  // 기존 정답 체크 로직
        }));
    }

    /// <summary>
    /// 딜레이 후 다음 단계로 이동
    /// </summary>
    IEnumerator GoToNextPhaseAfterDelay(float delay, System.Action callback)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }

    /// <summary>
    /// Phase1에서 정렬 버튼을 사용할 수 있는지 확인
    /// </summary>
    bool CanUseButton_Phase1()
    {
        // 로컬 테스트 모드에서는 무조건 허용
        if (localTestMode)
            return true;

        // 멀티플레이에서는 Player2(MasterClient)만 가능
        return PhotonNetwork.isMasterClient;
    }

    /// <summary>
    /// Phase3에서 정렬 버튼을 사용할 수 있는지 확인
    /// </summary>
    bool CanUseButton_Phase3()
    {
        // 로컬 테스트 모드에서는 무조건 허용
        if (localTestMode)
            return true;

        // 멀티플레이에서는 Player1(non-MasterClient)만 가능
        return !PhotonNetwork.isMasterClient;
    }


    // ============================================================
    // 조각 잡기 권한 체크
    // ============================================================

    /// <summary>
    /// 현재 Phase에서 조각을 잡을 수 있는지 확인
    /// </summary>
    public bool CanGrabPiece()
    {
        // 로컬 테스트 모드면 무조건 허용
        if (localTestMode)
            return true;

        // Phase별 권한 체크
        switch (currentPhase)
        {
            case PuzzlePhase.Phase1_Player2_MainLetters:
                // Phase1: 여주(MasterClient)만 가능
                return PhotonNetwork.isMasterClient;

            case PuzzlePhase.Phase2_Player1_InputArrows:
                // Phase2: 남주(Non-MasterClient)만 가능
                return !PhotonNetwork.isMasterClient;

            case PuzzlePhase.Phase3_Player1_MainArrows:
                // Phase3: 남주(Non-MasterClient)만 가능
                return !PhotonNetwork.isMasterClient;

            case PuzzlePhase.Phase4_Player2_InputLetters:
                // Phase4: 여주(MasterClient)만 가능
                return PhotonNetwork.isMasterClient;

            case PuzzlePhase.Completed:
                // 완료 상태에서는 조각 잡기 불가
                return false;

            default:
                return false;
        }
    }

    /// <summary>
    /// 조각을 잡을 수 없을 때 보여줄 메시지 (선택사항)
    /// </summary>
    public string GetCannotGrabMessage()
    {
        switch (currentPhase)
        {
            case PuzzlePhase.Phase1_Player2_MainLetters:
                return "엘마만 조각을 배치할 수 있습니다.";

            case PuzzlePhase.Phase2_Player1_InputArrows:
                return "호타로만 조각을 배치할 수 있습니다.";

            case PuzzlePhase.Phase3_Player1_MainArrows:
                return "호타로만 조각을 배치할 수 있습니다.";

            case PuzzlePhase.Phase4_Player2_InputLetters:
                return "엘마만 조각을 배치할 수 있습니다.";

            default:
                return "지금은 조각을 잡을 수 없습니다.";
        }
    }


    // ============================================================
    // 체크 버튼 - 현재 몇 개가 올바른 위치에 있는지 확인
    // ============================================================

    public enum FeedbackType
    {
        Perfect,    // 9/9
        Good,       // 7~8/9
        Normal,     // 4~6/9
        TryAgain,   // 0~3/9
        Warning     // 조각 없음
    }

    /// <summary>
    /// 체크 버튼 - 현재 몇 개가 올바른 위치에 있는지 확인
    /// </summary>
    public void OnCheckButtonClicked()
    {
        Debug.Log($"CheckButton 클릭 / Phase = {currentPhase}");

        switch (currentPhase)
        {
            case PuzzlePhase.Phase1_Player2_MainLetters:
                CheckPhase1Progress();
                break;

            case PuzzlePhase.Phase3_Player1_MainArrows:
                CheckPhase3Progress();
                break;

            default:
                Debug.Log("체크 버튼을 사용할 수 없습니다.");
                break;
        }
    }

    /// <summary>
    /// Phase1 진행도 체크
    /// </summary>
    void CheckPhase1Progress()
    {
        if (!CanUseButton_Phase1())
        {
            Debug.Log("체크 버튼을 사용할 수 없습니다. (엘마 차례)");
            return;
        }

        if (mainGrid == null)
        {
            Debug.LogError("mainGrid가 없습니다.");
            return;
        }

        // 배치된 조각 개수 확인
        int placedCount = mainGrid.GetPlacedPieceCount();

        if (placedCount == 0)
        {
            Debug.Log("조각을 먼저 배치해주세요!");
            ShowFeedback("조각을 먼저 배치해주세요!", FeedbackType.Warning);
            return;
        }

        // 올바른 위치에 있는 조각 개수
        int correctCount = mainGrid.GetCorrectLetterCount(phase1_letters);

        Debug.Log($"Phase1 진행도: {correctCount}/9 개 정답!");

        // 피드백 표시
        if (correctCount == 9)
        {
            ShowFeedback($"완벽합니다! 정렬 버튼을 눌러주세요!", FeedbackType.Perfect);
        }
        else if (correctCount >= 7)
        {
            ShowFeedback($"{correctCount}/9 개 정답! 거의 다 왔어요!", FeedbackType.Good);
        }
        else if (correctCount >= 4)
        {
            ShowFeedback($"{correctCount}/9 개 정답. 조금만 더!", FeedbackType.Normal);
        }
        else
        {
            ShowFeedback($"{correctCount}/9 개 정답. 다시 시도해보세요!", FeedbackType.TryAgain);
        }
    }

    /// <summary>
    /// Phase3 진행도 체크
    /// </summary>
    void CheckPhase3Progress()
    {
        if (!CanUseButton_Phase3())
        {
            Debug.Log("체크 버튼을 사용할 수 없습니다. (호타로 차례)");
            return;
        }

        if (mainGrid == null)
        {
            Debug.LogError("mainGrid가 없습니다.");
            return;
        }

        // 배치된 조각 개수 확인
        int placedCount = mainGrid.GetPlacedPieceCount();

        if (placedCount == 0)
        {
            Debug.Log("조각을 먼저 배치해주세요!");
            ShowFeedback("조각을 먼저 배치해주세요!", FeedbackType.Warning);
            return;
        }

        // 올바른 위치에 있는 조각 개수
        int correctCount = mainGrid.GetCorrectArrowCount(phase3_arrows);

        Debug.Log($"Phase3 진행도: {correctCount}/9 개 정답!");

        // 피드백 표시
        if (correctCount == 9)
        {
            ShowFeedback($"완벽합니다! 정렬 버튼을 눌러주세요!", FeedbackType.Perfect);
        }
        else if (correctCount >= 7)
        {
            ShowFeedback($"{correctCount}/9 개 정답! 거의 다 왔어요!", FeedbackType.Good);
        }
        else if (correctCount >= 4)
        {
            ShowFeedback($"{correctCount}/9 개 정답. 조금만 더!", FeedbackType.Normal);
        }
        else
        {
            ShowFeedback($"{correctCount}/9 개 정답. 다시 시도해보세요!", FeedbackType.TryAgain);
        }
    }

    /// <summary>
    /// 피드백 표시 (현재는 로그만, 나중에 UI 연결)
    /// </summary>
    void ShowFeedback(string message, FeedbackType type)
    {
        // 지금은 로그만 출력
        switch (type)
        {
            case FeedbackType.Perfect:
                Debug.Log($"<color=green><b>{message}</b></color>");
                break;
            case FeedbackType.Good:
                Debug.Log($"<color=yellow><b>{message}</b></color>");
                break;
            case FeedbackType.Normal:
                Debug.Log($"<color=orange><b>{message}</b></color>");
                break;
            case FeedbackType.TryAgain:
                Debug.Log($"<color=red><b>{message}</b></color>");
                break;
            case FeedbackType.Warning:
                Debug.Log($"<color=gray><b>{message}</b></color>");
                break;
        }

        // TODO: 나중에 여기에 UI 연결
        // if (feedbackText != null)
        //     feedbackText.text = message;
        // StartCoroutine(HideFeedbackAfterDelay(3f));
    }


    // ============================================================
    // 정답 제출 버튼 (SubmitButton) - 로컬 테스트 모드 지원
    // ============================================================
    public void OnSubmitButtonClicked()
    {
        Debug.Log($"SubmitButton 클릭 / Phase = {currentPhase}");

        switch (currentPhase)
        {
            case PuzzlePhase.Phase2_Player1_InputArrows:
                // Phase2: 남주만 사용
                if (!CanUseSubmit_Phase2())
                {
                    Debug.Log("Phase2: 이 플레이어는 Submit 사용할 수 없음");
                    return;
                }

                if (localTestMode)
                {
                    // 로컬 테스트: RPC 없이 직접 처리
                    HandlePhase2_Local();
                }
                else
                {
                    // 멀티플레이: RPC로 마스터에게 전송
                    int[] arrows = inputGrid.GetCurrentArrowArrangement();
                    photonView.RPC("RPC_SubmitPhase2", PhotonTargets.MasterClient, arrows);
                }
                break;

            case PuzzlePhase.Phase4_Player2_InputLetters:
                // Phase4: 여주만 사용
                if (!CanUseSubmit_Phase4())
                {
                    Debug.Log("호타로는 지금 이 버튼을 사용할 수 없습니다.");
                    return;
                }

                if (localTestMode)
                {
                    // 로컬 테스트: RPC 없이 직접 처리
                    HandlePhase4_Local_ForLocalTest();
                }
                else
                {
                    // 멀티플레이: 여주가 직접 Phase4 정답 체크 후 브로드캐스트
                    HandlePhase4();
                }
                break;

            default:
                Debug.Log("Submit 버튼을 사용할 수 없습니다.");
                break;
        }
    }

    bool CanUseSubmit_Phase2()
    {
        if (localTestMode) return true;
        return !PhotonNetwork.isMasterClient;  // Player1만
    }

    bool CanUseSubmit_Phase4()
    {
        if (localTestMode) return true;
        return PhotonNetwork.isMasterClient;  // Player2만
    }



    bool CheckPhase2()
    {
        int[] userInput = inputGrid.GetCurrentArrowArrangement();
        int[] correct = phase2_arrows;

        for (int i = 0; i < 9; i++)
        {
            if (userInput[i] != correct[i])
                return false;
        }
        return true;
    }



    // Phase 공통 처리 엔트리
    void OnPhaseSubmitInternal()
    {
        Debug.Log("OnPhaseSubmitInternal / CurrentPhase = " + currentPhase);

        switch (currentPhase)
        {
            case PuzzlePhase.Phase1_Player2_MainLetters:
                HandlePhase1();
                break;

            case PuzzlePhase.Phase3_Player1_MainArrows:
                HandlePhase3();
                break;

            case PuzzlePhase.Completed:
                Debug.Log("퍼즐이 이미 완료되었습니다.");
                break;
        }
    }


    // ------------------------------------------------------------------
    // 그리드 / 스포너 정리 함수
    // ------------------------------------------------------------------

    void ClearMainGrid()
    {
        if (mainGrid == null) return;
        for (int i = 0; i < 9; i++)
            mainGrid.RegisterPiece(i, null);
    }

    void ClearInputGrid()
    {
        if (inputGrid == null) return;
        for (int i = 0; i < 9; i++)
            inputGrid.RegisterPiece(i, null);
    }

    void ClearAllGrids()
    {
        ClearMainGrid();
        ClearInputGrid();
    }

    void ClearAllSpawners()
    {
        if (arrowPieceSpawner != null)
            arrowPieceSpawner.ClearAllPieces();
        if (letterPieceSpawner != null)
            letterPieceSpawner.ClearAllPieces();
    }

    // ------------------------------------------------------------------
    // Phase 셋업
    // ------------------------------------------------------------------

    // Phase1: Player2 – 문자 메인 그리드 배치 (설계자가 준 힌트대로 MSD / LIO / EAG)
    void SetupPhase1()
    {
        SetPhase(PuzzlePhase.Phase1_Player2_MainLetters);
        Debug.Log("=== Phase1 시작: 엘마 - 문자 메인 그리드 배치 ===");

        ClearAllSpawners();
        ClearAllGrids();

        if (letterPieceSpawner != null)
        {
            letterPieceSpawner.capGrid = mainGrid;   // 메인 그리드에서 쓸 조각
            letterPieceSpawner.isPlayer1View = false;
            letterPieceSpawner.SpawnWaitPieces();    // 하단 대기줄에 문자 9개
        }
    }

    // Phase2: Player1 – 화살표 입력 그리드 배치
    // 메인 그리드의 문자 패턴을 Player1 시점(화살표)으로 본 모양을 그대로 입력 그리드에 복사
    void SetupPhase2()
    {
        //currentPhase = PuzzlePhase.Phase2_Player1_InputArrows;
        Debug.Log("=== Phase2 시작: 호타로 - 화살표 입력 그리드 배치 ===");

        // Phase1 정답으로 채워진 메인 그리드는 그대로 유지
        arrowPieceSpawner.ClearWaitingPiecesOnly();
        ClearInputGrid(); // 입력 그리드만 깨끗하게

        if (arrowPieceSpawner != null)
        {
            arrowPieceSpawner.capGrid = inputGrid;
            arrowPieceSpawner.isPlayer1View = true;
            arrowPieceSpawner.SpawnWaitPieces();
        }
    }

    // Phase3: Player1 – 화살표 메인 그리드 배치 (두 번째 퍼즐, 시계 힌트 사용)
    void SetupPhase3()
    {
        currentPhase = PuzzlePhase.Phase3_Player1_MainArrows;
        Debug.Log("=== Phase3 시작: 호타로 - 화살표 메인 그리드 배치 ===");

        ClearAllSpawners();
        ClearAllGrids();   // 새로운 퍼즐이므로 둘 다 초기화

        if (arrowPieceSpawner != null)
        {
            arrowPieceSpawner.capGrid = mainGrid;
            arrowPieceSpawner.isPlayer1View = true;
            arrowPieceSpawner.SpawnWaitPieces();
        }
    }

    // Phase4: Player2 – 문자 입력 그리드 배치
    // Phase3에서 맞춘 화살표 패턴을 Player2 시점(문자)으로 본 모양을 입력 그리드에 복사
    void SetupPhase4()
    {
        // 디버그 로그 먼저 (Phase3 메인 그리드 상태 확인)
        Debug.Log($"[SetupPhase4] 메인 그리드 조각 개수: {mainGrid.GetPlacedPieceCount()}");
        for (int i = 0; i < 9; i++)
        {
            var piece = mainGrid.GetPieceAt(i);
            if (piece != null)
                Debug.Log($"  Grid {i}: Piece {piece.pieceData.pieceID}, Arrow {piece.pieceData.arrowID}");
            else
                Debug.Log($"  Grid {i}: EMPTY!");  // ← 여기서 사라진 조각 찾기
        }

        SetPhase(PuzzlePhase.Phase4_Player2_InputLetters);
        Debug.Log("=== Phase4 시작: 엘마 - 문자 입력 그리드 배치 ===");

        // 아무것도 지우지 않음! 메인 그리드 조각 보존해야 함

        if (letterPieceSpawner != null)
        {
            letterPieceSpawner.capGrid = inputGrid;
            letterPieceSpawner.isPlayer1View = false;
            letterPieceSpawner.SpawnWaitPieces();
        }

        //currentPhase = PuzzlePhase.Phase4_Player2_InputLetters;
        //Debug.Log("=== Phase4 시작: 엘마 - 문자 입력 그리드 배치 ===");

        //// Phase3 정답 화살표 패턴이 들어있는 메인 그리드는 그대로 유지
        //arrowPieceSpawner.ClearWaitingPiecesOnly();
        ////letterPieceSpawner.ClearWaitingPiecesOnly();
        //ClearInputGrid();

        //if (letterPieceSpawner != null)
        //{
        //    letterPieceSpawner.capGrid = inputGrid;
        //    letterPieceSpawner.isPlayer1View = false;
        //    letterPieceSpawner.SpawnWaitPieces();
        //}
    }

    // ------------------------------------------------------------------
    // Phase별 정답 판정
    // ------------------------------------------------------------------

    void HandlePhase1()
    {
        if (mainGrid == null)
        {
            Debug.LogError("mainGrid가 비어 있습니다.");
            return;
        }

        // 1) 메인 그리드에서 현재 문자 배열을 받아온다
        string[] userLetters = mainGrid.GetCurrentLettersArrangement();

        // 2) CAPGameFlow가 가지고 있는 Phase1 정답
        string[] correctLetters = phase1_letters;

        // 3) 수동 비교
        for (int i = 0; i < 9; i++)
        {
            if (userLetters[i] != correctLetters[i])
            {
                Debug.Log($"Phase1 오답: index {i}, 기대={correctLetters[i]}, 실제={userLetters[i]}");
                return;
            }
        }

        Debug.Log("Phase1 정답!");

        // 포톤 이용
        if (PhotonNetwork.inRoom)
        {
            // 방 안이라면 모든 클라이언트가 동시에 Phase2로 진입
            photonView.RPC("RPC_GotoPhase2", PhotonTargets.All);
        }
        else
        {
            // 로컬 테스트(싱글플레이)라면 그냥 혼자만 Phase2로
            GoToPhase2_Local();
        }
    }



    // Phase2: 입력 그리드 화살표 → Phase1 메인 그리드의 문자 패턴을 화살표로 변환한 것과 비교
    void HandlePhase2()
    {
        if (mainGrid == null || inputGrid == null || capManager == null)
        {
            Debug.LogError("Phase2: mainGrid / inputGrid / capManager 중 하나가 비어 있습니다.");
            return;
        }

        // 1) 메인 그리드의 문자 배열 (이미 Phase1에서 정답 상태)
        string[] mainLetters = mainGrid.GetCurrentLettersArrangement();

        // 2) 문자 → 화살표 매핑으로 "정답 화살표 배열" 만들기
        int[] correctArrows = new int[9];
        for (int i = 0; i < 9; i++)
        {
            string letter = mainLetters[i];
            if (string.IsNullOrEmpty(letter))
            {
                correctArrows[i] = 0;
                continue;
            }

            correctArrows[i] = capManager.GetArrowForLetter(letter);
        }

        // 3) 입력 그리드에 Player1이 배치한 실제 화살표 배열
        int[] inputArrows = inputGrid.GetCurrentArrowArrangement();

        for (int i = 0; i < 9; i++)
        {
            if (inputArrows[i] != correctArrows[i])
            {
                Debug.Log($"Phase2 오답: index {i}, 기대={correctArrows[i]}, 실제={inputArrows[i]}");
                return;
            }
        }

        Debug.Log("Phase2 정답! → 단서1 + Phase3 힌트 지급");
        capManager.OnClue1Obtained("Player1&2");
        // 필요하면 여기서 Phase3용 힌트 아이템도 capManager 통해 지급

        ClearAllSpawners();
        ClearAllGrids();
        SetupPhase3();
    }

    // ============================================================
    // 로컬 테스트용 Phase2 처리
    // ============================================================
    void HandlePhase2_Local()
    {
        if (mainGrid == null || inputGrid == null || capManager == null)
        {
            Debug.LogError("Phase2: mainGrid / inputGrid / capManager 중 하나가 비어 있습니다.");
            return;
        }

        // 1) 메인 그리드의 문자 배열 (Phase1에서 정답 상태)
        string[] mainLetters = mainGrid.GetCurrentLettersArrangement();

        // 2) 문자 → 화살표 매핑으로 정답 화살표 배열 만들기
        int[] correctArrows = new int[9];
        for (int i = 0; i < 9; i++)
        {
            string letter = mainLetters[i];
            if (string.IsNullOrEmpty(letter))
            {
                correctArrows[i] = 0;
                continue;
            }
            correctArrows[i] = capManager.GetArrowForLetter(letter);
        }

        // 3) 입력 그리드의 실제 화살표 배열
        int[] inputArrows = inputGrid.GetCurrentArrowArrangement();

        // 4) 비교
        for (int i = 0; i < 9; i++)
        {
            if (inputArrows[i] != correctArrows[i])
            {
                Debug.Log($"Phase2 오답: index {i}, 기대={correctArrows[i]}, 실제={inputArrows[i]}");
                return;
            }
        }

        Debug.Log("Phase2 정답! → 단서1 + Phase3로 이동 (로컬 테스트)");
        capManager.OnClue1Obtained("LocalTest");

        ClearAllSpawners();
        ClearAllGrids();
        SetupPhase3();
    }

    // Phase3: 메인 그리드 화살표 → CAPGrid.arrowAnswerPattern과 비교
    void HandlePhase3()
    {
        if (mainGrid == null)
        {
            Debug.LogError("Phase3: mainGrid가 비어 있습니다.");
            return;
        }

        // 1) 메인 그리드 현재 화살표 배열
        int[] userArrows = mainGrid.GetCurrentArrowArrangement();

        // 2) CAPGameFlow가 보관한 Phase3 정답 화살표 패턴
        int[] correctArrows = phase3_arrows;

        // 3) 비교
        for (int i = 0; i < 9; i++)
        {
            if (userArrows[i] != correctArrows[i])
            {
                Debug.Log($"Phase3 오답: index {i}, 기대={correctArrows[i]}, 실제={userArrows[i]}");
                return;
            }
        }

        Debug.Log("Phase3 정답! → Phase4로 이동");

        // 네트워크 동기화
        if (PhotonNetwork.inRoom)
        {
            photonView.RPC("RPC_GotoPhase4", PhotonTargets.All);
        }
        else
        {
            SetPhase(PuzzlePhase.Phase4_Player2_InputLetters);
            SetupPhase4();
        }
    }


    // Phase4: 입력 그리드 문자 → Phase3 메인 그리드 화살표를 문자로 변환한 것과 비교
    void HandlePhase4()
    {
        if (mainGrid == null || inputGrid == null || capManager == null)
        {
            Debug.LogError("Phase4: mainGrid / inputGrid / capManager 중 하나가 비어 있습니다.");
            return;
        }

        // 1) 메인 그리드의 화살표 배열 (이미 Phase3에서 정답 상태)
        int[] mainArrows = mainGrid.GetCurrentArrowArrangement();
        Debug.Log($"[Phase4] 메인 그리드 화살표: [{string.Join(", ", mainArrows)}]");

        // 2) 화살표 → 문자 매핑으로 "정답 문자 배열" 만들기
        string[] correctLetters = new string[9];
        for (int i = 0; i < 9; i++)
        {
            int arrowID = mainArrows[i];
            if (arrowID <= 0)
            {
                correctLetters[i] = "";
                Debug.LogWarning($"[Phase4] Grid {i}: 화살표 없음!");
                continue;
            }

            correctLetters[i] = capManager.GetLetterForArrow(arrowID);
            Debug.Log($"[Phase4] Grid {i}: Arrow {arrowID} → Letter {correctLetters[i]}");
        }

        // 3) 입력 그리드에 Player2가 배치한 실제 문자 배열
        string[] inputLetters = inputGrid.GetCurrentLettersArrangement();

        Debug.Log($"[Phase4 체크] 정답 문자: [{string.Join(", ", correctLetters)}]");
        Debug.Log($"[Phase4 체크] 입력 문자: [{string.Join(", ", inputLetters)}]");

        for (int i = 0; i < 9; i++)
        {
            if (inputLetters[i] != correctLetters[i])
            {
                Debug.Log($"Phase4 오답: index {i}, 기대={correctLetters[i]}, 실제={inputLetters[i]}");

                if (PhotonNetwork.inRoom)
                {
                    photonView.RPC("RPC_Phase4Result", PhotonTargets.All, false);
                }
                return;
            }
        }

        Debug.Log("Phase4 정답! → 단서2 획득, 퍼즐 완료!");
        capManager.OnClue2Obtained("Player1&2");

        // 네트워크 동기화
        if (PhotonNetwork.inRoom)
        {
            photonView.RPC("RPC_Phase4Result", PhotonTargets.All, true);
        }
        else
        {
            ClearAllSpawners();
            ClearAllGrids();
            SetPhase(PuzzlePhase.Completed);
        }
    }

    // ============================================================
    // 로컬 테스트용 Phase4 처리
    // ============================================================
    void HandlePhase4_Local_ForLocalTest()
    {
        if (mainGrid == null || inputGrid == null || capManager == null)
        {
            Debug.LogError("Phase4: mainGrid / inputGrid / capManager 중 하나가 비어 있습니다.");
            return;
        }

        // 1) 메인 그리드 화살표 배열
        int[] mainArrows = mainGrid.GetCurrentArrowArrangement();

        // 2) 화살표 → 문자 매핑으로 정답 문자 배열 만들기
        string[] correctLetters = new string[9];
        for (int i = 0; i < 9; i++)
        {
            int arrowID = mainArrows[i];
            correctLetters[i] = (arrowID > 0)
                ? capManager.GetLetterForArrow(arrowID)
                : "";
        }

        // 3) 입력 그리드의 실제 문자 배열
        string[] inputLetters = inputGrid.GetCurrentLettersArrangement();

        // 4) 비교
        for (int i = 0; i < 9; i++)
        {
            if (inputLetters[i] != correctLetters[i])
            {
                Debug.Log($"Phase4 오답: index {i}, 기대={correctLetters[i]}, 실제={inputLetters[i]}");
                return;
            }
        }

        Debug.Log("Phase4 정답! → 단서2 + 퍼즐 완료 (로컬 테스트)");
        capManager.OnClue2Obtained("LocalTest");

        ClearAllSpawners();
        ClearAllGrids();
        SetPhase(PuzzlePhase.Completed);
    }

    // ------------------------------------------------------------------
    // PhotonNetwork / RPC
    // ------------------------------------------------------------------

    [PunRPC]
    void RPC_SubmitPhase2(int[] submittedArrows, PhotonMessageInfo info)
    {
        // 이 RPC는 여주(마스터)만 받는다
        if (!IsPlayer2)
            return;

        Debug.Log($"[RPC_SubmitPhase2] from {info.sender}: {string.Join(",", submittedArrows)}");

        // 1) Phase2 정답 계산용 화살표 배열 만들기
        // 메인 그리드 문자 → 화살표 매핑 
        string[] mainLetters = mainGrid.GetCurrentLettersArrangement();
        int[] correctArrows = new int[9];
        for (int i = 0; i < 9; i++)
        {
            string letter = mainLetters[i];
            if (string.IsNullOrEmpty(letter))
            {
                correctArrows[i] = 0;
                continue;
            }
            correctArrows[i] = capManager.GetArrowForLetter(letter);
        }

        // 2) 비교
        for (int i = 0; i < 9; i++)
        {
            if (submittedArrows[i] != correctArrows[i])
            {
                Debug.Log($"Phase2 오답: index {i}, 기대={correctArrows[i]}, 실제={submittedArrows[i]}");

                // 다른 플레이어한테 오답이라고 알려주기
                photonView.RPC("RPC_Phase2Result", PhotonTargets.All, false);
                return;
            }
        }

        Debug.Log("Phase2 정답! → 단서1 지급 + Phase3로 이동");
        capManager.OnClue1Obtained("Player1&2");

        // Phase 3로 넘어간다고 알림
        photonView.RPC("RPC_Phase2Result", PhotonTargets.All, true);
    }


    [PunRPC]
    void RPC_Phase2Result(bool isCorrect)
    {
        if (!isCorrect)
        {
            Debug.Log("Phase2 오답 (모든 클라이언트 알림)");
            // 필요하면 여기서 UI 효과 (빨간 X, 사운드 등)
            return;
        }

        Debug.Log("Phase2 정답 (모든 클라이언트) → 그리드 초기화 후 Phase3 셋업");

        ClearAllSpawners();
        ClearAllGrids();

        SetPhase(PuzzlePhase.Phase3_Player1_MainArrows);
        SetupPhase3();   // 이 함수는 로컬에서 Phase3 준비 (조각 재스폰 등)
    }


    // 여주 로컬에서만 부르는 내부 함수
    void HandlePhase4_Local()
    {
        if (!IsPlayer2)
            return;

        if (mainGrid == null || inputGrid == null || capManager == null)
        {
            Debug.LogError("Phase4: mainGrid / inputGrid / capManager 중 하나가 비어 있습니다.");
            return;
        }

        // 1) 메인 그리드 화살표 배열
        int[] mainArrows = mainGrid.GetCurrentArrowArrangement();

        // 2) 화살표 → 문자 매핑으로 정답 문자 배열 만들기
        string[] correctLetters = new string[9];
        for (int i = 0; i < 9; i++)
        {
            int arrowID = mainArrows[i];
            correctLetters[i] = (arrowID > 0)
                ? capManager.GetLetterForArrow(arrowID)
                : "";
        }

        // 3) 입력 그리드(여주 시점)의 실제 문자 배열
        string[] inputLetters = inputGrid.GetCurrentLettersArrangement();

        for (int i = 0; i < 9; i++)
        {
            if (inputLetters[i] != correctLetters[i])
            {
                Debug.Log($"Phase4 오답: index {i}, 기대={correctLetters[i]}, 실제={inputLetters[i]}");
                photonView.RPC("RPC_Phase4Result", PhotonTargets.All, false);
                return;
            }
        }

        Debug.Log("Phase4 정답! → 단서2 + 퍼즐 완료");
        capManager.OnClue2Obtained("Player1&2");

        photonView.RPC("RPC_Phase4Result", PhotonTargets.All, true);
    }

    // 모든 클라이언트 공통 처리
    [PunRPC]
    void RPC_Phase4Result(bool isCorrect)
    {
        if (!isCorrect)
        {
            Debug.Log("Phase4 오답 (모든 클라이언트 알림)");
            return;
        }

        Debug.Log("Phase4 정답 (모든 클라이언트) → 퍼즐 종료");

        ClearAllSpawners();
        ClearAllGrids();
        SetPhase(PuzzlePhase.Completed);

        // 여기서 StageManager 등에 "퍼즐 클리어" 알려도 됨
    }

    // ------------------------------------------------------------------
    // 공동 로컬 Phase2 함수
    // ------------------------------------------------------------------

    private void GoToPhase2_Local()
    {
        Debug.Log("Phase2 진입 (로컬) → 스포너/그리드 초기화 후 Phase2 세팅");

        SetPhase(PuzzlePhase.Phase2_Player1_InputArrows);
        SetupPhase2();
    }

    [PunRPC]
    void RPC_GotoPhase2()
    {
        GoToPhase2_Local();
    }

    [PunRPC]
    void RPC_GotoPhase4()
    {
        SetPhase(PuzzlePhase.Phase4_Player2_InputLetters);
        SetupPhase4();
    }

    // ------------------------------------------------------------------
    // F1 치트키 (항상 한 키만 사용)
    // ------------------------------------------------------------------

    void Update()
    {
        //if (!localTestMode)
        //{
        //    return;
        //}

        bool ctrlPressed =
         Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (ctrlPressed && Input.GetKeyDown(KeyCode.G))
        {
            ForceSolveCurrentPhase();
        }
    }

    void ForceSolveCurrentPhase()
    {
        Debug.Log($"[CHEAT] {currentPhase} 강제 클리어");

        switch (currentPhase)
        {
            case PuzzlePhase.Phase1_Player2_MainLetters:
                SetPhase(PuzzlePhase.Phase2_Player1_InputArrows);
                SetupPhase2();
                break;

            case PuzzlePhase.Phase2_Player1_InputArrows:
                capManager.OnClue1Obtained("Cheat");
                ClearAllSpawners();
                ClearAllGrids();
                SetPhase(PuzzlePhase.Phase3_Player1_MainArrows);
                SetupPhase3();
                break;

            case PuzzlePhase.Phase3_Player1_MainArrows:
                SetPhase(PuzzlePhase.Phase4_Player2_InputLetters);
                SetupPhase4();
                break;

            case PuzzlePhase.Phase4_Player2_InputLetters:
                capManager.OnClue2Obtained("Cheat");
                ClearAllSpawners();
                ClearAllGrids();
                SetPhase(PuzzlePhase.Completed);
                break;

            case PuzzlePhase.Completed:
                Debug.Log("퍼즐이 끝났습니다.");
                break;
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ClockPointer : MonoBehaviour
{
    [Header("Hand/Art")]
    public Transform hand;
    [Tooltip("12시 기준 로컬 Z각도 보정")]
    public float zeroAngleZ = 0f;
    [Tooltip("시계방향=+1, 반시계=-1")]
    public int directionSign = 1;


    [Header("Rotate Input")]
    //[Tooltip("A/D 또는 좌/우키 회전 속도(도/초)")]
    //public float keyboardRotateSpeedDeg = 180f;
    [Tooltip("마우스 드래그 민감도(도/픽셀)")]
    public float mouseSensitivityDeg = 0.5f;
    [Tooltip("드래그 중일 때만 회전")]
    public bool requireMouseHold = true;
    [Tooltip("드래그 버튼(0:좌,1:우,2:휠)")]
    public int rotateMouseButton = 0;


    // 드래그 각도 기반 회전용
    private Camera cam;
    private float dragStartMouseAngleDeg;
    private float dragStartZDeg;


    // (옵션) 진짜 바늘을 잡은 경우에만 드래그 시작
    [Header("Grab Rule (Optional)")]
    public bool requireHitHand = true;
    public LayerMask handHitMask = ~0; // 전 레이어 허용


    [Header("Answer & Check")]
    [Tooltip("정답 시퀀스 (예: 12, 3, 7, 2)")]
    public List<int> answerSequence = new List<int>() { 12, 3, 7, 2 };
    [Tooltip("정답 각도 허용 오차(±deg)")]
    public float snapToleranceDeg = 7f;
    [Tooltip("정답 각도에서 잠깐 머문 뒤 클릭하도록 요구할지")]
    public bool requireHoldToLock = true;
    [Tooltip("오차 범위 안에서 유지해야 하는 시간(초)")]
    public float holdToLockSeconds = 0.2f;


    [Header("Confirm")]
    [Tooltip("확정에 사용할 마우스 버튼(0:좌,1:우,2:휠)")]
    public int confirmMouseButton = 0;
    [Tooltip("확정 시 바늘을 목표 각도에 스냅")]
    public bool snapOnConfirm = true;


    [Header("Events")]
    public UnityEvent<int> OnStepMatched;
    public UnityEvent<int> OnStepChanged;
    public UnityEvent OnWrongConfirm;
    public UnityEvent OnAllMatched;

    [Header("SFX")]
    public AudioClip SuccessMatchedSfx;
    public AudioClip AllMatchedSfx;


    // 내부 상태
    private int stepIndex = 0;
    private float currentZ;
    private bool isComplete = false;

    // 정답 각도 근접 유지 체크
    private bool isAligned = false;
    private float alignedTimer = 0f;

    // 드래그 상태
    private bool dragging = false;
    private Vector3 prevMousePos;


    private void Awake()
    {
        var selectSuccess = GetComponent<ClockPointer>();
        selectSuccess.OnStepMatched.AddListener(SuccessMatched);
    }

    void Start()
    {
        cam = Camera.main;

        if (!hand)
        {
            Debug.LogError("[ClockPuzzleDialConfirm] Hand가 비었습니다!");
            enabled = false;
            return;
        }

        currentZ = NormalizeAngle(hand.localEulerAngles.z);
        ApplyRotation();
        OnStepChanged?.Invoke(stepIndex);
    }

    void Update()
    {
        if (isComplete) return;

        HandleRotateInput();
        ApplyRotation();

        UpdateAlignment();
        HandleConfirmInput();
    }

    // --- 입력: 회전 ---
    private void HandleRotateInput()
    {
        bool mouseDown = Input.GetMouseButton(rotateMouseButton);
        if (!requireMouseHold) mouseDown = true;

        if (mouseDown)
        {
            if (!dragging)
            {
                // 드래그 시작 조건: (옵션) 실제로 바늘을 누른 경우만
                if (!IsPointerOnHand()) return;

                dragging = true;

                // 드래그 시작시 기준 각도 저장
                dragStartMouseAngleDeg = GetMouseAngleDeg();
                dragStartZDeg = currentZ; // 현재 바늘 각도 기준
            }
            else
            {
                // 현재 마우스 각도
                float curMouseAngle = GetMouseAngleDeg();

                // 드래그 시작각에서 현재각까지의 변화량(Δθ)
                float delta = Mathf.DeltaAngle(dragStartMouseAngleDeg, curMouseAngle);

                // 화면 각도는 +가 CCW. Unity의 Z+ 회전도 CCW 이므로 그대로 적용
                currentZ = NormalizeAngle(dragStartZDeg - delta);
            }
        }
        else
        {
            dragging = false;
        }
    }


    private void ApplyRotation()
    {
        hand.localEulerAngles = new Vector3(0, 0, currentZ);
    }

    // --- 정답 각도 근접 여부 업데이트 ---
    private void UpdateAlignment()
    {
        if (stepIndex >= answerSequence.Count) return;

        float target = AngleForNumber(answerSequence[stepIndex]);
        float diff = Mathf.DeltaAngle(currentZ, target);

        if (Mathf.Abs(diff) <= snapToleranceDeg)
        {
            isAligned = true;
            alignedTimer += Time.deltaTime;

            // 시각적으로 끌어당기고 싶으면 살짝 보간
            currentZ = Mathf.LerpAngle(currentZ, target, 0.18f);
            ApplyRotation();
        }
        else
        {
            isAligned = false;
            alignedTimer = 0f;
        }
    }

    // --- 확정 입력 처리 ---
    private void HandleConfirmInput()
    {
        if (Input.GetMouseButtonDown(confirmMouseButton))
        {
            // 정답 각도에 충분히 머물렀는지 확인 (옵션)
            if (isAligned && (!requireHoldToLock || alignedTimer >= holdToLockSeconds))
            {
                ConfirmSuccess();
            }
            else
            {
                // 틀린 위치에서 누르면 아무 일도 안 일어남
                OnWrongConfirm?.Invoke();
            }
        }
    }

    private void ConfirmSuccess()
    {
        if (stepIndex >= answerSequence.Count) return;

        int matched = answerSequence[stepIndex];

        if (snapOnConfirm)
        {
            // 확정 순간 정확히 스냅
            currentZ = AngleForNumber(matched);
            ApplyRotation();
        }

        OnStepMatched?.Invoke(matched);
        stepIndex++;

        if (stepIndex >= answerSequence.Count)
        {
            isComplete = true;
            OnAllMatched?.Invoke();
        }
        else
        {
            // 다음 목표 준비
            isAligned = false;
            alignedTimer = 0f;
            OnStepChanged?.Invoke(stepIndex);
        }
    }

    [HideInInspector]
    public void SuccessMatched(int matched)
    {
        if (SuccessMatchedSfx != null)
        {
            AudioSource.PlayClipAtPoint(SuccessMatchedSfx, transform.position);
        }
    }

    [HideInInspector]
    public void AllSuccessMatched(int matched)
    {
        if (AllMatchedSfx != null)
        {
            AudioSource.PlayClipAtPoint(AllMatchedSfx, transform.position);
        }
    }

    // --- 유틸 ---
    // 바늘(피벗) 위치 기준, 화면 공간에서 마우스 각도를 얻는다. (+X=0°, CCW=+)
    private float GetMouseAngleDeg()
    {
        if (!cam) cam = Camera.main;
        Vector3 handScreen = cam.WorldToScreenPoint(hand.position);
        Vector2 dir = (Vector2)(Input.mousePosition - handScreen);
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg; // -180~+180
    }

    private bool IsPointerOnHand()
    {
        if (!requireHitHand) return true;
        if (!cam) cam = Camera.main;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 1000f, handHitMask))
        {
            // hand 자신 또는 hand의 자식이면 잡은 것으로 인정
            return hit.transform == hand || hit.transform.IsChildOf(hand);
        }
        return false;
    }


    private float AngleForNumber(int number)
    {
        // 12시 = 0칸, 1시=1칸 ... 11시=11칸(각 30°)
        int n = number % 12;
        if (n < 0) n += 12;
        if (number == 12) n = 0;

        float angle = zeroAngleZ + directionSign * (n * 30f);
        return NormalizeAngle(angle);
    }

    private float NormalizeAngle(float z)
    {
        z %= 360f;
        if (z < 0f) z += 360f;
        return z;
    }

    // 외부 제어용
    public void SetSequence(List<int> seq)
    {
        answerSequence = seq;
        ResetPuzzle();
    }

    public void ResetPuzzle()
    {
        stepIndex = 0;
        isComplete = false;
        isAligned = false;
        alignedTimer = 0f;
        OnStepChanged?.Invoke(stepIndex);
    }

    public bool IsAlignedNow() => isAligned;
    public int CurrentStepIndex() => stepIndex;
    public bool IsComplete() => isComplete;
}

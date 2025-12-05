//#define CBT_MODE
//#define RELEASE_MODE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Events;
using Unity.VisualScripting;


public class PlayerCtrl : MonoBehaviour
{
    // ====== Inspector ======
    [Header("Refs")]
    private Animator anim;
    private Transform holdPivot;
    private Transform blockCheck;
    private Transform itemCheck;
    private Collider playerCol;
    private InputManager input;
    private InteractionManager interaction;

    [Header("Move")]
    private float walkSpeed = 1f;
    private float runSpeed = 3.5f;
    private float damp = 1f;
    private float turnSpeed = 90f;

    /// <summary>
    /// JK미소녀의 코드
    /// 
    /// UnityEvent onOpened << I를 입력하면 << UICanvas가 SetActive 왔다리갔다리
    /// </summary>
    /// 
    /// 
    [Header("아이템 시스템 연동")]
    public EquippedItemUser equippedUser;

    [Header("인벤토리 캔버스(최상위 GameObject)")]
    public GameObject inventoryCanvas;
    [Header("메뉴 캔버스(최상위 GameObject)")]
    public GameObject menuCanvas;
    // [Header("인벤토리 실제 저장소")]
    // public InventoryStore store;

    public PlayerItemEquick itemEquick;

    [Header("창이 열릴 때 호출")]
    public UnityEvent onOpened;

    [Header("던지기 관련")]
    public Transform throwMuzzle;
    public float throwForce = 12f;

    public StunTalisman pendingTalisman;

    bool isAiming;

    [Header("Jump")]
    private float jumpPower = 300f;
    private float jumpCooldown = 0.25f;
    private float nextJumpTime = 0f;
    private float groundCheckDistance = 0.5f;
    private LayerMask groundMask;

    [Header("Target Check")]
    private float checkDistance = 3f;
    private LayerMask blockMask;
    private LayerMask itemMask;

    [Header("Colors")]
    private Color originColor = Color.white;

    IInputSource inputSrc;

    // ====== Runtime ======
    private PhotonView pv;
    private Transform myTr;
    private Rigidbody rb;
    private Quaternion lastRotation;

    // 입력 상태
    private bool runHeld;

    // 지면/점프
    private bool isGrounded;

    // 상호작용
    private bool isInventory;
    private bool isGrab;          // 블록 타겟 보유 여부
    private bool isToggle;        // 아이템 타겟 보유 여부
    private int grabbingToggle;   // 0/1
    private GameObject grabBlock;
    private GameObject grabItem;


    //위치 정보를 송수신할 때 사용할 변수 선언 및 초기값 설정 
    Vector3 currPos = Vector3.zero;
    Quaternion currRot = Quaternion.identity;

    // ====== Unity ======
    private void Awake()
    {
        PhotonNetwork.logLevel = PhotonLogLevel.Full;
        pv = GetComponent<PhotonView>();

        myTr = GetComponent<Transform>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        lastRotation = transform.rotation;

        StartCoroutine(SetManager());

        if (!holdPivot) Debug.LogWarning("[PlayerCtrl1] holdPivot 미지정");
        if (!blockCheck) Debug.LogWarning("[PlayerCtrl1] blockCheck 미지정");
        if (!itemCheck) Debug.LogWarning("[PlayerCtrl1] itemCheck 미지정");
        if (inventoryCanvas) inventoryCanvas.SetActive(false);
        
     
        if (!equippedUser) equippedUser = GetComponent<EquippedItemUser>();

        //PhotonView Observed Components 속성에 PlayerCtrl(현재) 스크립트 Component를 연결
        pv.ObservedComponents[0] = this;
        //데이타 전송 타입을 설정
        pv.synchronization = ViewSynchronization.UnreliableOnChange;

        if (pv.isMine)
        {
            // 여기쓰면 망가질 거 같은데
            Debug.Log($"InputSource 적용 : {input}");
            inputSrc = new LocalInputSource(input);
            rb.isKinematic = false;
        }
        else
        {
            inputSrc = new NetInputSource();
            rb.isKinematic = false; // 절대 절대 false << 이 새끼 때문에 못 움직임
        }

        currPos = myTr.position;
        currRot = myTr.rotation;

        SetAnimMove(false, 0, 0);
    }

    void OnEnable()
    {
        if (PlayerRegistry.Instance)
            PlayerRegistry.Instance.Register(this);
    }

    void OnDisable()
    {
        if (PlayerRegistry.Instance)
            PlayerRegistry.Instance.Unregister(this);
    }

    private void Start()
    {
        menuCanvas = GameObject.FindGameObjectWithTag("SoundPanel");
        Debug.Log(menuCanvas + " 찾음");
        menuCanvas.SetActive(false);
        //GetComponent<InventoryStore>().Add("Talisman", 5);
    }


    IEnumerator SetManager()
    {
        yield return new WaitForSeconds(1f);

        while (input == null)
        {
            Debug.Log("input manager 못찾았다");
            yield return new WaitForSeconds(1f);
            input = GameObject.Find("InputManager").GetComponent<InputManager>();
        }
        while (interaction == null)
        {
            Debug.Log("interact manager 못찾았다");
            yield return new WaitForSeconds(1f);
            interaction = GameObject.Find("InteractManager").GetComponent<InteractionManager>();
        }

    }

    private void Update()
    {
#if CBT_MODE
        // 지면 체크
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.05f,
                                     Vector3.down, groundCheckDistance, groundMask);

        // 달리기
        runHeld = input.RunPressed;
        anim.SetBool("Run", input.RunPressed);

        // 애니메이터 아이들
        bool anyKey = input.AnyKey;
        // 이동
        float h = input.H;
        float v = input.V;
        SetAnimMove(anyKey, LR: -h, FB: v);

        // 인벤토리
        bool i = input.IsInventory;

        if (i)
        {
            bool next = !inventoryCanvas.activeSelf;
            inventoryCanvas.SetActive(next);
            if (next) onOpened?.Invoke(); // 열릴 때 한 번만 갱신 훅
        }

        // 상호작용 가능, 다른 플레이어가 그 퍼즐을 풀지 않고 있을 때 만 실행! // 준
        if (input.InteractPressed && input.CanInteractNow)
        {
            Debug.Log($"입력 들어감: {gameObject.name}");
            interaction.TryInteractByHover(this.gameObject, input.Hovered, input.HitInfo);
        }


        // @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@

        // // === 부적: 우클릭으로 준비, 좌클릭으로 던지기 ===
        // // ====== StunTalisman 에임 + 던지기 ======
        // if (pendingTalisman != null)
        // {
        //     // 마우스 누름 (에임 시작)
        //     if (Input.GetMouseButtonDown(0))
        //     {
        //         isAiming = true;
        //         Debug.Log("[PlayerCtrl] 부적 에임 시작!");
        //         // Todo : 애니메이션 추가
        //     }

        //     // 마우스 뗌 (던지기!)
        //     if (Input.GetMouseButtonUp(0))
        //     {
        //         if (isAiming)
        //         {
        //             Vector3 dir = (throwMuzzle != null ? throwMuzzle.forward : transform.forward);
        //             Vector3 velocity = dir.normalized * throwForce;
        //             pendingTalisman.Launch(velocity);
        //             pendingTalisman = null;
        //             isAiming = false;
        //             Debug.Log("[PlayerCtrl] 부적 던짐! (에임)");
        //             // anim.SetBool("Aiming", false);
        //         }
        //     }
        // }
        // else
        // {
        //     // 부적 없으면 에임 해제
        //     if (isAiming)
        //     {
        //         isAiming = false;
        //         // anim.SetBool("Aiming", false);
        //     }
        // }

        // StunTalisman 에임 + 던지기 (싱글플레이 전용) ★★★
        if (pendingTalisman != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                isAiming = true;
                Debug.Log("에임 시작! 부적 장전 완료");
            }

            if (Input.GetMouseButtonUp(0) && isAiming)
            {
                Vector3 dir = throwMuzzle ? throwMuzzle.forward : transform.forward;
                Vector3 velocity = dir * throwForce;

                pendingTalisman.Launch(velocity);
                pendingTalisman = null;
                isAiming = false;

                Debug.Log("부적 발사!");
            }
        }
        else
        {
            if (isAiming) isAiming = false;
        }

        // if (isAiming)
        // {
        //     // 카메라 FOV 줄이기 (줌인)
        //     Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, 50f, Time.deltaTime * 5f);

        //     // 조준선 (GUI 또는 LineRenderer)
        //     // 또는 throwMuzzle에 ParticleSystem (에임 이펙트) 활성
        // }
        // else if(pendingTalisman == null && Input.GetMouseButtonUp(0))
        // {
        //     // 카메라 FOV 원상복구
        //     Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, 60f, Time.deltaTime * 5f); 
        // }

        // @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@

        // 그랩 상호작용, 나중에 매니저로 빼려면 빼고
        // Grab();
        // ToggleGrab();
        // CheckTargets();
#else
        if (pv.isMine)
        {
            // 지면 체크
            isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.05f,
                                         Vector3.down, groundCheckDistance, groundMask);

            // 달리기
            runHeld = input.RunPressed;
            anim.SetBool("Run", input.RunPressed);

            // 애니메이터 아이들
            bool anyKey = input.AnyKey;
            // 이동
            float h = input.H;
            float v = input.V;
            SetAnimMove(anyKey, LR: -h, FB: v);

            // 인벤토리
            isInventory = input.IsInventory;

            if (isInventory)
            {
                bool next = !inventoryCanvas.activeSelf;
                inventoryCanvas.SetActive(next);
                if (next) onOpened?.Invoke(); // 열릴 때 한 번만 갱신 훅
            }

            // 일반 상태에서 메뉴창 
            if (input.EscPressed && (GameManager.Instance.CurrentState == GameState.Normal || GameManager.Instance.CurrentState == GameState.InMenu))
            {
                bool next = !menuCanvas.activeSelf;
                menuCanvas.SetActive(next);
                GameManager.Instance.SetState(next ? GameState.InMenu : GameState.Normal);
            }


            // 상호작용 가능, 다른 플레이어가 그 퍼즐을 풀지 않고 있을 때 만 실행! // 준
            if (input.InteractPressed && input.CanInteractNow)
            {
                Debug.Log($"입력 들어감: {gameObject.name}");
                interaction.TryInteractByHover(this.gameObject, input.Hovered, input.HitInfo);
                Debug.Log($"플레이어 현재 상태: {GameManager.Instance.CurrentState}");
            }
            // 상호작용 중 탈출하기
            if (input.EscPressed && GameManager.Instance.CurrentState == GameState.SolvingPuzzle)
            {
                Debug.Log($"퍼즐 탈출!!: {this.name}");
                interaction.TryGetOutInteract();
            }

            // === 부적: 우클릭으로 준비, 좌클릭으로 던지기 ===
            // 우클릭: 현재 장착 아이템 사용 시도 (소모 + 손에 부적 생성)
            if (pendingTalisman != null)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    isAiming = true;
                    pv.RPC(nameof(RpcSetAiming), PhotonTargets.All, true);
                }

                if (Input.GetMouseButtonUp(0) && isAiming)
                {
                    Vector3 dir = throwMuzzle ? throwMuzzle.forward : transform.forward;
                    Vector3 velocity = dir * throwForce;

                    pendingTalisman.Launch(velocity);
                    pv.RPC(nameof(RpcClearPending), PhotonTargets.All);
                    pendingTalisman = null;
                    isAiming = false;
                }
            }

            // 그랩 상호작용, 나중에 매니저로 빼려면 빼고
            // Grab();
            // ToggleGrab();
            // CheckTargets();



            ///////////////////////////////////////////////////////////////////////////
            /// 디버그용, 지울지 남겨둘지 선택할것
            /// ///////////////////////////////////////////////////////////////////////
            if (Input.GetKeyDown(KeyCode.F9) && GameManager.Instance.CurrentState == GameState.Normal)
            {
                // 누가 누르든 다음 씬 전환 요청
                MoveNextScene();
            }
        }
        else
        {
            //원격 플레이어의 아바타를 수신받은 위치까지 부드럽게 이동시키자
            myTr.position = Vector3.Lerp(myTr.position, currPos, Time.deltaTime * 3.0f);
            //원격 플레이어의 아바타를 수신받은 각도만큼 부드럽게 회전시키자
            myTr.rotation = Quaternion.Slerp(myTr.rotation, currRot, Time.deltaTime * 3.0f);

            // 테스트
            // 애니메이션 동기화 필요하면 여기서 뭐라도 해보자

        }


#endif
    }


    private void FixedUpdate()
    {
        if (!input) return;
#if CBT_MODE
#else
        if (!pv.isMine) return;
#endif

        if (GameManager.Instance.CurrentState == GameState.SolvingPuzzle) return;

        // 이동(전/후)
        if (input.MovePressed)
        {
            float v = input.V;
            float speed = (runHeld ? runSpeed : walkSpeed);
            if (runHeld && v < 0)
                speed = 0;

            Vector3 forward = lastRotation * Vector3.forward;
            Vector3 delta = forward * v * speed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + delta);
        }

        UpdateTurnByAD();

        // 점프 주석 임시

        // if (inputSrc.JumpPressed && isGrounded && Time.time >= nextJumpTime)
        // {
        //     rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        //     rb.AddRelativeForce(Vector3.up * jumpPower);                // 그대로 유지
        //     if (anim) anim.SetTrigger("Jump");                          // ?? null 방어코드 쓴거임?

        //     nextJumpTime = Time.time + jumpCooldown;
        // }
    }

    // ====== Move & Anim ======
    private void SetAnimMove(bool anyKey, float LR, float FB)
    {
        if (!anim) return;
        anim.SetBool("anyKey", anyKey);
        anim.SetFloat("MoveLR", LR, damp, Time.deltaTime);
        anim.SetFloat("MoveFB", FB, damp, Time.deltaTime);
    }

    private void UpdateTurnByAD()
    {
        bool right = Input.GetKey(KeyCode.D);
        bool left = Input.GetKey(KeyCode.A);

        float yawDelta = 0f;
        if (right) yawDelta += turnSpeed * Time.deltaTime * (input.V < 0 ? -1 : 1);
        if (left) yawDelta -= turnSpeed * Time.deltaTime * (input.V < 0 ? -1 : 1);  // 후진 시 방향 보정 (차량 후진처럼 바꿈)

        if (right || left)
        {
            float y = transform.rotation.eulerAngles.y + yawDelta;
            transform.rotation = Quaternion.Euler(0f, y, 0f);
            lastRotation = transform.rotation;
        }
    }
    // ====== OnGUI ======
    private void OnGUI()
    {
        if (!pv.isMine) return;
        // 상호작용 가능하면 띄워줌
        if (input.CanInteractNow && GameManager.Instance.CurrentState == GameState.Normal)
        {
            // 가운데 하단에 [E] 라고 띄움
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 32;                           // 글씨 크기
            style.normal.textColor = Color.white;          // 글씨 색
            style.alignment = TextAnchor.LowerCenter;      // 중앙 + 하단 정렬

            // 화면 하단 150px 영역
            float areaHeight = 150f;
            Rect area = new Rect(0, Screen.height - areaHeight, Screen.width, areaHeight);

            GUILayout.BeginArea(area);
            GUILayout.Label("[E]", style);
            GUILayout.EndArea();
        }
    }
    // ====== Interactions ======
    private void Grab()
    {
        if (!isGrab || !grabBlock || !holdPivot) return;

        if (Input.GetKey(KeyCode.E))
        {
            if (grabBlock.transform.parent != holdPivot)
            {
                var rend = grabBlock.GetComponent<Renderer>();
                if (rend != null && rend.material != null)
                    rend.material.color = Color.red;

                var rbBlock = grabBlock.GetComponent<Rigidbody>();
                if (rbBlock)
                {
                    rbBlock.isKinematic = true;
                    rbBlock.velocity = Vector3.zero;
                    rbBlock.angularVelocity = Vector3.zero;
                }

                var colBlock = grabBlock.GetComponent<Collider>();
                if (playerCol && colBlock)
                    Physics.IgnoreCollision(playerCol, colBlock, true);

                grabBlock.transform.SetParent(holdPivot, worldPositionStays: false);
                grabBlock.transform.localPosition = Vector3.zero;
                grabBlock.transform.localRotation = Quaternion.identity;
            }
        }
        else
        {
            isGrab = false;

            if (grabBlock)
            {
                var rend = grabBlock.GetComponent<Renderer>();
                if (rend != null && rend.material != null)
                    rend.material.color = originColor;

                var rbBlock = grabBlock.GetComponent<Rigidbody>();
                if (rbBlock) rbBlock.isKinematic = false;

                var colBlock = grabBlock.GetComponent<Collider>();
                if (playerCol && colBlock)
                    Physics.IgnoreCollision(playerCol, colBlock, false);

                grabBlock.transform.SetParent(null, worldPositionStays: true);
            }

            grabBlock = null;
        }
    }

    private void ToggleGrab()
    {
        if (!isToggle) return;

        // Q로 토글
        if (Input.GetKeyDown(KeyCode.Q))
        {
            grabbingToggle = (grabbingToggle == 0) ? 1 : 0;

            if (grabItem)
            {
                var r = grabItem.GetComponent<Renderer>();
                if (r != null && r.material != null)
                    r.material.color = (grabbingToggle == 1) ? Color.blue : originColor;
            }
        }

        // 잡기 상태
        if (grabbingToggle == 1)
        {
            if (!grabItem || !holdPivot) return;

            if (grabItem.transform.parent != holdPivot)
            {
                var rbItem = grabItem.GetComponent<Rigidbody>();
                if (rbItem)
                {
                    rbItem.isKinematic = true;
                    rbItem.velocity = Vector3.zero;
                    rbItem.angularVelocity = Vector3.zero;
                }

                var colItem = grabItem.GetComponent<Collider>();
                if (playerCol && colItem)
                    Physics.IgnoreCollision(playerCol, colItem, true);

                grabItem.transform.SetParent(holdPivot, worldPositionStays: false);
                grabItem.transform.localPosition = Vector3.zero;
                grabItem.transform.localRotation = Quaternion.identity;
            }
            return;
        }

        // 놓기 상태
        if (grabbingToggle == 0)
        {
            if (!grabItem) { isToggle = false; return; }

            var rbItem = grabItem.GetComponent<Rigidbody>();
            if (rbItem) rbItem.isKinematic = false;

            var colItem = grabItem.GetComponent<Collider>();
            if (playerCol && colItem)
                Physics.IgnoreCollision(playerCol, colItem, false);

            grabItem.transform.SetParent(null, worldPositionStays: true);

            isToggle = false;
            grabItem = null;
        }
    }

    // ====== Raycast (아이템 우선) ======
    private void CheckTargets()
    {
        if (isGrab || isToggle) return;
        if (!blockCheck || !itemCheck) return;

        for (float a = 0f; a <= 45f; a += 5f)
        {
            Vector3 dir = lastRotation * Quaternion.AngleAxis(-a, Vector3.right) * Vector3.forward;

            // 아이템 먼저
            if (Physics.Raycast(itemCheck.position, dir, out var hitItem, checkDistance, itemMask, QueryTriggerInteraction.Ignore))
            {
                Debug.DrawRay(itemCheck.position, dir * checkDistance, Color.green);
                isToggle = true;
                grabItem = hitItem.collider.gameObject;
                return;
            }

            // 아이템이 없을 때만 블록
            if (Physics.Raycast(blockCheck.position, dir, out var hitBlock, checkDistance, blockMask, QueryTriggerInteraction.Ignore))
            {
                isGrab = true;
                grabBlock = hitBlock.collider.gameObject;
            }
        }
    }

    public void MoveNextScene()
    {
        //if (PhotonNetwork.isMasterClient)
        //{
        //    // 방 완료 후 컷씬으로 이동
        //    SceneMoveManager.Instance.LoadScene("scCutscene");
        //}
        //else
        //{
        //    pv.RPC("RequestNextScene", PhotonTargets.MasterClient, null);           // 씬 다중 호출되는지 확인해볼것
        //}
        pv.RPC("RequestNextScene", PhotonTargets.All, null);
    }

    [PunRPC]
    void RequestNextScene()
    {
        // 방 완료 후 컷씬으로 이동
        SceneMoveManager.Instance.LoadScene("scCutscene");
    }

    // ====== Photon RPCs ======

    [PunRPC]
    void RpcSetAiming(bool aiming, PhotonMessageInfo info)
    {
        isAiming = aiming;
        // anim.SetBool("Aiming", aiming);
    }

    [PunRPC]
    void RpcClearPending(PhotonMessageInfo info)
    {
        pendingTalisman = null;
    }

    void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        //로컬, 원격 / 박싱, 언박싱 순서 똑같이 해야함.
        //로컬 플레이어의 위치 정보를 송신
        if (stream.isWriting)
        {
            // 박싱해서 전송

            // 위치/회전
            stream.SendNext(rb.position);
            stream.SendNext(rb.rotation);
            // 입력값(애니 동기화도 이 값으로 유도)
            stream.SendNext(input.AnyKey);
            stream.SendNext(input.H);
            stream.SendNext(input.V);
            stream.SendNext(input.RunPressed);
            stream.SendNext(input.MovePressed);
            // 엣지 펄스: true일 때만 1프레임 전송 → 수신측이 1프레임만 유지
            stream.SendNext(input.JumpPressed);
            stream.SendNext(input.EscPressed);
            stream.SendNext(input.InteractPressed);
            stream.SendNext(input.EquipItemUse);
            stream.SendNext(input.CanInteractNow);
            stream.SendNext(input.IsInventory);

            stream.SendNext(isAiming);

        }
        //원격 플레이어의 동작 정보를 수신
        else
        {
            // 언박싱해서 적용
            // 위치/ 회전
            currPos = (Vector3)stream.ReceiveNext();
            currRot = (Quaternion)stream.ReceiveNext();
            // 입력 파라미터
            bool anyKey = (bool)stream.ReceiveNext();
            float h = (float)stream.ReceiveNext();
            float v = (float)stream.ReceiveNext();

            bool run = (bool)stream.ReceiveNext();
            bool move = (bool)stream.ReceiveNext();
            bool jumpPulse = (bool)stream.ReceiveNext();
            bool esc = (bool)stream.ReceiveNext();
            bool interactPulse = (bool)stream.ReceiveNext();
            bool equipItemUse = (bool)stream.ReceiveNext();
            bool canInteract = (bool)stream.ReceiveNext();
            bool inventory = (bool)stream.ReceiveNext();
            isAiming = (bool)stream.ReceiveNext();

            // 원격 입력 소스에 반영
            (inputSrc as NetInputSource)?.Apply(h, v, anyKey, run, move, jumpPulse, esc, interactPulse, canInteract, inventory, equipItemUse);

            // 애니 값은 여기서도 바로 반영 가능
            if (anim)
            {
                anim.SetBool("Run", run);

                anim.SetBool("anyKey", anyKey);

                // 데드존 적용 (미세 흔들림 방지)
                float hAnim = Mathf.Abs(h) > 0.2f ? -h : 0f;
                float vAnim = Mathf.Abs(v) > 0.2f ? v : 0f;
                anim.SetFloat("MoveLR", hAnim, damp, Time.deltaTime);
                anim.SetFloat("MoveFB", vAnim, damp, Time.deltaTime);

                // 추가 // 원격 점프 모션
                // if (jumpPulse) anim.SetTrigger("Jump");
            }
        }
    }
    [PunRPC]
    void RpcJump()
    {
        // if (anim) anim.SetTrigger("Jump");
    }
}

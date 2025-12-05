using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleLock : MonoBehaviour, IInteractable
{
    [SerializeField] Transform gear1;
    [SerializeField] Transform gear2;
    [SerializeField] Transform gear3;
    [SerializeField] Transform hook;
    [SerializeField] Transform camPos;

    [SerializeField] CameraCtrl cam;

    [SerializeField] LockerDoorMove Locker;
    // 이 부분 추가 

    [Header("퍼즐 보상 위치")]
    [SerializeField] Transform reward1;
    [SerializeField] Transform reward2;

    bool isSolved = false;
    bool nowSolving = false;
    bool onCounter = false;
    bool isRotating = false;

    int gearNum1;
    int gearNum2;
    int gearNum3;

    int numCount = 10;
    int password = 241;

    float timer = 0f;
    [SerializeField] float clearTime;

    PhotonView pv = null;

    private void Awake()
    {
        StartCoroutine(FindCam());

        pv = GetComponent<PhotonView>();
        pv.ObservedComponents[0] = this;
        pv.synchronization = ViewSynchronization.UnreliableOnChange;

        Locker = GameObject.FindAnyObjectByType<LockerDoorMove>();
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

    private void Update()
    {
        // 퍼즐이 이미 풀려있다면 즉시 탈출, 아무 동작 안하겠다는 뜻
        if (isSolved) return;

        if (!isRotating)
        {
            gearNum1 = Rot2Num(gear1);
            gearNum2 = Rot2Num(gear2);
            gearNum3 = Rot2Num(gear3);
        }

        onCounter = (100 * gearNum1 + 10 * gearNum2 + gearNum3 == password);

        if (onCounter)
        {
            // timer 증가 시작
            timer += Time.deltaTime;
        }
        else
        {
            // timer 초기화
            timer = 0f;
        }

        // 타이머 1초 되면 끝
        if (timer >= clearTime)
        {
            PuzzleClear();
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

    // 활성화
    public void Interact(GameObject player)
    {
        //if (!PhotonNetwork.isMasterClient) return;
        if (nowSolving || isSolved) return;
        nowSolving = true;
        pv.RPC("NowSolving", PhotonTargets.AllBuffered, null);      // 둘 중 하나만 하면 되는거 아닌가? 타겟 All 이잖아

        Debug.Log($"nowSolving ? {nowSolving}");

        GameManager.Instance.SetState(GameState.SolvingPuzzle);

        gameObject.GetComponent<BoxCollider>().enabled = false;
        Debug.Log($"카메라 위치 조정 호출됨 : {gameObject.name}");
        cam.SetCamPos(camPos);
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

    public void RotateRight(Transform gear)
    {
        if (!isRotating)
            StartCoroutine(RotateGear(gear, 360f / numCount));
    }

    public void RotateLeft(Transform gear)
    {
        if (!isRotating)
            StartCoroutine(RotateGear(gear, -360f / numCount));
    }

    private IEnumerator RotateGear(Transform gear, float angle)
    {
        isRotating = true;
        SoundManager.manager.SFXPlay(4, this.gameObject.transform.position);
        Quaternion startRot = gear.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, angle, 0f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            gear.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        gear.localRotation = endRot;

        isRotating = false;

        yield break;
    }

    int Rot2Num(Transform gear)
    {
        int num = 0;

        float rot = gear.rotation.eulerAngles.y + (360f / 2 / numCount);

        // 자물쇠 숫자 개수(numCount) 만 정해지면 숫자 계산은 쉽게 계산 가능하다 (대략적)
        num = (int)((numCount * rot / 360) % numCount);

        return num;
    }

    [PunRPC]
    void PuzzleReset()
    {
        gear1.rotation = Quaternion.Euler(0, 0, 0);
        gear2.rotation = Quaternion.Euler(0, 0, 0);
        gear3.rotation = Quaternion.Euler(0, 0, 0);

        gearNum1 = 0;
        gearNum2 = 0;
        gearNum3 = 0;

        nowSolving = false;
    }

    void PuzzleClear()
    {
        SoundManager.manager.SFXPlay(5, this.gameObject.transform.position);
        // 퍼즐별 클리어 시 특수처리할 거 있으면 여기서
        //SolveAction();
        pv.RPC("SolveAction", PhotonTargets.AllBuffered, null);
        Debug.Log("풀었다!");

        // 액자 충돌박스 활성화
        gameObject.GetComponent<BoxCollider>().enabled = true;

        // 게임 상태 전환
        GameManager.Instance.SetState(GameState.Normal);

        // 카메라 이동 >> 플레이어
        cam.CamPosBack();

        Reward();
    }

    [PunRPC]
    void SolveAction()
    {
        // isSolved = true;
        isSolved = true;

        //Quaternion targetRot = hook.transform.rotation * Quaternion.Euler(0f, 45f, 0f);

        //hook.localRotation = Quaternion.Slerp(hook.localRotation, targetRot, Time.deltaTime * 2f);

        // 금고 열기
        Locker.DoorOpen();
    }

    void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // 로컬에서 정보 발사
        if (stream.isWriting)
        {
            // 박싱해서 전송
            // 근데 보낼게 없네??  이미 위에서 다 RPC로 동기화 해버렸는데??
        }
        // 발사된 정보 수신
        else
        {
            //언박싱해서 내부 적용
        }
    }

    // 이 부분 추가  
    void Reward()
    {
        SoundManager.manager.SFXPlay(7, this.gameObject.transform.position);

        PhotonNetwork.Instantiate("ItemPrefabs/Talisman", reward1.position, Quaternion.identity, 0);
        PhotonNetwork.Instantiate("ItemPrefabs/Talisman", reward2.position, Quaternion.identity, 0);
    }


}
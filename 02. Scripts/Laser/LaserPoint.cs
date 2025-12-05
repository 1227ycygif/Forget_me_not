//#define CBT_MODE
#define RELEASE_MODE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]

public class LaserPoint : MonoBehaviour
{
    // 포통
    PhotonView pv;

    // 레이저
    [SerializeField] LineRenderer laser;
    // 내부 저장용
    [SerializeField] List<Vector3> points;
    // 시작점
    [SerializeField] public Vector3 origin;   // 인스펙터에서 확인만

    // 레이저 퍼즐
    [SerializeField] MirrorClearCheck mirror;

    // 라인렌더러에 넣을 Vector3 배열
    Vector3[] lasers;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        laser = GetComponent<LineRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        origin = transform.position;
        points = new List<Vector3>();
        points.Add(origin);
        laser.positionCount = 1;
        laser.SetPosition(0, origin);
    }

    // Update is called once per frame
    void Update()
    {
        // 정답 판정 조회 후 레이저 비활성화
        if(mirror.isSolved == true)
        {
            if (laser.gameObject.activeSelf == false) 
            {
                Debug.Log($"[{this.name}.Update] 이미 비활성화됨");
                return; 
            }

            Debug.Log($"[{this.name}.Update] 정답 판정 확인");
            laser.gameObject.SetActive(false);
        }

        // points 리스트 갱신 시 라인 렌더러에 점 갱신
        if (points.Count != laser.positionCount)
        {
            Debug.Log("시발 갱신");
            pv.RPC(nameof(SetLinePoints), PhotonTargets.All, null);
        }
    }

    // 점 정보 전체 유저와 동기화, Others
    [PunRPC]
    void SetPointsPhoton(List<Vector3> positions)
    {
        Debug.Log("SetPointsPhoton 실행");
        points.Clear();               // 내부 점 정보 전부 정리
        points.AddRange(positions);   // List에 다시 채움
        SetLinePoints();              // 바로 라인 갱신해도 됨
    }

    // 라인 렌더러에 점 설정, 얘를 해야 레이저가 그려짐
    [PunRPC]
    void SetLinePoints()
    {
        if (points == null || points.Count == 0)
        {
                laser.positionCount = 0;
                return;
        }

        laser.positionCount = points.Count;

        var arr = points.ToArray();
        // 0번은 항상 origin으로 고정
        arr[0] = origin;

        laser.SetPositions(arr);
    }

    // 입력한 점을 추가
    public void AddPointList(Vector3 position)
    {
        // 내부 List에 변수 추가
        points.Add(position);

#if CBT_MODE
    // Do Nothing
#else
        // Vector3[] 로 변환해서 한 파라미터로 보냄
        var arr = points.ToArray();
        pv.RPC(nameof(SetPointsPhoton), PhotonTargets.Others, new object[] { arr });
        // 개수가 무조건 바뀐다, Update에서 자동 실행될 예정
        //pv.RPC(nameof(SetLinePoints), PhotonTargets.All, null);
#endif

    }

    // 포인트 전체 설정, 처음부터 설정하는 경우 
    public void ResetAllPointList(params Vector3[] positions)
    {
        // 내부 점 정보 전부 정리
        points.Clear();

        // 내부 List 갱신, 순회문
        foreach (var pos in positions)
        {
                points.Add(pos);
        }

#if CBT_MODE
// Do Nothing
// 점 설정 함수 호출
// 호출 이유 : Update 문 로직 때문, 6개 지우고 6개 입력하면 Update 안탐
SetLinePoints();
#else
        var arr = points.ToArray();
        pv.RPC(nameof(SetPointsPhoton), PhotonTargets.Others, new object[] { arr });
        pv.RPC(nameof(SetLinePoints), PhotonTargets.All, null);
#endif
}

// 포인트 전체 설정, 처음부터 설정하는 경우 (오버로딩)
public void ResetAllPointList(List<Vector3> positions)
{
        points.Clear();

        // 0번은 일단 origin으로 두고, 나머지 경로만 추가
        points.Add(origin);
        points.AddRange(positions);

#if CBT_MODE
SetLinePoints();
#else
        pv.RPC(nameof(SetPointsPhoton), PhotonTargets.Others, positions.ToArray());
        pv.RPC(nameof(SetLinePoints), PhotonTargets.All, null);
#endif
    }

    // 마지막 점 제거 함수
    public void RemoveLastPoints()
{
        // 지정 인덱스 제거 함수 : 마지막 인덱스 입력
        points.RemoveAt(points.Count - 1);

#if CBT_MODE
// Do Nothing
#else
        pv.RPC(nameof(SetPointsPhoton), PhotonTargets.Others, points);
        // 개수가 무조건 바뀐다, Update에서 자동 실행될 예정
        //pv.RPC(nameof(SetLinePoints), PhotonTargets.All, null);
#endif
    }

    // 입력 인덱스부터 끝까지 날리는 함수
    // 어느 인덱스부터 지워야하는지 알고 있을 경우 사용 가능
    public void RemoveSelect2Last(int index)
    {
        // 지정 인덱스 범위 제거 함수 : 입력 ~ 마지막
        points.RemoveRange(index, points.Count - 1);

#if CBT_MODE
// Do Nothing
#else
        var arr = points.ToArray();
        pv.RPC(nameof(SetPointsPhoton), PhotonTargets.Others, new object[] { arr });

        // 개수가 무조건 바뀐다, Update에서 자동 실행될 예정
        //pv.RPC(nameof(SetLinePoints), PhotonTargets.All, null);
#endif
    }


}

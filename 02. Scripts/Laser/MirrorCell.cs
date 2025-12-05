using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 거울의 4방향 상태
/// </summary>
public enum MirrorState
{
    RightUp,    // /
    RightDown,  // \
    LeftDown,   // /
    LeftUp      // \
}

[RequireComponent(typeof(PhotonView))]

/// <summary>
/// 각 칸(Node) 위에 붙여서 "여기는 거울이다"를 표시하는 컴포넌트.
/// - rot : 4가지 회전 상태
/// - absorbBack : 앞/뒷면이 아닌 방향에서 맞으면 레이저를 흡수할지 여부 
///                혹시 몰라서 일단은 냅둠.
/// </summary>
public class MirrorCell : MonoBehaviour
{
    public MirrorState mirrorState = MirrorState.RightUp;

    [Header("반사 가능한 두 방향 이외에서 맞으면 레이저를 흡수할지 여부")]
    public bool absorbBack = true;

    [SerializeField] PhotonView pv;
    [SerializeField] Node node;
    SpriteRenderer rend;

    [Header("거울 모델(판때기) Transform")]
    public Transform visual;

    Quaternion targetAngle;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
        //node = GetComponent<Node>();

        // 지정 안 했으면 자식 중 첫 번째를 visual로 사용
        if (!visual && transform.childCount > 0)
            visual = transform.GetChild(0);
        rend = GetComponent<SpriteRenderer>();

        // 거울임을 표시하기 위해 NodeType 변경 (옵션)
        node.SetNodeType(NodeType.Mirror);

        // 테스트용 색 (나중에 스프라이트 교체하면 지워도 됨)
        if (rend)
            rend.color = Color.cyan;

        UpdateTargetAngle();
    }

    void Update()
    {
        // transform.rotation = targetAngle;
        UpdateTargetAngle();
        ApplyRotation();
    }

    /// <summary>
    /// inDir 방향으로 들어온 레이저가 이 거울에서 반사될 수 있는지 확인.
    /// - 반사 가능하면 true와 outDir 반환
    /// - 단면/뒷면 등으로 인해 흡수되면 false
    /// </summary>
    public bool TryReflect(LaserDir inDir, out LaserDir outDir)
    {
        // 기본값
        outDir = inDir;

        switch (mirrorState)
        {
            //  - Right -> Up
            //  - Down  -> Left
            case MirrorState.LeftUp:
                if (inDir == LaserDir.Right) { outDir = LaserDir.Up; return true; }
                if (inDir == LaserDir.Down) { outDir = LaserDir.Left; return true; }
                break;
            //  - Left  -> Up
            //  - Down  -> Right
            case MirrorState.RightUp:
                if (inDir == LaserDir.Left) { outDir = LaserDir.Up; return true; }
                if (inDir == LaserDir.Down) { outDir = LaserDir.Right; return true; }
                break;
            //  - Left  -> Down
            //  - Up    -> Right
            case MirrorState.RightDown:
                if (inDir == LaserDir.Left) { outDir = LaserDir.Down; return true; }
                if (inDir == LaserDir.Up) { outDir = LaserDir.Right; return true; }
                break;
            //  - Right -> Down
            //  - Up    -> Left
            case MirrorState.LeftDown:
                if (inDir == LaserDir.Right) { outDir = LaserDir.Down; return true; }
                if (inDir == LaserDir.Up) { outDir = LaserDir.Left; return true; }
                break;
        }
        return false;
    }

    /// <summary>
    /// 거울을 시계 방향으로 1회 회전
    /// </summary>
    public void TurnOnce()
    {
#if CBT_MODE
        RotateNext();
#else
        // 이건 버튼을 누가 눌러서 호출했든, 버튼이 눌렸다면 모두 다 돌아야하므로
        // mirrorState = (mirrorState == MirrorState.LeftUp) ? MirrorState.RightUp : mirrorState + 1;
        pv.RPC(nameof(RotateNext), PhotonTargets.All, null);
#endif
    }

    [PunRPC]
    void RotateNext()
    {
        // 거울 상태 변경
        mirrorState = (mirrorState == MirrorState.LeftUp) ? 0 : mirrorState + 1;
        //UpdateTargetAngle();
    }

    /// <summary>
    /// 현재 mirrorState에 따라 목표 회전 각도를 설정합니다.
    /// </summary>
    // void UpdateTargetAngle()
    // {
    //     switch (mirrorState)
    //     {
    //         case MirrorState.RightUp:
    //             targetAngle = Quaternion.Euler(0, 45, 0);
    //             break;
    //         case MirrorState.RightDown:
    //             targetAngle = Quaternion.Euler(0, 135, 0);
    //             break;
    //         case MirrorState.LeftDown:
    //             targetAngle = Quaternion.Euler(0, 225, 0);
    //             break;
    //         case MirrorState.LeftUp:
    //             targetAngle = Quaternion.Euler(0, 315, 0);
    //             break;
    //     }
    // }
    void UpdateTargetAngle()
    {
        float y = 0f;
        switch (mirrorState)
        {
            case MirrorState.RightUp: y = 45f; break;
            case MirrorState.RightDown: y = 135f; break;
            case MirrorState.LeftDown: y = 225f; break;
            case MirrorState.LeftUp: y = 315f; break;
        }
        targetAngle = Quaternion.Euler(0, y, 0);
    }
    void ApplyRotation()
    {
        if (!visual) return;
        visual.localRotation = targetAngle;
    }
}




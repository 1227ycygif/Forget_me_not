using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CAPPieceSpawner : MonoBehaviour
{
    [Header("References")]
    public CAPGrid capGrid;
    public CAP_config cap_config;

    [Header("Prefab")]
    public GameObject capPiecePrefab;

    [Header("Settings")]
    public bool isPlayer1View = false;
    public bool autoSpawnOnStart = false;

    [Header("Spawn Rotation")]
    public Vector3 spawnRotationEuler = Vector3.zero;

    [Header("Wait Area")]
    public Vector3 waitAreaStart = new Vector3(-4f, 0f, -3f);
    public float waitAreaSpacing = 1.3f;

    [Header("Created Pieces")]
    public List<CAPPiece> spawnedPieces = new List<CAPPiece>();

    void Start()
    {
        if (autoSpawnOnStart)
        {
            SpawnWaitPieces();
        }
    }

    /// <summary>
    /// 대기 줄에 9개 조각 생성
    /// </summary>
    public void SpawnWaitPieces()
    {
        if (capGrid != null)
        {
            for (int i = 0; i < 9; i++)
            {
                capGrid.RegisterPiece(i, null);
            }
        }
        spawnedPieces.Clear();

        if (PhotonNetwork.inRoom)
        {
            // Phase별 생성 권한 체크
            bool shouldSpawn = false;

            // 화살표 조각(isPlayer1View=true)은 남주가 생성
            // 문자 조각(isPlayer1View=false)은 여주가 생성
            if (isPlayer1View && !PhotonNetwork.isMasterClient)
                shouldSpawn = true;
            else if (!isPlayer1View && PhotonNetwork.isMasterClient)
                shouldSpawn = true;

            if (!shouldSpawn)
                return;
        }

        if (capPiecePrefab == null || cap_config == null || capGrid == null)
        {
            Debug.LogError("필수 참조가 설정되지 않았습니다!");
            return;
        }
        Debug.Log("=== 대기 조각 생성 시작 ===");

        for (int i = 0; i < 9; i++)
        {
            // CAPManager 매핑
            int arrowId = CAPManager.Instance.currentMapping[i].arrowID;
            string letterId = CAPManager.Instance.currentMapping[i].letterID;
            Debug.Log($"[SpawnWait] Piece {i}: arrow={arrowId}, letter={letterId}, 맵핑 잘 되는지 확인하자");

            // 2) 새 데이터 생성 (cap_config 안에 뭐가 있든 상관없이 이 값으로 사용)
            CAPPieceData data = new CAPPieceData();
            data.pieceID = i;
            data.arrowID = arrowId;
            data.letterID = letterId;
            data.gridIndex = -1;      // 대기 줄

            // 3) 대기 위치 계산
            Vector3 waitPos = waitAreaStart + new Vector3(i * waitAreaSpacing, 0, 0);

            // 4) 프리팹 생성
            Quaternion rot = Quaternion.Euler(spawnRotationEuler);
            GameObject pieceObj;

            if (PhotonNetwork.inRoom)
            {
                // 1) 네트워크 생성
                pieceObj = PhotonNetwork.Instantiate(
                    "CAPPiece",   // Resources 폴더 안
                    waitPos,
                    rot,
                    0
                );
                pieceObj.name = $"CAPPiece_{i}";

                PhotonView pv = pieceObj.GetComponent<PhotonView>();
                pv.RPC("InitializePieceData", PhotonTargets.AllBuffered,
                       i, arrowId, letterId);

                CAPPiece piece = pieceObj.GetComponent<CAPPiece>();
                if (piece != null)
                {
                    spawnedPieces.Add(piece);
                }
            }
            else
            {
                // 로컬 플레이 테스트용
                pieceObj = Instantiate(capPiecePrefab, waitPos, rot, transform);
                pieceObj.name = $"CAPPiece_{i}";

                // 5) 초기화
                CAPPiece piece = pieceObj.GetComponent<CAPPiece>();
                bool localIsPlayer1View = GetLocalViewMode();
                piece.InitializeAtWaitPosition(data, localIsPlayer1View, capGrid);
                //ApplyMappingToPiece(piece);
                Debug.Log($"[After Mapping] Piece {i}: arrow={piece.pieceData.arrowID}, letter={piece.pieceData.letterID}");
                spawnedPieces.Add(piece);
            }
        }
        Debug.Log($"=== 대기 조각 생성 완료: {spawnedPieces.Count}개 ===");
    }

    private void ApplyMappingToPiece(CAPPiece piece)
    {
        if (piece == null) return;
        if (CAPManager.Instance == null || CAPManager.Instance.currentMapping == null) return;

        // pieceData.pieceID 를 인덱스로 사용 (0~8 or 1~9 사용하는지에 따라 조정)
        int id = piece.pieceData.pieceID;   // 만약 1~9라면: int id = piece.pieceData.pieceID - 1;

        if (id < 0 || id >= CAPManager.Instance.currentMapping.Length)
        {
            Debug.LogWarning($"[CAPPieceSpawner] 잘못된 pieceID: {id}");
            return;
        }

        var map = CAPManager.Instance.currentMapping[id];

        // 매핑에서 가져온 화살표/문자 ID를 조각 데이터에 반영
        piece.pieceData.arrowID = map.arrowID;
        piece.pieceData.letterID = map.letterID;

        // 뷰 타입은 CAPPiece 내부가 알고 있으니까 거기서 다시 그리게 시킴
        piece.RefreshVisual();
    }

    /// <summary>
    /// 이 클라이언트가 남주인지 여주인지에 따라
    /// 화살표/문자 뷰를 결정한다.
    /// </summary>
    private bool GetLocalViewMode()
    {
        // 포톤 방 안이 아니라면(싱글 플레이 등) 기존 인스펙터 값 사용
        if (!PhotonNetwork.inRoom)
        {
            return isPlayer1View;
        }

        // 규칙: MasterClient = Player2(여주) = 문자뷰(false)
        //       그 외 = Player1(남주) = 화살표뷰(true)
        bool isMaster = PhotonNetwork.isMasterClient;
        bool isPlayer1 = !isMaster;

        return isPlayer1;   // true면 화살표, false면 문자
    }

    /// <summary>
    /// 모든 조각 삭제
    /// </summary>
    public void ClearAllPieces()
    {
        foreach (var piece in spawnedPieces)
        {
            if (piece != null)
            {
                // 네트워크 오브젝트는 PhotonNetwork.Destroy 사용
                if (PhotonNetwork.inRoom && piece.photonView != null)
                {
                    PhotonNetwork.Destroy(piece.gameObject);
                }
                else
                {
                    Destroy(piece.gameObject);
                }
            }
        }
        spawnedPieces.Clear();

        // 그리드도 전부 비우기
        if (capGrid != null)
        {
            for (int i = 0; i < 9; i++)
            {
                capGrid.RegisterPiece(i, null);
            }
        }
    }

    public void ClearWaitingPiecesOnly()
    {
        if (spawnedPieces == null) return;

        for (int i = spawnedPieces.Count - 1; i >= 0; i--)
        {
            var piece = spawnedPieces[i];

            if (piece == null)
            {
                spawnedPieces.RemoveAt(i);
                continue;
            }

            // gridIndex < 0 이면 아직 그리드에 안 올라간 "대기 줄 조각"이라고 가정
            if (piece.pieceData == null || piece.pieceData.gridIndex < 0)
            {
                Destroy(piece.gameObject);
                spawnedPieces.RemoveAt(i);
            }
        }
    }
}

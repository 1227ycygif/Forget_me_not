using System;
using System.Collections.Generic;

// Firebase 저장/불러오기 전용 클래스
[Serializable]
public class FirebasePlayerData
{
    public string uid;

    // 인벤토리 (Dictionary는 Firebase에서 자동 변환)
    public Dictionary<string, int> femaleInventory = new Dictionary<string, int>();
    public Dictionary<string, int> maleInventory = new Dictionary<string, int>();

    // 컷씬 컬렉션 (비트마스크를 int로 저장)
    public int collectionBits;

    // 현재 세이브 포인트
    public int currentSavePoint;

    // 클리어한 퍼즐들 (List로 저장 - Firebase 효율적)
    public List<string> clearedPuzzlesList = new List<string>();

    public FirebasePlayerData() { }

    // PlayerData에서 FirebasePlayerData로 변환
    public FirebasePlayerData(PlayerData original)
    {
        this.uid = original.uid;

        // 인벤토리 복사
        this.femaleInventory = new Dictionary<string, int>(original.femaleInventory);
        this.maleInventory = new Dictionary<string, int>(original.maleInventory);

        this.collectionBits = original.collectionBits;
        this.currentSavePoint = original.stageProgress.currentSavePoint;

        // Dictionary를 List로 변환 (true인 것만 저장)
        this.clearedPuzzlesList = new List<string>();
        foreach (var puzzle in original.stageProgress.clearedPuzzles)
        {
            if (puzzle.Value)  // true인 것만 저장
            {
                this.clearedPuzzlesList.Add(puzzle.Key);
            }
        }
    }

    // FirebasePlayerData를 PlayerData로 변환
    public PlayerData ToPlayerData()
    {
        PlayerData data = new PlayerData(this.uid);

        // 인벤토리 복사
        data.femaleInventory = new Dictionary<string, int>(this.femaleInventory);
        data.maleInventory = new Dictionary<string, int>(this.maleInventory);

        data.collectionBits = this.collectionBits;
        data.stageProgress.currentSavePoint = this.currentSavePoint;

        // List를 Dictionary로 변환
        data.stageProgress.clearedPuzzles = new Dictionary<string, bool>();
        foreach (string puzzleKey in this.clearedPuzzlesList)
        {
            data.stageProgress.clearedPuzzles[puzzleKey] = true;
        }

        return data;
    }
}
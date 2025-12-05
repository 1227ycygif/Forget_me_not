using System;
using System.Collections.Generic;

[Serializable]
public class PlayerData
{
    public string uid; // Firebase UID

    public Dictionary<string, int> femaleInventory = new();
    public Dictionary<string, int> maleInventory = new();

    public StageProgress stageProgress = new();

    // 고정 개수 컬렉션 비트마스크
    public int collectionBits;

    public PlayerData() { }             // JSON 역직렬화용 기본 생성자
    public PlayerData(string uid) { this.uid = uid; }
}

[Serializable]
public class StageProgress
{
    public int currentSavePoint;
    public Dictionary<string, bool> clearedPuzzles = new();

    public StageProgress() { }
    public StageProgress(int savePoint) { currentSavePoint = savePoint; }
}

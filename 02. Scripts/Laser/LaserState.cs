using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserState
{
    // 현재 레이저가 위치한 노드
    public Node node;

    // 현재 진행 방향
    public LaserDir dir;

    // 시작에서 여기까지 누적 비용
    public int g;

    // 여기서 목표까지 추정 비용(휴리스틱)
    public int h;

    // 경로 복원용 부모
    public LaserState parent;

    // A* 우선순위용 F = G + H
    // 이었지만 굳이 쓸 필요가 없어보임
    /// <summary>
    /// A* 알고리즘으로 길을 찾을 필요가 없어졌기 때문.
    /// 방향이 정해져서 직선으로만 이동하는데 다익스트라 계산도 필요없고 
    /// 휴리스틱 계산할 필요도 사라짐
    /// </summary>
    public int f => g + h;
}

public enum LaserDir
{
    Up,
    Down,
    Left,
    Right
}
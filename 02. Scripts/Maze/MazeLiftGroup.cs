using UnityEngine;

public class MazeLiftGroup : MonoBehaviour
{
    [Header("이 그룹에 속한 큐브들 (LiftPlatform)")]
    [SerializeField] private LiftPlatform[] lifts;

    private int currentIndex = -1;

    /// <summary>
    /// index 번째 큐브만 올리고 나머지는 내리는 함수
    /// (마스터에서만 호출된다고 가정)
    /// </summary>
    public void Activate(int index)
    {
        if (lifts == null || lifts.Length == 0)
            return;
        if (index < 0 || index >= lifts.Length)
            return;

        for (int i = 0; i < lifts.Length; i++)
        {
            var lift = lifts[i];
            if (!lift) continue;

            if (i == index)
            {
                lift.MoveUp();
            }
            else
            {
                lift.MoveDown();
            }
        }

        currentIndex = index;
    }

    /// <summary>
    /// 이 그룹의 모든 큐브를 올리고 싶을 때 (도착 버튼용)
    /// </summary>
    public void OpenAll()
    {
        if (lifts == null) return;

        for (int i = 0; i < lifts.Length; i++)
        {
            var lift = lifts[i];
            if (!lift) continue;

            lift.MoveUp();
        }

        currentIndex = -1; // "한 개만 활성" 상태 해제
    }
}
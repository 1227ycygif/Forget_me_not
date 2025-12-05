using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public enum ControlTarget
{
    Master,
    BGM,
    SFX
}

public class SoundCtrlButton : MonoBehaviour
{
    [Header("볼륨 표시 텍스트")]
    [SerializeField]
    Text txtVolume;

    [Header("볼륨 옵션")]
    [SerializeField]
    ControlTarget controlTarget;

    // 볼륨 조절 시 최대, 최소 볼륨, 필요하다면 조정할 것
    float minDb = -80f;
    float maxDb = 20f;

    // 볼륨 조정 스텝
    int volume = 5;

    // 설정한 믹서 파라미터 이름
    string exposedParam;

    // Start is called before the first frame update
    void Start()
    {
        switch (controlTarget)
        {
            case ControlTarget.Master:
                exposedParam = "Master";
                break;
            case ControlTarget.BGM:
                exposedParam = "BGM";
                break;
            case ControlTarget.SFX:
                exposedParam = "SFX";
                break;
        }

        // 시작하면 볼륨 단계부터 설정
        volume = SoundManager.manager.GetVolumeStep(exposedParam);

        // 그 다음엔 그 단계에 맞춰 볼륨도 조정
        SoundManager.manager.mixer.SetFloat(exposedParam, dBVolumeCalc(volume));

        // 텍스트 갱신
        txtVolume.text = volume.ToString();
    }

    private void OnEnable()
    {
        // 시작하면 볼륨 단계부터 설정
        volume = SoundManager.manager.GetVolumeStep(exposedParam);

        // 그 다음엔 그 단계에 맞춰 볼륨도 조정
        SoundManager.manager.mixer.SetFloat(exposedParam, dBVolumeCalc(volume));

        // 텍스트 갱신
        txtVolume.text = volume.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        // 볼륨 조정 단계를 수시로 갱신
        SoundManager.manager.SetVolumeStep(exposedParam, volume);
    }

    // 볼륨 업
    // 클라마다 볼륨을 다르게 잡아야하니까, 얘는 포톤 안해도 되자나, 그치?
    public void OnClickVolumeDown()
    {
        // 볼륨 설정
        volume--;
        Debug.Log($"볼륨 줄였음 : {volume}");
        volume = Mathf.Clamp(volume, 0, 10);
        Debug.Log($"볼륨 줄였음(clamp) : {volume}");
        SoundManager.manager.mixer.SetFloat(exposedParam, dBVolumeCalc(volume));

        // 텍스트 갱신
        txtVolume.text = volume.ToString();
    }

    // 볼륨 다운
    // 위와 동일
    public void OnClickVolumeUp()
    {
        // 볼륨 설정
        volume++; Debug.Log($"볼륨 늘였음 : {volume}");
        volume = Mathf.Clamp(volume, 0, 10);
        Debug.Log($"볼륨 늘였음(clamp) : {volume}");
        SoundManager.manager.mixer.SetFloat(exposedParam, dBVolumeCalc(volume));

        // 텍스트 갱신
        txtVolume.text = volume.ToString();
    }

    float dBVolumeCalc(int step)
    {
        if (step == 0)
            return minDb; // 예: -80f

        float t = (float)step / 10f;                // 0.1 ~ 1.0
        float dB = Mathf.Log10(t) * 20f;     // -20 ~ 0 사이 값

        // 필요하면 여기서 min/max 한 번 더 제한
        dB = Mathf.Clamp(dB, minDb, maxDb);  // minDb=-80, maxDb=0 같은 값

        Debug.Log($"현재 볼륨 단계 : {step} / 변환 데시벨 : {dB}");

        return dB;
    }
}

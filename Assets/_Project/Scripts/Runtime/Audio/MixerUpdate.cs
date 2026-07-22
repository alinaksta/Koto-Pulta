using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class MixerUpdate : MonoBehaviour
{
    private const string SfxParameter = "SFXVolume";
    private const string MusicParameter = "MusicVolume";
    private const string SfxPrefsKey = "Audio.SFXVolume";
    private const string MusicPrefsKey = "Audio.MusicVolume";

    public AudioMixer mixer;

    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _musicSlider;

    private void Awake()
    {
        CacheSliders();
        LoadAndApplyValues();
    }

    private void OnEnable()
    {
        CacheSliders();
        RefreshSliderValues();
    }

    public void UpdSFX(float val)
    {
        SetMixerValue(SfxParameter, SfxPrefsKey, val);
    }

    public void UpdMus(float val)
    {
        SetMixerValue(MusicParameter, MusicPrefsKey, val);
    }

    public static void ApplySavedValues(AudioMixer targetMixer)
    {
        if (targetMixer == null)
            return;

        ApplySavedValue(targetMixer, SfxParameter, SfxPrefsKey);
        ApplySavedValue(targetMixer, MusicParameter, MusicPrefsKey);
    }

    private void CacheSliders()
    {
        if (_sfxSlider != null && _musicSlider != null)
            return;

        foreach (Slider slider in GetComponentsInChildren<Slider>(true))
        {
            if (_sfxSlider == null && slider.name.IndexOf("SFX", System.StringComparison.OrdinalIgnoreCase) >= 0)
                _sfxSlider = slider;

            if (_musicSlider == null && slider.name.IndexOf("Music", System.StringComparison.OrdinalIgnoreCase) >= 0)
                _musicSlider = slider;
        }
    }

    private void LoadAndApplyValues()
    {
        if (mixer == null)
            return;

        ApplySavedValues(mixer);
        RefreshSliderValues();
    }

    private static void ApplySavedValue(AudioMixer targetMixer, string parameter, string prefsKey)
    {
        if (!PlayerPrefs.HasKey(prefsKey))
            return;

        targetMixer.SetFloat(parameter, PlayerPrefs.GetFloat(prefsKey));
    }

    private void RefreshSliderValues()
    {
        RefreshSliderValue(_sfxSlider, SfxParameter);
        RefreshSliderValue(_musicSlider, MusicParameter);
    }

    private void RefreshSliderValue(Slider slider, string parameter)
    {
        if (slider == null || mixer == null || !mixer.GetFloat(parameter, out float value))
            return;

        slider.SetValueWithoutNotify(value);
    }

    private void SetMixerValue(string parameter, string prefsKey, float value)
    {
        if (mixer == null)
            return;

        mixer.SetFloat(parameter, value);
        PlayerPrefs.SetFloat(prefsKey, value);
        PlayerPrefs.Save();
    }
}

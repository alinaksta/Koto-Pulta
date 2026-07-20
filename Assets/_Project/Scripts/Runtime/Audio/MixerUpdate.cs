using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

public class MixerUpdate : MonoBehaviour
{
    public AudioMixer mixer;
    public void UpdSFX(float val)
    {
        mixer.SetFloat("SFXVolume", val);
    }
    public void UpdMus(float val)
    {
        mixer.SetFloat("MusicVolume", val);
    }
}
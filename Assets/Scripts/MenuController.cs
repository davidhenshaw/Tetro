using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public AudioMixerGroup _masterMixer;

    void SetMixerFloat(string property, float value)
    {
        _masterMixer.audioMixer.SetFloat(property, value);
    }

    public void SetMasterVolume(float value)
    {
        _masterMixer.audioMixer.SetFloat("Vol_Master", value);
    }

    public void SetSFXVolume(float value)
    {
        _masterMixer.audioMixer.SetFloat("Vol_SFX", value);
    }

    public void SetMusicVolume(float value)
    {
        _masterMixer.audioMixer.SetFloat("Vol_Music", value);
    }
}

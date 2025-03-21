using Fujin.Constants;
using System;
using UnityEngine;

namespace Fujin.Data
{
    [CreateAssetMenu(fileName = "NewSoundInfo", menuName = "Fujin/SoundInfo")]
    public class SoundInfo : ScriptableObject
    {
        public string fileName = "*Insert a unique name here*";
        // public string filePath;
        public ClipType clipType = ClipType.Oneshot;
        [Range(0.1f, 1f)] public float volumeScale = 1f;
        [Range(0.1f, 1f)] public float pitchScale = 1f;
        public int priority = 10;
        public AudioClip audioClip;
    }
}

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

        // public void Initialize(string fileName, string filePath, string clipType, bool affectedByGlobalModifier, 
        //     float volumeScale, float pitchScale, int priority)
        // {
        //     this.fileName = fileName;
        //     this.filePath = filePath;
        //     this.clipType = (ClipType)Enum.Parse(typeof(ClipType), clipType);
        //     this.affectedByGlobalModifier = affectedByGlobalModifier;
        //     this.volumeScale = volumeScale;
        //     this.pitchScale = pitchScale;
        //     this.priority = priority;
        // }
    }
}

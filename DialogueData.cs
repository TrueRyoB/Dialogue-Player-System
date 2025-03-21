using System;
using UnityEngine;
using System.Globalization;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

namespace Fujin.Data
{
    public class DialogueData
    {
        public string Text;
        public string NextID;

        public bool IsInitialized => _isInitialized;
        public bool HasBGM => BGMInfo != null;
        public bool HasImage => Image != null;
        public bool HasColor => _hasColor;
        
        private bool _isInitialized;
        private bool _hasColor;

        [NonSerialized] public SoundInfo BGMInfo;
        [NonSerialized] public Sprite Image;
        [NonSerialized] public Color Color;

        public async Task Initialize(string[] row)
        {
            if (row == null)
            {
                Debug.LogError("Error: row is null!");
                return;
            }
            
            // insert necessary information
            Text = row[1];
            NextID = row[4];
            
            // Load the image sprite
            if (!String.IsNullOrEmpty(row[2]))
            {
                var handle = Addressables.LoadAssetAsync<Sprite>(row[2]);
                await handle.Task;
                
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    Image = handle.Result;
                }
                else
                {
                    Debug.LogErrorFormat("Loading Image failed: {0}", row[2]);
                }
            }

            // Load the BGM scriptable object
            if (!String.IsNullOrEmpty(row[3]))
            {
                var handle = Addressables.LoadAssetAsync<SoundInfo>(row[3]);
                await handle.Task;
                
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    BGMInfo = handle.Result;
                }
                else
                {
                    Debug.LogErrorFormat("Loading BGM failed: {0}", row[3]);
                }
            }
            
            // Parse the color code
            if (!String.IsNullOrEmpty(row[5]) && TryParseColoringKey(row[5], out var vec4))
            {
                Color = vec4;
                _hasColor = true;
            }
            else
            {
                _hasColor = false;
            }
            
            _isInitialized = true;
        }
        
        public void Reset()
        {
            BGMInfo = null;
            Image = null;
            _hasColor = false;
            _isInitialized = false;
        }

        private bool TryParseColoringKey(string key, out Vector4 vec)
        {
            vec = Vector4.zero;
            
            key = key.Trim('(', ')');
            string[] values = key.Split(',');
                
            if (values.Length != 4)
            {
                Debug.LogError("Error: Invalid format. Expected (x, y, z, w)");
                return false;
            }
            
            try
            {
                int x = int.Parse(values[0].Trim(), CultureInfo.InvariantCulture);
                int y = int.Parse(values[1].Trim(), CultureInfo.InvariantCulture);
                int z = int.Parse(values[2].Trim(), CultureInfo.InvariantCulture);
                int w = int.Parse(values[3].Trim(), CultureInfo.InvariantCulture);
                
                vec = new Vector4(x / 255f, y / 255f, z / 255f, w / 100f);

                if (IsInRange01(vec.x) && IsInRange01(vec.y) && IsInRange01(vec.z) && IsInRange01(vec.w))
                {
                    return true;
                }
                else
                {
                    Debug.LogError("Error: one or more values are out of range.");
                    return false;
                }
            }
            catch (FormatException)
            {
                Debug.LogError("Error: Failed to parse numbers.");
                return false;
            }
        }
        
        private bool IsInRange01(float input) => input is >= 0f and <= 1f;
    }
}
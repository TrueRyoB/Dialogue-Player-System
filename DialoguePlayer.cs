using Fujin.Data;
using System.Threading.Tasks;
using System;
using Fujin.Constants;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Fujin.System
{
    public class DialoguePlayer : MonoBehaviour
    {
        // Singleton for managing an outside call
        public static DialoguePlayer Instance => _instance;
        private static DialoguePlayer _instance;
        private static bool _isLoadingOrLoaded;
        private static string[][] _dialogueDataMatrix;
        
        [SerializeField] private Image pictureHolder;
        [SerializeField] private Image backgroundHolder;
        [SerializeField] private TextMeshProUGUI textHolder;
        [SerializeField] private AudioSource audioSource;

        public static async Task LoadInstanceAsync()
        {
            if (_instance != null) return;

            if (!_isLoadingOrLoaded)
            {
                _isLoadingOrLoaded = true;
                
                // Load the prefab
                var handle = Addressables.LoadAssetAsync<GameObject>(AddressablesPath.DialoguePlayerPrefab);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject obj = Instantiate(handle.Result);
                    _instance = obj.GetComponent<DialoguePlayer>();
                    DontDestroyOnLoad(_instance);
                    obj.SetActive(false);
                }
                else
                {
                    Debug.LogError("Failed loading a prefab DialoguePlayer");
                    return;
                }
                
                //Load CSV as well
                if (_dialogueDataMatrix == null)
                {
                    var handleCsv = Addressables.LoadAssetAsync<TextAsset>(AddressablesPath.DialogueDataCSV);
            
                    await handleCsv.Task;

                    if (handleCsv.Status == AsyncOperationStatus.Succeeded)
                    {
                        string dialogueDataCsv = handleCsv.Result.text;
                        
                        // Convert a text asset to a decent string matrix
                        string[] rows = dialogueDataCsv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        int rowCount = rows.Length;
                        int colCount = SplitByComma(rows[0]).Count;
                        
                        _dialogueDataMatrix = new string[rowCount-1][];
                        
                        for (int i = 1; i < rowCount; ++i)
                        {
                            var columns = SplitByComma(rows[i]);
                            if (columns.Count != colCount)
                            {
                                Debug.LogError($"Error: csv file is corrupted at row {i}!");
                                _dialogueDataMatrix = null;
                                return;
                            }

                            _dialogueDataMatrix[i-1] = new string[colCount];
                            
                            for (int j = 0; j < colCount; ++j)
                            {
                                _dialogueDataMatrix[i-1][j] = columns[j];
                            }
                        }

                        // Initialize an accelerator array for a binary search
                        SortDDataMatrix();
                    }
                    else
                    {
                        Debug.LogError("Failed loading a prefab DialogueDataCSV");
                    }
                }
            }
        }

        private static List<string> SplitByComma(string line)
        {
            List<string> result = new List<string>();
            StringBuilder builder = new StringBuilder();

            int braceCount = 0;

            for(int i=0; i<line.Length; ++i)
            {
                if (line[i] == ',' && braceCount == 0)
                {
                    result.Add(builder.ToString().Trim());
                    builder.Clear();
                }
                else if (line[i] == '(')
                {
                    ++braceCount;
                }
                else if (line[i] == ')')
                {
                    --braceCount;
                }
                else if (line[i] == '\\')
                {
                    ++i;
                }
                else
                {
                    builder.Append(line[i]);
                }
            }
            
            result.Add(builder.ToString().Trim());

            return result;
        }

        private static int[] _indices;
        private static void SortDDataMatrix()
        {
            int n = _dialogueDataMatrix.GetLength(0);
            _indices = new int[n];
            for (int i = 0; i < n; ++i)
            {
                _indices[i] = i;
            }
            
            MergeSortIndices(0, n-1);
        }
        private static void MergeSortIndices(int l, int r)
        {
            if (l >= r) return;
            
            int m = l + (r - l) / 2 ;
            MergeSortIndices(l, m);
            MergeSortIndices(m + 1, r);
            MergeIndices(l, m, r);
        }
        private static void MergeIndices(int l, int m, int r)
        {
            // Copy substrings of both head and tail outside the range
            int n1 = m - l + 1, n2 = r - m;
            int[] left = new int[n1], right = new int[n2];
            
            Array.Copy(_indices, l, left, 0, n1);
            Array.Copy(_indices, m+1, right, 0, n2);

            int i = 0, j = 0, k = l;

            while (i < n1 && j < n2)
            {
                if(CompareID(_dialogueDataMatrix[left[i]][0], _dialogueDataMatrix[right[j]][0]) < 0)
                {
                    _indices[k++] = left[i++];
                }
                else
                {
                    _indices[k++] = right[j++];
                }
            }

            while (i < n1)
            {
                _indices[k++] = left[i++];
            }

            while (j < n2)
            {
                _indices[k++] = right[j++];
            }
        }

        private bool _canRead;
        private void ToggleReadPermissionStatus(bool value) => _canRead = value;
        private bool ConfirmKeyPressed() => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z);

        private void Update()
        {
            if (_canRead && _readyToProceed && ConfirmKeyPressed())
            {
                _permissionReceived = true;
            }
        }

        private void SetOnScreen(bool status)
        {
            // Switch the BGM and the input action
            AudioManager.Instance.PauseEveryBGMOver(0.3f);
            
            // Display/UnDisplay the gameObject on screen
            gameObject.SetActive(status);

            _isPlaying = status;

            if (status)
            {
                // Switch the input action
                GameInputHandler.Instance.RegisterInputReader(InputProcessor.DialogueAction, ToggleReadPermissionStatus);
                GameInputHandler.Instance.SwitchProcessorTo(InputProcessor.DialogueAction);
            }
            else
            {
                // Release everything
                Addressables.ReleaseInstance(_instance.gameObject);
                _instance = null;
                _isLoadingOrLoaded = false;
            }
        }

        private bool _isPlaying;

        private TaskCompletionSource<bool> _dialogueDone;
        
        private void ModifyForLocalization(ref string generalID)
        {
            switch (PreferenceDataManager.Instance.Language)
            {
                case Language.English:
                    generalID = String.Join(generalID, "_en");
                    break;
                case Language.Japanese:
                    generalID = String.Join(generalID, "_jp");
                    break;
            }
        }

        public async Task PlayDialogueAsync(string generalID)
        {
            // Prevent a double call
            if (_isPlaying)
            {
                Debug.LogError("Error: dialogue is already playing!");
                return;
            }
            
            // Modify an ID for localization
            ModifyForLocalization(ref generalID);

            generalID = "intro_en"; //TODO: test

            DialogueData current = new DialogueData();
            DialogueData next = new DialogueData();
            
            _dialogueDone = new TaskCompletionSource<bool>();
            SetOnScreen(true);
            
            _ = PlayFrameAsync(current, next, generalID);
            await _dialogueDone.Task;

            SetOnScreen(false);
        }

        private IEnumerator PlayTextCoroutine(string text)
        {
            textHolder.text = text;
            yield return null;
            _textPlayCoroutine = null;
        }

        private Coroutine _textPlayCoroutine;
        private bool _readyToProceed;
        private bool _permissionReceived;

        private async Task PlayFrameAsync(DialogueData current, DialogueData next, string firstKey = null) // TODO:
        {
            // Wait for a load
            if (!String.IsNullOrEmpty(firstKey))
            {
                await current.Initialize(GetDialogueInfoFromID(firstKey));
            }
            
            // Upload information to sprite
            if (current.HasImage)
            {
                if (pictureHolder == null)
                {
                    Debug.LogError("Picture holder is null");
                    return;
                }
                pictureHolder.sprite = current.Image;
            }

            if (current.HasColor)
            {
                if (backgroundHolder == null)
                {
                    Debug.LogError("Background holder is null");
                    return;
                }
                backgroundHolder.color = current.Color;
            }

            if (current.HasBGM)
            {
                if (backgroundHolder == null)
                {
                    Debug.LogError("Audio source is null");
                    return;
                }
                AudioManager.Instance.Play(audioSource, current.BGMInfo);
            }

            _textPlayCoroutine = StartCoroutine(PlayTextCoroutine(current.Text));
            
            // Load the next info if not the last frame
            bool isLastDialogue = String.IsNullOrEmpty(current.NextID);

            if (!isLastDialogue)
            {
                await next.Initialize(GetDialogueInfoFromID(current.NextID));
            }
            
            Debug.Log("debug starts from here");
            await Task.Delay(600);
            _dialogueDone.SetResult(true);
            Debug.Log("debug ends here");
            return;

            while (_textPlayCoroutine != null)
            {
                await Task.Delay(1); // waiting for a text play completion
            }
            
            // Waiting for a player input
            _readyToProceed = true;
            while (!_permissionReceived)
            {
                await Task.Delay(1); 
            }
            
            // Reset the current dialogue information
            current.Reset();
            
            if (!isLastDialogue)
            {
                await PlayFrameAsync(next, current);
            }
            else if (_dialogueDone != null && !_dialogueDone.Task.IsCompleted)
            {
                _dialogueDone.SetResult(true);
            }
        }

        private string[] GetDialogueInfoFromID(string id)
        {
            int l = 0, r = _indices.GetLength(0) - 1, m = 0;

            while (l <= r)
            {
                m = (l + r) / 2;
                int comp = CompareID(_dialogueDataMatrix[_indices[m]][0], id);

                if (comp == 0)
                {
                    break;
                }
                else if (comp < 0)
                {
                    l = m + 1;
                }
                else
                {
                    r = m - 1;
                }
            }

            if (l > r)
            {
                Debug.LogError($"Error: No row with an ID {id} was found");
                return null;
            }

            return _dialogueDataMatrix[_indices[m]];
        }

        private static int CompareID(string id1, string id2)
        {
            int i = 0;
            while (i < id1.Length && i < id2.Length && id1[i] == id2[i] && !Char.IsNumber(id1[i]))
            {
                ++i;
            }

            if (i >= id1.Length || i >= id2.Length)
            {
                return id1.Length.CompareTo(id2.Length);
            }

            if (!Char.IsNumber(id1[i]) || !Char.IsNumber(id2[i]))
            {
                return String.Compare(id1, id2, StringComparison.Ordinal);
            }
            
            int end1 = i, end2 = i;

            while (end1 < id1.Length && Char.IsNumber(id1[end1]))
            {
                ++end1;
            }

            while (end2 < id2.Length && Char.IsNumber(id2[end2]))
            {
                ++end2;
            }

            if (int.TryParse(id1.Substring(i, end1 - i), out int n1) && int.TryParse(id2.Substring(i, end2 - i), out int n2))
            {
                if (n1 != n2)
                {
                    return n1.CompareTo(n2);
                }
                
                return String.Compare(id1.Substring(end1), id2.Substring(end2), StringComparison.Ordinal);
            }
            else
            {
                Debug.LogError("Error: failed to parse string to int");
                return String.Compare(id1, id2, StringComparison.Ordinal);
            }
        }
    }
}

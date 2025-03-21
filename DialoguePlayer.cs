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
using Unity.VisualScripting;

namespace Fujin.System
{
    public class DialoguePlayer : MonoBehaviour
    {
        // Singleton for managing an outside call
        private static DialoguePlayer _instance;
        private static bool _isLoadingOrLoaded;
        private static string[][] _dialogueDataMatrix;
        
        [SerializeField] private Image pictureHolder;
        [SerializeField] private Image backgroundHolder;
        [SerializeField] private TextMeshProUGUI textHolder;
        [SerializeField] private AudioSource audioSource;

        public static async Task<DialoguePlayer> GetInstanceAsync()
        {
            if (_instance != null) return _instance;

            if (!_isLoadingOrLoaded)
            {
                _isLoadingOrLoaded = true;
                
                // Load the prefab
                var handle = Addressables.LoadAssetAsync<GameObject>(AddressablesPath.DialoguePlayer);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject obj = Instantiate(handle.Result);
                    _instance = obj.GetComponent<DialoguePlayer>();
                    DontDestroyOnLoad(_instance);
                }
                else
                {
                    Debug.LogError("Failed loading a prefab DialoguePlayer");
                }
                
                _instance.gameObject.SetActive(false);
                
                //Load CSV as well
                if (_dialogueDataMatrix == null)
                {
                    var handleCsv = Addressables.LoadAssetAsync<TextAsset>(AddressablesPath.DialogueDataCSV);
            
                    await handleCsv.Task;

                    if (handleCsv.Status == AsyncOperationStatus.Succeeded)
                    {
                        string dialogueDataCsv = handleCsv.Result.text;
                        
                        // Convert a text asset to a decent string matrix
                        string[] rows = dialogueDataCsv.Split('\n');
                        int rowCount = rows.Length;
                        int colCount = rows[0].Split(',').Length;
                        
                        _dialogueDataMatrix = new string[rowCount][];
                        
                        for (int i = 0; i < rowCount; ++i)
                        {
                            var columns = rows[i].Split(',');
                            _dialogueDataMatrix[i] = new string[colCount];
                            
                            for (int j = 0; j < colCount; ++j)
                            {
                                _dialogueDataMatrix[i][j] = columns[j];
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
            return _instance;
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

            int m = (l + r) / 2;
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
                int comp = String.Compare(_dialogueDataMatrix[_indices[l]][0], _dialogueDataMatrix[_indices[r]][0], StringComparison.Ordinal);
                // Prioritize r when l holds the index of a bigger string
                if (comp > 0) _indices[k++] = right[i++];
                else _indices[k++] = left[i++];
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
                Addressables.Release(_dialogueDataMatrix);
                Addressables.Release(_indices);
                Addressables.Release(_instance);
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
            }
            
            // Modify an ID for localization
            ModifyForLocalization(ref generalID);

            DialogueData current = new DialogueData();
            DialogueData next = new DialogueData();
            
            _dialogueDone = new TaskCompletionSource<bool>();
            SetOnScreen(true);
            
            await PlayFrameAsync(current, next, generalID); //TODO: 正しい呼び方わからん
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

        private async Task PlayFrameAsync(DialogueData current, DialogueData next, string firstKey = null) // Change a parameter here
        {
            // Wait for a load
            if (!String.IsNullOrEmpty(firstKey))
            {
                await current.Initialize(GetDialogueInfoFromID(firstKey));
            }
            
            // Upload information to sprite
            if (current.HasImage)
            {
                pictureHolder.sprite = current.Image;
            }

            if (current.HasColor)
            {
                backgroundHolder.color = current.Color;
            }

            if (current.HasBGM)
            {
                AudioManager.Instance.Play(audioSource, current.BGMInfo);
            }

            _textPlayCoroutine = StartCoroutine(PlayTextCoroutine(current.Text));
            
            // Load the next info if not the last frame
            bool isLastDialogue = String.IsNullOrEmpty(current.NextID);

            if (!isLastDialogue)
            {
                await next.Initialize(GetDialogueInfoFromID(current.NextID));
            }

            while (_textPlayCoroutine != null)
            {
                await Task.Delay(0); // waiting for a text play completion
            }
            
            // Waiting for a player input
            _readyToProceed = true;
            while (!_permissionReceived)
            {
                await Task.Delay(0); 
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
                int comp = String.Compare(_dialogueDataMatrix[_indices[m]][0], id, StringComparison.Ordinal);

                if (comp == 0)
                {
                    break;
                }
                else if (comp < l)
                {
                    l = m + 1;
                }
                else
                {
                    r = m - 1;
                }
            }

            if (l <= r)
            {
                Debug.LogError($"Error: No row with an ID {id} was found");
                return null;
            }

            return _dialogueDataMatrix[m];
        }
    }
}
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LLMCharacters
{
    /// <summary>
    /// Reference UI component. Fully event-driven: it subscribes to StreamHandler
    /// events (OnRequestStarted, OnTokenReceived, OnResponseComplete, OnError) and
    /// displays NPC responses with a typewriter effect, decoupled from the actual
    /// rate tokens arrive at — a chunky/fast stream still reveals at a steady pace.
    ///
    /// It only calls into NPCBrain to send player input (SendPlayerMessage) and to
    /// read the display name (CharacterName). Replace or extend this class with your
    /// own UI by subscribing to the same StreamHandler events — the rest of the SDK
    /// doesn't depend on it.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private TMP_Text npcNameText;
        [SerializeField] private TMP_Text responseText;
        [SerializeField] private TMP_Text playerMessageText;
        [SerializeField] private TMP_InputField playerInput;
        [SerializeField] private Button sendButton;
        [SerializeField] private NPCBrain npcBrain;
        [SerializeField] private StreamHandler streamHandler;

        [Header("Settings")]
        [SerializeField] private string placeholderText = "Say something...";
        [SerializeField] private bool clearInputOnSend = true;

        [Header("Typewriter")]
        [Tooltip("Characters revealed per second. Independent of how fast the LLM streams tokens.")]
        [SerializeField] private float charsPerSecond = 32f;

        [Header("Typing SFX")]
        [Tooltip("Optional. Leave empty to disable typing sound entirely.")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip typingSfx;
        [Tooltip("Play the SFX roughly every 3-4 revealed (non-whitespace) characters.")]
        [SerializeField] private Vector2Int sfxEveryCharsRange = new(3, 4);
        [SerializeField] private Vector2 pitchRange = new(0.9f, 1.1f);

        private const string ThinkingDots = "...";

        private bool _isWaitingForResponse;

        private readonly StringBuilder _incomingBuffer = new();
        private int _revealedCount;
        private int _charsSinceLastSfx;
        private int _nextSfxThreshold;
        private bool _streamComplete;
        private Coroutine _typewriterRoutine;

        private void Start()
        {
            if (sendButton != null)
                sendButton.onClick.AddListener(OnSendClicked);

            // onSubmit fires on Enter and is agnostic to the active input system
            // (works whether the project uses the old Input Manager or the new
            // Input System package).
            if (playerInput != null)
                playerInput.onSubmit.AddListener(OnInputSubmit);

            if (streamHandler != null)
            {
                streamHandler.OnRequestStarted += BeginResponse;
                streamHandler.OnTokenReceived += AppendToken;
                streamHandler.OnResponseComplete += HandleStreamComplete;
                streamHandler.OnError += ShowError;
            }

            if (npcNameText != null && npcBrain != null)
                npcNameText.text = npcBrain.CharacterName;

            if (responseText != null)
                responseText.text = "";
        }

        private void OnDestroy()
        {
            if (sendButton != null)
                sendButton.onClick.RemoveListener(OnSendClicked);

            if (playerInput != null)
                playerInput.onSubmit.RemoveListener(OnInputSubmit);

            if (streamHandler != null)
            {
                streamHandler.OnRequestStarted -= BeginResponse;
                streamHandler.OnTokenReceived -= AppendToken;
                streamHandler.OnResponseComplete -= HandleStreamComplete;
                streamHandler.OnError -= ShowError;
            }
        }

        private void OnInputSubmit(string _) => OnSendClicked();

        private void OnSendClicked()
        {
            if (_isWaitingForResponse || npcBrain == null || playerInput == null) return;

            string input = playerInput.text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            if (playerMessageText != null)
                playerMessageText.text = input;

            if (clearInputOnSend)
                playerInput.text = "";

            // BeginResponse is driven by StreamHandler.OnRequestStarted, so the UI
            // reacts even when SendPlayerMessage is triggered from elsewhere.
            npcBrain.SendPlayerMessage(input);
        }

        // ── Driven by StreamHandler events ─────────────────────────────────

        public void BeginResponse()
        {
            _isWaitingForResponse = true;

            _incomingBuffer.Clear();
            _revealedCount = 0;
            _charsSinceLastSfx = 0;
            _nextSfxThreshold = RollNextSfxThreshold();
            _streamComplete = false;

            if (_typewriterRoutine != null)
                StopCoroutine(_typewriterRoutine);
            _typewriterRoutine = null;

            if (responseText != null)
                responseText.text = ThinkingDots;
            if (sendButton != null)
                sendButton.interactable = false;
        }

        public void AppendToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return;

            _incomingBuffer.Append(token);

            if (_typewriterRoutine == null)
                _typewriterRoutine = StartCoroutine(TypewriterRoutine());
        }

        private void HandleStreamComplete(string fullResponse)
        {
            _streamComplete = true;

            // Covers the edge case of an empty response: no tokens ever arrived,
            // so the routine was never started. Start it now purely to close out.
            if (_typewriterRoutine == null)
                _typewriterRoutine = StartCoroutine(TypewriterRoutine());
        }

        private IEnumerator TypewriterRoutine()
        {
            bool clearedThinkingDots = false;
            float interval = charsPerSecond > 0f ? 1f / charsPerSecond : 0f;

            while (true)
            {
                if (_revealedCount < _incomingBuffer.Length)
                {
                    if (!clearedThinkingDots && responseText != null)
                    {
                        responseText.text = "";
                        clearedThinkingDots = true;
                    }

                    char c = _incomingBuffer[_revealedCount];
                    _revealedCount++;

                    if (responseText != null)
                        responseText.text += c;

                    if (!char.IsWhiteSpace(c))
                    {
                        _charsSinceLastSfx++;
                        if (_charsSinceLastSfx >= _nextSfxThreshold)
                        {
                            _charsSinceLastSfx = 0;
                            _nextSfxThreshold = RollNextSfxThreshold();
                            PlayTypingSfx();
                        }
                    }

                    yield return interval > 0f ? new WaitForSeconds(interval) : null;
                }
                else if (_streamComplete)
                {
                    break;
                }
                else
                {
                    yield return null;
                }
            }

            if (responseText != null && responseText.text == ThinkingDots)
                responseText.text = "";

            _typewriterRoutine = null;
            EndResponse();
        }

        private int RollNextSfxThreshold() =>
            Random.Range(Mathf.Min(sfxEveryCharsRange.x, sfxEveryCharsRange.y),
                         Mathf.Max(sfxEveryCharsRange.x, sfxEveryCharsRange.y) + 1);

        private void PlayTypingSfx()
        {
            if (audioSource == null || typingSfx == null) return;
            audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
            audioSource.PlayOneShot(typingSfx);
        }

        public void EndResponse()
        {
            _isWaitingForResponse = false;
            if (sendButton != null)
                sendButton.interactable = true;
        }

        public void ShowError(string errorMessage)
        {
            if (_typewriterRoutine != null)
            {
                StopCoroutine(_typewriterRoutine);
                _typewriterRoutine = null;
            }

            if (responseText != null)
                responseText.text = "...";
            Debug.LogWarning($"[DialogueUI] NPC response error: {errorMessage}");
            EndResponse();
        }
    }
}

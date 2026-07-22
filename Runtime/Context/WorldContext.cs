using UnityEngine;

namespace LLMCharacters
{
    /// <summary>
    /// Global context shared by all NPCs. Any game system can write here; the changes
    /// are picked up on each NPC's next turn. Create one asset and reference it everywhere.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldContext", menuName = "LLM Characters/World Context")]
    public class WorldContext : ContextProviderBase
    {
        private string _runtimeProse = "";

        public override string GetProse() => _runtimeProse;

        public void AppendProse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            _runtimeProse = string.IsNullOrEmpty(_runtimeProse)
                ? text
                : _runtimeProse + "\n" + text;
        }

        public void ClearProse() => _runtimeProse = "";

        protected override void OnDisable()
        {
            base.OnDisable();
            _runtimeProse = "";
        }
    }
}

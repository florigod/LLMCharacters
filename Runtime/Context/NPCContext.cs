using UnityEngine;

namespace LLMCharacters
{
    /// <summary>
    /// Context asset for a single NPC or a shared group. Default specificity is 100 (individual).
    /// Lower it in the Inspector when sharing this asset across a location or archetype.
    /// </summary>
    [CreateAssetMenu(fileName = "NPCContext", menuName = "LLM Characters/NPC Context")]
    public class NPCContext : ContextProviderBase
    {
        [Header("Scene Knowledge")]
        [TextArea(3, 10)]
        [Tooltip("Static facts this layer always knows. Use it to prevent hallucination — " +
                 "tavern layout, regular customers, NPC backstory, etc. " +
                 "Accumulated across all layers, never overwritten.")]
        public string sceneKnowledge = "";

        public override string GetProse() => sceneKnowledge;

        // New assets default to individual specificity. Lower it in the Inspector
        // when reusing this asset as a shared group/type layer.
        private void Reset() => specificity = 100;
    }
}

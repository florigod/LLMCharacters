using UnityEngine;

namespace LLMCharacters
{
    /// <summary>
    /// Per-NPC or per-group context. Holds static scene knowledge (prose) plus
    /// runtime key/value entries. Use it three ways, all with the same asset type:
    ///   • an individual NPC's private context (default specificity 100),
    ///   • a shared group/location layer (e.g. one TavernContext asset referenced
    ///     by every NPC in the tavern — lower its specificity, e.g. 10),
    ///   • a shared type/archetype layer (e.g. GuardKnowledge — e.g. 20).
    /// Precedence between layers is resolved by specificity, not by wiring order.
    /// </summary>
    [CreateAssetMenu(fileName = "NPCContext", menuName = "LLM Characters/NPC Context")]
    public class NPCContext : ContextProviderBase
    {
        [Header("Scene Knowledge")]
        [TextArea(3, 10)]
        [Tooltip("Static facts this layer always knows about its environment. " +
                 "Use it to prevent hallucination — e.g. tavern capacity, regular " +
                 "customers, layout. Injected as prose; prose from every layer is " +
                 "accumulated (never overwritten).")]
        public string sceneKnowledge = "";

        public override string GetProse() => sceneKnowledge;

        // New assets default to individual specificity. Lower it in the Inspector
        // when reusing this asset as a shared group/type layer.
        private void Reset() => specificity = 100;
    }
}

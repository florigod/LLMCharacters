# LLM Characters for Unity 6

Connect Unity NPCs to language models with real-time token streaming and dynamic context injection. Supports Anthropic's Claude (cloud) and Ollama (local). Drop-in ScriptableObject configuration, event-driven architecture, zero external dependencies.

Each NPC's knowledge is assembled from a stack of context layers, shared or private ScriptableObjects that get merged into the system prompt before every request. Any game system can write into those layers at runtime, so NPCs react to the current game state without extra wiring.

---

## Core ScriptableObjects

| Asset | Purpose |
|---|---|
| `NPCPersonality` | Character identity, tone, background, and response format rules |
| `NPCContext` | Scene knowledge (prose) + runtime key/value entries. Usable as a personal, zone, or type layer |
| `WorldContext` | Dynamic game state shared across all NPCs (weather, time, events) |
| `LLMConfig` | Provider-agnostic generation params: model, temperature, token limits |

Both `NPCContext` and `WorldContext` derive from `ContextProviderBase` and implement
`IContextProvider`, so they compose freely (see **Layered Context** below).

---

## Layered Context & Specificity

An NPC's factual knowledge is assembled from an ordered set of **context layers**.
`NPCBrain` exposes a `Context Providers` list, drag in any mix of context assets
**in any order**. Given that programmatic context may be stored in dictionnaries,
there's a precedence logic for different contexts modifying the same keys with
different values. Precedence does *not* come from list order; it comes from each
provider's **`Specificity`** value: a plain integer you assign, where the higher
number wins on a key collision. The SDK never hardcodes any tier, so the layout
below is only an example convention. Rename the tiers, change the numbers, or
insert new layers wherever you need:

```
World    specificity 0     WorldContext         shared by every NPC
Zone     specificity 10    TavernContext        shared by NPCs in the tavern
Type     specificity 20    GuardKnowledge       shared by all guards (optional extra tier)
Personal specificity 100   this NPC's NPCContext private to one NPC
```

Keep in mind that `Specificity` matters only for key collisions, when two layers write to the same key. It is not a general "importance" weight making one text prompt count for more than another: all prose is accumulated equally.

**Sharing is free:** a ScriptableObject is a single instance by reference. Point two
NPCs at the same `TavernContext.asset` and they share it live, a `Set()` from one is
seen by both on their next turn. No events or subscriptions needed.

**Collision rule:** when two layers define the same key (e.g. World says
`crowd: empty`, Tavern says `crowd: busy`), the **higher specificity wins**. Two
providers at *equal* specificity are peers, so a same-key collision between them is a
design error and is logged as a warning. Prose (`sceneKnowledge`) is **accumulated**
across all layers, never overwritten.

**Extending:** implement `IContextProvider` (or extend `ContextProviderBase`) to add
your own sources like quest logs, relationships, inventory, or factions, and drop them into
the same list. The SDK core never needs to change.

---

## Scene Knowledge

`NPCContext` exposes a **Scene Knowledge** text field (Inspector > `sceneKnowledge`) for static facts the NPC always knows about their environment.

**Why it matters:** Without grounding, the LLM may accept false player assertions. If a player says "the tavern is empty", the NPC will agree, because nothing in the prompt contradicts it. Scene Knowledge prevents this and it's important to set it correctly.
Please note: a Response Format should also include the behaviour to be dismissive when asked for context that isn't clear in the scene prompt, see **Response Format** section.

**Example value for a tavern keeper:**
```
The tavern has 20 seats. On a normal night, 8-12 regulars are present.
The tavern is worn out by time, but has cozy lighting and wooden architecture. 
Regular customers include Old Marta (corner seat, drinks ale alone),
the blacksmith Henrik, and three miners from the east ridge.
Behind the bar: two barrels of ale, one of mead. Specialty: beef stew.
```

The section is injected into every system prompt under `## Scene Knowledge`, before world state. The NPC will use it to resist contradictory player claims and answer environmental questions accurately.

Scene Knowledge is set in the Inspector (static, per-asset) or written programmatically via `NPCContext.Set(key, value)` for dynamic entries.

---

## Response Format

`NPCPersonality` includes a **Response Format** section with four controls:

| Field | Type | Default | Effect |
|---|---|---|---|
| `maxResponseSentences` | int (1–5) | 3 | Caps response length in the prompt rule |
| `allowAsteriskActions` | bool | true | When false, adds rule: *do not use \*action\* descriptions* |
| `allowEmojis` | bool | false | When true, removes the no-emoji rule |
| `additionalResponseRules` | TextArea | — | One rule per line, appended as bullet points |

**Use cases for `additionalResponseRules`:**
- `Respond only in Spanish.`
- `Keep all language family-friendly.`
- `Speak in archaic English (thee, thou, dost).`
- `Refer to the player as 'stranger' until they give their name.`

These rules are appended at the end of the `## Response Rules` section in the system prompt, after the standard rules.

A player asking what colour are the shoes of the NPCs may be problematic if we didn't give that context. Test if your NPC is accurately dismissive to these type of questions, and be sure to add direct rules in the additionalResponseRules, such as "Be dismissive for questions regarding context that you're unsure about", to patch hallucinations that could be contradictory to what the player sees in-game.

---

## Architecture

```
NPCBrain → StreamHandler → ILLMProvider (AnthropicProvider, OllamaProvider, or MockProvider)
               ↓
   OnRequestStarted / OnTokenReceived / OnResponseComplete / OnError
               ↓
           DialogueUI  (or your own subscriber)
```

`NPCBrain` is UI-agnostic. Subscribe to `StreamHandler` events from any component to build custom UIs without modifying SDK code.

**WebGL note:** `AnthropicProvider` and `OllamaProvider` both use `System.Net.Http` and do not work on WebGL. Use `MockProvider` for WebGL builds.

---

## Providers

| Provider | Backend | Cost | Notes |
|---|---|---|---|
| `MockProvider` | None (templated local text) | Free | Works everywhere, including WebGL. Use for UI/context iteration without burning API credits. |
| `AnthropicProvider` | Claude API (cloud) | Pay per token | Full quality, requires an API key and internet. |
| `OllamaProvider` | Local Ollama server | Free | Runs models like Llama/Mistral/Phi/Gemma locally, no internet or API key needed. |

Swap providers by assigning a different one to `StreamHandler`'s `Provider Component` field, with no other code changes.

### Where configuration lives

Configuration is split by concern so that adding a provider never requires editing shared code (Open/Closed):

- **`LLMConfig`, *what* to generate** (provider-agnostic): `model`, `temperature`, `maxTokens`, `timeoutSeconds`, `maxHistoryMessages`. One shared asset, referenced by every NPC.
- **The provider component, *how/where* to connect** (provider-specific): e.g. `AnthropicProvider.apiKey`, `OllamaProvider.baseUrl`. Each provider carries its own connection/credential fields.

A new provider that needs a new setting (an org ID, an auth header, a region) adds that field to *its own* component, while `LLMConfig` and the rest of the SDK stay untouched.

### OllamaProvider setup

1. Install Ollama from [ollama.com](https://ollama.com).
2. Pull a model: `ollama pull llama3.2`.
3. Make sure the Ollama server is running (`ollama serve`, or it auto-starts on some installs) **before** entering Play mode.
4. Add `OllamaProvider` to the NPC's GameObject and assign it to `StreamHandler`.

The SDK does not install, launch, or manage Ollama or its models. That's the developer's responsibility, same as having Docker running for a Docker-dependent tool. If the server isn't reachable, `OllamaProvider` raises a clear error via `StreamHandler.OnError` instead of failing silently.

Ollama needs no API key, so this provider has no credential field, only `baseUrl`. `LLMConfig.model` should match a model you've pulled locally (e.g. `llama3.2`, `mistral`, `phi3`). `temperature` and `maxTokens` map to Ollama's `options.temperature` and `options.num_predict`.

---

## Logging

Each play session writes a JSONL log to:
```
Application.persistentDataPath/LLMCharacters/logs/<NPCName>_<timestamp>.jsonl
```

The full path is printed to the Unity Console on session start. Each entry includes: system prompt, user message, NPC response, duration, estimated token counts, and estimated cost (Haiku 4.5 pricing).

## Author: Florian Mathe, 2026
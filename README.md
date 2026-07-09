# LLM Characters
Unity package built to explore LLM integration in games: real-time streaming and dynamic context injection for believable NPCs.

Supports Unity 6. Zero external dependencies.

## How it works
<!-- GIF of typewriter dialogue here -->
LLM Characters connects Unity NPCs to language models via HTTP streaming. When a player sends a message, the NPC's response arrives token by token, and can be written on screen as soon as possible, or using a type-writter effect like in the demo.

The communication pipeline is fully event-driven:

1. NPCBrain → StreamHandler → ILLMProvider (Anthropic / Ollama / Mock)

2. OnRequestStarted / OnTokenReceived / OnResponseComplete / OnError

3. DialogueUI (or your own subscriber)
   
4. NPCBrain assembles the system prompt and manages conversation history. StreamHandler owns the async request lifecycle and fires events that any component can subscribe to. ILLMProvider is the swappable backend — changing providers is a single Inspector field, no code changes required.

## NPC Context System

<!-- Screenshot of Inspector showing Context Providers list -->
What an NPC knows is assembled from a stack of context layers, each with a Specificity value that determines precedence on key collisions:

-World      specificity 0     shared by every NPC     (weather, time, events). 

-Location   specificity 10    shared by NPCs in area   (tavern crowd, classroom students). 

-Individual specificity 100   private to one NPC       (backstory, secrets).

Context is stored in ScriptableObjects. Two NPCs pointing at the same asset share it live, where a runtime Set("weather", "stormy") is seen by all of them on their next turn, with no events or subscriptions needed.

### Dynamic injection example:

worldContext.Set("weather", "stormy");
worldContext.Set("recent_event", "A fight broke out near the docks.");
npcContext.Set("player_reputation", "trusted");
These entries get merged into the system prompt automatically before every request.

## Providers
Provider	Model quality	Cost	Internet required

AnthropicProvider	High (Claude)	Pay per token	Yes

OllamaProvider	Medium (Llama, Mistral, Phi…)	Free	No

MockProvider	Templated text	Free	No

Anthropic (Claude) produces **significantly more coherent**, context-aware responses, especially for longer conversations or nuanced character voices. Requires an API key and an internet connection.

Ollama runs open-source models entirely on the player's machine. No API key, no data leaves the device, works offline. Response quality depends on the model and available hardware. Requires Ollama installed locally with the target model already pulled.

Mock is a local provider that returns templated responses. Useful for testing UI, context wiring, and scene setup without burning API credits or requiring a running server.

## Features
Real-time token streaming with typewriter reveal and Animal Crossing-style SFX
Conversation history (configurable turn count)
Scene Knowledge field per NPC — static grounding facts that prevent hallucination
Response Format controls per NPC: sentence cap, asterisk actions, emoji, custom rules
JSONL session logging with token estimates and cost tracking
Provider-agnostic generation config (LLMConfig) — credentials live on each provider component, not in shared assets
Limitations & production considerations
API key exposure: AnthropicProvider stores the API key as a serialized field on a GameObject. In a shipped build, Unity serialized data can be extracted with asset deserialization tools. This is acceptable for prototypes and game-jams, but not for shipped products. The recommended production architecture is a lightweight backend server that holds the key and proxies requests — the client calls your server, your server calls Anthropic. A custom ILLMProvider implementation pointing at your own endpoint takes around 30 lines of code.

Local LLM distribution: OllamaProvider requires Ollama installed and a model pulled on the player's machine before the game runs. The SDK does not bundle, download, or manage models. Shipping a game with a local LLM requires a separate distribution and installation strategy (a launcher, a bundled runtime, or an in-game download flow) that is outside the scope of this package.

## Installation
Add via Unity Package Manager using the Git URL:

https://github.com/juanmathe/llm-characters.git
Or clone and add as a local package.

## Documentation
Full API reference, context system details, and provider setup guides are in Documentation~/.


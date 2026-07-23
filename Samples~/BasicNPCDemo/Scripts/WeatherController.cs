using UnityEngine;

namespace LLMCharacters.Samples
{
    /// <summary>
    /// Sample: shows how any game system writes into WorldContext.
    /// The NPC never "knows" about a weather system — it simply reads
    /// the world state injected into its system prompt.
    /// </summary>
    public class WeatherController : MonoBehaviour
    {
        [SerializeField] private WorldContext worldContext;

        private readonly string[] _weatherTypes =
            { "sunny", "light rain", "heavy rain", "foggy", "stormy", "snowing" };

        // Awake (not Start) so the world state is ready before any Start()
        // reads it — e.g. WeatherHUD's initial label refresh.
        private void Awake()
        {
            ApplyTimeOfDay();
            SetRandomWeather();
        }

        public void SetRandomWeather()
        {
            if (worldContext == null) return;
            string weather = _weatherTypes[Random.Range(0, _weatherTypes.Length)];
            worldContext.Set("weather", weather);
            Debug.Log($"[WeatherController] weather → {weather}");
        }

        public void SetWeather(string weather)
        {
            worldContext?.Set("weather", weather);
        }

        public void SetEvent(string description)
        {
            worldContext?.Set("recent_event", description);
        }

        private void ApplyTimeOfDay()
        {
            if (worldContext == null) return;
            int hour = System.DateTime.Now.Hour;
            string time = hour switch
            {
                >= 5 and < 7   => "dawn",
                >= 7 and < 12  => "morning",
                >= 12 and < 14 => "noon",
                >= 14 and < 17 => "afternoon",
                >= 17 and < 20 => "dusk",
                _              => "night"
            };
            worldContext.Set("time_of_day", time);
        }
    }
}

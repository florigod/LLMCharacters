using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LLMCharacters.Samples
{
    /// <summary>
    /// Sample HUD: a button that randomizes the weather via WeatherController
    /// and a label showing the current world state. Demonstrates that changing
    /// WorldContext at runtime immediately affects the NPC's next response —
    /// no NPC code involved.
    /// </summary>
    public class WeatherHUD : MonoBehaviour
    {
        [SerializeField] private WeatherController weatherController;
        [SerializeField] private WorldContext worldContext;
        [SerializeField] private Button changeWeatherButton;
        [SerializeField] private TMP_Text weatherLabel;

        private void Start()
        {
            if (changeWeatherButton != null)
                changeWeatherButton.onClick.AddListener(OnChangeWeatherClicked);

            RefreshLabel();
        }

        private void OnDestroy()
        {
            if (changeWeatherButton != null)
                changeWeatherButton.onClick.RemoveListener(OnChangeWeatherClicked);
        }

        private void OnChangeWeatherClicked()
        {
            if (weatherController != null)
                weatherController.SetRandomWeather();

            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (weatherLabel == null || worldContext == null) return;

            string weather = worldContext.Get("weather") ?? "unknown";
            string time = worldContext.Get("time_of_day") ?? "unknown";
            weatherLabel.text = $"Weather: {weather}  ·  Time: {time}";
        }
    }
}

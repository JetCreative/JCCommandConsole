
using System;
using UnityEngine;
using UnityEngine.Serialization;
// ReSharper disable UnusedMember.Local
// ReSharper disable NotAccessedField.Local

namespace JetCreative.CommandConsolePro
{
    /// <summary>
    /// Example component containing various commands that can be called via the Command Console Pro.
    /// This serves as a demonstration of the different elements that can be exposed to the console.
    /// </summary>
    public class ConsoleExampleCommands : MonoBehaviour
    {
        #region Fields

        /// <summary>
        /// Example of a field that can be get/set via the console.
        /// Usage: get health or set health 100
        /// </summary>
        [FormerlySerializedAs("health")] [Command]
        public float Health = 100f;

        /// <summary>
        /// Example of a private field with a custom command name.
        /// Usage: get ammo or set ammo 50
        /// </summary>
        [Command("ammo", true)]
#pragma warning disable CS0414 // Field is assigned but its value is never used
        private int ammunition = 30;
#pragma warning restore CS0414 // Field is assigned but its value is never used

        /// <summary>
        /// Example of a static field accessible from anywhere.
        /// Usage: get gametime or set gametime 300
        /// </summary>
        [Command]
        public static float _GameTime;

        #endregion

        #region Properties

        /// <summary>
        /// Example of a property that can be get/set via the console.
        /// Usage: get score or set score 1000
        /// </summary>
        [Command]
        public int Score { get; set; } = 0;
        
        /// <summary>
        /// Example of a property with a public getter and a private setter.
        /// Editor Settings control private setter availability.
        /// </summary>
        [Command("Inventory", true)]
        public int InventorySize { get; private set; } = 0;

        /// <summary>
        /// Example of a read-only property (can only be "get").
        /// Usage: get healthpercent
        /// </summary>
        [Command("healthpercent")]
        public float HealthPercentage => (Health / 100f) * 100f;

        /// <summary>
        /// Example of a property with custom logic.
        /// Usage: get isalive or set isalive false
        /// </summary>
        [Command]
        public bool IsAlive
        {
            get => Health > 0;
            set
            {
                if (!value) Health = 0;
                else if (Health <= 0) Health = 1;
            }
        }

        /// <summary>
        /// Example of a static property accessible from anywhere.
        /// Usage: get difficulty or set difficulty 3
        /// </summary>
        [Command]
        public static int Difficulty { get; set; } = 1;

        #endregion

        #region Methods

        /// <summary>
        /// Example of a simple method with no parameters.
        /// Usage: call resethealth
        /// </summary>
        [Command]
        public void ResetHealth()
        {
            Health = 100f;
            Debug.Log("Health reset to 100");
        }

        /// <summary>
        /// Example of a method with parameters.
        /// Usage: call addhealth 25
        /// </summary>
        [Command]
        public float AddHealth(float amount)
        {
            Health = Mathf.Clamp(Health + amount, 0, 100);
            Debug.Log($"Added {amount} health. Current health: {Health}");
            return Health;
        }

        /// <summary>
        /// Example of a method with multiple parameters.
        /// Usage: call teleport 10,0,5
        /// </summary>
        [Command]
        public void Teleport(Vector3 position)
        {
            transform.position = position;
            Debug.Log($"Teleported to {position}");
        }

        /// <summary>
        /// Example of a method with a custom command name.
        /// Usage: call god true
        /// </summary>
        [Command("god")]
        public void SetInvincible(bool enabled)
        {
            Debug.Log($"God mode {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Example of a method returning a value.
        /// Usage: call getdistance
        /// </summary>
        [Command(includePrivate: true)]
        private float GetDistanceFromOrigin()
        {
            float distance = Vector3.Distance(transform.position, Vector3.zero);
            Debug.Log($"Distance from origin: {distance}");
            return distance;
        }

        /// <summary>
        /// Example of a static method accessible from anywhere.
        /// Usage: call restart
        /// </summary>
        [Command]
        public static void Restart()
        {
            Debug.Log("Game restart requested");
            // In a real implementation, this would restart the game
        }

        /// <summary>
        /// Example of a method with an enum parameter.
        /// Usage: call setdifficulty Easy
        /// </summary>
        [Command]
        public void SetDifficulty(DifficultyLevel level)
        {
            Difficulty = (int)level;
            Debug.Log($"Difficulty set to {level}");
        }

        #endregion

        #region Delegates and Events

        /// <summary>
        /// Example of a delegate that can be called.
        /// Usage: @nameOrTag call onhealthchanged 75
        /// </summary>
        [Command("onhealthchanged")]
        public Action<float> OnHealthChanged;

        /// <summary>
        /// Example of a static delegate accessible from anywhere.
        /// Usage: call ongamepaused true
        /// </summary>
        [Command(includePrivate: true)]
        private static Action<bool> _onGamePaused;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Initialize delegates to prevent null reference exceptions
            OnHealthChanged = newHealth => 
            {
                Health = newHealth;
                Debug.Log($"Health changed to {newHealth}");
            };

            _onGamePaused = isPaused => 
            {
                Debug.Log($"Game {(isPaused ? "paused" : "resumed")}");
                // In a real implementation, this would pause/resume the game
            };
        }

        private void Update()
        {
            // Update game time (for demonstration purposes)
            _GameTime += Time.deltaTime;
        }

        #endregion

        #region Enums

        /// <summary>
        /// Example enum for demonstration of enum parameter handling.
        /// </summary>
        public enum DifficultyLevel
        {
            Easy,
            Normal,
            Hard,
            Nightmare
        }

        #endregion
    }
}
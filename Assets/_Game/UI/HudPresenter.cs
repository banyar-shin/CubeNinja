using System;
using System.Collections.Generic;
using UnityEngine;

namespace CubeNinja.UI
{
    public sealed class HudPresenter : MonoBehaviour
    {
        private const float PopupLifetimeSeconds = 0.75f;

        [SerializeField] private Camera mainCamera;

        private readonly List<Popup> popups = new List<Popup>();
        private GUIStyle scoreStyle;
        private GUIStyle popupStyle;
        private GUIStyle centeredStyle;
        private int score;
        private int lives;
        private int comboMultiplier;
        private float comboRemainingSeconds;
        private bool gameOver;

        public event Action RestartRequested;

        public void SetCamera(Camera camera)
        {
            mainCamera = camera;
        }

        public void SetRunState(int newScore, int remainingLives, int combo, float comboRemaining, bool isGameOver)
        {
            score = Mathf.Max(0, newScore);
            lives = Mathf.Max(0, remainingLives);
            comboMultiplier = Mathf.Max(0, combo);
            comboRemainingSeconds = Mathf.Max(0f, comboRemaining);
            gameOver = isGameOver;
        }

        public void ShowComboPopup(Vector3 worldPosition, int multiplier)
        {
            if (multiplier <= 1)
            {
                return;
            }

            popups.Add(new Popup(worldPosition, $"Combo x{multiplier}", Color.white, Time.time));
        }

        public void ShowLifeLostPopup(Vector3 worldPosition)
        {
            popups.Add(new Popup(worldPosition, "Life -1", new Color(1f, 0.25f, 0.2f, 1f), Time.time));
        }

        public void ClearPopups()
        {
            popups.Clear();
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawTopBar();
            DrawPopups();

            if (gameOver)
            {
                DrawGameOverOverlay();
            }
        }

        private void DrawTopBar()
        {
            GUI.Label(new Rect(24f, 18f, 240f, 38f), $"Score {score}", scoreStyle);

            if (comboMultiplier > 1 && comboRemainingSeconds > 0f)
            {
                popupStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(24f, 54f, 180f, 28f), $"Combo x{comboMultiplier}", popupStyle);
            }

            DrawLifeCubes();
        }

        private void DrawLifeCubes()
        {
            const float size = 22f;
            const float gap = 8f;
            var startX = Screen.width - 24f - ((size + gap) * 3f);
            var y = 22f;
            var oldColor = GUI.color;

            for (var i = 0; i < 3; i++)
            {
                var rect = new Rect(startX + ((size + gap) * i), y, size, size);
                GUI.color = i < lives ? new Color(0.94f, 0.16f, 0.18f, 1f) : new Color(0.18f, 0.18f, 0.2f, 0.7f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Box(rect, GUIContent.none);
            }

            GUI.color = oldColor;
        }

        private void DrawPopups()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            for (var i = popups.Count - 1; i >= 0; i--)
            {
                var popup = popups[i];
                var age = Time.time - popup.StartTime;
                if (age >= PopupLifetimeSeconds)
                {
                    popups.RemoveAt(i);
                    continue;
                }

                if (mainCamera == null)
                {
                    continue;
                }

                var screenPoint = mainCamera.WorldToScreenPoint(popup.WorldPosition);
                if (screenPoint.z < 0f)
                {
                    continue;
                }

                var alpha = 1f - age / PopupLifetimeSeconds;
                popupStyle.normal.textColor = new Color(popup.Color.r, popup.Color.g, popup.Color.b, alpha);
                var rect = new Rect(screenPoint.x - 70f, Screen.height - screenPoint.y - 42f - age * 36f, 140f, 26f);
                GUI.Label(rect, popup.Text, popupStyle);
            }
        }

        private void DrawGameOverOverlay()
        {
            var oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.68f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = oldColor;

            var centerY = Screen.height * 0.5f - 86f;
            popupStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(0f, centerY, Screen.width, 52f), "Game Over", centeredStyle);
            GUI.Label(new Rect(0f, centerY + 48f, Screen.width, 36f), $"Final Score {score}", popupStyle);

            var buttonRect = new Rect(Screen.width * 0.5f - 70f, centerY + 96f, 140f, 42f);
            if (GUI.Button(buttonRect, "Restart"))
            {
                RestartRequested?.Invoke();
            }
        }

        private void EnsureStyles()
        {
            if (scoreStyle != null)
            {
                return;
            }

            scoreStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            popupStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            centeredStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 42,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        private readonly struct Popup
        {
            public Popup(Vector3 worldPosition, string text, Color color, float startTime)
            {
                WorldPosition = worldPosition;
                Text = text;
                Color = color;
                StartTime = startTime;
            }

            public Vector3 WorldPosition { get; }
            public string Text { get; }
            public Color Color { get; }
            public float StartTime { get; }
        }
    }
}

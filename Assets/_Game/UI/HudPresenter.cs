using System;
using System.Collections.Generic;
using UnityEngine;

namespace CubeNinja.UI
{
    public sealed class HudPresenter : MonoBehaviour
    {
        private const float PopupLifetimeSeconds = 0.75f;
        private const float DamageFlashDurationSeconds = 0.55f;

        [SerializeField] private Camera mainCamera;

        private readonly List<Popup> popups = new List<Popup>();
        private GUIStyle scoreStyle;
        private GUIStyle popupStyle;
        private GUIStyle centeredStyle;
        private GUIStyle menuTitleStyle;
        private GUIStyle menuHeaderStyle;
        private GUIStyle menuBodyStyle;
        private int score;
        private int lives;
        private int comboMultiplier;
        private float comboRemainingSeconds;
        private float comboWindowSeconds = 0.5f;
        private float damageFlashStartTime = float.NegativeInfinity;
        private LegendEntry[] legendEntries = new LegendEntry[0];
        private bool startMenuVisible;
        private bool gameOver;

        public event Action StartRequested;
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

        public void SetStartMenuVisible(bool visible)
        {
            startMenuVisible = visible;
        }

        public void SetStartMenuInfo(LegendEntry[] entries, float comboWindow)
        {
            legendEntries = entries ?? new LegendEntry[0];
            comboWindowSeconds = Mathf.Max(0.01f, comboWindow);
        }

        public void ShowScorePopup(Vector3 worldPosition, int pointsAdded, int multiplier)
        {
            var text = multiplier > 1
                ? $"+{Mathf.Max(0, pointsAdded)}  Combo x{multiplier}"
                : $"+{Mathf.Max(0, pointsAdded)}";
            var color = multiplier > 1 ? new Color(1f, 0.92f, 0.32f, 1f) : Color.white;
            popups.Add(new Popup(worldPosition, text, color, Time.time));
        }

        public void ShowLifeLostPopup(Vector3 worldPosition)
        {
            popups.Add(new Popup(worldPosition, "Life -1", new Color(1f, 0.25f, 0.2f, 1f), Time.time));
            damageFlashStartTime = Time.time;
        }

        public void ClearPopups()
        {
            popups.Clear();
            damageFlashStartTime = float.NegativeInfinity;
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (startMenuVisible)
            {
                DrawStartMenuOverlay();
                return;
            }

            DrawDamageFlash();
            DrawTopBar();
            DrawPopups();

            if (gameOver)
            {
                DrawGameOverOverlay();
                DrawEdgeGlow(0.65f);
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

        private void DrawStartMenuOverlay()
        {
            var oldColor = GUI.color;
            GUI.color = new Color(0.02f, 0.025f, 0.035f, 0.92f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = oldColor;

            var width = Mathf.Min(640f, Screen.width - 48f);
            var height = Mathf.Min(560f, Screen.height - 48f);
            var panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            GUI.Box(panel, GUIContent.none);

            var x = panel.x + 34f;
            var y = panel.y + 24f;
            var contentWidth = panel.width - 68f;

            GUI.Label(new Rect(x, y, contentWidth, 48f), "Cube Ninja", menuTitleStyle);
            y += 58f;

            GUI.Label(
                new Rect(x, y, contentWidth, 44f),
                "Click scoring cubes before they fall. Keep the streak alive, and leave danger cubes alone.",
                menuBodyStyle);
            y += 62f;

            GUI.Label(new Rect(x, y, contentWidth, 28f), "Cubes", menuHeaderStyle);
            y += 34f;

            for (var i = 0; i < legendEntries.Length; i++)
            {
                var entry = legendEntries[i];
                DrawLegendEntry(new Rect(x, y, contentWidth, 46f), entry);
                y += 52f;
            }

            y += 10f;
            GUI.Label(new Rect(x, y, contentWidth, 28f), "Combos", menuHeaderStyle);
            y += 34f;
            GUI.Label(
                new Rect(x, y, contentWidth, 64f),
                $"After each scoring click, you have {comboWindowSeconds:0.##} seconds to click another scoring cube. Each hit in that window grows the multiplier: x2, x3, and higher.",
                menuBodyStyle);

            var buttonRect = new Rect(panel.center.x - 82f, panel.yMax - 72f, 164f, 44f);
            if (GUI.Button(buttonRect, "Start"))
            {
                StartRequested?.Invoke();
            }
        }

        private void DrawLegendEntry(Rect rect, LegendEntry entry)
        {
            var oldColor = GUI.color;
            var swatch = new Rect(rect.x, rect.y + 7f, 30f, 30f);
            GUI.color = entry.Color;
            GUI.DrawTexture(swatch, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Box(swatch, GUIContent.none);

            GUI.color = oldColor;
            GUI.Label(new Rect(rect.x + 46f, rect.y, rect.width - 46f, 22f), entry.Name, menuHeaderStyle);
            GUI.Label(new Rect(rect.x + 46f, rect.y + 22f, rect.width - 46f, 24f), entry.Description, menuBodyStyle);
        }

        private void DrawDamageFlash()
        {
            var age = Time.time - damageFlashStartTime;
            if (age < 0f || age > DamageFlashDurationSeconds)
            {
                return;
            }

            var intensity = 1f - age / DamageFlashDurationSeconds;
            DrawEdgeGlow(intensity);
        }

        private void DrawEdgeGlow(float intensity)
        {
            var edgeDepth = Mathf.Max(24f, Mathf.Min(Screen.width, Screen.height) * 0.12f);
            const int bands = 5;
            var bandDepth = edgeDepth / bands;
            var oldColor = GUI.color;

            for (var i = 0; i < bands; i++)
            {
                var bandAlpha = 0.36f * intensity * (1f - i / (float)bands);
                var offset = i * bandDepth;
                GUI.color = new Color(1f, 0.05f, 0.03f, bandAlpha);

                GUI.DrawTexture(new Rect(0f, offset, Screen.width, bandDepth), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0f, Screen.height - offset - bandDepth, Screen.width, bandDepth), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(offset, 0f, bandDepth, Screen.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(Screen.width - offset - bandDepth, 0f, bandDepth, Screen.height), Texture2D.whiteTexture);
            }

            GUI.color = oldColor;
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

            menuTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 42,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            menuHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            menuBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = true,
                normal = { textColor = new Color(0.84f, 0.88f, 0.94f, 1f) }
            };
        }

        public readonly struct LegendEntry
        {
            public LegendEntry(string name, Color color, string description)
            {
                Name = name;
                Color = color;
                Description = description;
            }

            public string Name { get; }
            public Color Color { get; }
            public string Description { get; }
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

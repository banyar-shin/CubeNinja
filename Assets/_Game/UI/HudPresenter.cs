using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CubeNinja.UI
{
    public sealed class HudPresenter : MonoBehaviour
    {
        private const float PopupLifetimeSeconds = 0.75f;
        private const float ClickEffectLifetimeSeconds = 0.32f;
        private const float DamageFlashDurationSeconds = 0.55f;
        private const int EdgeGlowBands = 5;
        private const int ClickEffectPixelCount = 14;

        private static readonly Color BackgroundColor = new Color(0.085f, 0.075f, 0.18f, 0.24f);
        private static readonly Color PanelColor = new Color(0.14f, 0.12f, 0.28f, 0.42f);
        private static readonly Color PanelAccentColor = new Color(0.78f, 0.62f, 1f, 0.9f);
        private static readonly Color ButtonColor = new Color(1f, 0.34f, 0.29f, 1f);
        private static readonly Color ButtonHoverColor = new Color(1f, 0.48f, 0.39f, 1f);
        private static readonly Color BodyTextColor = new Color(0.82f, 0.9f, 0.98f, 1f);
        private static readonly Color MutedTextColor = new Color(0.58f, 0.68f, 0.76f, 1f);
        private static readonly Color ScorePopupColor = new Color(1f, 1f, 1f, 1f);
        private static readonly Color ComboPopupColor = new Color(1f, 0.91f, 0.24f, 1f);
        private static readonly Color DamageColor = new Color(1f, 0.05f, 0.03f, 1f);

        [SerializeField] private Camera mainCamera;

        private readonly List<FloatingPopup> activePopups = new List<FloatingPopup>();
        private readonly List<ClickPixelEffect> activeClickEffects = new List<ClickPixelEffect>();
        private readonly List<GameObject> legendRows = new List<GameObject>();

        private Canvas canvas;
        private RectTransform canvasRect;
        private RectTransform popupRoot;
        private RectTransform clickEffectRoot;
        private GameObject hudRoot;
        private GameObject startMenuRoot;
        private GameObject gameOverRoot;
        private GameObject comboBadge;
        private RectTransform legendContent;
        private Image[] lifeImages;
        private Image[] edgeGlowImages;
        private TMP_Text scoreText;
        private TMP_Text highScoreText;
        private TMP_Text menuHighScoreText;
        private TMP_Text comboText;
        private TMP_Text gameOverScoreText;
        private TMP_Text gameOverHighScoreText;
        private TMP_Text comboRuleText;
        private int score;
        private int highScore;
        private int lives;
        private int comboMultiplier;
        private float comboRemainingSeconds;
        private float comboWindowSeconds = 0.5f;
        private float damageFlashStartTime = float.NegativeInfinity;
        private LegendEntry[] legendEntries = Array.Empty<LegendEntry>();
        private bool startMenuVisible;
        private bool gameOver;
        private bool built;

        public event Action StartRequested;
        public event Action RestartRequested;

        private void Awake()
        {
            BuildUi();
            RefreshHud();
            RefreshMenu();
            RefreshGameOver();
        }

        private void Update()
        {
            if (!built)
            {
                return;
            }

            UpdateDamageGlow();
            if (Input.GetMouseButtonDown(0))
            {
                CreateClickEffect(Input.mousePosition);
            }

            UpdatePopups();
            UpdateClickEffects();
        }

        public void SetCamera(Camera camera)
        {
            mainCamera = camera;
        }

        public void SetRunState(int newScore, int bestScore, int remainingLives, int combo, float comboRemaining, bool isGameOver)
        {
            score = Mathf.Max(0, newScore);
            highScore = Mathf.Max(0, bestScore);
            lives = Mathf.Max(0, remainingLives);
            comboMultiplier = Mathf.Max(0, combo);
            comboRemainingSeconds = Mathf.Max(0f, comboRemaining);
            gameOver = isGameOver;

            BuildUi();
            RefreshHud();
            RefreshMenu();
            RefreshGameOver();
        }

        public void SetStartMenuVisible(bool visible)
        {
            startMenuVisible = visible;
            BuildUi();
            RefreshMenu();
        }

        public void SetStartMenuInfo(LegendEntry[] entries, float comboWindow)
        {
            legendEntries = entries ?? Array.Empty<LegendEntry>();
            comboWindowSeconds = Mathf.Max(0.01f, comboWindow);

            BuildUi();
            RebuildLegend();
            RefreshMenu();
        }

        public void ShowScorePopup(Vector3 worldPosition, int pointsAdded, int multiplier)
        {
            BuildUi();

            var safePoints = Mathf.Max(0, pointsAdded);
            var text = multiplier > 1 ? $"+{safePoints}  Combo x{multiplier}" : $"+{safePoints}";
            var color = multiplier > 1 ? ComboPopupColor : ScorePopupColor;
            CreateFloatingPopup(worldPosition, text, color, multiplier > 1 ? 32f : 28f);
        }

        public void ShowLifeLostPopup(Vector3 worldPosition)
        {
            BuildUi();
            CreateFloatingPopup(worldPosition, "Life -1", new Color(1f, 0.23f, 0.18f, 1f), 28f);
            damageFlashStartTime = Time.time;
            UpdateDamageGlow();
        }

        public void ClearPopups()
        {
            for (var i = activePopups.Count - 1; i >= 0; i--)
            {
                if (activePopups[i].GameObject != null)
                {
                    Destroy(activePopups[i].GameObject);
                }
            }

            activePopups.Clear();

            for (var i = activeClickEffects.Count - 1; i >= 0; i--)
            {
                if (activeClickEffects[i].GameObject != null)
                {
                    Destroy(activeClickEffects[i].GameObject);
                }
            }

            activeClickEffects.Clear();
            damageFlashStartTime = float.NegativeInfinity;
            UpdateDamageGlow();
        }

        private void BuildUi()
        {
            if (built)
            {
                return;
            }

            EnsureEventSystem();

            var canvasObject = CreateUiObject("CubeNinja Canvas", transform);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            canvasRect = canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRect);

            hudRoot = CreateUiObject("HUD", canvasRect);
            Stretch(hudRoot.GetComponent<RectTransform>());
            BuildHud();

            popupRoot = CreateUiObject("Floating Popups", canvasRect).GetComponent<RectTransform>();
            Stretch(popupRoot);

            startMenuRoot = CreateUiObject("Start Menu", canvasRect);
            Stretch(startMenuRoot.GetComponent<RectTransform>());
            BuildStartMenu();

            gameOverRoot = CreateUiObject("Game Over", canvasRect);
            Stretch(gameOverRoot.GetComponent<RectTransform>());
            BuildGameOver();

            BuildEdgeGlow();

            clickEffectRoot = CreateUiObject("Click Pixel Effects", canvasRect).GetComponent<RectTransform>();
            Stretch(clickEffectRoot);

            built = true;
        }

        private void BuildHud()
        {
            var scorePanel = CreatePanel("Score Panel", hudRoot.transform, new Color(0.12f, 0.105f, 0.24f, 0.78f), PanelAccentColor);
            var scoreRect = scorePanel.GetComponent<RectTransform>();
            Anchor(scoreRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -26f), new Vector2(320f, 86f));
            scoreText = CreateText("Score", scorePanel.transform, "Score 0", 33f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
            Anchor(scoreText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(24f, -8f), new Vector2(-42f, 44f));

            highScoreText = CreateText("High Score", scorePanel.transform, "Best 0", 17f, FontStyles.Bold, ComboPopupColor, TextAlignmentOptions.Left);
            highScoreText.textWrappingMode = TextWrappingModes.NoWrap;
            Anchor(highScoreText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(24f, 12f), new Vector2(-42f, 24f));

            comboBadge = CreatePanel("Combo Badge", hudRoot.transform, new Color(0.96f, 0.58f, 0.48f, 0.9f), ButtonHoverColor);
            var comboRect = comboBadge.GetComponent<RectTransform>();
            Anchor(comboRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(300f, 60f));
            comboText = CreateText("Combo", comboBadge.transform, "Combo x2", 28f, FontStyles.Bold, new Color(0.13f, 0.055f, 0.015f, 1f), TextAlignmentOptions.Center);
            Stretch(comboText.rectTransform, new Vector2(12f, 0f), new Vector2(-12f, 0f));

            var livesPanel = CreatePanel("Lives Panel", hudRoot.transform, new Color(0.12f, 0.105f, 0.24f, 0.78f), ButtonColor);
            var livesRect = livesPanel.GetComponent<RectTransform>();
            Anchor(livesRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -26f), new Vector2(176f, 70f));

            var livesLayout = livesPanel.AddComponent<HorizontalLayoutGroup>();
            livesLayout.padding = new RectOffset(18, 18, 16, 16);
            livesLayout.spacing = 12f;
            livesLayout.childAlignment = TextAnchor.MiddleCenter;
            livesLayout.childControlWidth = true;
            livesLayout.childControlHeight = true;
            livesLayout.childForceExpandWidth = false;
            livesLayout.childForceExpandHeight = false;

            lifeImages = new Image[3];
            for (var i = 0; i < lifeImages.Length; i++)
            {
                var life = CreateUiObject($"Life {i + 1}", livesPanel.transform);
                var image = life.AddComponent<Image>();
                image.color = new Color(0.94f, 0.16f, 0.18f, 1f);
                image.raycastTarget = false;
                var layout = life.AddComponent<LayoutElement>();
                layout.preferredWidth = 34f;
                layout.preferredHeight = 34f;
                lifeImages[i] = image;
            }
        }

        private void BuildStartMenu()
        {
            var blocker = startMenuRoot.AddComponent<Image>();
            blocker.color = BackgroundColor;

            var panel = CreatePanel("Menu Panel", startMenuRoot.transform, PanelColor, PanelAccentColor);
            var panelRect = panel.GetComponent<RectTransform>();
            Anchor(panelRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 700f));

            var title = CreateText("Title", panel.transform, "CUBE NINJA", 56f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(-72f, 76f));
            title.characterSpacing = 5f;

            var subtitle = CreateText(
                "Subtitle",
                panel.transform,
                "Hit the scoring cubes. Chain hits. Do not click the danger cube.",
                22f,
                FontStyles.Normal,
                BodyTextColor,
                TextAlignmentOptions.Center);
            Anchor(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(-96f, 54f));

            menuHighScoreText = CreateText("Menu High Score", panel.transform, "Best 0", 20f, FontStyles.Bold, ComboPopupColor, TextAlignmentOptions.Left);
            menuHighScoreText.textWrappingMode = TextWrappingModes.NoWrap;
            Anchor(menuHighScoreText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(64f, 70f), new Vector2(220f, 30f));

            var cubesHeader = CreateText("Cube Header", panel.transform, "Cube Rules", 24f, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            Anchor(cubesHeader.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(64f, -176f), new Vector2(-128f, 32f));

            legendContent = CreateUiObject("Cube Legend", panel.transform).GetComponent<RectTransform>();
            Anchor(legendContent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(64f, -222f), new Vector2(-128f, 210f));

            var comboHeader = CreateText("Combo Header", panel.transform, "Combo Window", 24f, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            Anchor(comboHeader.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(64f, -462f), new Vector2(-128f, 32f));

            comboRuleText = CreateText("Combo Rule", panel.transform, string.Empty, 20f, FontStyles.Normal, BodyTextColor, TextAlignmentOptions.Left);
            comboRuleText.textWrappingMode = TextWrappingModes.Normal;
            Anchor(comboRuleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(64f, -500f), new Vector2(-128f, 62f));

            var startButton = CreateButton("Start Button", panel.transform, "START RUN", ButtonColor, ButtonHoverColor);
            Anchor(startButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(230f, 62f));
            startButton.onClick.AddListener(() => StartRequested?.Invoke());
        }

        private void BuildGameOver()
        {
            var blocker = gameOverRoot.AddComponent<Image>();
            blocker.color = BackgroundColor;

            var panel = CreatePanel("Game Over Panel", gameOverRoot.transform, PanelColor, PanelAccentColor);
            var panelRect = panel.GetComponent<RectTransform>();
            Anchor(panelRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 390f));

            var title = CreateText("Game Over Title", panel.transform, "GAME OVER", 72f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(-80f, 92f));
            title.characterSpacing = 6f;

            gameOverScoreText = CreateText("Final Score", panel.transform, "Final Score 0", 30f, FontStyles.Bold, BodyTextColor, TextAlignmentOptions.Center);
            Anchor(gameOverScoreText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(-80f, 44f));

            gameOverHighScoreText = CreateText("Final High Score", panel.transform, "Best 0", 22f, FontStyles.Bold, ComboPopupColor, TextAlignmentOptions.Center);
            Anchor(gameOverHighScoreText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(-80f, 34f));

            var restartButton = CreateButton("Restart Button", panel.transform, "RESTART", ButtonColor, ButtonHoverColor);
            Anchor(restartButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(210f, 58f));
            restartButton.onClick.AddListener(() => RestartRequested?.Invoke());
        }

        private void BuildEdgeGlow()
        {
            var root = CreateUiObject("Red Edge Glow", canvasRect);
            Stretch(root.GetComponent<RectTransform>());

            edgeGlowImages = new Image[EdgeGlowBands * 4];
            for (var band = 0; band < EdgeGlowBands; band++)
            {
                var index = band * 4;
                edgeGlowImages[index] = CreateEdgeBand(root.transform, $"Top Edge {band}", EdgeSide.Top, band);
                edgeGlowImages[index + 1] = CreateEdgeBand(root.transform, $"Bottom Edge {band}", EdgeSide.Bottom, band);
                edgeGlowImages[index + 2] = CreateEdgeBand(root.transform, $"Left Edge {band}", EdgeSide.Left, band);
                edgeGlowImages[index + 3] = CreateEdgeBand(root.transform, $"Right Edge {band}", EdgeSide.Right, band);
            }

            SetEdgeGlowIntensity(0f);
        }

        private void RefreshHud()
        {
            if (!built || hudRoot == null || scoreText == null || highScoreText == null || comboBadge == null || comboText == null || lifeImages == null)
            {
                return;
            }

            hudRoot.SetActive(!startMenuVisible);
            scoreText.text = $"Score {score}";
            highScoreText.text = $"Best {highScore}";

            var comboActive = comboMultiplier > 1 && comboRemainingSeconds > 0f && !gameOver;
            comboBadge.SetActive(comboActive);
            if (comboActive)
            {
                comboText.text = $"Combo x{comboMultiplier}";
            }

            for (var i = 0; i < lifeImages.Length; i++)
            {
                lifeImages[i].color = i < lives
                    ? new Color(0.94f, 0.16f, 0.18f, 1f)
                    : new Color(0.18f, 0.15f, 0.28f, 0.82f);
            }
        }

        private void RefreshMenu()
        {
            if (!built || startMenuRoot == null || hudRoot == null || comboRuleText == null || legendContent == null || menuHighScoreText == null)
            {
                return;
            }

            startMenuRoot.SetActive(startMenuVisible);
            hudRoot.SetActive(!startMenuVisible);
            menuHighScoreText.text = $"Best {highScore}";
            comboRuleText.text = $"Score again within {comboWindowSeconds:0.##} seconds to grow the multiplier. A streak turns +1 into +2, +3, and more.";
        }

        private void RefreshGameOver()
        {
            if (!built || gameOverRoot == null || gameOverScoreText == null || gameOverHighScoreText == null)
            {
                return;
            }

            gameOverRoot.SetActive(gameOver && !startMenuVisible);
            gameOverScoreText.text = $"Final Score {score}";
            gameOverHighScoreText.text = $"Best {highScore}";
            UpdateDamageGlow();
        }

        private void RebuildLegend()
        {
            if (legendContent == null)
            {
                return;
            }

            for (var i = legendRows.Count - 1; i >= 0; i--)
            {
                if (legendRows[i] != null)
                {
                    Destroy(legendRows[i]);
                }
            }

            legendRows.Clear();

            for (var i = 0; i < legendEntries.Length; i++)
            {
                var entry = legendEntries[i];
                var row = CreateUiObject($"{entry.Name} Row", legendContent);
                var rowRect = row.GetComponent<RectTransform>();
                Anchor(rowRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -i * 68f), new Vector2(0f, 60f));

                var swatch = CreateUiObject("Swatch", row.transform);
                var swatchImage = swatch.AddComponent<Image>();
                swatchImage.color = entry.Color;
                swatchImage.raycastTarget = false;
                var swatchOutline = swatch.AddComponent<Outline>();
                swatchOutline.effectColor = new Color(1f, 1f, 1f, 0.26f);
                swatchOutline.effectDistance = new Vector2(1f, -1f);
                Anchor(swatch.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(46f, 46f));

                var title = CreateText("Name", row.transform, entry.Name, 18f, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
                title.textWrappingMode = TextWrappingModes.NoWrap;
                title.enableAutoSizing = true;
                title.fontSizeMin = 14f;
                title.fontSizeMax = 18f;
                Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(64f, -2f), new Vector2(-64f, 24f));

                var detail = CreateText("Description", row.transform, entry.Description, 15f, FontStyles.Normal, MutedTextColor, TextAlignmentOptions.Left);
                detail.textWrappingMode = TextWrappingModes.Normal;
                detail.enableAutoSizing = true;
                detail.fontSizeMin = 12f;
                detail.fontSizeMax = 15f;
                Anchor(detail.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(64f, -28f), new Vector2(-64f, 32f));

                legendRows.Add(row);
            }
        }

        private void CreateFloatingPopup(Vector3 worldPosition, string text, Color color, float fontSize)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            var screenPoint = mainCamera != null
                ? mainCamera.WorldToScreenPoint(worldPosition)
                : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

            if (screenPoint.z < 0f)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(popupRoot, screenPoint, null, out var localPoint);

            var popupText = CreateText("Score Popup", popupRoot, text, fontSize, FontStyles.Bold, color, TextAlignmentOptions.Center);
            popupText.rectTransform.anchoredPosition = localPoint;
            popupText.rectTransform.sizeDelta = new Vector2(360f, 66f);
            popupText.raycastTarget = false;

            activePopups.Add(new FloatingPopup(popupText.gameObject, popupText.rectTransform, popupText, localPoint, Time.time, color));
        }

        private void CreateClickEffect(Vector2 screenPosition)
        {
            if (clickEffectRoot == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(clickEffectRoot, screenPosition, null, out var localPoint);

            for (var i = 0; i < ClickEffectPixelCount; i++)
            {
                var angle = Mathf.PI * 2f * i / ClickEffectPixelCount;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var startRadius = i % 2 == 0 ? 5f : 10f;
                var travelDistance = i % 3 == 0 ? 60f : 44f;
                var startSize = i % 3 == 0 ? 6f : 4f;

                var pixelObject = CreateUiObject("Click Pixel", clickEffectRoot);
                var image = pixelObject.AddComponent<Image>();
                image.color = Color.white;
                image.raycastTarget = false;

                var rect = pixelObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = localPoint + direction * startRadius;
                rect.sizeDelta = Vector2.one * startSize;
                rect.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);

                activeClickEffects.Add(new ClickPixelEffect(pixelObject, rect, image, localPoint, direction, startRadius, travelDistance, startSize, Time.time));
            }
        }

        private void UpdatePopups()
        {
            for (var i = activePopups.Count - 1; i >= 0; i--)
            {
                var popup = activePopups[i];
                var age = Time.time - popup.StartTime;
                if (age >= PopupLifetimeSeconds)
                {
                    if (popup.GameObject != null)
                    {
                        Destroy(popup.GameObject);
                    }

                    activePopups.RemoveAt(i);
                    continue;
                }

                var t = age / PopupLifetimeSeconds;
                popup.RectTransform.anchoredPosition = popup.StartPosition + Vector2.up * (68f * t);
                popup.Text.color = new Color(popup.Color.r, popup.Color.g, popup.Color.b, 1f - t);
            }
        }

        private void UpdateClickEffects()
        {
            for (var i = activeClickEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeClickEffects[i];
                var age = Time.time - effect.StartTime;
                if (age >= ClickEffectLifetimeSeconds)
                {
                    if (effect.GameObject != null)
                    {
                        Destroy(effect.GameObject);
                    }

                    activeClickEffects.RemoveAt(i);
                    continue;
                }

                var t = age / ClickEffectLifetimeSeconds;
                var eased = 1f - (1f - t) * (1f - t);
                var distance = effect.StartRadius + effect.TravelDistance * eased;
                effect.RectTransform.anchoredPosition = effect.Origin + effect.Direction * distance;
                effect.RectTransform.sizeDelta = Vector2.one * Mathf.Lerp(effect.StartSize, 1f, t);
                effect.Image.color = new Color(1f, 1f, 1f, 1f - t);
            }
        }

        private void UpdateDamageGlow()
        {
            if (!built)
            {
                return;
            }

            if (gameOver && !startMenuVisible)
            {
                SetEdgeGlowIntensity(0.65f);
                return;
            }

            var age = Time.time - damageFlashStartTime;
            if (age < 0f || age > DamageFlashDurationSeconds)
            {
                SetEdgeGlowIntensity(0f);
                return;
            }

            SetEdgeGlowIntensity(1f - age / DamageFlashDurationSeconds);
        }

        private void SetEdgeGlowIntensity(float intensity)
        {
            if (edgeGlowImages == null)
            {
                return;
            }

            var clamped = Mathf.Clamp01(intensity);
            for (var band = 0; band < EdgeGlowBands; band++)
            {
                var bandAlpha = 0.34f * clamped * (1f - band / (float)EdgeGlowBands);
                for (var side = 0; side < 4; side++)
                {
                    edgeGlowImages[(band * 4) + side].color = new Color(DamageColor.r, DamageColor.g, DamageColor.b, bandAlpha);
                }
            }
        }

        private GameObject CreatePanel(string name, Transform parent, Color fillColor, Color borderColor, bool blocksRaycasts = false)
        {
            var panel = CreateUiObject(name, parent);
            var image = panel.AddComponent<Image>();
            image.color = fillColor;
            image.raycastTarget = blocksRaycasts;

            var outline = panel.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(2f, -2f);

            var shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.02f, 0.015f, 0.05f, 0.28f);
            shadow.effectDistance = new Vector2(0f, -8f);

            return panel;
        }

        private Button CreateButton(string name, Transform parent, string label, Color normalColor, Color highlightColor)
        {
            var buttonObject = CreateUiObject(name, parent);
            var image = buttonObject.AddComponent<Image>();
            image.color = normalColor;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = normalColor,
                highlightedColor = highlightColor,
                pressedColor = new Color(0.9f, 0.22f, 0.18f, 1f),
                selectedColor = highlightColor,
                disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.55f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };

            var outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.75f, 0.42f, 0.72f);
            outline.effectDistance = new Vector2(2f, -2f);

            var text = CreateText("Label", buttonObject.transform, label, 24f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            text.characterSpacing = 2.5f;
            Stretch(text.rectTransform, new Vector2(10f, 0f), new Vector2(-10f, 0f));

            return button;
        }

        private TMP_Text CreateText(string name, Transform parent, string value, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            var textObject = CreateUiObject(name, parent);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
            text.extraPadding = true;

            var outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.86f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;

            return text;
        }

        private Image CreateEdgeBand(Transform parent, string name, EdgeSide side, int band)
        {
            var imageObject = CreateUiObject(name, parent);
            var image = imageObject.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;

            var rect = imageObject.GetComponent<RectTransform>();
            var bandDepth = 24f;
            var offset = band * bandDepth;

            switch (side)
            {
                case EdgeSide.Top:
                    Anchor(rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -offset), new Vector2(0f, bandDepth));
                    break;
                case EdgeSide.Bottom:
                    Anchor(rect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, offset), new Vector2(0f, bandDepth));
                    break;
                case EdgeSide.Left:
                    Anchor(rect, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(offset, 0f), new Vector2(bandDepth, 0f));
                    break;
                case EdgeSide.Right:
                    Anchor(rect, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-offset, 0f), new Vector2(bandDepth, 0f));
                    break;
            }

            return image;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void Anchor(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
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

        private sealed class FloatingPopup
        {
            public FloatingPopup(GameObject gameObject, RectTransform rectTransform, TMP_Text text, Vector2 startPosition, float startTime, Color color)
            {
                GameObject = gameObject;
                RectTransform = rectTransform;
                Text = text;
                StartPosition = startPosition;
                StartTime = startTime;
                Color = color;
            }

            public GameObject GameObject { get; }
            public RectTransform RectTransform { get; }
            public TMP_Text Text { get; }
            public Vector2 StartPosition { get; }
            public float StartTime { get; }
            public Color Color { get; }
        }

        private sealed class ClickPixelEffect
        {
            public ClickPixelEffect(
                GameObject gameObject,
                RectTransform rectTransform,
                Image image,
                Vector2 origin,
                Vector2 direction,
                float startRadius,
                float travelDistance,
                float startSize,
                float startTime)
            {
                GameObject = gameObject;
                RectTransform = rectTransform;
                Image = image;
                Origin = origin;
                Direction = direction;
                StartRadius = startRadius;
                TravelDistance = travelDistance;
                StartSize = startSize;
                StartTime = startTime;
            }

            public GameObject GameObject { get; }
            public RectTransform RectTransform { get; }
            public Image Image { get; }
            public Vector2 Origin { get; }
            public Vector2 Direction { get; }
            public float StartRadius { get; }
            public float TravelDistance { get; }
            public float StartSize { get; }
            public float StartTime { get; }
        }

        private enum EdgeSide
        {
            Top,
            Bottom,
            Left,
            Right
        }
    }
}

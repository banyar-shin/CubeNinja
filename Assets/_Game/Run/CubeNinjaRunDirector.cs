using System.Collections.Generic;
using CubeNinja.Core;
using CubeNinja.Data;
using CubeNinja.Gameplay;
using CubeNinja.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace CubeNinja.Run
{
    public sealed class CubeNinjaRunDirector : MonoBehaviour, ICubeTargetListener
    {
        private const string BackgroundResourcePath = "Backgrounds/shrine_background";
        private const string HighScorePrefsKey = "CubeNinja.HighScore";
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        [SerializeField] private CubeSpawnSettings spawnSettings;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private HudPresenter hudPresenter;
        [SerializeField] private CubeTarget cubePrefab;
        [SerializeField] private AudioFeedbackPlayer audioFeedback;
        [SerializeField] private Texture2D backgroundTexture;
        [SerializeField] private float spawnPlaneZ;

        private readonly List<CubeTarget> activeCubes = new List<CubeTarget>();
        private ComponentPool<CubeTarget> cubePool;
        private ScoreComboTracker comboTracker;
        private Transform backgroundTransform;
        private MeshRenderer backgroundRenderer;
        private Material backgroundMaterial;
        private int score;
        private int highScore;
        private int lives;
        private float spawnTimer;
        private bool waitingToStart;
        private bool gameOver;

        public int Score => score;
        public int HighScore => highScore;
        public int Lives => lives;
        public bool IsGameOver => gameOver;

        private CubeSpawnSettings Settings
        {
            get
            {
                if (spawnSettings == null)
                {
                    spawnSettings = CubeSpawnSettings.CreateDefaultRuntime();
                }

                return spawnSettings;
            }
        }

        private void Awake()
        {
            highScore = Mathf.Max(0, PlayerPrefs.GetInt(HighScorePrefsKey, 0));
            EnsureSceneServices();
            comboTracker = new ScoreComboTracker(Settings.ComboWindowSeconds);
            cubePool = new ComponentPool<CubeTarget>(CreateCubeTarget);
            EnterStartMenu();
        }

        private void OnDestroy()
        {
            if (hudPresenter != null)
            {
                hudPresenter.StartRequested -= StartRun;
                hudPresenter.RestartRequested -= RestartRun;
            }
        }

        private void Update()
        {
            UpdateBackgroundTransform();

            if (waitingToStart)
            {
                UpdateHud();
                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartRun();
            }

            if (gameOver)
            {
                UpdateHud();
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnCube();
                var interval = Settings.SpawnIntervalRange;
                spawnTimer = Random.Range(interval.x, interval.y);
            }

            UpdateHud();
        }

        public void StartRun()
        {
            RestartRun();
        }

        public void RestartRun()
        {
            for (var i = activeCubes.Count - 1; i >= 0; i--)
            {
                ReleaseCube(activeCubes[i]);
            }

            activeCubes.Clear();
            score = 0;
            lives = Settings.StartingLives;
            spawnTimer = 0.2f;
            waitingToStart = false;
            gameOver = false;
            comboTracker.Reset();

            hudPresenter?.ClearPopups();
            hudPresenter?.SetStartMenuVisible(false);
            audioFeedback?.PlayGameStart();
            UpdateHud();
        }

        public void OnCubeClicked(CubeTarget target)
        {
            if (target == null)
            {
                return;
            }

            var type = target.CubeType;
            var popupPosition = target.transform.position;
            ReleaseCube(target);

            if (waitingToStart || gameOver || type == null)
            {
                return;
            }

            if (type.ClickOutcome == CubeClickOutcome.LoseLife)
            {
                comboTracker.Reset();
                LoseLife(popupPosition);
                return;
            }

            var multiplier = comboTracker.RegisterScoreHit(Time.time);
            var pointsAdded = type.PointValue * multiplier;
            score += pointsAdded;
            TryUpdateHighScore();
            hudPresenter?.ShowScorePopup(popupPosition, pointsAdded, multiplier);
            audioFeedback?.PlayScore(multiplier);

            UpdateHud();
        }

        public void OnCubeMissed(CubeTarget target)
        {
            if (target == null)
            {
                return;
            }

            var type = target.CubeType;
            var popupPosition = target.transform.position;
            ReleaseCube(target);

            if (waitingToStart || gameOver)
            {
                return;
            }

            if (type != null && type.ClickOutcome == CubeClickOutcome.LoseLife)
            {
                return;
            }

            comboTracker.Reset();
            LoseLife(popupPosition);
        }

        private void SpawnCube()
        {
            var type = Settings.PickCubeType(Random.value);
            if (type == null)
            {
                return;
            }

            var target = cubePool.Get();
            var bottom = ViewportToPlane(0.5f, 0f);
            var left = ViewportToPlane(0f, 0f);
            var right = ViewportToPlane(1f, 0f);
            var horizontalInset = Settings.CubeScale * 0.5f;
            var leftBoundX = left.x + horizontalInset;
            var rightBoundX = right.x - horizontalInset;
            var xPoint = ViewportToPlane(Random.Range(0.12f, 0.88f), 0f);
            var spawnX = Mathf.Clamp(xPoint.x, leftBoundX, rightBoundX);
            var spawnPosition = new Vector3(spawnX, bottom.y - Settings.SpawnPadding, spawnPlaneZ);
            var entryY = bottom.y + 0.1f;
            var missY = bottom.y - Settings.MissPadding;
            var launchSpeed = Random.Range(Settings.LaunchVelocityRange.x, Settings.LaunchVelocityRange.y);
            var velocity = new Vector3(Random.Range(-Settings.HorizontalVelocityRange, Settings.HorizontalVelocityRange), launchSpeed, 0f);
            var angular = Random.insideUnitSphere * Settings.AngularVelocityRange;

            target.Initialize(type, this, entryY, missY, leftBoundX, rightBoundX, Settings.CubeScale);
            target.Launch(spawnPosition, velocity, angular);
            activeCubes.Add(target);
        }

        private CubeTarget CreateCubeTarget()
        {
            CubeTarget target;
            if (cubePrefab != null)
            {
                target = Instantiate(cubePrefab, transform);
            }
            else
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Cube Target";
                cube.transform.SetParent(transform);
                target = cube.GetComponent<CubeTarget>();
                if (target == null)
                {
                    target = cube.AddComponent<CubeTarget>();
                }
            }

            var body = target.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = target.gameObject.AddComponent<Rigidbody>();
            }

            body.useGravity = true;
            target.PrepareForPool();
            target.gameObject.SetActive(false);
            return target;
        }

        private void ReleaseCube(CubeTarget target)
        {
            if (target == null)
            {
                return;
            }

            activeCubes.Remove(target);
            target.PrepareForPool();
            cubePool.Release(target);
        }

        private void LoseLife(Vector3 popupPosition)
        {
            lives = Mathf.Max(0, lives - 1);
            hudPresenter?.ShowLifeLostPopup(popupPosition);
            audioFeedback?.PlayLifeLost();

            if (lives <= 0)
            {
                EndRun();
            }

            UpdateHud();
        }

        private void EndRun()
        {
            TryUpdateHighScore();
            gameOver = true;
            audioFeedback?.PlayGameOver();

            for (var i = activeCubes.Count - 1; i >= 0; i--)
            {
                ReleaseCube(activeCubes[i]);
            }

            activeCubes.Clear();
        }

        private void EnsureSceneServices()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                mainCamera = Object.FindFirstObjectByType<Camera>();
            }

            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            mainCamera.tag = "MainCamera";
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 5.5f;
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            mainCamera.transform.rotation = Quaternion.identity;
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.98f, 0.68f, 0.76f, 1f);
            EnsureBackground();

            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lightObject = new GameObject("Directional Light");
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.3f;
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            if (hudPresenter == null)
            {
                hudPresenter = Object.FindFirstObjectByType<HudPresenter>();
            }

            if (hudPresenter == null)
            {
                var hudObject = new GameObject("HUD");
                hudPresenter = hudObject.AddComponent<HudPresenter>();
            }

            hudPresenter.SetCamera(mainCamera);
            hudPresenter.RestartRequested -= RestartRun;
            hudPresenter.RestartRequested += RestartRun;
            hudPresenter.StartRequested -= StartRun;
            hudPresenter.StartRequested += StartRun;
            ConfigureStartMenuInfo();

            if (audioFeedback == null)
            {
                audioFeedback = Object.FindFirstObjectByType<AudioFeedbackPlayer>();
            }

            if (audioFeedback == null)
            {
                var audioObject = new GameObject("Audio Feedback");
                audioFeedback = audioObject.AddComponent<AudioFeedbackPlayer>();
            }
        }

        private void EnsureBackground()
        {
            if (backgroundTexture == null)
            {
                backgroundTexture = Resources.Load<Texture2D>(BackgroundResourcePath);
            }

            if (backgroundTexture == null || mainCamera == null)
            {
                return;
            }

            if (backgroundTransform == null)
            {
                var backgroundObject = GameObject.Find("Shrine Background");
                if (backgroundObject == null)
                {
                    backgroundObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    backgroundObject.name = "Shrine Background";
                }

                backgroundTransform = backgroundObject.transform;
                backgroundRenderer = backgroundObject.GetComponent<MeshRenderer>();

                var backgroundCollider = backgroundObject.GetComponent<Collider>();
                if (backgroundCollider != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(backgroundCollider);
                    }
                    else
                    {
                        DestroyImmediate(backgroundCollider);
                    }
                }
            }

            if (backgroundRenderer == null)
            {
                backgroundRenderer = backgroundTransform.GetComponent<MeshRenderer>();
            }

            if (backgroundRenderer == null)
            {
                return;
            }

            if (backgroundMaterial == null)
            {
                var shader = FindBackgroundShader();
                if (shader == null)
                {
                    return;
                }

                backgroundMaterial = new Material(shader)
                {
                    name = "Shrine Background Runtime Material",
                    hideFlags = HideFlags.DontSave
                };
            }

            if (backgroundMaterial.HasProperty(MainTexId))
            {
                backgroundMaterial.SetTexture(MainTexId, backgroundTexture);
            }

            if (backgroundMaterial.HasProperty(BaseMapId))
            {
                backgroundMaterial.SetTexture(BaseMapId, backgroundTexture);
            }

            backgroundRenderer.sharedMaterial = backgroundMaterial;
            backgroundRenderer.shadowCastingMode = ShadowCastingMode.Off;
            backgroundRenderer.receiveShadows = false;

            UpdateBackgroundTransform();
        }

        private void EnterStartMenu()
        {
            for (var i = activeCubes.Count - 1; i >= 0; i--)
            {
                ReleaseCube(activeCubes[i]);
            }

            activeCubes.Clear();
            score = 0;
            lives = Settings.StartingLives;
            spawnTimer = 0.2f;
            waitingToStart = true;
            gameOver = false;
            comboTracker.Reset();

            hudPresenter?.ClearPopups();
            hudPresenter?.SetStartMenuVisible(true);
            ConfigureStartMenuInfo();
            UpdateHud();
        }

        private void ConfigureStartMenuInfo()
        {
            if (hudPresenter == null)
            {
                return;
            }

            var entries = new List<HudPresenter.LegendEntry>();
            var cubeTypes = Settings.CubeTypes;
            if (cubeTypes != null)
            {
                for (var i = 0; i < cubeTypes.Length; i++)
                {
                    var cubeType = cubeTypes[i].CubeType;
                    if (cubeType == null)
                    {
                        continue;
                    }

                    var description = cubeType.ClickOutcome == CubeClickOutcome.LoseLife
                        ? "Costs 1 life if clicked. Safe if missed."
                        : $"+{cubeType.PointValue} point{(cubeType.PointValue == 1 ? string.Empty : "s")} before combo.";
                    entries.Add(new HudPresenter.LegendEntry(cubeType.DisplayName, cubeType.Color, description));
                }
            }

            hudPresenter.SetStartMenuInfo(entries.ToArray(), Settings.ComboWindowSeconds);
        }

        private void UpdateBackgroundTransform()
        {
            if (backgroundTransform == null || mainCamera == null || backgroundTexture == null)
            {
                return;
            }

            var viewportHeight = mainCamera.orthographicSize * 2f;
            var viewportWidth = viewportHeight * mainCamera.aspect;
            var textureAspect = backgroundTexture.width / (float)backgroundTexture.height;
            var viewportAspect = viewportWidth / viewportHeight;

            var width = viewportWidth;
            var height = viewportHeight;
            if (textureAspect > viewportAspect)
            {
                width = viewportHeight * textureAspect;
            }
            else
            {
                height = viewportWidth / textureAspect;
            }

            backgroundTransform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, spawnPlaneZ + 8f);
            backgroundTransform.rotation = Quaternion.identity;
            backgroundTransform.localScale = new Vector3(width, height, 1f);
        }

        private Vector3 ViewportToPlane(float x, float y)
        {
            var distance = Mathf.Abs(mainCamera.transform.position.z - spawnPlaneZ);
            return mainCamera.ViewportToWorldPoint(new Vector3(x, y, distance));
        }

        private void UpdateHud()
        {
            if (hudPresenter == null || comboTracker == null)
            {
                return;
            }

            hudPresenter.SetRunState(
                score,
                highScore,
                lives,
                comboTracker.CurrentMultiplier,
                comboTracker.GetWindowRemaining(Time.time),
                gameOver);
        }

        private void TryUpdateHighScore()
        {
            if (score <= highScore)
            {
                return;
            }

            highScore = score;
            PlayerPrefs.SetInt(HighScorePrefsKey, highScore);
            PlayerPrefs.Save();
        }

        private static Shader FindBackgroundShader()
        {
            return Shader.Find("Unlit/Texture")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Standard");
        }
    }
}

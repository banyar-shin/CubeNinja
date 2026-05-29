using System.Collections.Generic;
using CubeNinja.Core;
using CubeNinja.Data;
using CubeNinja.Gameplay;
using CubeNinja.UI;
using UnityEngine;

namespace CubeNinja.Run
{
    public sealed class CubeNinjaRunDirector : MonoBehaviour, ICubeTargetListener
    {
        [SerializeField] private CubeSpawnSettings spawnSettings;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private HudPresenter hudPresenter;
        [SerializeField] private CubeTarget cubePrefab;
        [SerializeField] private float spawnPlaneZ;

        private readonly List<CubeTarget> activeCubes = new List<CubeTarget>();
        private ComponentPool<CubeTarget> cubePool;
        private ScoreComboTracker comboTracker;
        private int score;
        private int lives;
        private float spawnTimer;
        private bool gameOver;

        public int Score => score;
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
            EnsureSceneServices();
            comboTracker = new ScoreComboTracker(Settings.ComboWindowSeconds);
            cubePool = new ComponentPool<CubeTarget>(CreateCubeTarget);
            RestartRun();
        }

        private void OnDestroy()
        {
            if (hudPresenter != null)
            {
                hudPresenter.RestartRequested -= RestartRun;
            }
        }

        private void Update()
        {
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
            gameOver = false;
            comboTracker.Reset();

            hudPresenter?.ClearPopups();
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

            if (gameOver || type == null)
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
            score += type.PointValue * multiplier;

            if (multiplier > 1)
            {
                hudPresenter?.ShowComboPopup(popupPosition, multiplier);
            }

            UpdateHud();
        }

        public void OnCubeMissed(CubeTarget target)
        {
            if (target == null)
            {
                return;
            }

            var popupPosition = target.transform.position;
            ReleaseCube(target);

            if (gameOver)
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
            var xPoint = ViewportToPlane(Random.Range(0.12f, 0.88f), 0f);
            var spawnPosition = new Vector3(xPoint.x, bottom.y - Settings.SpawnPadding, spawnPlaneZ);
            var entryY = bottom.y + 0.1f;
            var missY = bottom.y - Settings.MissPadding;
            var launchSpeed = Random.Range(Settings.LaunchVelocityRange.x, Settings.LaunchVelocityRange.y);
            var velocity = new Vector3(Random.Range(-Settings.HorizontalVelocityRange, Settings.HorizontalVelocityRange), launchSpeed, 0f);
            var angular = Random.insideUnitSphere * Settings.AngularVelocityRange;

            target.Initialize(type, this, entryY, missY, Settings.CubeScale);
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

            if (lives <= 0)
            {
                EndRun();
            }

            UpdateHud();
        }

        private void EndRun()
        {
            gameOver = true;
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
            mainCamera.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);

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
                lives,
                comboTracker.CurrentMultiplier,
                comboTracker.GetWindowRemaining(Time.time),
                gameOver);
        }
    }
}

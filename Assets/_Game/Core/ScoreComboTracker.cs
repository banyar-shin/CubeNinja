namespace CubeNinja.Core
{
    public sealed class ScoreComboTracker
    {
        private float lastHitTime;

        public ScoreComboTracker(float comboWindowSeconds)
        {
            ComboWindowSeconds = comboWindowSeconds > 0f ? comboWindowSeconds : 0.01f;
            Reset();
        }

        public float ComboWindowSeconds { get; }
        public int CurrentMultiplier { get; private set; }

        public int RegisterScoreHit(float nowSeconds)
        {
            var continuesCombo = CurrentMultiplier > 0 && nowSeconds - lastHitTime <= ComboWindowSeconds;
            CurrentMultiplier = continuesCombo ? CurrentMultiplier + 1 : 1;
            lastHitTime = nowSeconds;
            return CurrentMultiplier;
        }

        public float GetWindowRemaining(float nowSeconds)
        {
            if (CurrentMultiplier <= 0)
            {
                return 0f;
            }

            var remaining = ComboWindowSeconds - (nowSeconds - lastHitTime);
            return remaining > 0f ? remaining : 0f;
        }

        public void Reset()
        {
            CurrentMultiplier = 0;
            lastHitTime = float.NegativeInfinity;
        }
    }
}

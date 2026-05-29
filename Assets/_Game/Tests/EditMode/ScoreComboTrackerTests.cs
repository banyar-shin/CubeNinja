using CubeNinja.Core;
using NUnit.Framework;

namespace CubeNinja.Tests.EditMode
{
    public sealed class ScoreComboTrackerTests
    {
        [Test]
        public void FirstHitStartsAtSingleMultiplier()
        {
            var tracker = new ScoreComboTracker(0.5f);

            var multiplier = tracker.RegisterScoreHit(10f);

            Assert.That(multiplier, Is.EqualTo(1));
        }

        [Test]
        public void HitInsideComboWindowIncrementsMultiplier()
        {
            var tracker = new ScoreComboTracker(0.5f);

            tracker.RegisterScoreHit(10f);
            var multiplier = tracker.RegisterScoreHit(10.4f);

            Assert.That(multiplier, Is.EqualTo(2));
        }

        [Test]
        public void HitAfterComboWindowResetsMultiplier()
        {
            var tracker = new ScoreComboTracker(0.5f);

            tracker.RegisterScoreHit(10f);
            var multiplier = tracker.RegisterScoreHit(10.6f);

            Assert.That(multiplier, Is.EqualTo(1));
        }

        [Test]
        public void ResetClearsWindow()
        {
            var tracker = new ScoreComboTracker(0.5f);

            tracker.RegisterScoreHit(10f);
            tracker.Reset();

            Assert.That(tracker.CurrentMultiplier, Is.EqualTo(0));
            Assert.That(tracker.GetWindowRemaining(10.1f), Is.EqualTo(0f));
        }
    }
}

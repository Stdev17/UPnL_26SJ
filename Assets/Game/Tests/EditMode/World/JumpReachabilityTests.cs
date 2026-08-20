using NUnit.Framework;
using UPnL.SignalRush.World;

namespace UPnL.SignalRush.Tests.World
{
    public sealed class JumpReachabilityTests
    {
        [TestCase(8f, 10f, 3.2f)]
        [TestCase(6f, 18f, 1f)]
        public void MaxHeightUsesJumpVelocityAndGravity(float jumpVelocity, float gravity, float expected)
        {
            Assert.That(JumpReachability.MaxHeight(jumpVelocity, gravity), Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void AirTimeAndMaxGapIncludeFasterFalling()
        {
            Assert.That(JumpReachability.AirTime(8f, 10f, 2f), Is.EqualTo(1.365685f).Within(0.0001f));
            Assert.That(JumpReachability.MaxGap(5f, 8f, 10f, 2f), Is.EqualTo(6.828427f).Within(0.0001f));
        }

        [TestCase(0f, 10f)]
        [TestCase(8f, 0f)]
        [TestCase(-8f, 10f)]
        [TestCase(8f, -10f)]
        public void NonPositiveBallisticInputsAreUnreachable(float jumpVelocity, float gravity)
        {
            Assert.That(JumpReachability.MaxHeight(jumpVelocity, gravity), Is.Zero);
            Assert.That(JumpReachability.AirTime(jumpVelocity, gravity, 2f), Is.Zero);
            Assert.That(JumpReachability.MaxGap(5f, jumpVelocity, gravity, 2f), Is.Zero);
        }

        [Test]
        public void NonPositiveHorizontalSpeedHasNoGap()
        {
            Assert.That(JumpReachability.MaxGap(0f, 8f, 10f, 2f), Is.Zero);
            Assert.That(JumpReachability.MaxGap(-5f, 8f, 10f, 2f), Is.Zero);
        }

        [Test]
        public void NonPositiveFallMultiplierHasNoAirTimeOrGap()
        {
            Assert.That(JumpReachability.AirTime(8f, 10f, 0f), Is.Zero);
            Assert.That(JumpReachability.MaxGap(5f, 8f, 10f, -1f), Is.Zero);
        }
    }
}

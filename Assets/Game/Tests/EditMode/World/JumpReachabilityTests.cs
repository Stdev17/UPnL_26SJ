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
        public void AirTimeAndMaxGapUseTheSameBallisticModel()
        {
            Assert.That(JumpReachability.AirTime(8f, 10f), Is.EqualTo(1.6f).Within(0.0001f));
            Assert.That(JumpReachability.MaxGap(5f, 8f, 10f), Is.EqualTo(8f).Within(0.0001f));
        }

        [TestCase(0f, 10f)]
        [TestCase(8f, 0f)]
        [TestCase(-8f, 10f)]
        [TestCase(8f, -10f)]
        public void NonPositiveBallisticInputsAreUnreachable(float jumpVelocity, float gravity)
        {
            Assert.That(JumpReachability.MaxHeight(jumpVelocity, gravity), Is.Zero);
            Assert.That(JumpReachability.AirTime(jumpVelocity, gravity), Is.Zero);
            Assert.That(JumpReachability.MaxGap(5f, jumpVelocity, gravity), Is.Zero);
        }

        [Test]
        public void NonPositiveHorizontalSpeedHasNoGap()
        {
            Assert.That(JumpReachability.MaxGap(0f, 8f, 10f), Is.Zero);
            Assert.That(JumpReachability.MaxGap(-5f, 8f, 10f), Is.Zero);
        }
    }
}

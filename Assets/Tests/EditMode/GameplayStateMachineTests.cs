using System.Collections.Generic;
using NUnit.Framework;

namespace ChaosArena.Platform.Tests
{
    public sealed class GameplayStateMachineTests
    {
        [Test]
        public void BridgeReadyReportsCurrentGameplayIntentOnce()
        {
            var state = new GameplayStateMachine();
            var transitions = new List<bool>();
            state.GameplayStateChanged += transitions.Add;

            state.SetGameplayIntent(true);
            Assert.That(transitions, Is.Empty);

            state.SetBridgeReady();
            state.SetBridgeReady();

            Assert.That(transitions, Is.EqualTo(new[] { true }));
        }

        [Test]
        public void PlatformResumeDoesNotOverrideLocalPause()
        {
            var state = new GameplayStateMachine();
            var transitions = new List<bool>();
            state.GameplayStateChanged += transitions.Add;
            state.SetBridgeReady();
            state.SetGameplayIntent(true);
            state.SetLocalPause(true);
            state.SetPlatformPause(true);
            state.SetPlatformPause(false);

            Assert.That(state.ShouldRunGameplay, Is.False);
            Assert.That(state.ShouldPauseAudio, Is.True);
            Assert.That(transitions, Is.EqualTo(new[] { true, false }));

            state.SetLocalPause(false);

            Assert.That(state.ShouldRunGameplay, Is.True);
            Assert.That(transitions, Is.EqualTo(new[] { true, false, true }));
        }
    }
}

using NUnit.Framework;

namespace ChaosArena.Platform.Tests
{
    public sealed class FullscreenAdGateTests
    {
        [Test]
        public void UnavailableBridge_ContinuesImmediatelyWithoutCallingBridge()
        {
            var bridge = new FakeBridge { IsAvailable = false };
            var gate = new FullscreenAdGate(bridge, new FakeClock());
            var continuations = 0;

            Assert.That(gate.Request(() => continuations++), Is.True);

            Assert.That(continuations, Is.EqualTo(1));
            Assert.That(bridge.RequestIds, Is.Empty);
            Assert.That(gate.IsRequestPending, Is.False);
        }

        [Test]
        public void CloseAndDuplicateResponse_ContinueExactlyOnce()
        {
            var bridge = new FakeBridge();
            var gate = new FullscreenAdGate(bridge, new FakeClock());
            var continuations = 0;

            gate.Request(() => continuations++);
            var requestId = bridge.RequestIds[0];

            Assert.That(gate.HandleTerminalResponse(requestId), Is.True);
            Assert.That(gate.HandleTerminalResponse(requestId), Is.False);

            Assert.That(continuations, Is.EqualTo(1));
            Assert.That(gate.IsRequestPending, Is.False);
        }

        [Test]
        public void ErrorResponse_ContinuesExactlyOnce()
        {
            var bridge = new FakeBridge();
            var gate = new FullscreenAdGate(bridge, new FakeClock());
            var continuations = 0;

            gate.Request(() => continuations++);

            Assert.That(gate.HandleTerminalResponse(bridge.RequestIds[0]), Is.True);
            Assert.That(continuations, Is.EqualTo(1));
        }

        [Test]
        public void Timeout_FailsOpenAndLateResponseIsIgnored()
        {
            var clock = new FakeClock();
            var bridge = new FakeBridge();
            var gate = new FullscreenAdGate(bridge, clock, timeoutSeconds: 10f);
            var continuations = 0;

            gate.Request(() => continuations++);
            var requestId = bridge.RequestIds[0];
            clock.Now = 10f;
            gate.Tick();

            Assert.That(continuations, Is.EqualTo(1));
            Assert.That(gate.HandleTerminalResponse(requestId), Is.False);
        }

        [Test]
        public void CrossedRequestIdsAndDoubleTap_DoNotCompleteTheWrongRestart()
        {
            var bridge = new FakeBridge();
            var gate = new FullscreenAdGate(bridge, new FakeClock());
            var first = 0;
            var second = 0;

            Assert.That(gate.Request(() => first++), Is.True);
            var firstId = bridge.RequestIds[0];
            Assert.That(gate.Request(() => second++), Is.False, "A double tap must not enqueue a second request.");
            Assert.That(gate.HandleTerminalResponse(firstId), Is.True);

            Assert.That(gate.Request(() => second++), Is.True);
            var secondId = bridge.RequestIds[1];
            Assert.That(gate.HandleTerminalResponse(firstId), Is.False, "A late callback from the prior request is stale.");
            Assert.That(gate.HandleTerminalResponse("unknown-request"), Is.False);
            Assert.That(gate.HandleTerminalResponse(secondId), Is.True);

            Assert.That(first, Is.EqualTo(1));
            Assert.That(second, Is.EqualTo(1));
        }

        [Test]
        public void NewRunThenMainMenu_LateTerminalAndTimeoutCannotContinue()
        {
            var clock = new FakeClock();
            var bridge = new FakeBridge();
            var gate = new FullscreenAdGate(bridge, clock, timeoutSeconds: 10f);
            var navigation = new GameOverRestartNavigation();
            var restarts = 0;

            Assert.That(navigation.TryBegin(out var token), Is.True);
            Assert.That(gate.Request(() => { if (navigation.TryComplete(token, isGameOverScene: true)) restarts++; }), Is.True);
            var requestId = bridge.RequestIds[0];

            navigation.Cancel();
            Assert.That(gate.CancelPendingRequest(), Is.True);
            Assert.That(gate.HandleTerminalResponse(requestId), Is.False);
            clock.Now = 10f;
            gate.Tick();

            Assert.That(restarts, Is.Zero);
        }

        [Test]
        public void DuplicateTapAndSceneMismatch_CannotRestart()
        {
            var navigation = new GameOverRestartNavigation();

            Assert.That(navigation.TryBegin(out var token), Is.True);
            Assert.That(navigation.TryBegin(out _), Is.False);
            Assert.That(navigation.TryComplete(token, isGameOverScene: false), Is.False);
            Assert.That(navigation.TryComplete(token, isGameOverScene: true), Is.False);
        }

        private sealed class FakeClock : IUnscaledClock
        {
            public float Now { get; set; }
        }

        private sealed class FakeBridge : IFullscreenAdBridge
        {
            public bool IsAvailable { get; set; } = true;
            public System.Collections.Generic.List<string> RequestIds { get; } = new();

            public void ShowFullscreen(string requestId) => RequestIds.Add(requestId);
        }
    }
}

using System;
using System.Reflection;
using NUnit.Framework;

public sealed class SfxPlaybackRulesTests
{
    private static readonly Type RulesType =
        Type.GetType("SfxPlaybackRules, Assembly-CSharp", throwOnError: true);

    private static int PickVariantIndex(int clipCount, int previousIndex, float randomValue) =>
        (int)InvokeRule(nameof(PickVariantIndex), clipCount, previousIndex, randomValue);

    private static bool CanPlay(float now, float lastPlayTime, float minimumInterval) =>
        (bool)InvokeRule(nameof(CanPlay), now, lastPlayTime, minimumInterval);

    private static object InvokeRule(string methodName, params object[] arguments)
    {
        var method = RulesType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.That(method, Is.Not.Null, $"Missing SfxPlaybackRules.{methodName}");
        return method.Invoke(null, arguments);
    }

    [Test]
    public void EmptyClipListDoesNotProduceAnIndex()
    {
        Assert.That(PickVariantIndex(0, -1, 0.5f), Is.EqualTo(-1));
    }

    [Test]
    public void MultipleVariantsDoNotImmediatelyRepeatThePreviousClip()
    {
        Assert.That(PickVariantIndex(3, 1, 0.2f), Is.EqualTo(0));
        Assert.That(PickVariantIndex(3, 1, 0.5f), Is.EqualTo(2));
    }

    [Test]
    public void HighRandomValueDoesNotWrapBackToTheFirstPreviousClip()
    {
        Assert.That(PickVariantIndex(3, 0, 0.99f), Is.EqualTo(2));
    }

    [Test]
    public void HighRandomValueDoesNotRepeatTheLastPreviousClip()
    {
        Assert.That(PickVariantIndex(3, 2, 0.99f), Is.EqualTo(1));
    }

    [Test]
    public void MinimumIntervalBlocksOnlyTheSpamWindow()
    {
        Assert.That(CanPlay(10.04f, 10f, 0.05f), Is.False);
        Assert.That(CanPlay(10.05f, 10f, 0.05f), Is.True);
        Assert.That(CanPlay(10f, 10f, 0f), Is.True);
    }
}

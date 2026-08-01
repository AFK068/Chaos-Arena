using NUnit.Framework;

public sealed class SfxPlaybackRulesTests
{
    [Test]
    public void EmptyClipListDoesNotProduceAnIndex()
    {
        Assert.That(SfxPlaybackRules.PickVariantIndex(0, -1, 0.5f), Is.EqualTo(-1));
    }

    [Test]
    public void MultipleVariantsDoNotImmediatelyRepeatThePreviousClip()
    {
        Assert.That(SfxPlaybackRules.PickVariantIndex(3, 1, 0.2f), Is.EqualTo(0));
        Assert.That(SfxPlaybackRules.PickVariantIndex(3, 1, 0.5f), Is.EqualTo(2));
    }

    [Test]
    public void MinimumIntervalBlocksOnlyTheSpamWindow()
    {
        Assert.That(SfxPlaybackRules.CanPlay(10.04f, 10f, 0.05f), Is.False);
        Assert.That(SfxPlaybackRules.CanPlay(10.05f, 10f, 0.05f), Is.True);
        Assert.That(SfxPlaybackRules.CanPlay(10f, 10f, 0f), Is.True);
    }
}

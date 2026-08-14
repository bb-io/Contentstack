using Apps.Contentstack.Helper;

namespace Tests.Contentstack;

[TestClass]
public class FileNameHelperTests
{
    [TestMethod]
    public void SanitizeBaseName_PlainTitle_IsUnchanged()
    {
        Assert.AreEqual("Remote Order Form Terms", FileNameHelper.SanitizeBaseName("Remote Order Form Terms"));
    }

    [TestMethod]
    public void SanitizeBaseName_PathSeparatorsAndReservedChars_AreRemoved()
    {
        Assert.AreEqual("Terms Conditions v2", FileNameHelper.SanitizeBaseName("Terms / Conditions: v2?"));
        Assert.AreEqual("A B", FileNameHelper.SanitizeBaseName("A\\B"));
    }

    [TestMethod]
    public void SanitizeBaseName_ControlCharactersAndWhitespace_AreCollapsed()
    {
        Assert.AreEqual("Line one two", FileNameHelper.SanitizeBaseName("  Line one \n\t two  "));
    }

    [TestMethod]
    public void SanitizeBaseName_LeadingAndTrailingDots_AreTrimmed()
    {
        Assert.AreEqual("policy", FileNameHelper.SanitizeBaseName("...policy..."));
    }

    [TestMethod]
    public void SanitizeBaseName_VeryLongTitle_IsTruncated()
    {
        var result = FileNameHelper.SanitizeBaseName(new string('a', 500));
        Assert.AreEqual(100, result.Length);
    }

    [TestMethod]
    public void SanitizeBaseName_EmptyOrUnusableTitle_FallsBackToEntryId()
    {
        Assert.AreEqual("blt123", FileNameHelper.SanitizeBaseName(null, "blt123"));
        Assert.AreEqual("blt123", FileNameHelper.SanitizeBaseName("   ", "blt123"));
        Assert.AreEqual("blt123", FileNameHelper.SanitizeBaseName("///", "blt123"));
    }

    [TestMethod]
    public void SanitizeBaseName_NoUsableTitleOrFallback_ReturnsDefault()
    {
        Assert.AreEqual("entry", FileNameHelper.SanitizeBaseName(null, null));
    }

    [TestMethod]
    public void SanitizeBaseName_NonAsciiCharacters_ArePreserved()
    {
        Assert.AreEqual("Política — Rich Text", FileNameHelper.SanitizeBaseName("Política — Rich Text"));
    }
}

using Bunit;
using FluentAssertions;
using StudieAssistenten.Client.Shared;
using static StudieAssistenten.Client.Shared.SummaryOptionsDialog;

namespace StudieAssistenten.Client.Tests.Components;

/// <summary>
/// bUnit tests for SummaryOptionsDialog component.
/// Tests user interactions, default values, and event callbacks.
/// </summary>
public class SummaryOptionsDialogTests : TestContext
{
    [Fact]
    public void Component_WhenRendered_DisplaysModalWithTitle()
    {
        // Act
        var cut = RenderComponent<SummaryOptionsDialog>();

        // Assert
        var title = cut.Find(".modal-title");
        title.TextContent.Should().Contain("Summary Options");
    }

    [Fact]
    public void Component_WhenRendered_HasDefaultValues()
    {
        // Act
        var cut = RenderComponent<SummaryOptionsDialog>();

        // Assert
        var selects = cut.FindAll("select");
        selects.Should().HaveCount(2);

        // Verify length select has "Standard" as default
        var lengthOptions = selects[0].QuerySelectorAll("option");
        lengthOptions[1].GetAttribute("value").Should().Be("Standard");

        // Verify format select has "Bullets" as default
        var formatOptions = selects[1].QuerySelectorAll("option");
        formatOptions[0].GetAttribute("value").Should().Be("Bullets");
    }

    [Fact]
    public void Component_WhenRendered_HasAllLengthOptions()
    {
        // Act
        var cut = RenderComponent<SummaryOptionsDialog>();

        // Assert
        var selects = cut.FindAll("select");
        var options = selects[0].QuerySelectorAll("option");

        options.Should().HaveCount(3); // Brief, Standard, Detailed
        options[0].GetAttribute("value").Should().Be("Brief");
        options[1].GetAttribute("value").Should().Be("Standard");
        options[2].GetAttribute("value").Should().Be("Detailed");
    }

    [Fact]
    public void Component_WhenRendered_HasAllFormatOptions()
    {
        // Act
        var cut = RenderComponent<SummaryOptionsDialog>();

        // Assert
        var selects = cut.FindAll("select");
        var options = selects[1].QuerySelectorAll("option");

        options.Should().HaveCount(3); // Bullets, Paragraphs, Outline
        options[0].GetAttribute("value").Should().Be("Bullets");
        options[1].GetAttribute("value").Should().Be("Paragraphs");
        options[2].GetAttribute("value").Should().Be("Outline");
    }

    [Fact]
    public void GenerateButton_WhenClickedWithDefaults_InvokesOnConfirmWithDefaultValues()
    {
        // Arrange
        SummaryOptions? capturedOptions = null;
        var cut = RenderComponent<SummaryOptionsDialog>(parameters => parameters
            .Add(p => p.OnConfirm, (options) => { capturedOptions = options; }));

        // Act
        var generateButton = cut.Find(".btn-warning");
        generateButton.Click();

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.Length.Should().Be("Standard");
        capturedOptions.Format.Should().Be("Bullets");
    }

    [Fact]
    public void GenerateButton_WhenClickedAfterSelectingLength_InvokesOnConfirmWithSelectedValue()
    {
        // Arrange
        SummaryOptions? capturedOptions = null;
        var cut = RenderComponent<SummaryOptionsDialog>(parameters => parameters
            .Add(p => p.OnConfirm, (options) => { capturedOptions = options; }));

        // Act - Select Brief length
        var selects = cut.FindAll("select");
        selects[0].Change("Brief");

        var generateButton = cut.Find(".btn-warning");
        generateButton.Click();

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.Length.Should().Be("Brief");
        capturedOptions.Format.Should().Be("Bullets");
    }

    [Fact]
    public void GenerateButton_WhenClickedAfterSelectingFormat_InvokesOnConfirmWithSelectedValue()
    {
        // Arrange
        SummaryOptions? capturedOptions = null;
        var cut = RenderComponent<SummaryOptionsDialog>(parameters => parameters
            .Add(p => p.OnConfirm, (options) => { capturedOptions = options; }));

        // Act - Select Outline format
        var selects = cut.FindAll("select");
        selects[1].Change("Outline");

        var generateButton = cut.Find(".btn-warning");
        generateButton.Click();

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.Length.Should().Be("Standard");
        capturedOptions.Format.Should().Be("Outline");
    }

    [Fact]
    public void GenerateButton_WhenClickedAfterSelectingBothOptions_InvokesOnConfirmWithBothValues()
    {
        // Arrange
        SummaryOptions? capturedOptions = null;
        var cut = RenderComponent<SummaryOptionsDialog>(parameters => parameters
            .Add(p => p.OnConfirm, (options) => { capturedOptions = options; }));

        // Act - Select Detailed length and Paragraphs format
        var selects = cut.FindAll("select");
        selects[0].Change("Detailed");

        // Re-query after re-render
        selects = cut.FindAll("select");
        selects[1].Change("Paragraphs");

        var generateButton = cut.Find(".btn-warning");
        generateButton.Click();

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.Length.Should().Be("Detailed");
        capturedOptions.Format.Should().Be("Paragraphs");
    }

    [Fact]
    public void CancelButton_WhenClicked_InvokesOnCancelCallback()
    {
        // Arrange
        var onCancelInvoked = false;
        var cut = RenderComponent<SummaryOptionsDialog>(parameters => parameters
            .Add(p => p.OnCancel, () => { onCancelInvoked = true; }));

        // Act
        var cancelButton = cut.Find(".btn-secondary");
        cancelButton.Click();

        // Assert
        onCancelInvoked.Should().BeTrue();
    }

    [Fact]
    public void CloseButton_WhenClicked_InvokesOnCancelCallback()
    {
        // Arrange
        var onCancelInvoked = false;
        var cut = RenderComponent<SummaryOptionsDialog>(parameters => parameters
            .Add(p => p.OnCancel, () => { onCancelInvoked = true; }));

        // Act
        var closeButton = cut.Find(".btn-close");
        closeButton.Click();

        // Assert
        onCancelInvoked.Should().BeTrue();
    }
}

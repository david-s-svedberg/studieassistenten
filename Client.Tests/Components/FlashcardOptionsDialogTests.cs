using Bunit;
using FluentAssertions;
using StudieAssistenten.Client.Shared;
using static StudieAssistenten.Client.Shared.FlashcardOptionsDialog;

namespace StudieAssistenten.Client.Tests.Components;

/// <summary>
/// bUnit tests for FlashcardOptionsDialog component.
/// Tests user interactions, default values, and event callbacks.
/// </summary>
public class FlashcardOptionsDialogTests : TestContext
{
    [Fact]
    public void Component_WhenRendered_DisplaysModalWithTitle()
    {
        // Act
        var cut = RenderComponent<FlashcardOptionsDialog>();

        // Assert
        var title = cut.Find(".modal-title");
        title.TextContent.Should().Contain("Flashcard Options");
    }

    [Fact]
    public void Component_WhenRendered_HasDefaultValues()
    {
        // Act
        var cut = RenderComponent<FlashcardOptionsDialog>();

        // Assert - Check selects exist and have correct default values
        var selects = cut.FindAll("select");
        selects.Should().HaveCount(2);

        // Verify number of cards select has expected structure
        var numberOfCardsOptions = selects[0].QuerySelectorAll("option");
        numberOfCardsOptions.Should().HaveCountGreaterThan(0);
        numberOfCardsOptions[0].GetAttribute("value").Should().BeNullOrEmpty(); // AI decides is first option

        // Verify difficulty select has "Mixed" as first option
        var difficultyOptions = selects[1].QuerySelectorAll("option");
        difficultyOptions[0].GetAttribute("value").Should().Be("Mixed");
    }

    [Fact]
    public void Component_WhenRendered_HasAllNumberOfCardsOptions()
    {
        // Act
        var cut = RenderComponent<FlashcardOptionsDialog>();

        // Assert
        var selects = cut.FindAll("select");
        var options = selects[0].QuerySelectorAll("option");

        options.Should().HaveCount(7); // AI decides + 5,10,15,20,25,30
        options[0].GetAttribute("value").Should().BeNullOrEmpty(); // AI decides
        options[1].GetAttribute("value").Should().Be("5");
        options[2].GetAttribute("value").Should().Be("10");
        options[6].GetAttribute("value").Should().Be("30");
    }

    [Fact]
    public void Component_WhenRendered_HasAllDifficultyOptions()
    {
        // Act
        var cut = RenderComponent<FlashcardOptionsDialog>();

        // Assert
        var selects = cut.FindAll("select");
        var options = selects[1].QuerySelectorAll("option");

        options.Should().HaveCount(4); // Mixed, Basic, Intermediate, Advanced
        options[0].GetAttribute("value").Should().Be("Mixed");
        options[1].GetAttribute("value").Should().Be("Basic");
        options[2].GetAttribute("value").Should().Be("Intermediate");
        options[3].GetAttribute("value").Should().Be("Advanced");
    }

    [Fact]
    public void CancelButton_WhenClicked_InvokesOnCancelCallback()
    {
        // Arrange
        var onCancelInvoked = false;
        var cut = RenderComponent<FlashcardOptionsDialog>(parameters => parameters
            .Add(p => p.OnCancel, () => { onCancelInvoked = true; }));

        // Act
        var cancelButton = cut.Find(".btn-secondary");
        cancelButton.Click();

        // Assert
        onCancelInvoked.Should().BeTrue();
    }

    [Fact]
    public void GenerateButton_WhenClicked_InvokesOnConfirmWithDefaultValues()
    {
        // Arrange
        FlashcardOptions? capturedOptions = null;
        var cut = RenderComponent<FlashcardOptionsDialog>(parameters => parameters
            .Add(p => p.OnConfirm, (options) => { capturedOptions = options; }));

        // Act
        var generateButton = cut.Find(".btn-success");
        generateButton.Click();

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.NumberOfCards.Should().BeNull(); // AI decides
        capturedOptions.DifficultyLevel.Should().Be("Mixed");
    }

    [Fact]
    public void GenerateButton_WhenClickedAfterSelectingNumberOfCards_InvokesOnConfirmWithSelectedValue()
    {
        // Arrange
        FlashcardOptions? capturedOptions = null;
        var cut = RenderComponent<FlashcardOptionsDialog>(parameters => parameters
            .Add(p => p.OnConfirm, (options) => { capturedOptions = options; }));

        // Act - Select 15 cards
        var selects = cut.FindAll("select");
        selects[0].Change("15");

        var generateButton = cut.Find(".btn-success");
        generateButton.Click();

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.NumberOfCards.Should().Be(15);
        capturedOptions.DifficultyLevel.Should().Be("Mixed");
    }

    [Fact]
    public void GenerateButton_WhenClickedAfterSelectingDifficulty_InvokesOnConfirmWithSelectedValue()
    {
        // Arrange
        FlashcardOptions? capturedOptions = null;
        var cut = RenderComponent<FlashcardOptionsDialog>(parameters => parameters
            .Add(p => p.OnConfirm, (options) => { capturedOptions = options; }));

        // Act - Select Advanced difficulty
        var selects = cut.FindAll("select");
        selects[1].Change("Advanced");

        var generateButton = cut.Find(".btn-success");
        generateButton.Click();

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.NumberOfCards.Should().BeNull();
        capturedOptions.DifficultyLevel.Should().Be("Advanced");
    }

    [Fact]
    public void GenerateButton_WhenClickedAfterSelectingBothOptions_InvokesOnConfirmWithBothValues()
    {
        // Arrange
        FlashcardOptions? capturedOptions = null;
        var cut = RenderComponent<FlashcardOptionsDialog>(parameters => parameters
            .Add(p => p.OnConfirm, (options) => { capturedOptions = options; }));

        // Act - Select 20 cards
        var selects = cut.FindAll("select");
        selects[0].Change("20");

        // Re-query after re-render to get updated event handler IDs
        selects = cut.FindAll("select");
        selects[1].Change("Intermediate");

        var generateButton = cut.Find(".btn-success");
        generateButton.Click();

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.NumberOfCards.Should().Be(20);
        capturedOptions.DifficultyLevel.Should().Be("Intermediate");
    }

    [Fact]
    public void CloseButton_WhenClicked_InvokesOnCancelCallback()
    {
        // Arrange
        var onCancelInvoked = false;
        var cut = RenderComponent<FlashcardOptionsDialog>(parameters => parameters
            .Add(p => p.OnCancel, () => { onCancelInvoked = true; }));

        // Act
        var closeButton = cut.Find(".btn-close");
        closeButton.Click();

        // Assert
        onCancelInvoked.Should().BeTrue();
    }
}

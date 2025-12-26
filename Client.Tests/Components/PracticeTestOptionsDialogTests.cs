using Bunit;
using FluentAssertions;
using StudieAssistenten.Client.Shared;
using static StudieAssistenten.Client.Shared.PracticeTestOptionsDialog;

namespace StudieAssistenten.Client.Tests.Components;

/// <summary>
/// bUnit tests for PracticeTestOptionsDialog component.
/// Tests user interactions, checkboxes, validation, and event callbacks.
/// </summary>
public class PracticeTestOptionsDialogTests : TestContext
{
    [Fact]
    public void Component_WhenRendered_DisplaysModalWithTitle()
    {
        // Act
        var cut = RenderComponent<PracticeTestOptionsDialog>();

        // Assert
        var title = cut.Find(".modal-title");
        title.TextContent.Should().Contain("Practice Test Options");
    }

    [Fact]
    public void Component_WhenRendered_HasDefaultValues()
    {
        // Act
        var cut = RenderComponent<PracticeTestOptionsDialog>();

        // Assert
        var checkboxes = cut.FindAll("input[type='checkbox']");
        checkboxes.Should().HaveCount(6); // 5 question types + 1 explanations

        // Only "Mixed" and "Include explanations" should be checked by default
        var chkMixed = cut.Find("#chkMixed");
        var chkExplanations = cut.Find("#chkExplanations");

        chkMixed.HasAttribute("checked").Should().BeTrue();
        chkExplanations.HasAttribute("checked").Should().BeTrue();
    }

    [Fact]
    public void GenerateButton_WhenRenderedWithDefaults_IsEnabled()
    {
        // Act
        var cut = RenderComponent<PracticeTestOptionsDialog>();

        // Assert - Button should be enabled because "Mixed" is checked by default
        var generateButton = cut.Find(".btn-info");
        generateButton.HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void GenerateButton_WhenAllCheckboxesUnchecked_IsDisabled()
    {
        // Arrange
        var cut = RenderComponent<PracticeTestOptionsDialog>();

        // Act - Uncheck the default "Mixed" checkbox
        var chkMixed = cut.Find("#chkMixed");
        chkMixed.Change(false);

        // Assert - Button should now be disabled
        var generateButton = cut.Find(".btn-info");
        generateButton.HasAttribute("disabled").Should().BeTrue();

        // Error message should be visible
        var errorMessage = cut.Find("small.text-danger");
        errorMessage.TextContent.Should().Contain("Select at least one question type");
    }

    [Fact]
    public void GenerateButton_WhenClickedWithDefaults_InvokesOnConfirmWithDefaultValues()
    {
        // Arrange
        PracticeTestOptions? capturedOptions = null;
        var cut = RenderComponent<PracticeTestOptionsDialog>(parameters => parameters
            .Add(p => p.OnConfirm, (options) => { capturedOptions = options; }));

        // Act
        var generateButton = cut.Find(".btn-info");
        generateButton.Click();

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.NumberOfQuestions.Should().BeNull(); // AI decides
        capturedOptions.QuestionTypes.Should().ContainSingle().Which.Should().Be("Mixed");
        capturedOptions.IncludeAnswerExplanations.Should().BeTrue();
    }

    [Fact]
    public void GenerateButton_WhenClickedAfterSelectingNumberOfQuestions_InvokesOnConfirmWithSelectedValue()
    {
        // Arrange
        PracticeTestOptions? capturedOptions = null;
        var cut = RenderComponent<PracticeTestOptionsDialog>(parameters => parameters
            .Add(p => p.OnConfirm, (options) => { capturedOptions = options; }));

        // Act - Select 10 questions
        var select = cut.Find("select");
        select.Change("10");

        var generateButton = cut.Find(".btn-info");
        generateButton.Click();

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.NumberOfQuestions.Should().Be(10);
        capturedOptions.QuestionTypes.Should().ContainSingle().Which.Should().Be("Mixed");
    }

    [Fact]
    public void GenerateButton_WhenMultipleQuestionTypesSelected_InvokesOnConfirmWithAllSelectedTypes()
    {
        // Arrange
        PracticeTestOptions? capturedOptions = null;
        var cut = RenderComponent<PracticeTestOptionsDialog>(parameters => parameters
            .Add(p => p.OnConfirm, (options) => { capturedOptions = options; }));

        // Act - Select Multiple Choice and True/False (in addition to default Mixed)
        var chkMultipleChoice = cut.Find("#chkMultipleChoice");
        chkMultipleChoice.Change(true);

        var chkTrueFalse = cut.Find("#chkTrueFalse");
        chkTrueFalse.Change(true);

        var generateButton = cut.Find(".btn-info");
        generateButton.Click();

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.QuestionTypes.Should().HaveCount(3);
        capturedOptions.QuestionTypes.Should().Contain("MultipleChoice");
        capturedOptions.QuestionTypes.Should().Contain("TrueFalse");
        capturedOptions.QuestionTypes.Should().Contain("Mixed");
    }

    [Fact]
    public void GenerateButton_WhenMixedUncheckedAndOthersSelected_InvokesOnConfirmWithSelectedTypesOnly()
    {
        // Arrange
        PracticeTestOptions? capturedOptions = null;
        var cut = RenderComponent<PracticeTestOptionsDialog>(parameters => parameters
            .Add(p => p.OnConfirm, (options) => { capturedOptions = options; }));

        // Act - Uncheck Mixed, select Short Answer and Essay
        var chkMixed = cut.Find("#chkMixed");
        chkMixed.Change(false);

        var chkShortAnswer = cut.Find("#chkShortAnswer");
        chkShortAnswer.Change(true);

        var chkEssay = cut.Find("#chkEssay");
        chkEssay.Change(true);

        var generateButton = cut.Find(".btn-info");
        generateButton.Click();

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.QuestionTypes.Should().HaveCount(2);
        capturedOptions.QuestionTypes.Should().Contain("ShortAnswer");
        capturedOptions.QuestionTypes.Should().Contain("Essay");
        capturedOptions.QuestionTypes.Should().NotContain("Mixed");
    }

    [Fact]
    public void GenerateButton_WhenExplanationsUnchecked_InvokesOnConfirmWithExplanationsFalse()
    {
        // Arrange
        PracticeTestOptions? capturedOptions = null;
        var cut = RenderComponent<PracticeTestOptionsDialog>(parameters => parameters
            .Add(p => p.OnConfirm, (options) => { capturedOptions = options; }));

        // Act - Uncheck explanations
        var chkExplanations = cut.Find("#chkExplanations");
        chkExplanations.Change(false);

        var generateButton = cut.Find(".btn-info");
        generateButton.Click();

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.IncludeAnswerExplanations.Should().BeFalse();
    }

    [Fact]
    public void GenerateButton_WhenAllOptionsCustomized_InvokesOnConfirmWithAllValues()
    {
        // Arrange
        PracticeTestOptions? capturedOptions = null;
        var cut = RenderComponent<PracticeTestOptionsDialog>(parameters => parameters
            .Add(p => p.OnConfirm, (options) => { capturedOptions = options; }));

        // Act - Customize all options
        var select = cut.Find("select");
        select.Change("15");

        // Uncheck Mixed, select Multiple Choice only
        var chkMixed = cut.Find("#chkMixed");
        chkMixed.Change(false);

        var chkMultipleChoice = cut.Find("#chkMultipleChoice");
        chkMultipleChoice.Change(true);

        // Uncheck explanations
        var chkExplanations = cut.Find("#chkExplanations");
        chkExplanations.Change(false);

        var generateButton = cut.Find(".btn-info");
        generateButton.Click();

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.NumberOfQuestions.Should().Be(15);
        capturedOptions.QuestionTypes.Should().ContainSingle().Which.Should().Be("MultipleChoice");
        capturedOptions.IncludeAnswerExplanations.Should().BeFalse();
    }

    [Fact]
    public void CancelButton_WhenClicked_InvokesOnCancelCallback()
    {
        // Arrange
        var onCancelInvoked = false;
        var cut = RenderComponent<PracticeTestOptionsDialog>(parameters => parameters
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
        var cut = RenderComponent<PracticeTestOptionsDialog>(parameters => parameters
            .Add(p => p.OnCancel, () => { onCancelInvoked = true; }));

        // Act
        var closeButton = cut.Find(".btn-close");
        closeButton.Click();

        // Assert
        onCancelInvoked.Should().BeTrue();
    }
}

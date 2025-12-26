using System.ComponentModel.DataAnnotations;

namespace StudieAssistenten.Shared.Validation;

/// <summary>
/// Validates that all question types in the list are valid options
/// </summary>
public class ValidQuestionTypesAttribute : ValidationAttribute
{
    private static readonly string[] ValidTypes = { "MultipleChoice", "TrueFalse", "ShortAnswer", "Essay", "Mixed" };

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            // Null is allowed (optional field)
            return ValidationResult.Success;
        }

        if (value is not List<string> questionTypes)
        {
            return new ValidationResult("Question types must be a list of strings");
        }

        if (questionTypes.Count == 0)
        {
            return new ValidationResult("At least one question type must be specified");
        }

        if (questionTypes.Count > 10)
        {
            return new ValidationResult("Cannot specify more than 10 question types");
        }

        var invalidTypes = questionTypes.Where(qt => !ValidTypes.Contains(qt)).ToList();
        if (invalidTypes.Any())
        {
            return new ValidationResult($"Invalid question types: {string.Join(", ", invalidTypes)}. Valid types are: {string.Join(", ", ValidTypes)}");
        }

        return ValidationResult.Success;
    }
}

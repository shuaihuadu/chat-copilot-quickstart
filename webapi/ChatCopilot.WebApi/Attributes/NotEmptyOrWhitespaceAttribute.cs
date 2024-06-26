﻿namespace ChatCopilot.WebApi.Attributes;

public class NotEmptyOrWhitespaceAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        if (value is string s)
        {
            if (!string.IsNullOrWhiteSpace(s))
            {
                return ValidationResult.Success;
            }

            return new ValidationResult($"'{validationContext.MemberName}' cannot be empty or whitespace.");
        }

        return new ValidationResult($"'{validationContext.MemberName}' must be a string");
    }
}

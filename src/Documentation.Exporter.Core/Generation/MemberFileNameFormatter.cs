using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Generation;

/// <summary>
/// Default implementation of <see cref="IMemberFileNameFormatter"/>.
/// </summary>
public sealed class MemberFileNameFormatter : IMemberFileNameFormatter
{
    private static readonly Dictionary<string, string> OperatorFileTokenMap = new(StringComparer.Ordinal)
    {
        ["operator +"] = "op_UnaryOrBinaryPlus",
        ["operator -"] = "op_UnaryOrBinaryMinus",
        ["operator checked -"] = "op_CheckedMinus",
        ["operator !"] = "op_LogicalNot",
        ["operator ~"] = "op_OnesComplement",
        ["operator ++"] = "op_Increment",
        ["operator checked ++"] = "op_CheckedIncrement",
        ["operator --"] = "op_Decrement",
        ["operator checked --"] = "op_CheckedDecrement",
        ["operator true"] = "op_True",
        ["operator false"] = "op_False",
        ["operator checked +"] = "op_CheckedAddition",
        ["operator *"] = "op_Multiply",
        ["operator checked *"] = "op_CheckedMultiply",
        ["operator /"] = "op_Division",
        ["operator checked /"] = "op_CheckedDivision",
        ["operator %"] = "op_Modulus",
        ["operator &"] = "op_BitwiseAnd",
        ["operator |"] = "op_BitwiseOr",
        ["operator ^"] = "op_ExclusiveOr",
        ["operator <<"] = "op_LeftShift",
        ["operator >>"] = "op_RightShift",
        ["operator >>>"] = "op_UnsignedRightShift",
        ["operator =="] = "op_Equality",
        ["operator !="] = "op_Inequality",
        ["operator <"] = "op_LessThan",
        ["operator >"] = "op_GreaterThan",
        ["operator <="] = "op_LessThanOrEqual",
        ["operator >="] = "op_GreaterThanOrEqual",
        ["operator +="] = "op_AdditionAssignment",
        ["operator checked +="] = "op_CheckedAdditionAssignment",
        ["operator -="] = "op_SubtractionAssignment",
        ["operator checked -="] = "op_CheckedSubtractionAssignment",
        ["operator *="] = "op_MultiplicationAssignment",
        ["operator checked *="] = "op_CheckedMultiplicationAssignment",
        ["operator /="] = "op_DivisionAssignment",
        ["operator checked /="] = "op_CheckedDivisionAssignment",
        ["operator %="] = "op_ModulusAssignment",
        ["operator &="] = "op_BitwiseAndAssignment",
        ["operator |="] = "op_BitwiseOrAssignment",
        ["operator ^="] = "op_ExclusiveOrAssignment",
        ["operator <<="] = "op_LeftShiftAssignment",
        ["operator >>="] = "op_RightShiftAssignment",
        ["operator >>>="] = "op_UnsignedRightShiftAssignment",
        ["op_IncrementAssignment"] = "op_IncrementAssignment",
        ["op_CheckedIncrementAssignment"] = "op_CheckedIncrementAssignment",
        ["op_DecrementAssignment"] = "op_DecrementAssignment",
        ["op_CheckedDecrementAssignment"] = "op_CheckedDecrementAssignment",
        ["operator implicit"] = "op_Implicit",
        ["operator explicit"] = "op_Explicit",
        ["operator checked explicit"] = "op_CheckedExplicit"
    };

    /// <inheritdoc />
    public string GetMemberDetailFileName(MemberDocumentationModel memberModel, bool isOverloaded, bool isConstructor = false)
    {
        ArgumentNullException.ThrowIfNull(memberModel);

        var safeMemberName = SanitizeBaseMemberFileName(memberModel.MemberName);
        var baseFileName = isConstructor ? $"{safeMemberName}.ctor" : safeMemberName;
        var genericArityToken = isOverloaded && memberModel.GenericTypeParameters.Count > 0
            ? string.Create(CultureInfo.InvariantCulture, $"_T{memberModel.GenericTypeParameters.Count}")
            : string.Empty;
        var conversionReturnSuffix = isOverloaded && IsConversionOperator(memberModel.MemberName) && !string.IsNullOrWhiteSpace(memberModel.ReturnTypeName)
            ? $"_{SanitizeIdentifier(memberModel.ReturnTypeName)}"
            : string.Empty;

        if (!isOverloaded || memberModel.MemberParameters.Count == 0)
        {
            return $"{baseFileName}{genericArityToken}{conversionReturnSuffix}.md";
        }

        var parameterSuffix = string.Join(
            "_",
            memberModel.MemberParameters.Select(FormatParameterFileToken));
        return $"{baseFileName}{genericArityToken}_{parameterSuffix}{conversionReturnSuffix}.md";
    }

    /// <inheritdoc />
    public string FormatMemberHeaderTitle(MemberDocumentationModel memberModel, bool isOverloaded)
    {
        ArgumentNullException.ThrowIfNull(memberModel);

        if (string.Equals(memberModel.MemberName, "this", StringComparison.Ordinal) ||
            memberModel.MemberName.EndsWith(".this", StringComparison.Ordinal))
        {
            var indexParameters = string.Join(", ", memberModel.MemberParameters.Select(FormatParameterHeaderDisplay));
            return $"{memberModel.MemberName}[{indexParameters}]";
        }

        if (memberModel.MemberName.StartsWith('~') && memberModel.MemberName.Length > 1)
        {
            return $"{memberModel.MemberName}()";
        }

        var genericSuffix = memberModel.GenericTypeParameters.Count > 0
            ? $"<{string.Join(", ", memberModel.GenericTypeParameters.Select(parameter => parameter.ParameterName))}>"
            : string.Empty;
        var baseMemberTitle = $"{memberModel.MemberName}{genericSuffix}";
        var conversionReturnDisplay = isOverloaded && IsConversionOperator(memberModel.MemberName) && !string.IsNullOrWhiteSpace(memberModel.ReturnTypeName)
            ? $" to {memberModel.ReturnTypeName}"
            : string.Empty;

        return !isOverloaded
            ? baseMemberTitle
            : memberModel.MemberParameters.Count > 0
                ? $"{baseMemberTitle}({string.Join(", ", memberModel.MemberParameters.Select(FormatParameterHeaderDisplay))}){conversionReturnDisplay}"
                : $"{baseMemberTitle}(){conversionReturnDisplay}";
    }

    private static bool IsConversionOperator(string memberName) =>
        string.Equals(memberName, "operator implicit", StringComparison.Ordinal) ||
        string.Equals(memberName, "operator explicit", StringComparison.Ordinal) ||
        string.Equals(memberName, "operator checked explicit", StringComparison.Ordinal);

    private static string SanitizeBaseMemberFileName(string rawMemberName)
    {
        if (OperatorFileTokenMap.TryGetValue(rawMemberName, out var operatorFileToken))
        {
            return operatorFileToken;
        }

        if (rawMemberName.StartsWith('~') && rawMemberName.Length > 1)
        {
            return "Finalize";
        }

        StringBuilder sanitizedBuilder = new();
        foreach (var character in rawMemberName)
        {
            if (char.IsLetterOrDigit(character) || character is '_' or '.' or '-')
            {
                _ = sanitizedBuilder.Append(character);
            }
        }

        return sanitizedBuilder.Length > 0 ? sanitizedBuilder.ToString() : "Member";
    }

    private static string FormatParameterFileToken(ParameterDocumentationModel parameterModel)
    {
        var typeToken = SanitizeIdentifier(parameterModel.ParameterTypeName);
        var modifierText = parameterModel.ParameterModifier?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(modifierText))
        {
            return typeToken;
        }

        if (modifierText.Contains("ref readonly", StringComparison.Ordinal))
        {
            return $"ref_readonly_{typeToken}";
        }

        var tokens = modifierText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens.Contains("out", StringComparer.Ordinal)
            ? $"out_{typeToken}"
            : tokens.Contains("in", StringComparer.Ordinal)
                ? $"in_{typeToken}"
                : tokens.Contains("ref", StringComparer.Ordinal)
                    ? $"ref_{typeToken}"
                    : typeToken;
    }

    private static string FormatParameterHeaderDisplay(ParameterDocumentationModel parameterModel) =>
        !string.IsNullOrWhiteSpace(parameterModel.ParameterModifier)
            ? $"{parameterModel.ParameterModifier} {parameterModel.ParameterTypeName}"
            : parameterModel.IsParams
                ? $"params {parameterModel.ParameterTypeName}"
                : parameterModel.ParameterTypeName;

    private static string SanitizeIdentifier(string rawTypeName)
    {
        StringBuilder sanitizedBuilder = new();
        foreach (var character in rawTypeName)
        {
            if (char.IsLetterOrDigit(character))
            {
                _ = sanitizedBuilder.Append(character);
            }
        }

        return sanitizedBuilder.Length > 0 ? sanitizedBuilder.ToString() : "Param";
    }
}

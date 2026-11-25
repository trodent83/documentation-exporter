using System.Reflection;

namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Formats C# type, constructor, property, method, operator, field, event, and parameter signatures and type display names.
/// </summary>
public interface ICSharpSignatureFormatter
{
    /// <summary>
    /// Gets the clean simple type name without generic arity backtick suffixes (for example <c>"List"</c>).
    /// </summary>
    /// <param name="targetType">The target runtime type.</param>
    /// <returns>Clean simple type name without generic arity suffixes.</returns>
    string GetCleanTypeName(Type targetType);

    /// <summary>
    /// Formats a type's simple display name including array brackets, pointers, nullable value types, and generic type arguments.
    /// </summary>
    /// <param name="targetType">The target runtime type.</param>
    /// <returns>Human-readable C# type name.</returns>
    string FormatDisplayTypeName(Type targetType);

    /// <summary>
    /// Formats a type's simple display name including nullable reference type annotations (<c>?</c>), array brackets, and generic arguments.
    /// </summary>
    /// <param name="nullabilityInfo">The nullability metadata inspected from a parameter, property, field, event, or return value.</param>
    /// <returns>Human-readable C# type name preserving nullability annotations.</returns>
    string FormatDisplayTypeName(NullabilityInfo nullabilityInfo);

    /// <summary>
    /// Formats a type's namespace-qualified display name including generic type arguments.
    /// </summary>
    /// <param name="targetType">The target runtime type.</param>
    /// <returns>Namespace-qualified C# type name.</returns>
    string FormatFullDisplayTypeName(Type targetType);

    /// <summary>
    /// Builds the full C# declaration signature for a reflected type.
    /// </summary>
    /// <param name="targetType">The reflected runtime type.</param>
    /// <returns>C# declaration signature string.</returns>
    string BuildTypeDeclarationSignature(Type targetType);

    /// <summary>
    /// Builds the C# declaration signature for a reflected constructor.
    /// </summary>
    /// <param name="constructorInfo">The constructor reflection metadata.</param>
    /// <param name="targetType">The declaring type.</param>
    /// <returns>C# constructor signature string.</returns>
    string BuildConstructorSignature(ConstructorInfo constructorInfo, Type targetType);

    /// <summary>
    /// Builds the C# declaration signature for a reflected property or indexer.
    /// </summary>
    /// <param name="propertyInfo">The property reflection metadata.</param>
    /// <returns>C# property or indexer signature string.</returns>
    string BuildPropertySignature(PropertyInfo propertyInfo);

    /// <summary>
    /// Builds the C# declaration signature for a reflected method or operator.
    /// </summary>
    /// <param name="methodInfo">The method reflection metadata.</param>
    /// <param name="isAsyncMethod">Whether the method should include the <c>async</c> modifier.</param>
    /// <returns>C# method or operator signature string.</returns>
    string BuildMethodSignature(MethodInfo methodInfo, bool isAsyncMethod);

    /// <summary>
    /// Builds the C# declaration signature for a reflected field.
    /// </summary>
    /// <param name="fieldInfo">The field reflection metadata.</param>
    /// <param name="fieldTypeName">The formatted field type name.</param>
    /// <returns>C# field signature string.</returns>
    string BuildFieldSignature(FieldInfo fieldInfo, string fieldTypeName);

    /// <summary>
    /// Builds the C# declaration signature for a reflected event.
    /// </summary>
    /// <param name="eventInfo">The event reflection metadata.</param>
    /// <returns>C# event signature string.</returns>
    string BuildEventSignature(EventInfo eventInfo);

    /// <summary>
    /// Formats a single method or constructor parameter declaration token including attributes, modifiers, type, name, and default value.
    /// </summary>
    /// <param name="parameterInfo">The reflected parameter metadata.</param>
    /// <param name="isExtensionMethodFirstParameter">Whether this parameter is the <c>this</c> receiver of an extension method.</param>
    /// <returns>Formatted parameter declaration string.</returns>
    string FormatParameterSignature(ParameterInfo parameterInfo, bool isExtensionMethodFirstParameter);

    /// <summary>
    /// Determines whether the specified type declares a C# primary constructor.
    /// </summary>
    /// <param name="targetType">The declaring type.</param>
    /// <param name="primaryConstructor">When <see langword="true"/>, receives the primary constructor metadata.</param>
    /// <returns><see langword="true"/> if a primary constructor was detected; otherwise <see langword="false"/>.</returns>
    bool TryGetPrimaryConstructor(Type targetType, out ConstructorInfo primaryConstructor);

    /// <summary>
    /// Determines the human-readable constructor classification (<c>None</c>, <c>Default</c>, <c>Primary (...)</c>, or <c>Overloaded (...)</c>).
    /// </summary>
    /// <param name="targetType">The declaring type.</param>
    /// <returns>The constructor classification string.</returns>
    string DetermineConstructorKind(Type targetType);

    /// <summary>
    /// Determines whether the specified return type represents an awaitable <see cref="Task"/> or <see cref="ValueTask"/>.
    /// </summary>
    /// <param name="returnType">The method return type.</param>
    /// <returns><see langword="true"/> if awaitable; otherwise <see langword="false"/>.</returns>
    bool IsAwaitableReturnType(Type returnType);

    /// <summary>
    /// Determines whether the specified return type is <see cref="IAsyncEnumerable{T}"/> or <see cref="IAsyncEnumerator{T}"/>.
    /// </summary>
    /// <param name="returnType">The method return type.</param>
    /// <returns><see langword="true"/> if asynchronous stream; otherwise <see langword="false"/>.</returns>
    bool IsAsyncEnumerableType(Type returnType);

    /// <summary>
    /// Determines whether the specified method is compiled with an async or async-iterator state machine attribute.
    /// </summary>
    /// <param name="methodInfo">The method reflection metadata.</param>
    /// <returns><see langword="true"/> if decorated with an async state machine attribute; otherwise <see langword="false"/>.</returns>
    bool HasAsyncStateMachineAttribute(MethodInfo methodInfo);
}

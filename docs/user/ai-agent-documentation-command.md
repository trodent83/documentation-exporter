# AI Agent Command for Authoring C# Interface & API XML Documentation

> **Related User Guides:** [User Guide Hub](index.md) · [Recommended AI Agent Rules for Any C# Project (`AGENTS.md`)](ai-agent-recommended-csharp-rules.md) · [Authoring XML Documentation Comments](xml-documentation-authoring.md) · [CLI Commands, Options, & JSON Configuration](cli-and-configuration.md) · [External Examples & Sidecar Overrides](examples-overrides-and-external-links.md)

Copy the command file below into your target C# repository (for example, as `.agents/workflows/document-interfaces.md`, `.claude/commands/document-interfaces.md`, or `.github/prompts/document-interfaces.prompt.md`) or paste it directly into your AI coding agent.

It instructs the agent on **how to document C# interfaces, contracts, DTOs, and members** so that `Documentation.Exporter.Cli` generates rich Microsoft Learn-style Markdown pages complete with detailed descriptions, architecture notes, decision tables, runnable examples, exception matrices, and cross-references.

---

## Copy-Ready Agent Command (`document-interfaces.md`)

````markdown
---
description: Add comprehensive, production-grade C# XML documentation comments (summaries, detailed remarks, runnable examples, parameter/return tables, exceptions, and cross-links) to interfaces and public API contracts for Documentation.Exporter.Cli.
---

# Task: Document C# Interfaces & Public API Contracts for `Documentation.Exporter.Cli`

You are documenting the C# source code in this repository—focusing primarily on **`public interface` contracts** and their collaborating public request/response models, records, structs, enums, and delegates—so that `Documentation.Exporter.Cli` produces comprehensive, developer-ready Markdown documentation.

Follow every rule below strictly.

---

## 1. Prerequisites & Scope Rules

1. **Enable Compiler XML Output**:
   - Ensure every non-test `.csproj` in the solution includes:
     ```xml
     <GenerateDocumentationFile>true</GenerateDocumentationFile>
     ```
2. **Preserve Behavior**:
   - **NEVER** alter runtime logic, method signatures, parameter names, return types, or visibility modifiers. Only add or enrich `///` XML documentation comments (and `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in `.csproj` if missing).
3. **Prioritize Interfaces & Public Contracts First**:
   - Document all `public interface` types and every member they declare (methods, properties, indexers, events, static abstract/virtual members).
   - Document all public DTOs, records, enums, delegates, and options referenced in interface signatures.
   - On concrete classes that implement a documented interface, use `/// <inheritdoc />` on the implementing members (adding implementation-specific `<remarks>` or `<example>` blocks alongside `<inheritdoc />` when the concrete implementation has notable behavior such as caching, retries, or database transactions).

---

## 2. Mandatory XML Tags & Quality Standards

Every documented interface, type, and member **MUST** go beyond restating the symbol name. Write clear, domain-accurate documentation using the tags below (all of which are natively rendered by `Documentation.Exporter.Cli`):

### A. Type-Level Documentation (`interface`, `class`, `record`, `struct`, `enum`, `delegate`)

1. **`<summary>` (Required on all types)**:
   - Explain the contract's responsibility, architectural role, and primary consumers in 1–3 sentences.
2. **`<typeparam name="...">` (Required on all generic type parameters)**:
   - Explain what the type parameter represents and any domain or variance (`in`/`out`) expectations.
3. **`<param name="...">` (Required on types with C# 12 Primary Constructors and on `delegate` types)**:
   - Document every primary constructor or delegate parameter at the type level (`Documentation.Exporter.Cli` automatically propagates type-level `<param>` and `<exception>` tags to the primary constructor).
4. **`<remarks>` (Required on all interfaces and core domain types)**:
   - Provide deep architectural and usage context structured with:
     - `<para>` paragraphs (never write a single giant unformatted block of text).
     - `<list type="bullet">` or `<list type="number">` for lifecycle steps, invariants, or thread-safety/idempotency guarantees.
     - `<list type="table">` with `<listheader>` and `<item><term>...</term><description>...</description></item>` when comparing modes, implementations, state transitions, or retry policies.
     - `<note type="tip | important | warning | caution | security">` for callout banners (rendered as GitHub Alerts `> [!TIP]`, `> [!IMPORTANT]`, `> [!WARNING]`, `> [!CAUTION]`).
5. **`<example>` with `<code language="csharp">` (Required on all interfaces and key public types)**:
   - Provide at least one realistic, copy-pasteable C# usage snippet inside `<example><code>...</code></example>` showing dependency injection/resolution, calling the interface, handling the return value, and error/cancellation handling.
6. **`<seealso cref="..."/>` (Recommended on all interfaces)**:
   - Cross-link related contracts, DTOs, exceptions, or default implementations.

### B. Member-Level Documentation (Methods, Properties, Indexers, Events, Enum Values)

1. **`<summary>` (Required on every member, including every `enum` value)**:
   - Describe what the operation or property does, its side effects, and when a caller should use it.
2. **`<typeparam name="...">` (Required on every generic method parameter)**:
   - Describe the role and constraints of the method type parameter.
3. **`<param name="...">` (Required on every method and indexer parameter)**:
   - State valid input ranges, format expectations, nullability behavior (what happens when `null` is passed), and `CancellationToken` behavior. Reference other parameters inline using `<paramref name="..."/>`.
4. **`<returns>` or `<value>` (Required on all non-void methods, `Task<T>`/`ValueTask<T>`/`IAsyncEnumerable<T>` methods, properties, and indexers)**:
   - Explain the exact meaning of the returned value, whether a returned collection can be empty (or `null`), and what asynchronous completion represents.
5. **`<exception cref="...">` (Required whenever a method, property, or constructor can throw or contracts require validation)**:
   - Document every exception the caller must anticipate (e.g., `ArgumentNullException`, `ArgumentException`, `ArgumentOutOfRangeException`, `InvalidOperationException`, `OperationCanceledException`). If multiple conditions throw the same exception type, list multiple `<exception cref="...">` tags (`Documentation.Exporter.Cli` merges them cleanly into one table row).
6. **`<remarks>` & `<example>` (Required on non-trivial interface methods)**:
   - Add `<remarks>` explaining edge cases, performance/complexity, or ordering guarantees, and `<example><code language="csharp">...</code></example>` showing a concrete call to the method.
7. **Inline Cross-Links (`<see>` / `<c>`)**:
   - Use `<see cref="TypeOrMember"/>` (or `<see cref="OverloadedMethod(string, int)">custom label</see>`) to link types and overloaded methods.
   - Use `<see langword="null"/>`, `<see langword="true"/>`, `<see langword="false"/>`, `<see langword="async"/>`, or `<c>inlineCode</c>` for literals and keywords.

---

## 3. Golden Reference Template (C# Interface & Model)

Model your XML comments after this exact standard:

```csharp
/// <summary>
/// Defines the asynchronous contract for validating, reserving, and settling customer payment orders
/// across external payment gateways.
/// </summary>
/// <typeparam name="TPayload">
/// The domain payload type attached to each settlement request. Must be a non-null reference type.
/// </typeparam>
/// <remarks>
/// <para>
/// Implementations of <see cref="IOrderSettlementService{TPayload}"/> must be thread-safe and support
/// concurrent invocations across scoped request pipelines.
/// </para>
/// <para>
/// The settlement workflow progresses through the following stages:
/// </para>
/// <list type="table">
/// <listheader>
/// <term>Stage</term>
/// <description>Behavioral Guarantee</description>
/// </listheader>
/// <item>
/// <term>1. Validation</term>
/// <description>Verifies payload integrity and currency limits without mutating remote state.</description>
/// </item>
/// <item>
/// <term>2. Reservation</term>
/// <description>Acquires an idempotent hold using <see cref="SettlementRequest{TPayload}.IdempotencyKey"/>.</description>
/// </item>
/// <item>
/// <term>3. Capture</term>
/// <description>Finalizes the funds transfer and emits a <see cref="SettlementReceipt"/>.</description>
/// </item>
/// </list>
/// <note type="important">
/// Callers must pass the same <c>IdempotencyKey</c> when retrying transient network failures to prevent duplicate charges.
/// </note>
/// </remarks>
/// <example>
/// Resolve <see cref="IOrderSettlementService{TPayload}"/> from dependency injection and settle an order:
/// <code language="csharp">
/// public async Task&lt;SettlementReceipt&gt; CheckoutAsync(
///     IOrderSettlementService&lt;InvoicePayload&gt; settlementService,
///     InvoicePayload invoice,
///     CancellationToken cancellationToken)
/// {
///     var request = new SettlementRequest&lt;InvoicePayload&gt;(
///         IdempotencyKey: "ORD-2026-0042",
///         Amount: 149.95m,
///         CurrencyCode: "EUR",
///         Payload: invoice);
///
///     return await settlementService.SettleOrderAsync(request, cancellationToken);
/// }
/// </code>
/// </example>
/// <seealso cref="SettlementRequest{TPayload}"/>
/// <seealso cref="SettlementReceipt"/>
public interface IOrderSettlementService<TPayload>
    where TPayload : class
{
    /// <summary>
    /// Gets the unique provider identifier of the underlying settlement gateway (for example, <c>"STRIPE-EU"</c>).
    /// </summary>
    /// <value>
    /// A non-empty, uppercase ASCII identifier representing the active gateway provider.
    /// </value>
    string ProviderIdentifier { get; }

    /// <summary>
    /// Validates and settles the specified <paramref name="request"/> asynchronously.
    /// </summary>
    /// <param name="request">
    /// The settlement request containing the monetary amount, three-letter ISO currency code, idempotency key,
    /// and <typeparamref name="TPayload"/> metadata. Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the asynchronous gateway operation before capture is committed.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous settlement operation. The task result contains the immutable
    /// <see cref="SettlementReceipt"/> with the transaction reference and settlement timestamp.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If a request with the same <see cref="SettlementRequest{TPayload}.IdempotencyKey"/> has already succeeded
    /// within the past 24 hours, this method returns the original <see cref="SettlementReceipt"/> without
    /// re-charging the customer.
    /// </para>
    /// <note type="tip">
    /// Pair this method with a bounded timeout on <paramref name="cancellationToken"/> (such as 15 seconds)
    /// to avoid blocking upstream HTTP workers when a gateway experiences degraded latency.
    /// </note>
    /// </remarks>
    /// <example>
    /// Execute a settlement call and handle validation errors:
    /// <code language="csharp">
    /// try
    /// {
    ///     SettlementReceipt receipt = await service.SettleOrderAsync(request, cancellationToken);
    ///     Console.WriteLine($"Settled {receipt.TransactionId} at {receipt.SettledAtUtc:O}");
    /// }
    /// catch (InvalidOperationException exception)
    /// {
    ///     Console.Error.WriteLine($"Gateway rejected settlement: {exception.Message}");
    /// }
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <see cref="SettlementRequest{TPayload}.Amount"/> is less than or equal to zero.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the payment gateway rejects the currency code or the idempotency key conflicts with a different payload.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken"/> is triggered before the gateway responds.
    /// </exception>
    /// <seealso cref="TryGetReceiptAsync(string, CancellationToken)"/>
    Task<SettlementReceipt> SettleOrderAsync(
        SettlementRequest<TPayload> request,
        CancellationToken cancellationToken = default);
}
```

---

## 4. Execution & Verification Workflow

1. **Scan the Solution**:
   - Locate all `.csproj` files and all `public interface`, `public class`, `public record`, `public struct`, `public enum`, and `public delegate` declarations.
2. **Author XML Comments**:
   - Add complete XML documentation comments to every public interface and contract type following Sections 2 and 3.
3. **Build & Verify Compiler Warnings**:
   - Run `dotnet build` and resolve any `CS1591` (missing XML comment), `CS1570` (malformed XML), `CS1572`/`CS1573` (mismatched `<param>`), or `CS1574`/`CS1584` (unresolved `cref`) warnings.
4. **Verify with `Documentation.Exporter.Cli` (if installed/available)**:
   - Run `Documentation.Exporter.Cli` with `--strict` and `--report-file` to confirm `100%` documentation coverage, `0` `DOC001`–`DOC005` missing-tag diagnostics, and `0` broken links (`DOC006`–`DOC007`):
     ```bash
     Documentation.Exporter.Cli solution \
       --solution <YourSolution.sln> \
       --output docs/api \
       --strict \
       --fail-on-undocumented 95 \
       --report-file docs/api-quality-report.json \
       --report-format Json
     ```
   - Inspect `docs/api-quality-report.json` and fix any remaining `DOC001` (missing type summary), `DOC002` (missing member summary), `DOC003` (missing `<param>`), `DOC004` (missing `<typeparam>`), `DOC005` (missing `<returns>`), `DOC006` (broken link), or `DOC007` (broken anchor) diagnostics until the report shows **0 errors and 0 warnings**.
````

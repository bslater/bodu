C# Code Style Guidelines
File and Header Formatting

Include the copyright header in the standard format with separator banner lines.
Preserve consistent spacing and alignment within the header.
Follow the established file presentation style for partial classes and related files.


XML Documentation
All documentation must be in British English

<summary>
Write a concise, professional summary describing the purpose, intent, or responsibility of the type or member.
Keep the tone factual and API-consumer focused.
Do not mechanically repeat the member name.
Prefer strong verb-led phrasing: Provides…, Gets…, Initializes…, Attempts to…, Returns…, Removes…, Adds…

<param>
Add a <param> for every parameter.
Keep descriptions concise — ideally a single line.
Describe the parameter in the context of the member's behavior.
Use "Must not be <see langword="null" />." style wording where applicable.

<returns>
Add <returns> for every non-void member.
Describe the result in the context of the method's purpose, not merely the raw type.

<exception>
Document all exceptions the member can throw, including ArgumentNullException, ArgumentException, ArgumentOutOfRangeException, and InvalidOperationException.
Describe the exact condition that causes each exception using the established style:

<paramref name="capacity" /> ≤ 0.
The buffer is empty.
Thrown if <paramref name="owner" /> is <see langword="null" />.


<remarks>
Add <remarks> when it materially helps the consumer understand concurrency behavior, snapshot semantics, ordering guarantees, side effects, edge cases, stability caveats, performance trade-offs, or design intent.
Use <para> blocks within remarks where appropriate to maintain visual structure.

<example>
Add examples when they improve usability or remove ambiguity.
Keep examples minimal, realistic, and consumer-focused.
Prefer examples for public types or members where usage is not immediately obvious.

<value>
Include <value> on properties where the semantics require clarification beyond the summary.

<inheritdoc />
Use <inheritdoc /> where the implementation intentionally inherits interface or base member documentation and no further clarification is needed.


Documentation Tone
- Be concise, but not abrupt.
- Be precise, but not overly academic.
- Explain observable behavior, guarantees, and limitations.
- Use standard XML documentation idioms consistently.
- Do not write vague or filler summaries.
- Do not repeat obvious type information unnecessarily.
- Do not over-explain trivial members.
- Do not use casual or conversational wording.


Inline Comments
- Add inline comments only where they provide real value.
- Use them to explain non-obvious logic, concurrency coordination, lock-free or low-level - state transitions, defensive clamping, important sequencing requirements, or why a block exists when it is not self-evident.
- Explain why, the protocol intent, or subtle state meaning — not basic syntax.
- Do not add comments that merely narrate obvious code.


Formatting and Layout
Blank Lines
- Insert blank lines between logical groups of code to make structure visually clear.
- Separate guard clauses and validation, field assignments, setup and initialization, core logic branches, success and failure paths, event invocation or side effects, and return statements.

Member Layout
- Maintain consistent spacing between members.
- Group related members logically.
- Use expression-bodied members where the body is trivially concise.
- Use block bodies for members with meaningful logic.

Braces and Wrapping
- Follow standard modern C# brace style as shown in the examples.
- Wrap long XML documentation lines and remarks sensibly for readability.

Naming and Qualification
- Use consistent naming and qualification patterns aligned to the examples.
- Retain explicit interface qualification where it improves clarity.
- Use framework types and language keywords consistently.


Code Quality
- Write code that is clear, maintainable, consistent, review-friendly, defensive where appropriate, and idiomatic C#.
- Prefer clarity over cleverness.
- All code must be suitable for shared library or framework-style use.


Updating Existing Code
- Preserve the original intent and behavior unless explicitly instructed otherwise.
- Improve documentation, formatting, naming clarity, and readability without introducing unnecessary rewrites.
- Keep style consistent across the file — do not mix documentation styles.
- Avoid excessive comments or overlong XML documentation.
- Extend an established style consistently rather than replacing it.


Test Method Naming
- Follow the convention: <MethodOrPropertyName>_When<Condition>[_For<TypedCondition>]_Should<ExpectedResult>
- When<Condition> describes the state or input that sets up the scenario.
- _For<TypedCondition> is optional — include only when the condition is specific to a particular type, overload, or variant.
- Should<ExpectedResult> describes the expected observable outcome.

Examples:
csharpEnqueue_WhenFull_ShouldThrowInvalidOperationException()
Parse_WhenInputIsEmpty_ForNullableInt_ShouldReturnNull()
Capacity_WhenSetToZero_ShouldThrowArgumentOutOfRangeException()

Test Method Documentation
- Include a <summary> on every test method.
- Provide a short 1–2 sentence description of the scenario under test and the expected result.
- Write the summary so the test's intent is immediately clear without requiring the reader to inspect the body.
- Summaries should start with 'Verifies that ...'
Example:
csharp/// <summary>
/// Verifies that enqueueing an item into a full buffer throws
/// <see cref="InvalidOperationException" />.
/// </summary>
[Fact]
public void Enqueue_WhenFull_ShouldThrowInvalidOperationException() { ... }
# ReSharper vs Roslyn comparison

Generated (UTC): 2026-08-20T08:02:25.7280343Z

Corpus: characterization fixtures in `test/comparison/corpus`.

## Tools

- InspectCode: JetBrains Inspect Code 2025.3.5 (Wave 253)
- Plugin: `ReSharper.Structured.Logging` 2025.1.0.373 (Wave 251) loaded as an InspectCode extension
- InspectCode report: `test/comparison/reports/inspectcode.xml`
- Plugin issue types present in report: **True**
- InspectCode plugin findings: 23
- Roslyn SLA findings: 23

Keys are `file.cs:SLAxxxx`. Spans are not compared: SLA0011 highlights the trailing period, while the plugin highlights the whole literal.

## InspectCode vs Roslyn

- Matches: **23**
- inspectcode only: **0**
- roslyn only: **0**

### Message text differences (same file and rule)

- `PropertiesNamingAnalyzer_SerilogIgnoredInvalidNamedProperty.cs:SLA0009`: InspectCode “Property name 'MY_IGNORED.Property_' does not match naming rules. Suggested name is 'MYIgnoredProperty'.” vs Roslyn “Property name 'MY_IGNORED.Property_' does not match naming rules. Suggested name is 'MyIgnoredProperty'.”

## Roslyn findings

| File | ID | Line | Message |
|---|---|---:|---|
| `AnonymousTypeDestructure_SerilogWithComplexPropertyWithoutDestructure.cs` | SLA0001 | 11 | Anonymous objects must be destructured |
| `AnonymousTypeDestructure_SerilogWithoutDestructure.cs` | SLA0001 | 10 | Anonymous objects must be destructured |
| `ComplexTypeDestructure_SerilogContextWithoutDestructure.cs` | SLA0003 | 12 | Complex objects with default ToString() implementation probably need to be destructured |
| `ComplexTypeDestructure_SerilogCustomExceptionWithoutDestructure.cs` | SLA0002 | 11 | Complex objects with default ToString() implementation probably need to be destructured |
| `ComplexTypeDestructure_SerilogWithoutDestructure.cs` | SLA0002 | 11 | Complex objects with default ToString() implementation probably need to be destructured |
| `ContextualLoggerConstructor_MicrosoftWrongContextType.cs` | SLA0004 | 10 | Incorrect type is used for contextual logger |
| `ContextualLoggerConstructor_MicrosoftWrongContextTypeMultipleNamespaces.cs` | SLA0004 | 15 | Incorrect type is used for contextual logger |
| `ContextualLoggerConstructor_MicrosoftWrongContextTypeMultipleParameters.cs` | SLA0004 | 10 | Incorrect type is used for contextual logger |
| `ContextualLoggerSerilogFactory_SerilogWrongContextType.cs` | SLA0004 | 8 | Incorrect type is used for contextual logger |
| `CorrectExceptionPassing_SerilogIncorrectExceptionPassing.cs` | SLA0005 | 11 | Exception should be passed to the exception argument |
| `CorrectExceptionPassing_SerilogIncorrectExceptionPassingDynamicTemplate.cs` | SLA0005 | 11 | Exception should be passed to the exception argument |
| `CorrectExceptionPassing_SerilogIncorrectExceptionPassingDynamicTemplate.cs` | SLA0007 | 11 | Message template should be compile time constant |
| `DuplicatePropertiesTemplate_SerilogDuplicateNamedProperty.cs` | SLA0006 | 10 | Duplicate properties in message template |
| `DuplicatePropertiesTemplate_SerilogDuplicateNamedProperty.cs` | SLA0006 | 10 | Duplicate properties in message template |
| `LogMessageIsSentence_SerilogSentenceMessage.cs` | SLA0011 | 10 | Log event messages should be fragments, not sentences. Avoid a trailing period/full stop. |
| `PositionalPropertiesUsage_SerilogPositionProperty.cs` | SLA0008 | 10 | Prefer named properties instead of positional ones |
| `PropertiesNamingAnalyzerDotNet6_ZLoggerInvalidNamedProperty.cs` | SLA0009 | 11 | Property name 'myProperty' does not match naming rules. Suggested name is 'MyProperty'. |
| `PropertiesNamingAnalyzer_SerilogContextInvalidNamedProperty.cs` | SLA0010 | 11 | Property name 'test' does not match naming rules. Suggested name is 'Test'. |
| `PropertiesNamingAnalyzer_SerilogIgnoredInvalidNamedProperty.cs` | SLA0009 | 10 | Property name 'MY_IGNORED.Property_' does not match naming rules. Suggested name is 'MyIgnoredProperty'. |
| `PropertiesNamingAnalyzer_SerilogInvalidElasticNamedProperty.cs` | SLA0009 | 10 | Property name 'myProperty' does not match naming rules. Suggested name is 'MyProperty'. |
| `PropertiesNamingAnalyzer_SerilogInvalidNamedProperty.cs` | SLA0009 | 10 | Property name 'myProperty' does not match naming rules. Suggested name is 'MyProperty'. |
| `PropertiesNamingAnalyzer_SerilogInvalidNamedPropertyWithDot.cs` | SLA0009 | 10 | Property name 'My.Property' does not match naming rules. Suggested name is 'MyProperty'. |
| `PropertiesNamingAnalyzer_SerilogInvalidNamedPropertyWithSpace.cs` | SLA0009 | 10 | Property name 'My Property' does not match naming rules. Suggested name is 'MyProperty'. |

## InspectCode findings

| File | ID | Line | Message |
|---|---|---:|---|
| `AnonymousTypeDestructure_SerilogWithComplexPropertyWithoutDestructure.cs` | SLA0001 | 11 | Anonymous objects must be destructured |
| `AnonymousTypeDestructure_SerilogWithoutDestructure.cs` | SLA0001 | 10 | Anonymous objects must be destructured |
| `ComplexTypeDestructure_SerilogContextWithoutDestructure.cs` | SLA0003 | 12 | Complex objects with default ToString() implementation probably need to be destructured |
| `ComplexTypeDestructure_SerilogCustomExceptionWithoutDestructure.cs` | SLA0002 | 11 | Complex objects with default ToString() implementation probably need to be destructured |
| `ComplexTypeDestructure_SerilogWithoutDestructure.cs` | SLA0002 | 11 | Complex objects with default ToString() implementation probably need to be destructured |
| `ContextualLoggerConstructor_MicrosoftWrongContextType.cs` | SLA0004 | 10 | Incorrect type is used for contextual logger |
| `ContextualLoggerConstructor_MicrosoftWrongContextTypeMultipleNamespaces.cs` | SLA0004 | 15 | Incorrect type is used for contextual logger |
| `ContextualLoggerConstructor_MicrosoftWrongContextTypeMultipleParameters.cs` | SLA0004 | 10 | Incorrect type is used for contextual logger |
| `ContextualLoggerSerilogFactory_SerilogWrongContextType.cs` | SLA0004 | 8 | Incorrect type is used for contextual logger |
| `CorrectExceptionPassing_SerilogIncorrectExceptionPassing.cs` | SLA0005 | 11 | Exception should be passed to the exception argument |
| `CorrectExceptionPassing_SerilogIncorrectExceptionPassingDynamicTemplate.cs` | SLA0007 | 11 | Message template should be compile time constant |
| `CorrectExceptionPassing_SerilogIncorrectExceptionPassingDynamicTemplate.cs` | SLA0005 | 11 | Exception should be passed to the exception argument |
| `DuplicatePropertiesTemplate_SerilogDuplicateNamedProperty.cs` | SLA0006 | 10 | Duplicate properties in message template |
| `DuplicatePropertiesTemplate_SerilogDuplicateNamedProperty.cs` | SLA0006 | 10 | Duplicate properties in message template |
| `LogMessageIsSentence_SerilogSentenceMessage.cs` | SLA0011 | 10 | Log event messages should be fragments, not sentences. Avoid a trailing period/full stop. |
| `PositionalPropertiesUsage_SerilogPositionProperty.cs` | SLA0008 | 10 | Prefer named properties instead of positional ones |
| `PropertiesNamingAnalyzerDotNet6_ZLoggerInvalidNamedProperty.cs` | SLA0009 | 11 | Property name 'myProperty' does not match naming rules. Suggested name is 'MyProperty'. |
| `PropertiesNamingAnalyzer_SerilogContextInvalidNamedProperty.cs` | SLA0010 | 11 | Property name 'test' does not match naming rules. Suggested name is 'Test'. |
| `PropertiesNamingAnalyzer_SerilogIgnoredInvalidNamedProperty.cs` | SLA0009 | 10 | Property name 'MY_IGNORED.Property_' does not match naming rules. Suggested name is 'MYIgnoredProperty'. |
| `PropertiesNamingAnalyzer_SerilogInvalidElasticNamedProperty.cs` | SLA0009 | 10 | Property name 'myProperty' does not match naming rules. Suggested name is 'MyProperty'. |
| `PropertiesNamingAnalyzer_SerilogInvalidNamedProperty.cs` | SLA0009 | 10 | Property name 'myProperty' does not match naming rules. Suggested name is 'MyProperty'. |
| `PropertiesNamingAnalyzer_SerilogInvalidNamedPropertyWithDot.cs` | SLA0009 | 10 | Property name 'My.Property' does not match naming rules. Suggested name is 'MyProperty'. |
| `PropertiesNamingAnalyzer_SerilogInvalidNamedPropertyWithSpace.cs` | SLA0009 | 10 | Property name 'My Property' does not match naming rules. Suggested name is 'MyProperty'. |

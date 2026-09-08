---
"Alexaka1.Analyzers.StructuredLogging": patch
---

Make rule-scoped `property_naming` and `ignored_properties_regex` settings take precedence over prefix-level settings. Explicit `pascal_case` naming values are now recognized at both scopes. Invalid rule-scoped values, including malformed ignored-property regexes, fall through to valid prefix-level settings. Users who relied on a prefix-level key to override a rule-scoped key will see the rule-scoped setting applied instead.

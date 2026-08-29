# Security policy

## Supported versions

Only the latest version published to NuGet is eligible for security fixes. Older releases and builds from untrusted forks are not supported.

## Report a vulnerability

Use [GitHub private vulnerability reporting](https://github.com/alexaka1/structured-logging-analyzers/security/advisories/new). Do not open a public issue, discussion, or pull request for an undisclosed vulnerability.

Include:

- the affected package version and compiler or IDE host
- a minimal reproducer or exact steps
- the security impact and a realistic attack scenario
- relevant logs or stack traces with credentials and private source removed
- any suggested fix or mitigation, if you have one

You may publish the report after we agree on disclosure or after GitHub publishes the resulting security advisory. If you plan to disclose on a fixed date, state that date in the report.

## What belongs here

Security issues include:

- arbitrary code execution while the analyzer or code-fix assembly loads or runs
- unexpected filesystem, network, environment-variable, credential, or process access
- crafted source, configuration, or package input that crashes or hangs the compiler with a practical denial-of-service impact
- package contents or build automation that could ship untrusted runtime or analyzer code
- diagnostics, logs, artifacts, or telemetry that expose source code, secrets, or machine data

Normal analyzer false positives, false negatives, code-fix defects, compatibility problems, and performance regressions without a security impact belong in the public issue tracker.

## Research expectations

- Test only against systems, repositories, packages, and data you own or have permission to use.
- Stop if testing could affect another person or service.
- Keep the report confidential while remediation and disclosure are being discussed.
- Do not include secrets, credentials, private source code, or personal data in the report.

This project does not offer a bug bounty or payment for reports.

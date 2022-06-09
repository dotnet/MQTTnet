---
name: Bug report
about: Create a report to help us improve.
title: ''
labels: 'bug'
assignees: ''

---

### Verification

Before opening a bug make sure that the following conditions are met.
<!-- Remove this chapter when all conditions are met. -->

1. Performance issues are also appearing in RELEASE mode of your application.
2. An increased memory consumption or high CPU load also happens when your application runs in RELEASE mode AND logging is DISABLED.
3. Client issues with not received messages, connection drops etc. can be reproduced via another client application like "MQTTnetApp" (https://github.com/chkr1011/MQTTnetApp) or similar.
4. The bug also appears in the VERY LATEST version of this library. There is no support for older version (due to limited resources) but pull requests for older versions are welcome.

### Describe the bug
A clear and concise description of what the bug is.

### Which component is your bug related to?
<!-- Remove the items which don't apply from the following list. -->
- Client
- ManagedClient
- RpcClient
- Server

### To Reproduce
Steps to reproduce the behavior:
1. Using this version of MQTTnet '...'.
2. Run this code '....'.
3. With these arguments '....'.
4. See error.

### Expected behavior
A clear and concise description of what you expected to happen.

### Screenshots
If applicable, add screenshots to help explain your problem.

### Additional context / logging
Add any other context about the problem here.
Include debugging or logging information here:

```batch
\\ Put your logging output here.
```
### Code example
Please provide full code examples below where possible to make it easier for the developers to check your issues.

**Ideally a Unit Test (which shows the error) is provided so that the behavior can be reproduced easily.**
 
```csharp
\\ Put your code here.
```

* Core: Used new language features across the entire library
* Core: Performance improvements
* Server: Improved performance of retained messages when no event handler is attached (#2093, thanks to @zhaowgit)
* Server: The event `InterceptingClientEnqueue` is now also called for retained messages (BREAKING CHANGE!)
* Server: The local end point is now also exposed in the channel adapter (#2179)
* Marked all projects as AOT compatible
* Restored the strong name of the nugets
* Embedded license file in all nugets (#2197, thanks to @JensNordenbro)

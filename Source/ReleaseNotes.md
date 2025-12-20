* Core: Used new language features across the entire library
* Core: Performance improvements
* Core: Added validations for variable byte integers which do not match the .NET uint perfectly
* Client: Added support for pre-encoded UTF-8 binary buffers for user properties (#2228, thanks to @koepalex)
* Server: Improved performance of retained messages when no event handler is attached (#2093, thanks to @zhaowgit)
* Server: The event `InterceptingClientEnqueue` is now also called for retained messages (BREAKING CHANGE!)
* Server: The local end point is now also exposed in the channel adapter (#2179)
* Marked all projects as AOT compatible
* Restored the strong name of the nugets
* Embedded license file in all nugets (#2197, thanks to @JensNordenbro)
* Added support for dotnet10

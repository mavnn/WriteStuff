# Event Sourced Writing Projects

This is a simple server project for an event sourced writing project server. Unfortunately running it has some steep requirements if you're not set up for .NET development:

- a fairly recent install of the [.NET Core SDK](https://www.microsoft.com/net/learn/get-started-with-dotnet-tutorial)
- unless you're on windows, mono installed (to run Paket, the package manager). Brew or your OS package manager of choice should do the job, and you might have it installed already.
- an F# editor (see suggestions at https://fsharp.org/guides/mac-linux-cross-platform/ , but the Spacemacs fsharp is of course the correct answer ;) )
- docker, to run `docker-compose`

More about the point of this project on my blog at https://blog.mavnn.co.uk/blog/categories/writestuff/

To actually run the server:

- run `docker-compose up` in the root of the project
- go to the server directory and run `dotnet run` (or `dotnet watch run` to reload the server on changes to the source code)

# DevCoreApp
## Purpose 
DevCoreApp is a starter template for building applications that require advanced security, role-based access control, user management, organization hierarchy, and other enterprise features out of the box. Rather than rebuilding these foundational systems from scratch for every project, teams can fork DevCoreApp and focus on their domain-specific functionality.

The solution is built upon standard Visual Studio ASP.NET and Blazor project templates, featuring enhancements and a structure that facilitates the construction of large-scale enterprise applications. DevCoreApp is an open-source project that welcomes use and contributions from anyone.

Drawing inspiration from Robert Martin's Clean Architecture book and SOLID design principles, the solution is organized into multiple logical layers. However, this is not simply another canonical Clean Architecture project with an "onion" structure. Instead, it is a collection of practical approaches to address common developer challenges, rather than focusing on architecture for its own sake.

Benefits of this approach include flexibility, scalability, and improved unit test coverage. It allows for the hosting of applications on a small server and can later be scaled to a distributed cloud solution using a microservices architecture. Additionally, changing databases or email providers becomes more manageable. Teams with multiple developers will find this solution advantageous, as the various layers can be encapsulated as separate components or projects and developed independently.

## What It Is Not 

DevCoreApp is not a ready-made product; it is a set of guidelines, tools, and predefined structures. Its purpose is not to speed up coding but to promote cleaner code. As such, it may not be suitable for developers who prioritize immediate delivery without considering future implications.

## Help Needed

DevCoreApp is a living codebase. It contains bugs, and some design decisions may be improved upon. New tools and approaches can be incorporated. Comments, suggestions, or pull requests are greatly appreciated.

# Project Structure

The repository is organized in the following way:
-	```/deployment```: Contains artifacts needed for production deployment, including SQL scripts.
-	```/src```: Contains source code.
  -	```/src/Server```: Contains code that runs on the server side. Please refer to the [Server's ReadMe](/src/Server#readme).
  - ```/src/Shared```: Holds shared code between client and server, common utilities, and shared models.
  - ```/src/Client```: Contains code that executes on the client side (Blazor WebAssembly).
-	```/tests```: A collection of unit tests with a structure that mirrors the "src" folder.
-	```/mocks```: Mock service implementations for UI development without a database.

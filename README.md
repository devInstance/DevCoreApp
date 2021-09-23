# SampleWebApp
This project is collection of the best practices structuring ASP.NET Core project. It based on **Consumer**->**Service**->**Provider** architecture described in series of blog [articles]( https://devinstance.net/blog/aspnet-core-introduction-to-the-clean-architecture).

# Project Structure

This git repository has three branches: 
-	**web-api-app**:  Typical Web API project with no HTML rendering and any SPA client support.
-	**blazor-app**: Web API + Blazor application.
-	**angular-app**: Web API + Angular application.

The ASP.NET Core project (server side) is almost the same for all three branches.

The repository organized in the following way:
-	```/deployment```: any artifacts needed for production deployment including sql scripts
-	```/src```: source code
  -	```/src/Server```: code that runs one server side
    - ```/src/Server/Database```: database providers. Please see [Database ReadMe]( /src/Server/Database#readme)
    - ```/src/Server/Email```: Email provider(s)
    - ```/src/Server/WebService```: Web App project
  - ```/src/Shared```: shared code between client and server, common utilities, shared models
  - ```/src/Client``` (blazor-app only): Code that executes on the client side
-	```/tests```: Collection of unit tests. The structure is the same as “src”

# Notes

Please the serious of articles dedicated this sample app:

-	[Introduction to the clean architecture](https://devinstance.net/blog/aspnet-core-introduction-to-the-clean-architecture)

-	[Web API and Services](https://devinstance.net/blog/aspnet-core-web-api-and-services)

-	[Designing and implementing providers for SQLServer and PostgreSQL](https://devinstance.net/blog/aspnet-core-providers-for-sqlserver-and-postgresql)

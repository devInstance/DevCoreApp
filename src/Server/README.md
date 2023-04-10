# DevCoreApp: Web API Service
## Justification

The typical .NET Core Web API project in Visual Studio contains a "Controllers" folder, Startup.cs, and Program.cs. If authentication is chosen, it creates a database model and a "Migration" folder in the same project. This is a perfect project structure to start with.

As development progresses, controllers can grow to an enormous size. They might directly call EF code, execute queries, send emails, etc. Moving reusable code to a "BaseController" that every controller can inherit from may help, but this approach could complicate providing sufficient unit testing coverage (if any). This is when the following structure comes to the rescue:

- ```/src/Server/Database```: Includes database providers. Please refer to the [Database ReadMe](/src/Server/Database#readme).
- ```/src/Server/Email```: Contains email provider(s).
- ```/src/Server/WebService```: Houses the Web App project.
- ```/src/Server/WebService/Controller```: Contains controllers implementing the APIs.
- ```/src/Server/WebService/Services```: Contains services (see more about the purpose of services below).
- ```/src/Server/WebService/Authentication```: Contains authentication and authorization logic.
- ```/src/Server/WebService/Tools```: Includes a set of classes to support the framework.

## Controllers

Controllers live on the frontier of the web service. Their primary function is handling web requests. Controllers are responsible for accepting HTTP requests, reading inputs (query or request body), calling the service, returning results from the service back to the caller, and handling exceptions by converting them to HTTP codes. They "consume" a service. Any logic not directly related to HTTP should be handled by a dedicated service. Typically, controllers do not need to be unit tested.

## Services

A Service is a middleware component and contains all high-level logic, also known as business logic. It is the heart of the whole application and binds the controller, data access components, and other providers together. It is designed to abstract the controller from implementation details and keep the controller's code simple. In turn, the Service is protected by abstraction from details of database or other framework features implementations. The idea is that you can take a service, place it in a unit test, mock all the interfaces needed for it to operate, and it will just work without any changes.

A Service should follow the single responsibility pattern. It should not fall into the trap of satisfying the needs of a specific controller. It should have a single purpose. For instance, a "StudentService" should include functions needed to list, create, update, and delete students but have nothing to do with teachers.

A typical service is located in the "Services" folder of the project. It refers to the "Tools" namespace for some basic tools for service configuration. Potentially, tools and all or some of the services can be moved into a separate assembly.

## Providers

The sample app currently has several providers:

- **Identity and Authentication**: A set of interfaces implementing user and password validation and management. Interface declarations and implementations reside in the "Authentication" folder of the project. Potentially, it can be moved to a separate assembly if the authentication process gets more complicated.
- **Email provider**: A set of interfaces for composing and sending emails wrapped into a separate assembly called "EmailProcessor." There is an implementation based on MailKit.
- **Database providers**: Please see more in the [Database ReadMe](/src/Server/Database#readme).

Built a full-stack ASP.NET Core MVC web application to manage drivers, vehicles and maintenance records.  
Integrated an AI assistant that answers natural-language questions about fleet data using LLM function calling, with read-only, GDPR-aware data access.  
Implemented role-based access control (Admin/User) with ASP.NET Core Identity.  
Designed a SQL Server database with EF Core migrations, stored procedures and audit triggers to log deletions.  
Added PDF report exports with optional email delivery.

**AI Assistant:** Users ask questions in natural language and the LLM uses function calling to query the database through predefined, read-only functions. The model never accesses the database directly and personal data is protected, in line with GDPR principles. It runs locally with Ollama.  
**Authentication & Authorization:** ASP.NET Core Identity with Admin and User roles. Users have limited access.  
**Database:** SQL Server with Entity Framework Core (code-first migrations), stored procedures for specific queries, and triggers that record every deletion in an audit table (who, what and when).  
**Reports:** Export data to PDF and optionally send the report by email.

<img src="./prints/print1.jpg" alt="Captura de ecrã do projeto" width="500">
<img src="./prints/print2.jpg" alt="Captura de ecrã do projeto" width="500">
<img src="./prints/print3.jpg" alt="Captura de ecrã do projeto" width="500">
<img src="./prints/print4.jpg" alt="Captura de ecrã do projeto" width="500">

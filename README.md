# ProjV-Group-4-Security-City-Management
This is part of a Enterprise Project, where we implement the Security service of our City Management system.


Stack:

    - Frontend: React, Typescript, vite
    - Frontend testing: vitest, react testing library
    - Backend: ASP.NET core web API, C#
    - Backend testing: xUnit
    - Database: SQL server with EF core

Prerequisites:

    - Node.js and npm
    - .NET 8 SDK
    - Git

How to run: 

    Frontend: 
        cd frontend
        npm install 
        npm run dev
    
    Backend: 
        cd backend
        dotnet run --project CmSecurityService

    How to check incdents are in DB:
    |   - docker compose up --build
    |   - Add a Incident in GET/Incident
    |   - docker compose exec db mysql -uroot -prootpassword security_db -e "SELECT incident_id, incident_title, incident_location, created_at FROM incident;"
    
    Tests: 
        frontend: npm test
        backend: dotnet test (from root)


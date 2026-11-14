ProjV-Group-4-Security-City-Management
This is part of a Enterprise Project, where we implement the Security service of our City Management system.


Stack:
    
    Frontend: React, Typescript, vite
    Frontend testing: vitest, react testing library
    Backend: ASP.NET core web API, C#
    Backend testing: xUnit
    Database: SQL server with EF core

Prerequisites:

    Node.js and npm
    .NET 8 SDK
    Git
    docker

How to run: 

    Frontend: 
        cd frontend
        npm install 
        npm run dev

    Backend: 
        
        docker running in the background (docker desktop recommended)
        docker compose up --build (from root)
        To rebuild: docker compose up --build -d security-api

    Tests: 
        frontend: npm test
        backend: dotnet test (from root)

    database: 
        
        docker compose up -d mysql (initialize)
        docker compose exec mysql mysql -u security_app -p security_db (enter application password when asked)
        SHOW TABLES; (shows tables in database)
        run different types of queries if needed.

Important: run the backend before the frontend
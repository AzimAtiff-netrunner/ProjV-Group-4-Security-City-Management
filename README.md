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
    - docker

How to run: 

    Frontend: 
        cd frontend
        npm install 
        npm run dev
    
    Backend: 
        - docker running in the background (docker desktop recommended)
        - docker compose up --build (from root)

    Tests: 
        frontend: npm test
        backend: dotnet test (from root)

Important: run the backend before the frontend


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
        - docker compose up -d (from root)
        - docker compose up --build (from root)
        To rebuild: 
            - docker compose up --build -d security-api

    How to check incdents are in DB:
    |   - docker compose up --build
    |   - Add a Incident in GET/Incident
    |   - docker compose exec db mysql -uroot -prootpassword security_db -e "SELECT incident_id, incident_title, incident_location, created_at FROM incident;"
    
    Tests: 
        frontend: npm test
        backend: dotnet test (from root)

    database: 
        - docker compose up -d mysql (initialize)
        - docker compose exec mysql mysql -u security_app -p security_db (enter application password when asked)
        - SHOW TABLES; (shows tables in database)
        - run different types of queries if needed.

Important: run the backend before the frontend
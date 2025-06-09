## Running PIDAR with Docker


This project includes Docker support for easy deployment and development. The provided Dockerfile builds the application using .NET 9.0 and runs it in a secure, non-root environment. The Docker Compose file exposes the application on port 8080.

This project includes Docker support for easy deployment and development. The provided Dockerfile builds the application using .NET 8.0 and runs it in a secure, non-root environment. The Docker Compose file exposes the application on port 4430.


### Requirements
- Docker and Docker Compose installed
- No external database or cache service is required by default
- .NET version: 8.0 (as specified in the Dockerfile)

### Build and Run Instructions

```bash
docker compose up --build
```


This command will build the Docker image and start the PIDAR service. The application will be available at [http://localhost:8080](http://localhost:8080).

This command will build the Docker image and start the PIDAR service. The application will be available at [https://localhost:4430](https://localhost:4430).


### Configuration
- The application runs as a non-root user for security
- No environment variables are required by default, but you can add a `.env` file if needed and uncomment the `env_file` line in `docker-compose.yml`
- If you add a database or other services, update the `docker-compose.yml` accordingly

### Ports
- The PIDAR service is exposed on **https://localhost:4430/**

---

## The Preclinical Image DAtaset Repository (PIDAR) is a public repository of metadata information of preclinical image datasets from any imaging modality associated to peer-review publications. ##

The Preclinical Image DAtaset Repository (PIDAR) is a public repository of metadata information of preclinical image datasets from any imaging modality associated to peer-review publications. The imaging metadata are organized as “collections” defined by a common disease, image modality or sub type (MRI, CT, PET, etc) or research focus. An emphasis is made to provide supporting data related to the images such as outcomes, treatment details, genomics and expert analyses. The main aim of this repository is to make the dataset FAIR: Findable, Accessible, Interoperable and Reusable inside the scientific community. All the dataset are supposed to be accessible through a license.


Added features:
1. download metadata in excel, PDF Json and csv formats
2. contribution page added.
3. Admin login for add, edit and delete metadatas
4. Swagger support added
5. Added index page
6. Major Changes:
   Migration to Postgres (free for commercial purpose)
7. DOCKERIZED the application with 3 services Pidar-web, Pidar-Db and Pidar-PgAdmin
8. Modified Metadata Details, Edit, Create pages for better Readability (categorized)
9. (only stastic page left for now 05/05/2025)
10. changed the index page to metadatas/index to display metadata table.
11. Added the cards to display the number of Datasets, total samples used and Total Metadatas.
12. Added statistics page
13. Fixed null exception error while Deleting a dataset
14.  changed the configuration to work on pidar website
15.  Added goto top button in _layout.cshtml

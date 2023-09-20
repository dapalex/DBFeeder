# DBFeeder

_The development is in progress as well as the documentation_

## Introduction

DBFeeder is an all-in-one solution that crawls and scraps information from the web to then populate a relational database.

## Using DBFeeder

### Configuration

The solution can be configured following the steps below:

1) Create json configuration files for crawler (instructions [here](https://github.com/dapalex/DBFeeder/blob/main/CrawlerService/configs/README.md))

2) Create json configuration files for scraper (instructions [here](https://github.com/dapalex/DBFeeder/blob/main/ScraperService/configs/README.md))

3) Define entities (EF Core) using Devart Entity Developer (instructions [here]((https://github.com/dapalex/DBFeeder/blob/main/DBFeederEntity/README.md)).

4) Update [`docker-compose.yml`](https://github.com/dapalex/DBFeeder/blob/main/docker-compose.yml) file in order to create a DAC service for each entity created

### Launching the solution

The solution runs using docker-compose.yml file:

#### Build

```bash
docker compose build
```

#### Launch

```bash
docker compose up
```

## Execution workflow

A complete retrieval of a single entity information comprehends the following phases:

- Crawler extracting the target url
- Scraper extracting information from the target url
- Data Access Command generating the entity and populating the corresponding table


## Architecture
The solution is composed of the following docker images:

- Crawler: a container from an image of a .Net 7 worker service running in multithreading, 1 task for each source/configuration
- Scraper: a container containing multiple .Net 7 worker service processes, 1 process for each source
- DataAccessCommand: 1 container for each entity/DB table

![image](https://github.com/dapalex/DBFeeder/blob/main/Docs/DBFeeder%20Architecture.png)

Stack:
- Docker
- .Net 7
- RabbitMQ
- EF Core
- SQLite

maximize throughput
allow scalability
efficiency
ensure robustness (needs more work)
allow reusability

A simplified CQRS pattern has been applied consisting of a single DB and one DAC service for each table


## Services Overview

### Crawler

In charge of retrieving urls from an HTML source page.
More information [here](https://github.com/dapalex/DBFeeder/blob/main/CrawlerService)

### Scraper

In charge of retrieving information for the database population from the crawled urls.
More information [here](https://github.com/dapalex/DBFeeder/blob/main/ScraperService)

### Data Access Command

In charge of populating the database with the information scraped.
More information [here](https://github.com/dapalex/DBFeeder/blob/main/DACService)

## Services instantiation

![image](https://github.com/dapalex/DBFeeder/blob/main/Docs/DBFeeder%20Creation%20Workflow.png)


## Last words

This repo is dedicated to Peter, a friend who gave me the chance to learn how life can be enjoyable.

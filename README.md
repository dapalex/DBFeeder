# DBFeeder

## Introduction

DBFeeder is an all-in-one solution that crawls and scraps information from the web to then populating a relational database.

The development is in progress as well as the documentation

## Architecture

Given the workload and complexity of the ..components.. has been designed as follows:

- Crawler: multithread, 1 task for each source
- Scraper: multiprocess, 1 process for each source
- DataAccessCommand: multicontainer, 1 container for each DB table


The architecture has been designed primarily to widen my personal knowledge .... hands on as deep as possible ... on the following stacks:
- Docker
- .Net 7
Secondarily in order to:
maximize throughput
allow scalability
ensure robustness (needs more work)
allow reusability


...........in terms of efficiency, robustness, scalability and reusability

A simplified CQRS pattern has been applied consisting of a single DB and one DAC service for each table

## Services

It is composed of three main docker service models:

- Crawler Service: the 
- Scraper Service:
- Data Access Command Service: 



### Crawler

More information [here](https://github.com/dapalex/DBFeeder/CrawlerService)

### Scraper

More information [here](https://github.com/dapalex/DBFeeder/ScraperService)

### Data Access Command

More information [here](https://github.com/dapalex/DBFeeder/DACService)

## Services instantiation

### docker-compose


## Execution workflow

## Using DBFeeder

The solution can be utilized just following the steps below:

1) Create json crawler configuration file (instructions [here](https://github.com/dapalex/DBFeeder/CrawlerService/configs/README.md))
2) Create json scraper configuration file (instructions [here](https://github.com/dapalex/DBFeeder/ScraperService/configs/README.md))
3) Define entities (EF Core) using Entity Developer (instructions [here]((https://github.com/dapalex/DBFeeder/DBFeederEntity/README.md))


## Last words

The solution works, when there is an elevated number of producers/consumers of the Event Bus (depending on the number of configuration files) the RabbitMQ .Net Client can struggle to remain consistent:
there are some issues occurring randomly like deadlocks, loss of communication, etc that are waiting for resolution [here](https://soiaofioe).

This repo is dedicated to Peter, a friend who gave me the chance to learn how life can be enjoyable.
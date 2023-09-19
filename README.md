# DBFeeder

## Introduction

DBFeeder is an all-in-one solution that crawls and scraps information from the web to then populating a relational database.

_The development is in progress as well as the documentation_

## Architecture

- Crawler: multithread, 1 task for each source
- Scraper: multiprocess, 1 process for each source
- DataAccessCommand: multicontainer, 1 container for each DB table

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


### Crawler

More information [here](https://github.com/dapalex/DBFeeder/CrawlerService)

### Scraper

More information [here](https://github.com/dapalex/DBFeeder/ScraperService)

### Data Access Command

More information [here](https://github.com/dapalex/DBFeeder/DACService)

## Services instantiation

![image](https://github.com/dapalex/DBFeeder/blob/main/Docs/DBFeeder%20Creation%20Workflow.png)


## Execution workflow

## Using DBFeeder

The solution can be utilized following the steps below:

1) Create json configuration files for crawler (instructions [here](https://github.com/dapalex/DBFeeder/CrawlerService/configs/README.md))
2) Create json configuration files for scraper (instructions [here](https://github.com/dapalex/DBFeeder/ScraperService/configs/README.md))
3) Define entities (EF Core) using Devart Entity Developer (instructions [here]((https://github.com/dapalex/DBFeeder/DBFeederEntity/README.md))


## Last words

The solution works, when there is an elevated number of producers/consumers of the Event Bus (depending on the number of configuration files) the RabbitMQ .Net Client can struggle to remain consistent:
there are some issues occurring randomly like deadlocks, loss of communication, etc that are waiting for resolution [here](https://soiaofioe).

This repo is dedicated to Peter, a friend who gave me the chance to learn how life can be enjoyable.

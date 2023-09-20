# DBFeeder - Data Access Command

_The development is in progress as well as the documentation_

The objective of this service is to populate the database with the entity information received from the [_Scraper_](https://github.com/dapalex/DBFeeder/blob/main/Scraper) via Event Bus.

## Introduction

Data Access Command is a background service receiving the entity objects from the scraper and communicating with the database in order to populate it.
The project uses a generic service which will work with any entity created from the model in DBFeederEntity.


## Design
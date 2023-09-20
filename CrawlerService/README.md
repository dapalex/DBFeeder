# DBFeeder - Crawler Service

_The development is in progress as well as the documentation_

The objective of this service is to extract urls from an initial HTML page and pass it to the [_Scraper_](https://github.com/dapalex/DBFeeder/blob/main/ScraperService) via Event Bus.

## Introduction

This is the first service to be instantiated.
Starting from an inital source url defined in the configuration file it retrieves the urls to scrap and send them to the Event Bus.

The retrieval constists of fetching the source HTML page and Navigate through in order to find the Target (a string representing an URL).
For each target/url it will create an `AMQPUrlMessage` object that will be sent to the Event Bus.


## Design

The Crawler works in multithreading, a new background thread for each configuration file present in the configs folder.


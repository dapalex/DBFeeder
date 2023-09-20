# DBFeeder- Common library

_The development is in progress as well as the documentation_

## Introduction

This library contains classes shared among the services:

- AMQP classes: allowing components to interact with the Event Bus
- Serializer: translating created configuration files in a `Model` object
- Surfer: classes navigating through HTML pages
- Persistence & Logger: to keep track of progress

## AMQP classes

## HtmlAgilityPack Extensions

HtmlAgilityPack is the library used to navigate through the HTML pages.
Some extensions have been created to 

## Persistence & Logger

A SQLite database, `feeder-progress.db` is used to keep track of the progress made by the solution.
In detail there is a progress tracking for the Crawler (table `URL_CRAWLED`) and a log of errors occurred during the execution for all components (table `LOG_ERRORS`).


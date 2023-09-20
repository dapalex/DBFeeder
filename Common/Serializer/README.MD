# DBFeeder - Configuration Model

_The development is in progress as well as the documentation_

This document explains the structure of the Model class corresponding to the configuration files.
The structure is shared between Crawler and Scraper.

## Extraction

This is the main class containing the whole configuration.

- `urlBase` [Optional]: `string` used by _Crawler_, this field contains the domain of the source url where to start crawling

- `urlSuffix` [Optional]: `string` used by _Crawler_, suffix of the source url. Combined with urlBase it will give the starting url for crawling.

- `directUrls` [Optional]: `List<string>` used by _Crawler_, array of urls used in case links can be directly passed to the scraper (this can be used in addition or to substitute `urlBase`/`Navigation` structure)

- `separatorId` [Optional]: `string` In case it is convenient to truncate directly an initial part of an HTML page this field can be set with a (unique) html string identifying where to truncate.

- `Navigation` [Mandatory]: [`Navigation`](https://github.com/dapalex/DBFeeder/Common/README.md#Navigation) object containing direction on how to traverse the HTML

- `Target` [Mandatory]: [`Target`](https://github.com/dapalex/DBFeeder/Common/README.md#Target) object containing the target information to gather

- `Next` [Optional]: [`Next`](https://github.com/dapalex/DBFeeder/Common/README.md#Next) object used by Crawler, in case there are consequent pages to crawl from this will guide to the pagination


## Navigation


## Target

## Recon


### Classification

## Next

## Base classes

### HtmlElementProperty

### HtmlElementBase

### NameValue

### HtmlAttr

It has extensions

## Util classes

### Regexing

### RegexString


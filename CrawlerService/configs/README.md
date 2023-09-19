# DBFeeder - Configuration

_Documentation in progress..._

## Introduction

This can be the hardest part to setup, it can be simple with a few lines as complex involving several options available (explained below).

## Config file name structure

\<source>-\<entity/table>-\<optional first-level-category>.json


This document explains the structure of the Model class corresponding to the configuration files.
The structure is shared between Crawler and Scraper.

## Extraction

This is the main class containing the whole configuration.

The corresponding json is as follow:

```json
{
  "extraction": {
    "name": "...",
    "urlBase": "...",
    "urlSuffix": "...",
    "directUrls": [ ... ],
    "separatorId": "...",
        "navigation": { ... },
    "target": { ... },
    "next": [ ... ]
  }
}
```

It is composed of the following fields:

- `urlBase` [Optional]: `string` used by _Crawler_, this field contains the domain of the source url where to start crawling

- `urlSuffix` [Optional]: `string` used by _Crawler_, suffix of the source url. Combined with urlBase it will give the starting url for crawling.

- `directUrls` [Optional]: `List<string>` used by _Crawler_, array of urls used in case links can be directly passed to the scraper (this can be used in addition or to substitute `urlBase`/`Navigation` structure)

- `separatorId` [Optional]: `string` In case it is convenient to truncate directly an initial part of an HTML page this field can be set with a (unique) html string identifying where to truncate.

- `Navigation` [Mandatory]: [`Navigation`](#Navigation) object containing direction on how to traverse the HTML

- `Target` [Mandatory]: [`Target`](#Target) object containing the target information to gather

- `Next` [Optional]: [`List<Next>`](#Next) object used by Crawler, in case there are consequent pages to crawl from this will guide to the pagination


## Navigation

```json
"navigation": {
      "tag": "...",
      "keyProperty": "...",
      "valueProperty": "...",
      "reconRelation": "...",
      "recon": { ... },
      "nav": { ... }
}
```

- `tag` [Mandatory]: [`HtmlAttr`](https://github.com/dapalex/DBFeeder/Common/README.md#HtmlAttr) 

- `keyProperty` [Optional]: [`HtmlAttr?`](https://github.com/dapalex/DBFeeder/Common/README.md#HtmlAttr) 

- `valueProperty` [Optional]: [`RegexString`](https://github.com/dapalex/DBFeeder/Common/README.md#RegexString)

- `recon` [Optional]: [`Recon`](#Recon)

- `reconRelation` [Optional]: [`Relation`](https://github.com/dapalex/DBFeeder/Common/README.md#Relation) enum used when a recon field is defined

- `nav` [Optional]: [`Navigation`](#Navigation) self reference

## Target

```json
"target": {
      "reconRelation": "...",
      "name": "...",
      "value": "...",
      "classType": "...",
      "recon": { ... },
      "HCValues": [ ... ]
}
```

- `tag` [Mandatory]: [`HtmlAttr`](https://github.com/dapalex/DBFeeder/Common/README.md#HtmlAttr)

- `keyProperty` [Optional]: [`HtmlAttr?`](https://github.com/dapalex/DBFeeder/Common/README.md#HtmlAttr) 

- `valueProperty` [Optional]: [`RegexString`](https://github.com/dapalex/DBFeeder/Common/README.md#RegexString)

- `name` [Mandatory]: `string`

- `value` [Optional]: [`HtmlAttr`](https://github.com/dapalex/DBFeeder/Common/README.md#HtmlAttr) 

- `recon` [Optional]: [`Recon`](#Recon)

- `reconRelation` [Optional]: [`Relation`](https://github.com/dapalex/DBFeeder/Common/README.md#Relation) enum used when a recon field is defined

- `regex` [Optional]: [`Regexing`](https://github.com/dapalex/DBFeeder/Common/README.md#Regexing)

- `HCValues` [Optional]: [`NameValue[]`](https://github.com/dapalex/DBFeeder/Common/README.md#NameValue)

- `classType` [Mandatory]: `string`


## Recon

```json
"recon": {
    "tag": "...",
    "keyProperty": "...",
    "valueProperty": "...",
    "reconValue": "...",
    "regex": { ... },
    "classification": [ ... ]
}
```

- `tag` [Mandatory]: [`HtmlAttr`](https://github.com/dapalex/DBFeeder/Common/README.md#HtmlAttr)

- `keyProperty` [Optional]: [`HtmlAttr?`](https://github.com/dapalex/DBFeeder/Common/README.md#HtmlAttr) 

- `valueProperty` [Optional]: [`RegexString`](https://github.com/dapalex/DBFeeder/Common/README.md#RegexString)

- `reconValue` [Optional]: [`HtmlAttr?`](https://github.com/dapalex/DBFeeder/Common/README.md#HtmlAttr)

- `regex` [Optional]: [`Regexing`](https://github.com/dapalex/DBFeeder/Common/README.md#Regexing)

- `classification` [Optional]: [`Classification[]`](#Classification)

### Classification

This is the object used by recon in charge of creating data to use during DB population.


- `name` [Optional]: `string`

- `value` [Optional]: `string`

- `findBy` [Optional]: [`RegexString`](https://github.com/dapalex/DBFeeder/Common/README.md#RegexString)

- `isExclusive` [Optional]: `bool`

Following the behavior based on whether the fields are defined:

| name   | value    | findBy | Behavior |
| :----: | :------: | :----: | :------- |
|  True  |   True   | True   | the classification is created only if findBy is satisfied, name is the column name, value is the corresponding value         |
| True   |   True   | False  |  it behaves as HCValues        |
| True   |  False   | True   |  the classification is created only if findBy is satisfied, name is the column name,  recon.reconValue is the corresponding value         |
| True   |  False   | False  | name is column name, recon.reconValue is the corresponding value         |

## Next

- `Navigation` [Mandatory]: [`Navigation`](#Navigation)

- `level` [Optional]: `int`

- `tag` [Mandatory]: [`HtmlAttr`](https://github.com/dapalex/DBFeeder/Common/README.md#HtmlAttr)

- `keyProperty` [Optional]: [`HtmlAttr?`](https://github.com/dapalex/DBFeeder/Common/README.md#HtmlAttr) 

- `valueProperty` [Optional]: [`RegexString`](https://github.com/dapalex/DBFeeder/Common/README.md#RegexString)

- `name` [Mandatory]: `string`

- `value` [Optional]: [`HtmlAttr`](https://github.com/dapalex/DBFeeder/Common/README.md#HtmlAttr) 

- `recon` [Optional]: [`Recon`](#Recon)

- `reconRelation` [Optional]: [`Relation`](https://github.com/dapalex/DBFeeder/Common/README.md#Relation) enum used when a recon field is defined

- `regex` [Optional]: [`Regexing`](https://github.com/dapalex/DBFeeder/Common/README.md#Regexing)

- `HCValues` [Optional]: [`NameValue[]`](https://github.com/dapalex/DBFeeder/Common/README.md#NameValue)

- `classType` [Mandatory]: `string`



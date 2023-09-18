# DBFeeder - Configuration Model

This document explains the structure of the Model class corresponding to the configuration files.
The structure is shared between Crawler and Scraper.

#### Extraction

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

- `urlBase` [Optional]: used by _Crawler_, this field contains the domain of the source url where to start crawling

- `urlSuffix` [Optional]: used by _Crawler_, suffix of the source url. Combined with urlBase it will give the starting url for crawling.

- `directUrls` [Optional]: used by _Crawler_, array of urls used in case links can be directly passed to the scraper (this can be used in addition or to substitute `urlBase`/`Navigation` structure)

- `separatorId` [Optional]: In case it is convenient to truncate directly an initial part of an HTML page this field can be set with a (unique) html string identifying where to truncate.

- `Navigation` [Mandatory]: object containing direction on how to traverse the HTML

- `Target` [Mandatory]: object containing the target information to gather

- `Next` [Optional]: used by Crawler, in case there are consequent pages to crawl from this will guide to the pagination


#### Navigation

```json
"navigation": {
      "tag": "...",
      "keyProperty": "...",
      "valueProperty": "...",
      "recon": { ... },
      "nav": { ... }
}
```

#### Target

```json
"target": {
      "reconRelation": "CHILD",
      "value": "...",
      "classType": "...",
      "recon": { ... },
      "HCValues": [ ... ]
}
```

#### Recon

```json
"recon": {
    "tag": "strong",
    "keyProperty": "class",
    "valueProperty": "pagenav_%",
    "reconValue": "InnerText",
    "regex": { ... },
    "classification": [ ... ]
}
```

#### Next


#### Base classes

##### HtmlElementProperty

##### HtmlElementBase

##### NameValue

##### HtmlAttr

It has extensions

#### Util classes

##### Regexing

##### RegexString


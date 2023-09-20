# DBFeeder - Configuration

_The development is in progress as well as the documentation_

The purpose of the configuration file is to extract the information needed from a source (HTML page) in order to populate the entities defined in [_DBFeederEntity_](https://github.com/dapalex/DBFeeder/blob/main/DBFeederEntity) for the database.

## Introduction

This document lists the items necessary and available for the creation of a configuration file, the structure corresponds to the [`Model`](https://github.com/dapalex/DBFeeder/blob/main/Common/Serializer/README.md#model) object, shared between Crawler and Scraper.


## Config file name structure

\<source>-\<entity/table>-\<optional additional text>.json

## Extraction

This is the main class containing the whole configuration.

The corresponding json is as follow:

```javascript
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

- `urlBase` [Optional]: `string` used by _Crawler_, this field contains the domain of the source url where to start crawling

- `urlSuffix` [Optional]: `string` used by _Crawler_, suffix of the source url. Combined with urlBase it will give the starting url for crawling.

- `directUrls` [Optional]: `List<string>` used by _Crawler_, array of urls used in case links can be directly passed to the _Scraper_ (this can be used in addition or to substitute `urlBase`/`Navigation` structure)

- `separatorId` [Optional]: `string` In case it is convenient to truncate directly an initial part of an HTML page this field can be set with a (unique) html string to identify where to truncate the HTML

- `Navigation` [Mandatory]: [`Navigation`](#navigation) containing instructions on how to traverse the HTML

- `Target` [Mandatory]: [`Target`](#target) containing the target information to gather

- `Next` [Optional]: [`List<Next>`](#next) used by _Crawler_, in case there are consequent pages to crawl this object will instruct how to proceed to the next page


## Navigation

The purpose of this object is to navigate to the target HTML element.
Navigation objects nested in themselves allow to traverse the HTML down to it.
During the na

Following the json structure:

```javascript
"navigation": {
      "tag": "...",
      "keyProperty": "...",
      "valueProperty": "...",
      "nav": { ... }
}
```

Starting from the tag `body` each Navigation object will contain the information to recognize the HTML node where to navigate to.

Following details of the fields:

- `tag` [Mandatory]: [`HtmlAttr`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#htmlattr) enum, defines the tag of the HTML node to traverse

- `keyProperty` [Optional]: [`HtmlAttr?`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#htmlattr) enum, if valued it corresponds to the attribute name of the HTML node to traverse

- `valueProperty` [Optional]: [`RegexString`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#regexstring) object, valid only if `keyProperty` has a value, it corresponds to the attribute value of the HTML node to traverse. If null, only the attribute name in `keyProperty` will be checked

- `nav` [Optional]: [`Navigation`](#navigation) self reference, defines the child HTML node to traverse

## Target

```javascript
"target": {
      "reconRelation": "...",
      "value": "...",
      "classType": "...",
      "recon": { ... },
}
```

- `tag` [Mandatory]: [`HtmlAttr`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#htmlattr) enum, defines the tag of the target HTML node

- `keyProperty` [Optional]: [`HtmlAttr?`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#htmlattr) enum, if valued it corresponds to the attribute name of the target HTML node

- `valueProperty` [Optional]: [`RegexString`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#regexstring), valid only if `keyProperty` has a value, it corresponds to the attribute value of the target HTML node. If null, only the attribute name in `keyProperty` will be checked

- `value` [Optional]: [`HtmlAttr`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#htmlattr) enum, indicates where to get the target information

- `recon` [Optional]: [`Recon`](#recon), used to retrieve the information in the target corresponding to the variable name of the entity

- `reconRelation` [Optional]: [`Relation`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#relation) enum, valid only if `recon` has a value, it defines how the `recon` is related to the [`Target`](#target) object

- `regex` [Optional]: [`Regexing`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#regexing), allows to apply some rules to the target `value`

- `HCValues` [Optional]: [`NameValue[]`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#namevalue) array, it can contain hard-coded values corresponding to _variable name_ and _variable value_ of the entity

- `classType` [Mandatory]: `string`


## Recon

```javascript
"recon": {
    "tag": "...",
    "keyProperty": "...",
    "valueProperty": "...",
    "reconValue": "...",
    "regex": { ... },
    "classification": [ ... ]
}
```

- `tag` [Mandatory]: [`HtmlAttr`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#htmlattr) enum, defines the tag of the recon HTML node

- `keyProperty` [Optional]: [`HtmlAttr?`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#htmlattr) enum, if valued it corresponds to the attribute name of the recon HTML node

- `valueProperty` [Optional]: [`RegexString`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#regexstring), valid only if `keyProperty` has a value, it corresponds to the attribute value of the target HTML node. If null, only the attribute name in `keyProperty` will be checked

- `reconValue` [Optional]: [`HtmlAttr?`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#htmlattr) enum, indicates where to get the recon information

- `regex` [Optional]: [`Regexing`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#regexing), allows to apply some rules to the target `value`

- `classification` [Optional]: [`Classification[]`](#classification)

## Next

- `Navigation` [Mandatory]: [`Navigation`](#navigation)

- `level` [Optional]: `int`

- `tag` [Mandatory]: [`HtmlAttr`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#htmlattr)

- `keyProperty` [Optional]: [`HtmlAttr?`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#htmlattr)

- `valueProperty` [Optional]: [`RegexString`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#regexstring)

- `name` [Mandatory]: `string`

- `value` [Optional]: [`HtmlAttr`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#htmlattr)

- `recon` [Optional]: [`Recon`](#recon)

- `reconRelation` [Optional]: [`Relation`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#relation) enum used when a recon field is defined

- `regex` [Optional]: [`Regexing`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#regexing)

- `HCValues` [Optional]: [`NameValue[]`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#namevalue)

- `classType` [Mandatory]: `string`, for the Crawler it must be `Common.AMQP.AMQPUrlMessage`



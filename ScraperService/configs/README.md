# DBFeeder - Configuration

_The development is in progress as well as the documentation_

The purpose of the configuration file is to extract the information needed from a source (HTML page) in order to populate the entities defined in [_DBFeederEntity_](https://github.com/dapalex/DBFeeder/blob/main/DBFeederEntity) for the database.

## Introduction

This document lists the items necessary and available for the creation of a configuration file, the same structure is shared between Crawler and Scraper.

The creation of a configuration file can be simple with a few lines as complex involving several options available (explained below), depending on where information need to be gathered from the HTML.

## Config file name structure

\<source>-\<entity/table>-\<optional additional text>.json

## Extraction

This is the main class containing the whole configuration.

The corresponding json is as follow:

```javascript
{
  "extraction": {
    "name": "...",
    "separatorId": "...",
    "navigation": { ... },
    "target": { ... }
  }
}
```

- `separatorId` [Optional]: `string` In case it is convenient to truncate directly an initial part of an HTML page this field can be set with a (unique) html string to identify where to truncate the HTML

- `Navigation` [Mandatory]: [`Navigation`](#navigation) containing instructions on how to traverse the HTML

- `Target` [Mandatory]: [`Target`](#target) containing the target information to gather


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
      "reconRelation": "...",
      "recon": { ... },
      "nav": { ... }
}
```

Starting from the tag `body` each Navigation object will contain the information to recognize the HTML node where to navigate to.

Following details of the fields:

- `tag` [Mandatory]: [`HtmlAttr`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#htmlattr) enum, defines the tag of the HTML node to traverse

- `keyProperty` [Optional]: [`HtmlAttr?`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#htmlattr) enum, if valued it corresponds to the attribute name of the HTML node to traverse

- `valueProperty` [Optional]: [`RegexString`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#regexstring) object, valid only if `keyProperty` has a value, it corresponds to the attribute value of the HTML node to traverse. If null, only the attribute name in `keyProperty` will be checked

- `recon` [Optional]: [`Recon`](#recon) object, used in case it is needed to retrieve information for the destination entity while navigating to the target

- `reconRelation` [Optional]: [`Relation`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#relation) enum, valid only if `recon` has a value, it defines how the `recon` is related to the [`Navigation`](#navigation) object

- `nav` [Optional]: [`Navigation`](#navigation) defines the child HTML node to traverse

## Target

```javascript
"target": {
      "reconRelation": "...",
      "value": "...",
      "classType": "...",
      "recon": { ... },
      "HCValues": [ ... ]
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

- `classType` [Mandatory]: `string`, it must be the assembly qualified name of the destination entity


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

### Classification

This is the object used by recon in charge of creating data to use during DB population.


- `name` [Optional]: `string`

- `value` [Optional]: `string`

- `findBy` [Optional]: [`RegexString`](https://github.com/dapalex/DBFeeder/blob/main/Common/README.md#regexstring)

- `isExclusive` [Optional]: `bool`

Following the behavior based on whether the fields are defined:

| name   | value    | findBy | Behavior |
| :----: | :------: | :----: | :------- |
|  True  |   True   | True   | the classification is created only if findBy is satisfied, name is the column name, value is the corresponding value         |
| True   |   True   | False  |  it behaves as HCValues        |
| True   |  False   | True   |  the classification is created only if findBy is satisfied, name is the column name,  recon.reconValue is the corresponding value         |
| True   |  False   | False  | name is column name, recon.reconValue is the corresponding value         |

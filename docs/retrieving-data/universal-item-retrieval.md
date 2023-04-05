# Universal item retrieval

Intro



## Retrieve data

* use sync vs. async method for IUniversalModelProvider?

## Data structure

* mention why not record classes
* withCodename method/extension
* do we want to use IList/IEnumerable for modular content?

## Convert to strongly type models

Explain, or raise issue

## Exceptions

Mention `IPropertyValueConverter` and `IPropertyMapper` being excluded

## Customization

* Mention DeliveryCLientFactory for registering custom

## Caching

* Mention retrieval key `IUniversalContentItem` + `IList<IUniversalContentItem>`
* Mention max cache count (maybe somewhere else) `CacheHelpers.MAX_DEPENDENCY_ITEMS`

> TODOS
> 
> * [] Link from the root and in-between pages
> * [] Finish the docs
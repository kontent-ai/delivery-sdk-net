using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;

namespace Kontent.Ai.Delivery.Rx;

/// <summary>
/// Provides extension methods for converting IDeliveryResult instances to Observables.
/// </summary>
public static class DeliveryResultObservableExtensions
{
    /// <summary>
    /// Converts a single item delivery result to an observable sequence.
    /// </summary>
    /// <typeparam name="T">The type of the content item elements.</typeparam>
    /// <param name="result">The delivery result to convert.</param>
    /// <returns>An observable sequence containing the item's elements, or an error if the result was not successful.</returns>
    public static IObservable<T> ToObservable<T>(this IDeliveryResult<IContentItem<T>> result)
        where T : class, IElementsModel
    {
        if (!result.IsSuccess)
        {
            return Observable.Throw<T>(new DeliveryException(result.Error, result.StatusCode));
        }

        return Observable.Return(result.Value.Elements);
    }

    /// <summary>
    /// Converts a dynamic single item delivery result to an observable sequence.
    /// </summary>
    /// <param name="result">The delivery result to convert.</param>
    /// <returns>An observable sequence containing the content item, or an error if the result was not successful.</returns>
    public static IObservable<IContentItem<IElementsModel>> ToObservableDynamic(this IDeliveryResult<IContentItem<IElementsModel>> result)
    {
        if (!result.IsSuccess)
        {
            return Observable.Throw<IContentItem<IElementsModel>>(new DeliveryException(result.Error, result.StatusCode));
        }

        return Observable.Return(result.Value);
    }

    /// <summary>
    /// Converts a multiple items delivery result to an observable sequence.
    /// </summary>
    /// <typeparam name="T">The type of the content item elements.</typeparam>
    /// <param name="result">The delivery result to convert.</param>
    /// <returns>An observable sequence containing each item's elements, or an error if the result was not successful.</returns>
    public static IObservable<T> ToObservableMany<T>(this IDeliveryResult<IReadOnlyList<IContentItem<T>>> result)
        where T : class, IElementsModel
    {
        if (!result.IsSuccess)
        {
            return Observable.Throw<T>(new DeliveryException(result.Error, result.StatusCode));
        }

        return result.Value.Select(item => item.Elements).ToObservable();
    }

    /// <summary>
    /// Converts a dynamic multiple items delivery result to an observable sequence.
    /// </summary>
    /// <param name="result">The delivery result to convert.</param>
    /// <returns>An observable sequence containing content items, or an error if the result was not successful.</returns>
    public static IObservable<IContentItem<IElementsModel>> ToObservableDynamicMany(this IDeliveryResult<IReadOnlyList<IContentItem<IElementsModel>>> result)
    {
        if (!result.IsSuccess)
        {
            return Observable.Throw<IContentItem<IElementsModel>>(new DeliveryException(result.Error, result.StatusCode));
        }

        return result.Value.ToObservable();
    }

    /// <summary>
    /// Converts a single content type delivery result to an observable sequence.
    /// </summary>
    /// <param name="result">The delivery result to convert.</param>
    /// <returns>An observable sequence containing the content type, or an error if the result was not successful.</returns>
    public static IObservable<IContentType> ToObservableType(this IDeliveryResult<IContentType> result)
    {
        if (!result.IsSuccess)
        {
            return Observable.Throw<IContentType>(new DeliveryException(result.Error, result.StatusCode));
        }

        return Observable.Return(result.Value);
    }

    /// <summary>
    /// Converts a multiple content types delivery result to an observable sequence.
    /// </summary>
    /// <param name="result">The delivery result to convert.</param>
    /// <returns>An observable sequence containing content types, or an error if the result was not successful.</returns>
    public static IObservable<IContentType> ToObservableTypes(this IDeliveryResult<IReadOnlyList<IContentType>> result)
    {
        if (!result.IsSuccess)
        {
            return Observable.Throw<IContentType>(new DeliveryException(result.Error, result.StatusCode));
        }

        return result.Value.ToObservable();
    }

    /// <summary>
    /// Converts a content element delivery result to an observable sequence.
    /// </summary>
    /// <param name="result">The delivery result to convert.</param>
    /// <returns>An observable sequence containing the content element, or an error if the result was not successful.</returns>
    public static IObservable<IContentElement> ToObservableElement(this IDeliveryResult<IContentElement> result)
    {
        if (!result.IsSuccess)
        {
            return Observable.Throw<IContentElement>(new DeliveryException(result.Error, result.StatusCode));
        }

        return Observable.Return(result.Value);
    }

    /// <summary>
    /// Converts a single taxonomy delivery result to an observable sequence.
    /// </summary>
    /// <param name="result">The delivery result to convert.</param>
    /// <returns>An observable sequence containing the taxonomy group, or an error if the result was not successful.</returns>
    public static IObservable<ITaxonomyGroup> ToObservableTaxonomy(this IDeliveryResult<ITaxonomyGroup> result)
    {
        if (!result.IsSuccess)
        {
            return Observable.Throw<ITaxonomyGroup>(new DeliveryException(result.Error, result.StatusCode));
        }

        return Observable.Return(result.Value);
    }

    /// <summary>
    /// Converts a multiple taxonomies delivery result to an observable sequence.
    /// </summary>
    /// <param name="result">The delivery result to convert.</param>
    /// <returns>An observable sequence containing taxonomy groups, or an error if the result was not successful.</returns>
    public static IObservable<ITaxonomyGroup> ToObservableTaxonomies(this IDeliveryResult<IReadOnlyList<ITaxonomyGroup>> result)
    {
        if (!result.IsSuccess)
        {
            return Observable.Throw<ITaxonomyGroup>(new DeliveryException(result.Error, result.StatusCode));
        }

        return result.Value.ToObservable();
    }

    /// <summary>
    /// Converts a languages delivery result to an observable sequence.
    /// </summary>
    /// <param name="result">The delivery result to convert.</param>
    /// <returns>An observable sequence containing languages, or an error if the result was not successful.</returns>
    public static IObservable<ILanguage> ToObservableLanguages(this IDeliveryResult<IReadOnlyList<ILanguage>> result)
    {
        if (!result.IsSuccess)
        {
            return Observable.Throw<ILanguage>(new DeliveryException(result.Error, result.StatusCode));
        }

        return result.Value.ToObservable();
    }
}

/// <summary>
/// Exception thrown when a delivery operation fails.
/// </summary>
public class DeliveryException : Exception
{
    /// <summary>
    /// Gets the delivery error details.
    /// </summary>
    public IError? DeliveryError { get; }

    /// <summary>
    /// Gets the HTTP status code of the failed operation.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryException"/> class.
    /// </summary>
    /// <param name="error">The delivery error.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    public DeliveryException(IError? error, int statusCode)
        : base(error?.Message ?? $"Delivery operation failed with status {statusCode}")
    {
        DeliveryError = error;
        StatusCode = statusCode;
    }
}
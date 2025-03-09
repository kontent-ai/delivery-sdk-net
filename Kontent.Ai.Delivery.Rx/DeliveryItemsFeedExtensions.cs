using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Rx;

/// <summary>
/// Provides extension methods for working with <see cref="IDeliveryItemsFeed{T}"/> instances in a reactive programming context.
/// </summary>
public static class DeliveryItemsFeedExtensions
{
    /// <summary>
    /// Converts an <see cref="IDeliveryItemsFeed{T}"/> into an <see cref="IObservable{T}"/> sequence.
    /// </summary>
    /// <typeparam name="T">The type of content items in the feed.</typeparam>
    /// <param name="feed">The <see cref="IDeliveryItemsFeed{T}"/> instance to convert.</param>
    /// <returns>
    /// An <see cref="IObservable{T}"/> sequence that emits items retrieved from the feed.
    /// If the feed is <c>null</c>, an empty observable sequence is returned.
    /// </returns>
    /// <exception cref="Exception">
    /// Propagates any exceptions that occur during the retrieval of items from the feed.
    /// </exception>
    public static IObservable<T> ToObservable<T>(this IDeliveryItemsFeed<T> feed) where T : class
    {
        if (feed == null)
        {
            return Observable.Empty<T>();
        }
        return Observable.Create<T>(async observer =>
        {
            try
            {
                await foreach (var item in EnumerateFeed(feed))
                {
                    observer.OnNext(item);
                }
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        });
    }

    private static async IAsyncEnumerable<T> EnumerateFeed<T>(IDeliveryItemsFeed<T> feed) where T : class
    {
        while (feed.HasMoreResults)
        {
            var batch = await feed.FetchNextBatchAsync();
            foreach (var contentItem in batch.Items)
            {
                yield return contentItem;
            }
        }
    }
}
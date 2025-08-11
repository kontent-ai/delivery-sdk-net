using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery
{
    /// <summary>
    /// Executes requests against the Kontent.ai Delivery API.
    /// </summary>
    internal sealed class DeliveryClient : IDeliveryClient
    {
        private readonly IDeliveryApi _deliveryApi;
        private readonly IOptionsMonitor<DeliveryOptions> _deliveryOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClient"/> class for retrieving content of the specified environment.
        /// </summary>
        /// <param name="deliveryApi">The Refit-generated API client.</param>
        /// <param name="deliveryOptions">The settings of the Kontent.ai environment.</param>
        public DeliveryClient(
            IDeliveryApi deliveryApi,
            IOptionsMonitor<DeliveryOptions> deliveryOptions)
        {
            _deliveryApi = deliveryApi ?? throw new ArgumentNullException(nameof(deliveryApi));
            _deliveryOptions = deliveryOptions ?? throw new ArgumentNullException(nameof(deliveryOptions));
        }

        /// <summary>
        /// Gets a strongly typed content item by its codename. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="IDeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        public async Task<IDeliveryItemResponse<T>> GetItemAsync<T>(string codename, IEnumerable<IQueryParameter>? parameters = null)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered item codename is not valid.", nameof(codename));
            }

            var queryParams = ConvertToSingleItemParams(parameters);
            return await _deliveryApi.GetItemInternalAsync<T>(codename, queryParams);
        }

        /// <summary>
        /// Returns strongly typed content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="IDeliveryItemListingResponse{T}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<IDeliveryItemListingResponse<T>> GetItemsAsync<T>(IEnumerable<IQueryParameter>? parameters = null)
        {
            var queryParams = ConvertToListItemsParams(parameters);
            return await _deliveryApi.GetItemsInternalAsync<T>(queryParams);
        }

        /// <summary>
        /// Returns a feed that is used to traverse through strongly typed content items matching the optional filtering parameters.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering or ordering.</param>
        /// <returns>The <see cref="IDeliveryItemsFeed{T}"/> instance that can be used to enumerate through content items. If no query parameters are specified, all content items are enumerated.</returns>
        public IDeliveryItemsFeed<T> GetItemsFeed<T>(IEnumerable<IQueryParameter>? parameters = null)
        {
            ValidateItemsFeedParameters(parameters);

            var enumParams = ConvertToEnumItemsParams(parameters);

            return new ContentItems.DeliveryItemsFeed<T>(async continuation =>
            {
                var response = await _deliveryApi.GetItemsFeedInternalAsync<T>(enumParams, null, continuation);
                return new ContentItems.DeliveryItemsFeedResponse<T>(response.ApiResponse, response.Items);
            });
        }

        /// <summary>
        /// Returns a content type.
        /// </summary>
        /// <param name="codename">The codename of a content type.</param>
        /// <returns>The <see cref="IDeliveryTypeResponse"/> instance that contains the content type with the specified codename.</returns>
        public async Task<IDeliveryTypeResponse> GetTypeAsync(string codename)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered type codename is not valid.", nameof(codename));
            }

            return await _deliveryApi.GetTypeInternalAsync(codename);
        }

        /// <summary>
        /// Returns content types.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IDeliveryTypeListingResponse"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public async Task<IDeliveryTypeListingResponse> GetTypesAsync(IEnumerable<IQueryParameter>? parameters = null)
        {
            var queryParams = ConvertToListTypesParams(parameters);
            return await _deliveryApi.GetTypesInternalAsync(queryParams);
        }

        /// <summary>
        /// Returns a content type element.
        /// </summary>
        /// <param name="contentTypeCodename">The codename of the content type.</param>
        /// <param name="contentElementCodename">The codename of the content type element.</param>
        /// <param name="parameters">A collection of query parameters. Currently not used for this endpoint.</param>
        /// <returns>The <see cref="IDeliveryElementResponse"/> instance that contains the specified content type element.</returns>
        public async Task<IDeliveryElementResponse> GetContentElementAsync(string contentTypeCodename, string contentElementCodename, IEnumerable<IQueryParameter>? parameters = null)
        {
            if (string.IsNullOrEmpty(contentTypeCodename))
            {
                throw new ArgumentException("Entered content type codename is not valid.", nameof(contentTypeCodename));
            }

            if (string.IsNullOrEmpty(contentElementCodename))
            {
                throw new ArgumentException("Entered content element codename is not valid.", nameof(contentElementCodename));
            }

            return await _deliveryApi.GetContentElementInternalAsync(contentTypeCodename, contentElementCodename);
        }

        /// <summary>
        /// Returns a taxonomy group.
        /// </summary>
        /// <param name="codename">The codename of a taxonomy group.</param>
        /// <returns>The <see cref="IDeliveryTaxonomyResponse"/> instance that contains the taxonomy group with the specified codename.</returns>
        public async Task<IDeliveryTaxonomyResponse> GetTaxonomyAsync(string codename)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered taxonomy codename is not valid.", nameof(codename));
            }

            return await _deliveryApi.GetTaxonomyInternalAsync(codename);
        }

        /// <summary>
        /// Returns taxonomy groups.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IDeliveryTaxonomyListingResponse"/> instance that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        public async Task<IDeliveryTaxonomyListingResponse> GetTaxonomiesAsync(IEnumerable<IQueryParameter>? parameters = null)
        {
            var queryParams = ConvertToListTaxonomyGroupsParams(parameters);
            return await _deliveryApi.GetTaxonomiesInternalAsync(queryParams);
        }

        /// <summary>
        /// Returns all active languages assigned to a given environment and matching the optional filtering parameters.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IDeliveryLanguageListingResponse"/> instance that represents the languages. If no query parameters are specified, all languages are returned.</returns>
        public async Task<IDeliveryLanguageListingResponse> GetLanguagesAsync(IEnumerable<IQueryParameter>? parameters = null)
        {
            var queryParams = ConvertToLanguagesParams(parameters);
            return await _deliveryApi.GetLanguagesInternalAsync(queryParams);
        }

        /// <summary>
        /// Returns a feed that is used to traverse through strongly typed parent content items matching the optional filtering parameters.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters for filtering.</param>
        /// <returns>The <see cref="IDeliveryItemsFeed{IUsedInItem}"/> instance that can be used to enumerate through content item parents for the specified item codename. If no query parameters are specified, default language parents are enumerated.</returns>
        public IDeliveryItemsFeed<IUsedInItem> GetItemUsedIn(string codename, IEnumerable<IQueryParameter>? parameters = null)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Item codename is not specified.", nameof(codename));
            }

            ValidateUsedInParameters(parameters);

            return new DeliveryUsedInItems(async continuation =>
            {
                var response = await _deliveryApi.GetItemUsedInInternalAsync(codename, null, continuation);
                return new UsedIn.DeliveryUsedInResponse(response.ApiResponse, response.Items);
            });
        }

        /// <summary>
        /// Returns a feed that is used to traverse through strongly typed parent content items matching the optional filtering parameters.
        /// </summary>
        /// <param name="codename">The codename of an asset.</param>
        /// <param name="parameters">A collection of query parameters for filtering.</param>
        /// <returns>The <see cref="IDeliveryItemsFeed{IUsedInItem}"/> instance that can be used to enumerate through asset parents for the specified asset codename. If no query parameters are specified, default language parents are enumerated.</returns>
        public IDeliveryItemsFeed<IUsedInItem> GetAssetUsedIn(string codename, IEnumerable<IQueryParameter>? parameters = null)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Asset codename is not specified.", nameof(codename));
            }

            ValidateUsedInParameters(parameters);

            return new DeliveryUsedInItems(async continuation =>
            {
                var response = await _deliveryApi.GetAssetUsedInInternalAsync(codename, null, continuation);
                return new UsedIn.DeliveryUsedInResponse(response.ApiResponse, response.Items);
            });
        }

        // Helper methods to convert legacy IQueryParameter to new typed parameters
        private Api.QueryParams.Items.SingleItemParams? ConvertToSingleItemParams(IEnumerable<IQueryParameter>? parameters)
        {
            if (parameters == null) return null;
            var adapter = new ParamsAdapter()
                .WithLanguage(parameters)
                .WithElements(parameters)
                .WithDepth(parameters);
            return adapter.ToSingleItemParams();
        }

        private Api.QueryParams.Items.ListItemsParams? ConvertToListItemsParams(IEnumerable<IQueryParameter>? parameters)
        {
            if (parameters == null) return null;
            var adapter = new ParamsAdapter()
                .WithLanguage(parameters)
                .WithElements(parameters)
                .WithExcludeElements(parameters)
                .WithDepth(parameters)
                .WithPaging(parameters)
                .WithOrdering(parameters)
                .WithIncludeTotalCount(_deliveryOptions.CurrentValue.IncludeTotalCount, parameters);
            return adapter.ToListItemsParams();
        }

        private Api.QueryParams.Types.ListTypesParams? ConvertToListTypesParams(IEnumerable<IQueryParameter>? parameters)
        {
            if (parameters == null) return null;
            var adapter = new ParamsAdapter()
                .WithElements(parameters)
                .WithPaging(parameters);
            return adapter.ToListTypesParams();
        }

        private Api.QueryParams.TaxonomyGroups.ListTaxonomyGroupsParams? ConvertToListTaxonomyGroupsParams(IEnumerable<IQueryParameter>? parameters)
        {
            if (parameters == null) return null;
            var adapter = new ParamsAdapter()
                .WithPaging(parameters);
            return adapter.ToListTaxonomyGroupsParams();
        }

        private Api.QueryParams.Languages.LanguagesParams? ConvertToLanguagesParams(IEnumerable<IQueryParameter>? parameters)
        {
            if (parameters == null) return null;
            var adapter = new ParamsAdapter()
                .WithOrdering(parameters)
                .WithPaging(parameters);
            return adapter.ToLanguagesParams();
        }

        private Api.QueryParams.Items.EnumItemsParams? ConvertToEnumItemsParams(IEnumerable<IQueryParameter>? parameters)
        {
            if (parameters == null) return null;
            var adapter = new ParamsAdapter()
                .WithLanguage(parameters)
                .WithElements(parameters)
                .WithExcludeElements(parameters)
                .WithDepth(parameters)
                .WithOrdering(parameters);
            return adapter.ToEnumItemsParams();
        }

        private static void ValidateItemsFeedParameters(IEnumerable<IQueryParameter>? parameters)
        {
            if (parameters == null) return;
            foreach (var p in parameters)
            {
                switch (p)
                {
                    case DepthParameter:
                    case LimitParameter:
                    case SkipParameter:
                        throw new ArgumentException("Unsupported parameter for items feed.");
                }
            }
        }

        private static void ValidateUsedInParameters(IEnumerable<IQueryParameter>? parameters)
        {
            if (parameters == null) return;
            foreach (var p in parameters)
            {
                // For now, UsedIn endpoints do not accept query parameters
                throw new ArgumentException("Used-in feed does not support query parameters.");
            }
        }

        private sealed class ParamsAdapter
        {
            public string? Language { get; set; }
            public string[]? Elements { get; set; }
            public string[]? ExcludeElements { get; set; }
            public int? Depth { get; set; }
            public int? Skip { get; set; }
            public int? Limit { get; set; }
            public string? OrderBy { get; set; }
            public bool? IncludeTotalCount { get; set; }

            public ParamsAdapter WithLanguage(IEnumerable<IQueryParameter> parameters)
            {
                var lang = parameters.OfType<LanguageParameter>().FirstOrDefault();
                if (lang != null) Language = lang.Language;
                return this;

            }

            public ParamsAdapter WithElements(IEnumerable<IQueryParameter> parameters)
            {
                var inc = parameters.OfType<ElementsParameter>().FirstOrDefault();
                if (inc != null) Elements = inc.ElementCodenames?.ToArray();
                return this;
            }

            public ParamsAdapter WithExcludeElements(IEnumerable<IQueryParameter> parameters)
            {
                var exc = parameters.OfType<ExcludeElementsParameter>().FirstOrDefault();
                if (exc != null) ExcludeElements = exc.ElementCodenames?.ToArray();
                return this;
            }

            public ParamsAdapter WithDepth(IEnumerable<IQueryParameter> parameters)
            {
                var depth = parameters.OfType<DepthParameter>().FirstOrDefault();
                if (depth != null) Depth = depth.Depth;
                return this;
            }

            public ParamsAdapter WithPaging(IEnumerable<IQueryParameter> parameters)
            {
                var skip = parameters.OfType<SkipParameter>().FirstOrDefault();
                if (skip != null) Skip = skip.Skip;

                var limit = parameters.OfType<LimitParameter>().FirstOrDefault();
                if (limit != null) Limit = limit.Limit;

                return this;
            }

            public ParamsAdapter WithOrdering(IEnumerable<IQueryParameter> parameters)
            {
                var order = parameters.OfType<OrderParameter>().FirstOrDefault();
                if (order != null)
                {
                    var direction = order.SortOrder == SortOrder.Ascending ? "[asc]" : "[desc]";
                    OrderBy = $"{order.ElementOrAttributePath}{direction}";
                }
                return this;
            }

            public ParamsAdapter WithIncludeTotalCount(bool globalInclude, IEnumerable<IQueryParameter> parameters)
            {
                // Explicit IncludeTotalCount parameter wins; otherwise use global option
                var explicitParam = parameters.OfType<IncludeTotalCountParameter>().Any();
                IncludeTotalCount = explicitParam ? true : (bool?)(globalInclude ? true : null);
                return this;
            }

            public Api.QueryParams.Items.SingleItemParams ToSingleItemParams() => new()
            {
                Language = Language,
                Elements = Elements,
                Depth = Depth
            };

            public Api.QueryParams.Items.ListItemsParams ToListItemsParams() => new()
            {
                Language = Language,
                Elements = Elements,
                ExcludeElements = ExcludeElements,
                Depth = Depth,
                Skip = Skip,
                Limit = Limit,
                OrderBy = OrderBy,
                IncludeTotalCount = IncludeTotalCount
            };

            public Api.QueryParams.Items.EnumItemsParams ToEnumItemsParams() => new()
            {
                Language = Language,
                Elements = Elements,
                ExcludeElements = ExcludeElements,
                Depth = Depth,
                OrderBy = OrderBy
            };

            public Api.QueryParams.Types.ListTypesParams ToListTypesParams() => new()
            {
                Elements = Elements,
                Skip = Skip,
                Limit = Limit
            };

            public Api.QueryParams.TaxonomyGroups.ListTaxonomyGroupsParams ToListTaxonomyGroupsParams() => new()
            {
                Skip = Skip,
                Limit = Limit
            };

            public Api.QueryParams.Languages.LanguagesParams ToLanguagesParams() => new()
            {
                OrderBy = OrderBy,
                Skip = Skip,
                Limit = Limit
            };
        }
    }
}
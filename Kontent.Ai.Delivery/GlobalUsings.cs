global using Kontent.Ai.Delivery.Abstractions;
global using Kontent.Ai.Delivery.Api;
global using Kontent.Ai.Delivery.Api.QueryParams.Items;
global using Kontent.Ai.Delivery.Api.QueryParams.Languages;
global using Kontent.Ai.Delivery.Api.QueryParams.TaxonomyGroups;
global using Kontent.Ai.Delivery.Api.QueryParams.Types;
global using Kontent.Ai.Delivery.Api.QueryBuilders;
global using Kontent.Ai.Delivery.Extensions;
global using Kontent.Ai.Delivery.SharedModels;
global using Refit;
global using System.Threading.Tasks;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Net.Http;

global using Scalar = OneOf.OneOf<string, double, System.DateTime, bool>;
global using RangeTuple = OneOf.OneOf<(double Lower, double Upper), (System.DateTime Lower, System.DateTime Upper)>;
global using Comparable = OneOf.OneOf<double, System.DateTime, string>;


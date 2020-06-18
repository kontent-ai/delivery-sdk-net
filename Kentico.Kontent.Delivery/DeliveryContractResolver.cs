using System;
using System.Collections.Generic;
using System.Linq;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentItems;
using Kentico.Kontent.Delivery.ContentTypes;
using Kentico.Kontent.Delivery.ContentTypes.Element;
using Kentico.Kontent.Delivery.TaxonomyGroups;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;

namespace Kentico.Kontent.Delivery
{
    public class DeliveryContractResolver : DefaultContractResolver
    {
        protected override JsonContract CreateContract(Type objectType)
        {
            var contract = base.CreateContract(objectType);


            var typeBindings = new Dictionary<Type, Type>
                {//TODO:refactor
                { typeof(IAsset), typeof(Asset) },
                    { typeof(ITaxonomyTerm), typeof(TaxonomyTerm) },
                    { typeof(IMultipleChoiceOption), typeof(MultipleChoiceOption) },
                    { typeof(IContentElement), typeof(ContentElement) },
                    { typeof(IContentType), typeof(ContentType) }
                };
            var success = typeBindings.TryGetValue(objectType, out Type binding);
            if(success)
            {
                contract = base.CreateContract(binding);

            }

            //if (objectType == typeof(IContentElement))
            //{
            //    contract = base.CreateContract(typeof(ContentElement));
            //}
            DeliveryServiceCollection sc = new DeliveryServiceCollection();
            var sp = sc.ServiceProvider;
            return contract;
        }
    }
}

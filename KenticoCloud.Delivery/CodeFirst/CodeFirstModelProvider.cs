using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KenticoCloud.Delivery
{
    internal static class CodeFirstModelProvider
    {
        public static T GetContentItemModel<T>(JToken item, JToken modularContent, DeliveryClient client)
        {
            T instance = (T)Activator.CreateInstance(typeof(T));

            foreach (var property in instance.GetType().GetProperties())
            {
                if (property.SetMethod != null)
                {
                    if (property.PropertyType == typeof(IEnumerable<ContentItem>))
                    {
                        var contentItemCodenames = ((JObject)item["elements"])
                            .Properties()
                            ?.FirstOrDefault(p => p.Name.Replace("_", "").ToLower() == property.Name.ToLower())
                            ?.FirstOrDefault()["value"].ToObject<IEnumerable<string>>();

                        if (contentItemCodenames != null && contentItemCodenames.Any())
                        {
                            var modularContentNode = (JObject)modularContent;
                            var contentItems = new List<ContentItem>();
                            foreach (string codename in contentItemCodenames)
                            {
                                var modularContentItemNode = modularContentNode.Properties()
                                    .First(p => p.Name == codename).First;

                                if (modularContentItemNode != null)
                                {
                                    contentItems.Add(new ContentItem(modularContentItemNode, modularContentNode, client));
                                }
                            }

                            property.SetValue(instance, contentItems);
                        }
                    }
                    else if (property.PropertyType == typeof(IEnumerable<MultipleChoiceOption>)
                             || property.PropertyType == typeof(IEnumerable<Asset>)
                             || property.PropertyType == typeof(IEnumerable<TaxonomyTerm>)
                             || property.PropertyType == typeof(string)
                             || property.PropertyType.GetTypeInfo().IsValueType)
                    {
                        object value = ((JObject)item["elements"])
                            .Properties()
                            ?.FirstOrDefault(child => child.Name.Replace("_", "").ToLower() == property.Name.ToLower())
                            ?.First["value"].ToObject(property.PropertyType);

                        if (value != null)
                        {
                            property.SetValue(instance, value);
                        }
                    }
                    else if (property.PropertyType == typeof(ContentItemSystemAttributes))
                    {
                        object value = ((JObject)item["system"]).ToObject(typeof(ContentItemSystemAttributes));

                        if (value != null)
                        {
                            property.SetValue(instance, value);
                        }
                    }
                }
            }

            return instance;
        }
    }
}

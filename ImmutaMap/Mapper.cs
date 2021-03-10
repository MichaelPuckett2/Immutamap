﻿using ImmutaMap.Exceptions;
using ImmutaMap.Interfaces;
using ImmutaMap.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ImmutaMap
{
    public class Mapper
    {
        private readonly ITypeFormatter typeFormatter;

        public Mapper(ITypeFormatter typeFormatter)
        {
            this.typeFormatter = typeFormatter;
        }

        public static Mapper GetNewInstance() => new Mapper(new TypeFormatter());

        public TResult Build<TSource, TResult>(Map<TSource, TResult> map, Func<object[]> args = null)
        {
            TResult result;
            result = typeFormatter.GetInstance<TResult>(args);

            Copy(map, result);

            return result;
        }

        private void Copy<TSource, TTarget>(Map<TSource, TTarget> map, TTarget target)
        {
            var sourcePropertyInfos = typeof(TSource).GetProperties().ToList();
            var targetPropertyInfos = typeof(TTarget).GetProperties().ToList();

            var joinedPropertyInfos = GetSourceResultPropeties(sourcePropertyInfos, targetPropertyInfos);
            AddPropertyNameMaps(map, sourcePropertyInfos, targetPropertyInfos, joinedPropertyInfos);

            foreach (var (sourcePropertyInfo, targetPropertyInfo) in joinedPropertyInfos)
            {
                var propertyMapFuncsKey = (sourcePropertyInfo.Name, sourcePropertyInfo.PropertyType);
                if (map.PropertyMapFuncs.Keys.Contains(propertyMapFuncsKey))
                {
                    var func = map.PropertyMapFuncs[propertyMapFuncsKey];
                    var targetValue = func?.Invoke(sourcePropertyInfo.GetValue(map.Source));
                    if (!targetPropertyInfo.PropertyType.IsAssignableFrom(targetValue.GetType()))
                    {
                        throw new MappedPropertyException(typeof(TSource), targetPropertyInfo.PropertyType, targetValue.GetType());
                    }
                    SetTargetValue(target, targetPropertyInfo, targetValue);
                }
                else
                {
                    var targetValue = sourcePropertyInfo.GetValue(map.Source);
                    SetTargetValue(target, targetPropertyInfo, targetValue);
                }
            }
        }

        private static void SetTargetValue<TTarget>(TTarget target, PropertyInfo targetPropertyInfo, object targetValue)
        {
            if (targetPropertyInfo.CanWrite)
            {
                targetPropertyInfo.SetValue(target, targetValue);
            }
            else
            {
                var fields = typeof(TTarget).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                var backingField = fields.FirstOrDefault(x => x.Name == $"<{targetPropertyInfo.Name}>k__BackingField");

                if (backingField != null)
                {
                    backingField.SetValue(target, targetValue);
                }
            }
        }

        private void AddPropertyNameMaps<TSource, TResult>(Map<TSource, TResult> map, List<PropertyInfo> sourceProperties, List<PropertyInfo> resultProperties, List<(PropertyInfo sourcePropertyInfo, PropertyInfo resultPropertyInfo)> joinedPropertyInfos)
        {
            foreach (var (sourcePropertyName, resultPropertyName) in map.PropertyNameMaps)
            {
                var sourcePropertyInfo = sourceProperties.FirstOrDefault(x => x.Name == sourcePropertyName);
                if (sourcePropertyInfo == null) continue;
                var resultPropertyInfo = resultProperties.FirstOrDefault(x => x.Name == resultPropertyName);
                if (resultPropertyInfo == null) continue;
                if (joinedPropertyInfos.Any(x => x.sourcePropertyInfo.Name == sourcePropertyName && x.resultPropertyInfo.Name == resultPropertyName)) continue;
                joinedPropertyInfos.Add((sourcePropertyInfo, resultPropertyInfo));
            }
        }

        private List<(PropertyInfo sourceProperty, PropertyInfo resultProperty)> GetSourceResultPropeties(List<PropertyInfo> sourceProperties, List<PropertyInfo> resultProperties)
        {
            return sourceProperties.Join(resultProperties,
                sourceProperty => sourceProperty.Name,
                resultProperty => resultProperty.Name,
                (sourceProperty, resultProperty) => (sourceProperty, resultProperty))
                .ToList();
        }
    }
}

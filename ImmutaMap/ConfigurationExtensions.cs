﻿namespace ImmutaMap;

 public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds a property name map for cases where property names in the source do not match the name in the target.
    /// </summary>
    /// <typeparam name="TResult">Property result value Type.</typeparam>
    /// <param name="sourceExpression">Expression to gain source property name.</param>
    /// <param name="targetExpression">Expression to gain target property name.</param>
    /// <returns>Current MapConfiguration.</returns>
     public static IConfiguration<TSource, TTarget> MapProperty<TSource, TTarget, TResult>(
        this IConfiguration<TSource, TTarget> configuration,
        Expression<Func<TSource, TResult>> sourceExpression,
        Expression<Func<TTarget, TResult>> targetExpression)
    {
        configuration.PropertyNameMaps.Add((sourceExpression.GetMemberName(), targetExpression.GetMemberName()));
        return configuration;
    }

    /// <summary>
    /// Maps the source property value to the target with the given expression value.
    /// </summary>
    /// <typeparam name="TSourcePropertyType">The source property type being mapped.</typeparam>
    /// <param name="sourceExpression">The expression used to get the source property name and value. Invoked on Build()</param>
    /// <param name="propertyResultFunc">The function used to get the target property value. Invoked on Build()</param>
    /// <returns>Current mapConfiguration.</returns>
     public static IConfiguration<TSource, TTarget> MapPropertyType<TSource, TTarget, TSourcePropertyType, TTargetPropertyType>(
        this IConfiguration<TSource, TTarget> configuration,
        Expression<Func<TSource, TSourcePropertyType>> sourceExpression,
        Func<TSourcePropertyType, TTargetPropertyType> propertyResultFunc)
    {
        if (sourceExpression.Body is MemberExpression sourceMemberExpression)
        {
            configuration.Transformers.Add(new PropertyTransformer<TSourcePropertyType, TTargetPropertyType>(sourceMemberExpression.Member.Name, propertyResultFunc));
        }
        return configuration;
    }

    /// <summary>
    /// Maps the source properties, containing the given attribute, in a defined way.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type.</typeparam>
    /// <param name="func">The function defined to work on the attribute mapping. Passes the attribute found, the source value, and expects the target value in return.</param>
    /// <returns>Current mapConfiguration.</returns>
     public static IConfiguration<TSource, TTarget> MapSourceAttribute<TSource, TTarget, TAttribute>(
        this IConfiguration<TSource, TTarget> configuration,
        Func<TAttribute, object, object> func)
        where TAttribute : Attribute
    {
        var att = new SourceAttributeTransformer<TAttribute>(new Func<Attribute, object, object>((attribute, target) => func.Invoke((TAttribute)attribute, target)));
        configuration.Transformers.Add(att);
        return configuration;
    }

    /// <summary>
    /// Maps the target properties, containing the given attribute, in a defined way.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <typeparam name="TAttribute">The attribute type.</typeparam>
    /// <param name="map">The map used in this method.</param>
    /// <param name="func">The function defined to work on the attribute mapping. Passes the attribute found, the source value, and expects the target value in return.</param>
    /// <returns>Current Map.</returns>
     public static IConfiguration<TSource, TTarget> MapTargetAttribute<TSource, TTarget, TAttribute>(
        this IConfiguration<TSource, TTarget> configuration,
        Func<TAttribute, object, object> func)
        where TAttribute : Attribute
    {
        var att = new TargetAttributeTransformer<TAttribute>(new Func<Attribute, object, object>((attribute, target) => func.Invoke((TAttribute)attribute, target)));
        configuration.Transformers.Add(att);
        return configuration;
    }

    /// <summary>
    /// Maps a type from source property.
    /// </summary>
    /// <typeparam name="TType">The source property type being mapped.</typeparam>
    /// <param name="typeMapFunc">The function used to get the result value.</param>
    /// <returns>Current Map.</returns>
     public static IConfiguration<TSource, TTarget> MapType<TSource, TTarget, TType>(
        this IConfiguration<TSource, TTarget> configuration,
        Func<object, object> typeMapFunc)
    {
        var typeMapping = new SourceTypeTransformer(typeof(TType), typeMapFunc);
        configuration.Transformers.Add(typeMapping);
        return configuration;
    }
}
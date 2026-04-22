using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HudClock.Infrastructure.Reflection;

/// <summary>
/// Compiled-delegate wrapper around a reflected instance field. Reflection cost
/// (<see cref="BindingFlags"/> lookup and expression-tree compilation) is paid
/// once at construction; every subsequent <see cref="Get"/> call runs at
/// roughly the speed of a direct field access.
/// </summary>
/// <typeparam name="TTarget">The type that declares the field.</typeparam>
/// <typeparam name="TField">
/// The declared type of the field, or any type the field's declared type is
/// assignable to.
/// </typeparam>
/// <remarks>
/// Used to read private fields on Vintage Story internals (the storm system's
/// <c>data</c> field, the rift mod's <c>curPattern</c> field) without per-call
/// reflection overhead and without the boxing the
/// <c>Reflector.ToDynamic</c> / <see cref="System.Dynamic.ExpandoObject"/>
/// pattern caused in 3.x.
/// </remarks>
internal sealed class FieldAccessor<TTarget, TField>
    where TTarget : class
{
    private readonly Func<TTarget, TField> _getter;

    /// <summary>Name of the field this accessor was built for. Useful for diagnostics.</summary>
    public string FieldName { get; }

    /// <summary>Build an accessor for a public or non-public instance field.</summary>
    /// <param name="fieldName">Exact name of the field declared on <typeparamref name="TTarget"/>.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when no instance field named <paramref name="fieldName"/> exists on
    /// <typeparamref name="TTarget"/> or any of its base types.
    /// </exception>
    public FieldAccessor(string fieldName)
    {
        FieldName = fieldName;

        FieldInfo? field = typeof(TTarget).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (field is null)
        {
            throw new ArgumentException(
                $"Field '{fieldName}' not found on type '{typeof(TTarget).FullName}'.",
                nameof(fieldName));
        }

        // (TTarget instance) => (TField)instance.field
        ParameterExpression instance = Expression.Parameter(typeof(TTarget), "instance");
        MemberExpression access = Expression.Field(instance, field);
        UnaryExpression convert = Expression.Convert(access, typeof(TField));
        _getter = Expression.Lambda<Func<TTarget, TField>>(convert, instance).Compile();
    }

    /// <summary>Read the field's current value from the given instance.</summary>
    /// <exception cref="ArgumentNullException">When <paramref name="instance"/> is null.</exception>
    public TField Get(TTarget instance)
    {
        if (instance is null) throw new ArgumentNullException(nameof(instance));
        return _getter(instance);
    }

    /// <summary>
    /// Attempt to build an accessor. Returns null if the target field does not
    /// exist — useful when a game-internal field may have been renamed across
    /// Vintage Story versions and we want to degrade gracefully rather than
    /// crash the mod on startup.
    /// </summary>
    public static FieldAccessor<TTarget, TField>? TryCreate(string fieldName)
    {
        try
        {
            return new FieldAccessor<TTarget, TField>(fieldName);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}

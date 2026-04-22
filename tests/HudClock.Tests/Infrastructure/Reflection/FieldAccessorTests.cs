using System;
using HudClock.Infrastructure.Reflection;

namespace HudClock.Tests.Infrastructure.Reflection;

public class FieldAccessorTests
{
    // --- test fixtures ---

    private sealed class PublicIntHolder
    {
        public int Value = 42;
    }

    private sealed class PrivateStringHolder
    {
        // ReSharper disable once InconsistentNaming — field name is asserted on by tests
        private readonly string _secret = "hidden";

        // Prevent unused-field warnings from static analyzers. Not called by tests.
        public string Peek() => _secret;
    }

    private sealed class PrivateDataHolder
    {
        // Simulates the shape of SystemTemporalStability's "data" field — an
        // opaque private object read by reflection.
        private readonly object _data = new { nextStormTotalDays = 1.5, nowStormActive = false };
    }

    // --- tests ---

    [Fact]
    public void Reads_public_int_field()
    {
        var accessor = new FieldAccessor<PublicIntHolder, int>("Value");
        var target = new PublicIntHolder { Value = 99 };

        Assert.Equal(99, accessor.Get(target));
    }

    [Fact]
    public void Reads_private_string_field()
    {
        var accessor = new FieldAccessor<PrivateStringHolder, string>("_secret");

        Assert.Equal("hidden", accessor.Get(new PrivateStringHolder()));
    }

    [Fact]
    public void Reads_private_object_field_as_object()
    {
        // Mirrors the real-world use case: reading SystemTemporalStability.data
        // without casting to its internal type at the call site.
        var accessor = new FieldAccessor<PrivateDataHolder, object>("_data");

        object value = accessor.Get(new PrivateDataHolder());

        Assert.NotNull(value);
    }

    [Fact]
    public void Reflects_live_value_not_snapshot()
    {
        var accessor = new FieldAccessor<PublicIntHolder, int>("Value");
        var target = new PublicIntHolder { Value = 1 };

        Assert.Equal(1, accessor.Get(target));

        target.Value = 2;
        Assert.Equal(2, accessor.Get(target));
    }

    [Fact]
    public void Throws_ArgumentException_when_field_missing()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new FieldAccessor<PublicIntHolder, int>("NoSuchField"));

        Assert.Contains("NoSuchField", ex.Message);
    }

    [Fact]
    public void Throws_ArgumentNullException_when_target_is_null()
    {
        var accessor = new FieldAccessor<PublicIntHolder, int>("Value");

        Assert.Throws<ArgumentNullException>(() => accessor.Get(null!));
    }

    [Fact]
    public void FieldName_property_reflects_constructor_argument()
    {
        var accessor = new FieldAccessor<PublicIntHolder, int>("Value");

        Assert.Equal("Value", accessor.FieldName);
    }

    [Fact]
    public void TryCreate_returns_null_when_field_missing()
    {
        FieldAccessor<PublicIntHolder, int>? accessor =
            FieldAccessor<PublicIntHolder, int>.TryCreate("NoSuchField");

        Assert.Null(accessor);
    }

    [Fact]
    public void TryCreate_returns_working_accessor_when_field_exists()
    {
        FieldAccessor<PublicIntHolder, int>? accessor =
            FieldAccessor<PublicIntHolder, int>.TryCreate("Value");

        Assert.NotNull(accessor);
        Assert.Equal(42, accessor!.Get(new PublicIntHolder()));
    }
}

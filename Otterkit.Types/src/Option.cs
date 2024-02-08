using System.Diagnostics.CodeAnalysis;

namespace Otterkit.Types;

public readonly struct Option<TValue> : 
    IEquatable<Option<TValue>>
    where TValue : notnull
{
    internal readonly TValue Value;
    public readonly bool Exists;

    public static Option<TValue> Null
    {
        get => new Option<TValue>();
    }

    public Option(TValue value)
    {
        Value = value;
        Exists = true;
    }

    public void Deconstruct(out TValue? value, out bool exists)
    {
        value = Value;
        exists = Exists;
    }

    public bool TryUnwrap([NotNullWhen(true)] out TValue? value)
    {
        if (Exists)
        {
            value = Value;
            return true;
        }

        value = default;
        return false;
    }

    public TValue Unwrap()
    {
        if (Exists) return Value;

        throw new NullReferenceException("Instance of Option did not have a value");
    }

    public static implicit operator Option<TValue>(TValue? value)
    {
        if (value is null) return Null;

        return new Option<TValue>(value);
    }

    public static explicit operator TValue(Option<TValue> option)
    {
        if (option.Exists) return option.Value;

        throw new NullReferenceException("Instance of Option did not have a value");
    }

    public static implicit operator bool(Option<TValue> option)
    {
        return option.Exists;
    }

    public static bool operator ==(Option<TValue> left, Option<TValue> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Option<TValue> left, Option<TValue> right)
    {
        return !left.Equals(right);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Option<TValue> value && Equals(value);
    }

    public bool Equals(Option<TValue> other)
    {
        if (Exists && other.Exists)
        {
            return EqualityComparer<TValue>.Default.Equals(Value, other.Value);
        }

        if (!Exists && !other.Exists)
        {
            return true;
        }

        return false;
    }

    public override int GetHashCode()
    {
        if (!Exists) return 0;

        return HashCode.Combine(Value, Exists);
    }
}

using Coflnet.Sky.PlayerState.Models;
using System.Linq;
using MongoDB.Driver;
using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using Newtonsoft.Json;
#nullable enable
namespace Coflnet.Sky.PlayerState.Services;
public class ItemCompare : IEqualityComparer<Item>
{
    private ElementComparer internalComparer = new();
    public bool Equals(Item? x, Item? y)
    {
        return x != null && y != null &&
            (AttributeMatch(x, y))
           && EnchantMatch(x, y)
           && x.Color == y.Color
           && x.Tag == y.Tag;
    }

    private bool AttributeMatch(Item x, Item y)
    {
        return x.ExtraAttributes == null && y.ExtraAttributes == null || x.ExtraAttributes != null && y.ExtraAttributes != null && x.ExtraAttributes.Count == y.ExtraAttributes.Count && !x.ExtraAttributes.Except(y.ExtraAttributes, internalComparer).Any();
    }

    private static bool EnchantMatch(Item? x, Item? y)
    {
        return y?.Enchantments == null && x.Enchantments == null || (  y?.Enchantments != null && x?.Enchantments != null &&
            y.Enchantments?.Count == x.Enchantments?.Count
                && x.Enchantments!.Sum(x => x.Value) == y.Enchantments!.Sum(x => x.Value) && !x.Enchantments!.Except(y.Enchantments!).Any());
    }

    public int GetHashCode(Item obj)
    {
        return HashCode.Combine(obj.ExtraAttributes?.GetValueOrDefault("uuid"), obj.Tag);
    }

    public class ElementComparer : IEqualityComparer<KeyValuePair<string, object>>
    {
        public bool Equals(KeyValuePair<string, object> x, KeyValuePair<string, object> y)
        {
            if (x.Value is IEnumerable<KeyValuePair<object, object>> dumb)
                x = new(x.Key, dumb.Select(a => new KeyValuePair<string, object>(a.Key.ToString()!, a.Value)));
            if (y.Value is IEnumerable<KeyValuePair<object, object>> dumb2)
                y = new(y.Key, dumb2.Select(a => new KeyValuePair<string, object>(a.Key.ToString()!, a.Value)));

            return x.Key == y.Key && (x.Value.Equals(y.Value)
                || x.Value is IEnumerable<KeyValuePair<string, object>> xi && y.Value is IEnumerable<KeyValuePair<string, object>> yi && this.Equal(xi, yi)
                || x.Value is IEnumerable<object> xe && y.Value is IEnumerable<object> ye && Enumerable.SequenceEqual<object>(xe, ye)
                || IsNumeric(x.Value) && Convert.ToInt64(x.Value) == Convert.ToInt64(y.Value)
            );
        }

        private static bool IsNumeric(object x)
        {
            return x is byte || x is short || x is int || x is long || x is ushort || x is uint || x is ulong || x is double || x is float;
        }


        private bool Equal(IEnumerable<KeyValuePair<string, object>> a, IEnumerable<KeyValuePair<string, object>> b)
        {
            var combined = a.Zip(b);
            foreach (var item in combined)
            {
                if (!Equals(item.First, item.Second))
                    return false;
            }
            return true;
        }

        public int GetHashCode([DisallowNull] KeyValuePair<string, object> obj)
        {
            var code = 0;
            if (obj.Value is IEnumerable col)
                foreach (var p in col)
                    if (p is KeyValuePair<string, object> xi)
                        code ^= GetHashCode(xi);
                    else
                        code ^= p.GetHashCode();
            else if (IsNumeric(obj.Value))
                code = (int)(Convert.ToInt64(obj.Value) & int.MaxValue);
            else
                code = obj.Value.GetHashCode();
            return HashCode.Combine(obj.Key, code);
        }
    }
}

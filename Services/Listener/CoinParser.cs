using System;
using System.Globalization;
using Coflnet.Sky.PlayerState.Models;
using System.Collections.Generic;
using System.Linq;

namespace Coflnet.Sky.PlayerState.Services;

public class CoinParser
{
    public long GetCoinAmount(Item item)
    {
        if (IsCoins(item))
        {
            var stringAmount = item.ItemName!.Substring(2, item.ItemName.Length - 8);
            return ParseCoinAmount(stringAmount);
        }
        return 0;
    }

    private static long ParseCoinAmount(string stringAmount)
    {
        return Core.CoinParser.ParseCoinAmount(stringAmount);
    }

    public long GetInventoryCoinSum(IEnumerable<Item> items)
    {
        if(Core.CoinParser.TryParseFromDescription(items.Select(i => i.Description), out var result))
        {
            return result;
        }
        return items.Sum(GetCoinAmount);
    }

    internal bool IsCoins(Item item)
    {
        return item.ItemName?.EndsWith(" coins") ?? false;
    }
}
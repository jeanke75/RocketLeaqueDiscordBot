using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using RLBot.Models;

namespace RLBot.TypeReaders
{
    public class RLRegionTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            switch (input.ToLower())
            {
                case "eu":
                case "europe":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RLRegion.Europe));
                case "na":
                case "northamerica":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RLRegion.NorthAmerica));
                case "sa":
                case "southamerica":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RLRegion.SouthAmerica));
                case "oc":
                case "oceania":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RLRegion.Oceania));
                case "ac":
                case "asiacentral":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RLRegion.AsiaCentral));
                case "me":
                case "middleeast":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RLRegion.MiddleEast));
                case "af":
                case "africa":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RLRegion.Africa));
            }

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, $"Not a valid region. {string.Join(", ", Enum.GetValues(typeof(RLRegion)).Cast<RLRegion>())}"));
        }
    }
}
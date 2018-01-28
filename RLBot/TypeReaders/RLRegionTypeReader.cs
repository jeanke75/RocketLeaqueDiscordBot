using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using RLBot.API.RLS.Data;

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
                    return Task.FromResult(TypeReaderResult.FromSuccess(RlsRegion.Europe));
                case "na":
                case "northamerica":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RlsRegion.NorthAmerica));
                case "sa":
                case "southamerica":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RlsRegion.SouthAmerica));
                case "oc":
                case "oceania":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RlsRegion.Oceania));
                case "ac":
                case "asiacentral":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RlsRegion.AsiaCentral));
                case "me":
                case "middleeast":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RlsRegion.MiddleEast));
                case "af":
                case "africa":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RlsRegion.Africa));
            }

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, $"Not a valid region. {string.Join(", ", Enum.GetValues(typeof(RlsRegion)).Cast<RlsRegion>())}"));
        }
    }
}